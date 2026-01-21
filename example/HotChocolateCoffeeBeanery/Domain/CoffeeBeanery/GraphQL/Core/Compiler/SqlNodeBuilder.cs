using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.Compiler
{
    public static class SqlNodeBuilder
    {
        public static (
            Dictionary<string, SqlNode> select,
            Dictionary<string, SqlNode> edge,
            Dictionary<string, SqlNode> mutation)
            BuildFromModel<TModel>()
        {
            var select   = new Dictionary<string, SqlNode>(StringComparer.OrdinalIgnoreCase);
            var edge     = new Dictionary<string, SqlNode>(StringComparer.OrdinalIgnoreCase);
            var mutation = new Dictionary<string, SqlNode>(StringComparer.OrdinalIgnoreCase);

            var modelType = typeof(TModel);

            var entityMaps = MappingRegistry.All
                .SelectMany(d => d.EntityMaps)
                .Where(m =>
                    m.GetType().IsGenericType &&
                    m.GetType().GetGenericTypeDefinition() == typeof(EntityMap<,>) &&
                    m.GetType().GenericTypeArguments[0] == modelType)
                .ToList();

            foreach (var map in entityMaps)
            {
                var mapType    = map.GetType();
                var entityType = mapType.GenericTypeArguments[1];

                var entityName = entityType.Name;
                var schema     = (string?)mapType.GetProperty(nameof(EntityMap<object, object>.Schema))?
                                    .GetValue(map) ?? "public";

                // Root Id
                select[$"{entityName}~Id"] = new SqlNode
                {
                    EntityName = entityName,
                    ColumnName = "Id",
                    Schema     = schema,
                    EntityType = entityType,
                    SqlNodeType = SqlNodeType.Select
                };

                // Field maps
                var fieldMaps = (IEnumerable?)mapType
                    .GetProperty(nameof(EntityMap<object, object>.FieldMaps))
                    ?.GetValue(map);

                if (fieldMaps != null)
                {
                    foreach (dynamic fm in fieldMaps)
                    {
                        var column = fm.Destination.Body.ToString().Split('.').Last();
                        select[$"{entityName}~{column}"] = new SqlNode
                        {
                            EntityName  = entityName,
                            ColumnName  = column,
                            Schema      = schema,
                            EntityType  = entityType,
                            SqlNodeType = SqlNodeType.Select
                        };
                    }
                }

                // Link maps (edges)
                var linkMaps = (IEnumerable?)mapType
                    .GetProperty(nameof(EntityMap<object, object>.LinkMaps))
                    ?.GetValue(map);

                if (linkMaps != null)
                {
                    foreach (dynamic lm in linkMaps)
                    {
                        var from = lm.SourceKey.Body.ToString().Split('.').Last();
                        var to   = lm.EntityKey.Body.ToString().Split('.').Last();

                        edge[$"{entityName}~{from}"] = new SqlNode
                        {
                            EntityName  = entityName,
                            ColumnName  = from,
                            Schema      = schema,
                            EntityType  = entityType,
                            SqlNodeType = SqlNodeType.Edge,
                            LinkKeys = new List<LinkKey>
                            {
                                new LinkKey
                                {
                                    From   = $"{entityName}.{from}",
                                    To     = to,
                                    FromId = from,
                                    ToId   = to
                                }
                            }
                        };
                    }
                }
            }

            return (select, edge, mutation);
        }
    }
}
