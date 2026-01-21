namespace CoffeeBeanery.GraphQL.Core.Mapping
{
    public sealed class UpsertKey
    {
        public string Entity { get; set; }
        public string Column { get; set; }

        public UpsertKey() { }
        public UpsertKey(string entity, string column)
        {
            Entity = entity;
            Column = column;
        }
    }
}