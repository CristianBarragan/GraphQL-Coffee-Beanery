using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using CoffeeBeanery.GraphQL.Core.Mapping.Generators.Model;

namespace CoffeeBeanery.GraphQL.Core.Mapping.Generators.Passes
{
    /// <summary>
    /// Compile-time replacement for NodeBuilder.BuildEntityChildren's use of
    /// EfEntityMetadata&lt;TContext&gt;.GetNavigations. Since the generator runs
    /// at compile time it has no live DbContext model to query, so navigations
    /// are discovered by convention over the entity's own property shape:
    ///
    ///   1. [EntityForeignKey] on the entity always wins (explicit escape hatch).
    ///   2. Otherwise: a property of type TRelated (or ICollection&lt;TRelated&gt;/
    ///      List&lt;TRelated&gt;/IEnumerable&lt;TRelated&gt;) is treated as a navigation
    ///      if a sibling scalar property named "{NavigationName}Key" or
    ///      "{TRelated.Name}Key" exists on the entity - mirroring the
    ///      CustomerBankingRelationshipKey / ContractKey / AccountKey pattern
    ///      already used throughout the codebase.
    ///   3. Multiple navigations to the same related type require either
    ///      [EntityForeignKey(..., navigationName: ...)] per navigation, or a
    ///      ModelToEntity entry whose AliasProperty matches the navigation name
    ///      (same disambiguation contract NodeBuilder enforces today).
    ///      Unresolved ambiguity is CBMAP003 (build error, not runtime throw).
    ///
    /// rootEntityTypes identifies navigation targets whose owning model is itself one of
    /// Wrapper's root payload properties (resolved by WrapperRootModelResolver + a
    /// cross-mapping entityType->modelType lookup built by the caller). Those targets
    /// register under a single bare, un-prefixed alias - never a role-prefixed one - so
    /// NavigationInfo.TargetIsRoot is set accordingly for NodeTreeEmitter.AliasForExpr to
    /// respect, the same way it already special-cases navigations back to the mapping's own
    /// entity type.
    /// </summary>
    internal static class EntityNavigationConvention
    {
        public static NavigationResolutionResult Resolve(
            MappingClassInfo info,
            SourceProductionContext spc,
            ISet<INamedTypeSymbol> rootEntityTypes)
        {
            var result = new NavigationResolutionResult();

            if (info.EntityType is null)
                return result;

            var explicitAttrs = GetEntityForeignKeyAttributes(info.EntityType);
            var properties = info.EntityType.GetMembers().OfType<IPropertySymbol>()
                .Where(p => p.GetMethod is not null && !p.IsStatic)
                .ToList();

            var navigationCandidates = new List<(IPropertySymbol Property, INamedTypeSymbol RelatedType, bool IsCollection)>();

            foreach (var prop in properties)
            {
                var (elementType, isCollection) = UnwrapCollection(prop.Type);
                if (elementType is not INamedTypeSymbol named)
                    continue;

                if (IsScalarLike(named))
                    continue;

                navigationCandidates.Add((prop, named, isCollection));
            }

            var groupedByRelatedType = navigationCandidates
                .GroupBy(c => c.RelatedType, SymbolEqualityComparer.Default)
                .ToList();

            foreach (var group in groupedByRelatedType)
            {
                var relatedType = (INamedTypeSymbol)group.Key!;
                var isAmbiguous = group.Count() > 1;
                var targetIsRoot = rootEntityTypes.Contains(relatedType);

                var attrsForType = explicitAttrs
                    .Where(a => SymbolEqualityComparer.Default.Equals(a.RelatedEntityType, relatedType))
                    .ToList();

                var aliasKeysForType = info.ModelToEntity
                    .Where(k => SymbolEqualityComparer.Default.Equals(k.EntityType, relatedType) && k.AliasProperty is not null)
                    .ToList();

                foreach (var (property, related, isCollection) in group)
                {
                    // 1. Explicit attribute match (by navigation name when ambiguous, or the lone entry otherwise).
                    var attrMatch = isAmbiguous
                        ? attrsForType.FirstOrDefault(a => a.NavigationName == property.Name)
                        : attrsForType.FirstOrDefault();

                    if (attrMatch is not null)
                    {
                        result.Navigations.Add(new NavigationInfo
                        {
                            NavigationName = property.Name,
                            RelatedEntityType = related,
                            ForeignKeyProperty = attrMatch.ForeignKeyProperty,
                            PrincipalKeyProperty = attrMatch.PrincipalKeyProperty,
                            IsCollection = isCollection,
                            TargetIsRoot = targetIsRoot
                        });
                        continue;
                    }

                    // 2. Convention: "{NavigationName}Key" then "{RelatedType.Name}Key".
                    var fkProp = FindScalarSibling(properties, property.Name + "Key")
                                 ?? FindScalarSibling(properties, related.Name + "Key");

                    if (fkProp is null)
                    {
                        spc.ReportDiagnostic(Diagnostic.Create(
                            MappingDiagnostics.UnresolvedForeignKey,
                            info.EntityType.Locations.FirstOrDefault() ?? Location.None,
                            info.EntityType.Name, property.Name, related.Name));
                        result.HasBlockingAmbiguity = true;
                        continue;
                    }

                    // 3. Ambiguity without any disambiguating attribute or ModelToEntity alias -> error.
                    if (isAmbiguous)
                    {
                        var aliasMatch = aliasKeysForType.FirstOrDefault(k =>
                            string.Equals(k.AliasProperty, property.Name, System.StringComparison.OrdinalIgnoreCase));

                        if (aliasMatch is null)
                        {
                            spc.ReportDiagnostic(Diagnostic.Create(
                                MappingDiagnostics.AmbiguousNavigation,
                                info.EntityType.Locations.FirstOrDefault() ?? Location.None,
                                info.EntityType.Name,
                                related.Name,
                                string.Join(", ", group.Select(g => g.Property.Name)),
                                property.Name,
                                info.ModelType.Name));
                            result.HasBlockingAmbiguity = true;
                            continue;
                        }
                    }

                    // Principal key: assume the related entity's own "{Name}Key" property
                    // (matches every entity in the sample mapping: ContractKey, AccountKey, etc.).
                    var principalKeyName = related.GetMembers().OfType<IPropertySymbol>()
                        .FirstOrDefault(p => p.Name == related.Name + "Key")?.Name
                        ?? related.Name + "Key";

                    result.Navigations.Add(new NavigationInfo
                    {
                        NavigationName = property.Name,
                        RelatedEntityType = related,
                        ForeignKeyProperty = fkProp.Name,
                        PrincipalKeyProperty = principalKeyName,
                        IsCollection = isCollection,
                        TargetIsRoot = targetIsRoot
                    });
                }
            }

            return result;
        }

        private static IPropertySymbol? FindScalarSibling(List<IPropertySymbol> properties, string name) =>
            properties.FirstOrDefault(p => string.Equals(p.Name, name, System.StringComparison.OrdinalIgnoreCase));

        private static (ITypeSymbol ElementType, bool IsCollection) UnwrapCollection(ITypeSymbol type)
        {
            if (type is INamedTypeSymbol { IsGenericType: true } named &&
                named.TypeArguments.Length == 1 &&
                named.Name is "List" or "ICollection" or "IList" or "IEnumerable")
            {
                return (named.TypeArguments[0], true);
            }

            return (type, false);
        }

        private static bool IsScalarLike(INamedTypeSymbol type)
        {
            if (type.TypeKind == TypeKind.Enum) return true;
            if (type.SpecialType != SpecialType.None) return true;

            return type.Name is "String" or "Guid" or "DateTime" or "DateTimeOffset" or "Decimal";
        }

        private record ForeignKeyAttrInfo(INamedTypeSymbol RelatedEntityType, string ForeignKeyProperty, string PrincipalKeyProperty, string? NavigationName);

        private static List<ForeignKeyAttrInfo> GetEntityForeignKeyAttributes(INamedTypeSymbol entityType)
        {
            var list = new List<ForeignKeyAttrInfo>();

            foreach (var attr in entityType.GetAttributes())
            {
                if (attr.AttributeClass?.Name != "EntityForeignKeyAttribute")
                    continue;

                var args = attr.ConstructorArguments;
                if (args.Length < 3)
                    continue;

                if (args[0].Value is not INamedTypeSymbol relatedType)
                    continue;

                var fk = args[1].Value as string;
                var pk = args[2].Value as string;
                var navName = args.Length > 3 ? args[3].Value as string : null;

                if (fk is null || pk is null)
                    continue;

                list.Add(new ForeignKeyAttrInfo(relatedType, fk, pk, navName));
            }

            return list;
        }
    }
}