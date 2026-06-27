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

            var allMappings = mappingClasses.Collect();

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

            ModelChildrenInference.Apply(info);
            FieldMapGeneration.Apply(info, spc);

            var rootEntityTypes = ResolveRootEntityTypes(allMappings, rootModelTypes);

            var navResult = EntityNavigationConvention.Resolve(info, spc, rootEntityTypes);

            if (navResult.HasBlockingAmbiguity)
                return;

            var source = NodeTreeEmitter.EmitRegisterOverride(info, navResult);

            spc.AddSource($"{info.ClassName}.MappingRegistration.g.cs", source);
        }

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