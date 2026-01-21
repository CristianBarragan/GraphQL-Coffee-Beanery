namespace CoffeeBeanery.GraphQL.CoreNew.Sql
{
    public sealed class UpsertKey
    {
        public string Key { get; init; }
        public UpsertKey(string key) => Key = key;
    }
}
