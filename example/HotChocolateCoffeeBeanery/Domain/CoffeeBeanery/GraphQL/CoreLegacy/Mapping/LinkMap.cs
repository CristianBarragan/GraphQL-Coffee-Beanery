using System.Linq.Expressions;

namespace CoffeeBeanery.GraphQL.Core.Mapping
{
    /// <summary>
    /// Describes a relationship key mapping between a model and an entity.
    /// </summary>
    public sealed class LinkMap
    {
        /// <summary>
        /// Property in the model that links to a value in the entity.
        /// e.g.: model.CustomerKey
        /// </summary>
        public LambdaExpression SourceKey { get; set; }

        /// <summary>
        /// Property in the entity that corresponds to the source key.
        /// e.g.: entity.CustomerKey
        /// </summary>
        public LambdaExpression EntityKey { get; set; }
    }
}