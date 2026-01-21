namespace CoffeeBeanery.GraphQL.Core.Helper
{
    public class EntityNode<T>
    {
        public T Item { get; }
        public string Cursor { get; }

        public EntityNode(T item, string cursor)
        {
            Item = item;
            Cursor = cursor;
        }
    }
}