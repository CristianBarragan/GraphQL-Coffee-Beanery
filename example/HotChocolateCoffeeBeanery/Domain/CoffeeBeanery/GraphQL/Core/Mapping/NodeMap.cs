using System.Linq.Expressions;
using System.Reflection;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.Mapping
{
    public sealed class NodeMap
    {
        public int Id { get; set; }

        public string Schema { get; set; } = "public";

        public bool IsRoot { get; set; }

        public bool IsModel { get; set; }

        public bool IsEntity { get; set; }

        public string Prefix { get; set; }

        public List<FieldMap> FieldMaps { get; } = new();

        public List<UpsertKey> UpsertKeys { get; private set; } = new();

        public string PrimaryKey { get; set; }

        public List<EntityKey> ModelToEntity { get; private set; } = new();

        public List<EntityKey> EntityChildrenRelated { get; set; } = new();

        public List<FieldMap> ExcludedFieldMappings { get; } = new();

        public List<ModelKey> ModelChildren { get; set; } = new();

        public bool IsGraph { get; set; }

        public Type ModelType { get; set; }

        public Type EntityType { get; set; }

        public List<EntityKey> EntityChildren { get; set; } = new();

        public Dictionary<string, PropertyInfo> ModelProperties { get; set; }
            = new();

        public Dictionary<string, PropertyInfo> EntityProperties { get; set; }
            = new();

        public Func<object, object> CreateMapper { get; set; }

        public Action<object, object> UpdateMapper { get; set; }

        public string Alias { get; set; }

        public string ModelName { get; set; }

        public GraphMap? GraphMap { get; set; }

        public EntityKey AddModelToEntity<TModel, TEntity>(
            Expression<Func<TModel, object?>> fk,
            Expression<Func<TEntity, object?>> pk,
            Expression<Func<TModel, object?>>? alias = null,
            bool isPrimary = false)
        {
            var entityKey = new EntityKey
            {
                EntityType = typeof(TEntity),
                FromColumn = GetMemberName(fk),
                ToColumn = GetMemberName(pk),
                To = typeof(TEntity).Name,
                AliasProperty = alias != null
                    ? GetMemberName(alias)
                    : string.Empty
            };

            ModelToEntity.Add(entityKey);

            if (isPrimary || EntityType is null)
            {
                EntityType = typeof(TEntity);
                IsEntity = true;
            }

            return entityKey;
        }

        private static string GetMemberName(LambdaExpression expression)
        {
            Expression body = expression.Body;

            if (body is UnaryExpression unary &&
                unary.NodeType == ExpressionType.Convert)
            {
                body = unary.Operand;
            }

            if (body is MemberExpression member)
            {
                return member.Member.Name;
            }

            throw new ArgumentException(
                $"Expression '{expression}' does not reference a property.");
        }
    }
}