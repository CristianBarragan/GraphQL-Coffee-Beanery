using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using CoffeeBeanery.GraphQL.Core.Mapping.Generators.Model;

namespace CoffeeBeanery.GraphQL.Core.Mapping.Generators.Passes
{
    /// <summary>
    /// Direct symbol-walking port of NodeBuilder.GenerateReflectedFieldMaps.
    /// For every scalar Model property, searches every entity referenced in
    /// ModelToEntity for a same-name, type-compatible property and generates
    /// a FieldMap for every match (fan-out across multiple entities allowed,
    /// e.g. Product.Amount -> Contract.Amount and Transaction.Amount).
    /// Manually-declared FieldMaps always take precedence and are never
    /// duplicated. Warnings become real diagnostics instead of Console.WriteLine.
    /// </summary>
    internal static class FieldMapGeneration
    {
        private static readonly HashSet<string> ScalarTypeNames = new()
        {
            "String", "Guid", "DateTime", "DateTimeOffset", "Decimal",
            "Boolean", "Byte", "SByte", "Int16", "UInt16", "Int32", "UInt32",
            "Int64", "UInt64", "Single", "Double", "Char"
        };

        public static void Apply(MappingClassInfo info, SourceProductionContext spc)
        {
            if (info.ModelToEntity.Count == 0)
                return;

            var modelProperties = info.ModelType.GetMembers().OfType<IPropertySymbol>()
                .Where(p => p.GetMethod is not null && !p.IsStatic)
                .ToList();

            var candidateEntities = info.ModelToEntity
                .Select(k => k.EntityType)
                .Distinct(SymbolEqualityComparer.Default)
                .Cast<INamedTypeSymbol>()
                .ToList();

            foreach (var modelProp in modelProperties)
            {
                var unwrapped = UnwrapCollection(modelProp.Type);
                if (!IsScalar(UnwrapNullable(unwrapped)))
                    continue;

                var matchedAny = false;

                foreach (var entityType in candidateEntities)
                {
                    if (IsExcluded(info, modelProp.Name, entityType.Name))
                        continue;

                    if (HasManualFieldMap(info, modelProp.Name, entityType.Name))
                    {
                        matchedAny = true;
                        continue;
                    }

                    var entityProp = entityType.GetMembers().OfType<IPropertySymbol>()
                        .Where(p => p.SetMethod is not null)
                        .FirstOrDefault(p => string.Equals(p.Name, modelProp.Name, System.StringComparison.OrdinalIgnoreCase));

                    if (entityProp is null)
                        continue;

                    if (!AreTypesCompatible(modelProp.Type, entityProp.Type))
                    {
                        spc.ReportDiagnostic(Diagnostic.Create(
                            MappingDiagnostics.TypeIncompatible,
                            modelProp.Locations.FirstOrDefault() ?? Location.None,
                            info.ModelType.Name, modelProp.Name, modelProp.Type.Name,
                            entityType.Name, entityProp.Name, entityProp.Type.Name));
                        continue;
                    }

                    matchedAny = true;

                    info.FieldMaps.Add(new FieldMapInfo
                    {
                        SourceName = modelProp.Name,
                        DestinationEntity = entityType.Name,
                        DestinationName = entityProp.Name,
                        IsGenerated = true
                    });
                }

                if (!matchedAny)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        MappingDiagnostics.NoMatchingProperty,
                        modelProp.Locations.FirstOrDefault() ?? Location.None,
                        info.ModelType.Name, modelProp.Name,
                        string.Join(", ", candidateEntities.Select(e => e.Name))));
                }
            }
        }

        private static bool IsExcluded(MappingClassInfo info, string sourceName, string destinationEntity) =>
            info.ExcludedFieldMappings.Any(x =>
                string.Equals(x.SourceName, sourceName, System.StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.DestinationEntity, destinationEntity, System.StringComparison.OrdinalIgnoreCase));

        private static bool HasManualFieldMap(MappingClassInfo info, string sourceName, string destinationEntity) =>
            info.FieldMaps.Any(f =>
                !f.IsGenerated &&
                string.Equals(f.SourceName, sourceName, System.StringComparison.OrdinalIgnoreCase) &&
                string.Equals(f.DestinationEntity, destinationEntity, System.StringComparison.OrdinalIgnoreCase));

        public static bool AreTypesCompatible(ITypeSymbol modelType, ITypeSymbol entityType)
        {
            var a = UnwrapNullable(modelType);
            var b = UnwrapNullable(entityType);

            if (SymbolEqualityComparer.Default.Equals(a, b)) return true;
            if (a.Name == "Guid" && b.SpecialType == SpecialType.System_String) return true;
            if (a.SpecialType == SpecialType.System_String && b.Name == "Guid") return true;
            if (a.TypeKind == TypeKind.Enum && IsNumeric(b)) return true;
            if (b.TypeKind == TypeKind.Enum && IsNumeric(a)) return true;
            if (a.TypeKind == TypeKind.Enum && b.TypeKind == TypeKind.Enum) return true;
            if (IsNumeric(a) && IsNumeric(b)) return true;

            return false;
        }

        private static ITypeSymbol UnwrapCollection(ITypeSymbol type)
        {
            if (type.SpecialType == SpecialType.System_String)
                return type;

            if (type is INamedTypeSymbol { IsGenericType: true } named &&
                named.TypeArguments.Length == 1 &&
                named.Name is "List" or "IEnumerable" or "ICollection" or "IList")
            {
                return named.TypeArguments[0];
            }

            return type;
        }

        private static ITypeSymbol UnwrapNullable(ITypeSymbol type) =>
            type is INamedTypeSymbol { Name: "Nullable", TypeArguments.Length: 1 } nullable
                ? nullable.TypeArguments[0]
                : type;

        private static bool IsScalar(ITypeSymbol type)
        {
            if (type.TypeKind == TypeKind.Enum) return true;
            if (type.IsValueType && type.SpecialType != SpecialType.None) return true;
            return ScalarTypeNames.Contains(type.Name);
        }

        private static bool IsNumeric(ITypeSymbol t) => t.Name is
            "Byte" or "SByte" or "Int16" or "UInt16" or "Int32" or "UInt32" or
            "Int64" or "UInt64" or "Single" or "Double" or "Decimal";
    }
}
