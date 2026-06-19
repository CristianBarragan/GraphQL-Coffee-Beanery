using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CoffeeBeanery.GraphQL.Core.Mapping.Generators.Model;
using CoffeeBeanery.GraphQL.Core.Mapping.Generators.Passes;
using CoffeeBeanery.GraphQL.Core.Mapping.Generators.Emit;
using CoffeeBeanery.GraphQL.Core.Mapping.Generators.Parsing;

namespace CoffeeBeanery.GraphQL.Core.Mapping.Generators
{
    /// <summary>
    /// Replaces NodeBuilder's five reflective passes (InferModelChildren,
    /// GenerateReflectedFieldMaps, ResolveFieldMapAliases, BuildTree, BuildModel)
    /// with a compile-time pipeline over Roslyn symbols. Output: a generated
    /// partial class per BaseModelMappingRegistration&lt;T&gt; subtype overriding
    /// Register() with literal NodeRegistry population - no reflection, fully
    /// trim/Native AOT safe.
    ///
    /// REQUIRED changes to hand-written code:
    ///   1. Mapping classes (e.g. ProductMapping) must be declared `partial`.
    ///   2. BaseModelMappingRegistration&lt;T&gt;.Register() must be `virtual`
    ///      (the generated partial provides the `override`).
    ///   3. BaseModelMappingRegistration&lt;T&gt; must expose the constructor's
    ///      alias/model strings as `protected string Alias` / `protected string ModelName`
    ///      (or equivalent names - update NodeTreeEmitter if named differently).
    ///
    /// NOTE: per-class parsing alone can't tell whether a navigation target is a
    /// Wrapper-rooted, globally-aliased model (e.g. CustomerCustomerEdge, registered once
    /// under a bare alias) or a role-scoped child that should inherit the navigating
    /// mapping's alias prefix (e.g. Customer under InnerCustomer/OuterCustomer) - that
    /// requires (a) parsing Wrapper itself, and (b) seeing every other mapping's
    /// ModelType/EntityType to bridge a navigation's RelatedEntityType back to its owning
    /// model. Both are computed once per compilation below and threaded into navigation
    /// resolution as `rootEntityTypes`, instead of being decided inside
    /// EntityNavigationConvention/NodeTreeEmitter from a single mapping's own data.
    /// </summary>
    [Generator(LanguageNames.CSharp)]
    public sealed class MappingNodeGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(static ctx =>
            {
                ctx.AddSource("EntityForeignKeyAttribute.g.cs", EntityForeignKeyAttributeSourceText.Value);
            });

            var mappingClasses = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) => node is ClassDeclarationSyntax { BaseList: not null },
                    transform: static (ctx, ct) => TryGetMappingClass(ctx, ct))
                .Where(static info => info is not null)
                .Select(static (info, _) => info!);

            // All parsed mappings, collected once per compilation - needed to bridge a
            // navigation's RelatedEntityType (an entity symbol) back to the model type that
            // owns it, since Wrapper only ever names model types, not entity types.
            var allMappings = mappingClasses.Collect();

            // Wrapper's own root model types (e.g. { CustomerCustomerEdge }), resolved once
            // per compilation rather than re-parsing Wrapper for every mapping class.
            var rootModelTypes = context.CompilationProvider
                .Select(static (compilation, ct) => WrapperRootModelResolver.Resolve(compilation, ct));

            var combined = mappingClasses
                .Combine(allMappings)
                .Combine(rootModelTypes);

            context.RegisterSourceOutput(combined, static (spc, data) =>
            {
                var ((info, all), rootModelTypes) = data;
                Emit(spc, info, all, rootModelTypes);
            });
        }

        private static MappingClassInfo? TryGetMappingClass(GeneratorSyntaxContext ctx, CancellationToken ct)
        {
            var classDecl = (ClassDeclarationSyntax)ctx.Node;
            var symbol = ctx.SemanticModel.GetDeclaredSymbol(classDecl, ct) as INamedTypeSymbol;
            if (symbol is null || symbol.IsAbstract)
                return null;

            var baseType = symbol.BaseType;
            if (baseType is null || baseType.OriginalDefinition.Name != "BaseModelMappingRegistration")
                return null;

            if (baseType.TypeArguments.Length != 1 || baseType.TypeArguments[0] is not INamedTypeSymbol modelType)
                return null;

            var buildMap = classDecl.Members
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(m => m.Identifier.Text == "BuildMap");

            if (buildMap is null)
                return null;

            return MappingClassParser.Parse(symbol, modelType, buildMap, ctx.SemanticModel, ct);
        }

        private static void Emit(
            SourceProductionContext spc,
            MappingClassInfo info,
            ImmutableArray<MappingClassInfo> allMappings,
            ImmutableHashSet<INamedTypeSymbol> rootModelTypes)
        {
            foreach (var d in info.Diagnostics)
                spc.ReportDiagnostic(d);

            if (info.Diagnostics.Any(x => x.Severity == DiagnosticSeverity.Error))
                return;

            // order now irrelevant for aliasing (safe)
            ModelChildrenInference.Apply(info);
            FieldMapGeneration.Apply(info, spc);

            var rootEntityTypes = ResolveRootEntityTypes(allMappings, rootModelTypes);

            var navResult = EntityNavigationConvention.Resolve(info, spc, rootEntityTypes);

            if (navResult.HasBlockingAmbiguity)
                return;

            var source = NodeTreeEmitter.EmitRegisterOverride(info, navResult);

            spc.AddSource($"{info.ClassName}.MappingRegistration.g.cs", source);
        }

        /// <summary>
        /// Bridges Wrapper's root MODEL types to the ENTITY types navigations actually
        /// target: for every parsed mapping whose ModelType is one of Wrapper's root
        /// properties, its EntityType (if any) is a "root entity type" - any navigation
        /// landing on that entity type targets a globally-aliased mapping and must not be
        /// prefixed by the navigating mapping's own Prefix.
        /// </summary>
        private static ImmutableHashSet<INamedTypeSymbol> ResolveRootEntityTypes(
            ImmutableArray<MappingClassInfo> allMappings,
            ImmutableHashSet<INamedTypeSymbol> rootModelTypes)
        {
            if (rootModelTypes.IsEmpty)
                return ImmutableHashSet.Create<INamedTypeSymbol>(SymbolEqualityComparer.Default);

            var builder = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>(SymbolEqualityComparer.Default);

            foreach (var mapping in allMappings)
            {
                if (mapping.EntityType is null)
                    continue;

                if (rootModelTypes.Contains(mapping.ModelType))
                    builder.Add(mapping.EntityType);
            }

            return builder.ToImmutable();
        }
    }
}