namespace CoffeeBeanery.GraphQL.Core.Relationships
{
    public sealed class RelationshipKey
    {
        public string EntityName { get; }
        public string PropertyName { get; }

        public string Key => $"{EntityName}~{PropertyName}";

        public RelationshipKey(string entityName, string propertyName)
        {
            EntityName = entityName;
            PropertyName = propertyName;
        }

        public override string ToString() => Key;
    }
}