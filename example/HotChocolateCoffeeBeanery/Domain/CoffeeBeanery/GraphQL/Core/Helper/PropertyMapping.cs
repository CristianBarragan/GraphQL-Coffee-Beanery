using System;
using System.Linq.Expressions;

namespace CoffeeBeanery.GraphQL.Core.Helper
{
    public sealed class PropertyMapping<TSrc, TDst>
    {
        public Expression<Func<TSrc, object>> SourceExpression { get; init; }
        public Expression<Func<TDst, object>> DestinationExpression { get; init; }

        public Dictionary<string, string>? FromEnum { get; init; }
        public Dictionary<string, string>? ToEnum { get; init; }
    }
}