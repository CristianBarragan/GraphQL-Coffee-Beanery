namespace CoffeeBeanery.GraphQL.Core.Sql
{
    public sealed class EntityKey
    {
        public string From { get; set; } = "";
        public string AliasFrom { get; set; } = "";
        public string FromColumn { get; set; } = "";
        public string To { get; set; } = "";
        public string AliasTo { get; set; } = "";
        public string ToColumn { get; set; } = "";

        public Type EntityType { get; set; }

        public string AliasProperty { get; set; } = "";
    }

    public sealed class ModelKey
    {
        public string To { get; set; } = "";

        public string FieldName { get; set; } = "";
        
        public string AliasTo { get; set; } = "";
    }
}