namespace CoffeeBeanery.GraphQL.Core.Sql
{
    public sealed class UpsertKey
    {
        public string EntityName { get; }
        public string PropertyName { get; }

        public UpsertKey(string entityName, string propertyName)
        {
            EntityName = entityName;
            PropertyName = propertyName;
        }
    }
}