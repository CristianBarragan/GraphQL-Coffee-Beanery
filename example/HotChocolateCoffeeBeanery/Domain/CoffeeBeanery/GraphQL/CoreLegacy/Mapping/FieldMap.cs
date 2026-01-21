using System.Linq.Expressions;

namespace CoffeeBeanery.GraphQL.Core.Mapping
{
    /// <summary>
    /// Describes a property mapping between a model and an entity.
    /// </summary>
    public sealed class FieldMap
    {
        /// <summary>
        /// Source expression (model property)
        /// </summary>
        public LambdaExpression Source { get; set; }

        /// <summary>
        /// Destination expression (entity property)
        /// </summary>
        public LambdaExpression Destination { get; set; }
    }
}