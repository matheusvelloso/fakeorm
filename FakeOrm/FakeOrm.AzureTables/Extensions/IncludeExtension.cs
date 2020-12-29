using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace FakeOrm.AzureTables.Extensions
{
    public static class IncludeExtension
    {
        public static IQueryable<T> Include<T, TProperty>(this IQueryable<T> queryable, Expression<Func<T, TProperty>> expression)
        {
            var query = Enumerable.Empty<T>().AsQueryable();

            return queryable;
        }

        public static IList<IncludePropertyCls<T>> IncludeProperty<T>(this T entity, Expression<Func<T, object>> expression)
        {
            var memberExpression = expression.Body as MemberExpression ?? ((UnaryExpression)expression.Body).Operand as MemberExpression;

            return new List<IncludePropertyCls<T>>() { new IncludePropertyCls<T>(memberExpression.Member.Name) };
        }

        public static IList<IncludePropertyCls<T>> IncludeProperty<T>(this IList<IncludePropertyCls<T>> list, Expression<Func<T, object>> expression)
        {
            var memberExpression = expression.Body as MemberExpression ?? ((UnaryExpression)expression.Body).Operand as MemberExpression;

            list.Add(new IncludePropertyCls<T>(memberExpression.Member.Name));

            return list;
        }
    }

    public class IncludePropertyCls<TClass>
    {
        public IncludePropertyCls(string propertyName)
        {
            PropertyName = propertyName;
        }

        public TClass Type { get; set; }

        public string PropertyName { get; set; }
    }
}
