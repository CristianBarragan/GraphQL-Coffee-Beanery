using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CoffeeBeanery.GraphQL.Core.Mapping.Generators.Model;
using CoffeeBeanery.GraphQL.Core.Mapping.Generators.Parsing;

namespace CoffeeBeanery.GraphQL.Core.Mapping.Generators
{
    /// <summary>
    /// Walks every mapping class deriving from BaseMappingRegistration&lt;,&gt; or
    /// BaseModelMappingRegistration&lt;&gt;, statically computes the same
    /// FieldMaps/ModelChildren that NodeBuilder's GenerateReflectedFieldMaps/
    /// InferModelChildren compute via reflection at runtime, and emits a
    /// partial class overriding ApplyGeneratedMappings(NodeMap) with the
    /// result - removing the need for reflection over Model/Entity types at
    /// AOT runtime.
    ///
    /// Scope note: this generator covers Model-side FieldMap/ModelChildren
    /// generation only. Entity-side navigation resolution (the equivalent of
    /// NodeBuilder.BuildEntityChildren, and the AmbiguousNavigation/
    /// UnresolvedForeignKey diagnostics in MappingDiagnostics) is NOT covered
    /// here pending scope confirmation - that remains a runtime concern via
    /// EfEntityMetadata<TContext> for now.
    ///
    /// Implementation note: this re-scans the whole compilation's syntax
    /// trees on every recompute rather than using a fully incremental
    /// per-class pipeline node. That trades IDE-typing-latency performance
    /// for being straightforward to read/maintain - revisit if generator
    /// runtime becomes a problem on a large mapping set.
    /// </summary>
    [Generator(LanguageNames.CSharp)]
    public sealed class MappingFieldMapGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterSourceOutput(context.CompilationProvider, static (spc, compilation) =>
            {
                foreach (var tree in compilation.SyntaxTrees)
                {
                    var semanticModel = compilation.GetSemanticModel(tree);
                    var root = tree.GetRoot(spc.CancellationToken);

                    foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
                    {
                        spc.CancellationToken.ThrowIfCancellationRequested();
                        ProcessClass(classDecl, semanticModel, compilation, spc);
                    }
                }
            });
        }

        private static void ProcessClass(
            ClassDeclarationSyntax classDecl,
            SemanticModel semanticModel,
            Compilation compilation,
            SourceProductionContext spc)
        {
            if (semanticModel.GetDeclaredSymbol(classDecl, spc.CancellationToken) is not INamedTypeSymbol classSymbol)
                return;

            var modelType = ResolveModelType(classSymbol);
            if (modelType is null)
                return; // not a BaseMappingRegistration<,>/BaseModelMappingRegistration<> descendant

            var buildMap = classDecl.Members
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(m => m.Identifier.Text == "BuildMap" && m.ParameterList.Parameters.Count == 0);

            if (buildMap is null)
                return; // no override in this class - nothing for us to read here

            var info = MappingClassParser.Parse(classSymbol, modelType, buildMap, semanticModel, spc.CancellationToken);

            // Critical: if this class's BuildMap() starts by calling
            // base.BuildMap(), the manual FieldMaps/ExcludedFieldMappings/
            // ModelChildren declared in the BASE class's own BuildMap() are
            // invisible to the Parse() call above (it only reads this one
            // method's statements). Without this merge, generated mappings
            // for the derived class would re-derive (and potentially
            // conflict with - e.g. losing a FromEnum/ToEnum conversion)
            // anything the base class already declared manually.
            if (CallsBaseBuildMap(buildMap))
                MergeBaseClassMappings(classSymbol, info, compilation, spc.CancellationToken);

            foreach (var diagnostic in info.Diagnostics)
                spc.ReportDiagnostic(diagnostic);

            var (fieldMaps, modelChildren) = ComputeGeneratedMappings(info, spc);

            if (fieldMaps.Count == 0 && modelChildren.Count == 0)
                return; // nothing generated - don't emit an empty file or demand `partial` for no reason

            if (!classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    MappingDiagnostics.ClassNotPartial,
                    classDecl.Identifier.GetLocation(),
                    classSymbol.Name));
                return;
            }

            var source = GenerateSource(info, fieldMaps, modelChildren);
            spc.AddSource($"{classSymbol.Name}.Mappings.g.cs", source);
        }

        // -------------------------------------------------------------------------
        // ResolveModelType
        //
        // Walks the base-type chain looking for BaseMappingRegistration<TModel,TEntity>
        // or BaseModelMappingRegistration<TModel>, returning TModel - the symbol-level
        // equivalent of NodeMap.ModelType.
        // -------------------------------------------------------------------------
        private static INamedTypeSymbol? ResolveModelType(INamedTypeSymbol classSymbol)
        {
            for (var current = classSymbol.BaseType; current is not null; current = current.BaseType)
            {
                if (current.OriginalDefinition.Name is "BaseMappingRegistration" or "BaseModelMappingRegistration")
                {
                    return current.TypeArguments.Length > 0
                        ? current.TypeArguments[0] as INamedTypeSymbol
                        : null;
                }
            }
            return null;
        }

        private static bool CallsBaseBuildMap(MethodDeclarationSyntax buildMap)
        {
            if (buildMap.Body is null) return false;

            return buildMap.Body.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Any(inv => inv.Expression is MemberAccessExpressionSyntax
                {
                    Expression: BaseExpressionSyntax,
                    Name.Identifier.Text: "BuildMap"
                });
        }

        // -------------------------------------------------------------------------
        // MergeBaseClassMappings
        //
        // Re-parses the immediate base class's own BuildMap() (and recurses if
        // THAT also chains to its base) and folds its manual dedup data into
        // the derived class's info, before any generated mappings are computed.
        // -------------------------------------------------------------------------
        private static void MergeBaseClassMappings(
            INamedTypeSymbol classSymbol,
            MappingClassInfo info,
            Compilation compilation,
            CancellationToken ct)
        {
            var baseType = classSymbol.BaseType;
            if (baseType is null) return;

            foreach (var syntaxRef in baseType.OriginalDefinition.DeclaringSyntaxReferences)
            {
                if (syntaxRef.GetSyntax(ct) is not ClassDeclarationSyntax baseDecl) continue;

                var baseBuildMap = baseDecl.Members
                    .OfType<MethodDeclarationSyntax>()
                    .FirstOrDefault(m => m.Identifier.Text == "BuildMap" && m.ParameterList.Parameters.Count == 0);

                if (baseBuildMap is null) continue;

                var baseSemanticModel = compilation.GetSemanticModel(syntaxRef.SyntaxTree);
                var baseInfo = MappingClassParser.Parse(baseType, info.ModelType, baseBuildMap, baseSemanticModel, ct);

                info.ManualFieldMaps.AddRange(baseInfo.ManualFieldMaps);
                info.ExcludedFieldMappings.AddRange(baseInfo.ExcludedFieldMappings);
                info.ModelChildren.AddRange(baseInfo.ModelChildren);
                foreach (var t in baseInfo.ModelToEntityTypes)
                    info.ModelToEntityTypes.Add(t);

                if (CallsBaseBuildMap(baseBuildMap))
                    MergeBaseClassMappings(baseType, info, compilation, ct);
            }
        }

        // -------------------------------------------------------------------------
        // ComputeGeneratedMappings
        //
        // Symbol-level port of NodeBuilder.InferModelChildren +
        // GenerateReflectedFieldMaps. Dedups against everything
        // MappingClassParser found (now including anything merged in from a
        // base class above), exactly like the runtime version dedups against
        // map.FieldMaps / map.ExcludedFieldMappings / map.ModelChildren.
        //
        // Unlike the runtime version's Console.WriteLine calls, mismatches
        // and unmatched properties are reported as real build diagnostics
        // (TypeIncompatible / NoMatchingProperty) via spc.
        // -------------------------------------------------------------------------
        private static (List<(string Source, string Entity, string Dest)> FieldMaps, List<string> ModelChildren)
            ComputeGeneratedMappings(MappingClassInfo info, SourceProductionContext spc)
        {
            var fieldMaps = new List<(string Source, string Entity, string Dest)>();
            var modelChildren = new List<string>();
            var modelType = info.ModelType;
            var classLocation = info.ClassSymbol.Locations.FirstOrDefault() ?? Location.None;

            var existingChildren = new HashSet<string>(
                info.ModelChildren.Select(c => c.To),
                StringComparer.OrdinalIgnoreCase);

            foreach (var prop in GetAllPublicInstanceProperties(modelType))
            {
                var elementType = UnwrapCollection(prop.Type);
                var scalarCheck = UnwrapNullable(elementType);

                if (IsScalarType(scalarCheck) || scalarCheck.SpecialType == SpecialType.System_String)
                    continue;

                if (existingChildren.Add(elementType.Name))
                    modelChildren.Add(elementType.Name);
            }

            if (info.ModelToEntityTypes.Count == 0)
                return (fieldMaps, modelChildren);

            var candidateEntityNames = string.Join(", ", info.ModelToEntityTypes.Select(t => t.Name));

            foreach (var prop in GetAllPublicInstanceProperties(modelType))
            {
                var unwrapped = UnwrapCollection(prop.Type);
                var scalarCheck = UnwrapNullable(unwrapped);
                if (!IsScalarType(scalarCheck) && scalarCheck.SpecialType != SpecialType.System_String)
                    continue;

                var matchedAny = false;

                foreach (var entityType in info.ModelToEntityTypes)
                {
                    if (IsExcluded(info, prop.Name, entityType.Name))
                        continue;

                    if (HasManualFieldMap(info, prop.Name, entityType.Name))
                    {
                        matchedAny = true;
                        continue; // already hand-written (with or without FromEnum/ToEnum) - never duplicated
                    }

                    var entityProp = FindWritableProperty(entityType, prop.Name);
                    if (entityProp is null)
                        continue;

                    if (!AreTypesCompatible(prop.Type, entityProp.Type))
                    {
                        spc.ReportDiagnostic(Diagnostic.Create(
                            MappingDiagnostics.TypeIncompatible,
                            classLocation,
                            modelType.Name, prop.Name, prop.Type.Name,
                            entityType.Name, entityProp.Name, entityProp.Type.Name));
                        continue;
                    }

                    matchedAny = true;
                    fieldMaps.Add((prop.Name, entityType.Name, entityProp.Name));
                }

                if (!matchedAny)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        MappingDiagnostics.NoMatchingProperty,
                        classLocation,
                        modelType.Name, prop.Name, candidateEntityNames));
                }
            }

            return (fieldMaps, modelChildren);
        }

        private static bool IsExcluded(MappingClassInfo info, string sourceName, string destEntity) =>
            info.ExcludedFieldMappings.Any(x =>
                string.Equals(x.SourceName, sourceName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.DestinationEntity, destEntity, StringComparison.OrdinalIgnoreCase));

        private static bool HasManualFieldMap(MappingClassInfo info, string sourceName, string destEntity) =>
            info.ManualFieldMaps.Any(f =>
                string.Equals(f.SourceName, sourceName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(f.DestinationEntity, destEntity, StringComparison.OrdinalIgnoreCase));

        // -------------------------------------------------------------------------
        // Symbol-level mirrors of NodeBuilder's Type-based helpers
        // -------------------------------------------------------------------------
        private static IEnumerable<IPropertySymbol> GetAllPublicInstanceProperties(INamedTypeSymbol type)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var current = type; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
            {
                foreach (var member in current.GetMembers().OfType<IPropertySymbol>())
                {
                    if (member.DeclaredAccessibility != Accessibility.Public) continue;
                    if (member.IsStatic) continue;
                    if (member.GetMethod is null) continue;
                    if (seen.Add(member.Name))
                        yield return member;
                }
            }
        }

        private static IPropertySymbol? FindWritableProperty(INamedTypeSymbol type, string name)
        {
            for (var current = type; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
            {
                foreach (var member in current.GetMembers().OfType<IPropertySymbol>())
                {
                    if (member.DeclaredAccessibility != Accessibility.Public) continue;
                    if (member.IsStatic) continue;
                    if (member.SetMethod is null) continue;
                    if (string.Equals(member.Name, name, StringComparison.OrdinalIgnoreCase))
                        return member;
                }
            }
            return null;
        }

        private static ITypeSymbol UnwrapNullable(ITypeSymbol type) =>
            type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } named
                ? named.TypeArguments[0]
                : type;

        private static ITypeSymbol UnwrapCollection(ITypeSymbol type)
        {
            if (type.SpecialType == SpecialType.System_String)
                return type;

            if (type is INamedTypeSymbol { IsGenericType: true } named)
            {
                var defName = named.OriginalDefinition.ToDisplayString();
                if (defName is "System.Collections.Generic.List<T>"
                    or "System.Collections.Generic.IEnumerable<T>"
                    or "System.Collections.Generic.ICollection<T>"
                    or "System.Collections.Generic.IList<T>")
                {
                    return named.TypeArguments[0];
                }
            }

            return type;
        }

        private static bool IsScalarType(ITypeSymbol type)
        {
            var unwrapped = UnwrapNullable(type);

            if (unwrapped.TypeKind == TypeKind.Enum)
                return true;

            switch (unwrapped.SpecialType)
            {
                case SpecialType.System_String:
                case SpecialType.System_Boolean:
                case SpecialType.System_Byte:
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_Decimal:
                case SpecialType.System_Char:
                    return true;
            }

            var fullName = unwrapped.ToDisplayString();
            return fullName is "System.Guid" or "System.DateTime" or "System.DateTimeOffset";
        }

        private static bool IsNumeric(ITypeSymbol type) => type.SpecialType switch
        {
            SpecialType.System_Byte or SpecialType.System_SByte or
            SpecialType.System_Int16 or SpecialType.System_UInt16 or
            SpecialType.System_Int32 or SpecialType.System_UInt32 or
            SpecialType.System_Int64 or SpecialType.System_UInt64 or
            SpecialType.System_Single or SpecialType.System_Double or
            SpecialType.System_Decimal => true,
            _ => false
        };

        private static bool AreTypesCompatible(ITypeSymbol modelType, ITypeSymbol entityType)
        {
            var a = UnwrapNullable(modelType);
            var b = UnwrapNullable(entityType);

            if (SymbolEqualityComparer.Default.Equals(a, b))
                return true;

            var aName = a.ToDisplayString();
            var bName = b.ToDisplayString();

            if (aName == "System.Guid" && bName == "string") return true;
            if (aName == "string" && bName == "System.Guid") return true;

            if (a.TypeKind == TypeKind.Enum && IsNumeric(b)) return true;
            if (b.TypeKind == TypeKind.Enum && IsNumeric(a)) return true;
            if (a.TypeKind == TypeKind.Enum && b.TypeKind == TypeKind.Enum) return true;
            if (IsNumeric(a) && IsNumeric(b)) return true;

            return false;
        }

        // -------------------------------------------------------------------------
        // GenerateSource
        //
        // Emits literal strings rather than nameof(...) - the mapping classes'
        // own usings/aliases (e.g. `DataEntity = Database.Entity`) aren't
        // available in this generated file, and FieldMap/ModelKey only ever
        // need the plain string at runtime anyway - exactly what
        // MappingClassParser.EvaluateStringLikeExpression already reduces
        // nameof(...) down to when reading the hand-written BuildMap().
        // -------------------------------------------------------------------------
        private static string GenerateSource(
            MappingClassInfo info,
            List<(string Source, string Entity, string Dest)> fieldMaps,
            List<string> modelChildren)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("#nullable enable");
            sb.AppendLine("using CoffeeBeanery.GraphQL.Core.Mapping;");
            sb.AppendLine("using CoffeeBeanery.GraphQL.Core.Sql;");
            sb.AppendLine();

            var hasNamespace = !string.IsNullOrEmpty(info.Namespace);
            var indent = hasNamespace ? "    " : "";

            if (hasNamespace)
            {
                sb.AppendLine($"namespace {info.Namespace}");
                sb.AppendLine("{");
            }

            sb.AppendLine($"{indent}partial class {info.ClassName}");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    protected override void ApplyGeneratedMappings(NodeMap map)");
            sb.AppendLine($"{indent}    {{");

            foreach (var child in modelChildren)
                sb.AppendLine($"{indent}        map.ModelChildren.Add(new ModelKey {{ To = \"{Escape(child)}\" }});");

            if (modelChildren.Count > 0 && fieldMaps.Count > 0)
                sb.AppendLine();

            foreach (var (source, entity, dest) in fieldMaps)
            {
                sb.AppendLine($"{indent}        map.FieldMaps.Add(new FieldMap");
                sb.AppendLine($"{indent}        {{");
                sb.AppendLine($"{indent}            SourceName        = \"{Escape(source)}\",");
                sb.AppendLine($"{indent}            DestinationEntity = \"{Escape(entity)}\",");
                sb.AppendLine($"{indent}            DestinationName   = \"{Escape(dest)}\"");
                sb.AppendLine($"{indent}        }});");
            }

            sb.AppendLine($"{indent}    }}");
            sb.AppendLine($"{indent}}}");

            if (hasNamespace)
                sb.AppendLine("}");

            return sb.ToString();
        }

        private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}