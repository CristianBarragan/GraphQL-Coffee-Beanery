using System;
using System.Linq.Expressions;
using System.Reflection;

namespace CoffeeBeanery.GraphQL.Core.Helper
{
    public static class BulkMapper
    {
        public static Action<TSrc, TDst> Compile<TSrc, TDst>(
            PropertyMapping<TSrc, TDst>[] maps)
        {
            return (src, dst) =>
            {
                foreach (var m in maps)
                {
                    var getter = m.SourceExpression.Compile();
                    object value = getter(src);

                    if (value != null && m.FromEnum != null && m.ToEnum != null)
                    {
                        value = EnumMapper.Convert((Enum)value, m.FromEnum);
                    }

                    var body = m.DestinationExpression.Body;
                    if (body is UnaryExpression ue) body = ue.Operand;

                    var member = (MemberExpression)body;
                    var prop = (PropertyInfo)member.Member;
                    prop.SetValue(dst, value);
                }
            };
        }
    }
}