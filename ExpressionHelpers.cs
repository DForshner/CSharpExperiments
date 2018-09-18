using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ExpressionHelpers
{
    public static class ExpressionHelpers
    {
        public static string ResolveCollectionName<TEntity, TCollection>(Expression<Func<TEntity, ICollection<TCollection>>> expression)
        {
            var expr = expression.Body as MemberExpression;
            if (expr.IsNull())
            {
                var u = expression.Body as UnaryExpression;
                expr = u.Operand as MemberExpression;
            }
            return expr.ToString().Substring(expr.ToString().IndexOf(".") + 1);
        }

        public static string ResolveCollectionPropertyName<TEntity, TCollection>(Expression<Func<TCollection, object>> expression)
        {
            var expr = expression.Body as MemberExpression;

            if (expr.IsNull())
            {
                var u = expression.Body as UnaryExpression;
                expr = u.Operand as MemberExpression;
            }

            return expr.ToString().Substring(expr.ToString().IndexOf(".") + 1);
        }

        public static string ResolvePropertyName<TEntity>(Expression<Func<TEntity, object>> expression)
        {
            var expr = expression.Body as MemberExpression;

            if (expr.IsNull())
            {
                var u = expression.Body as UnaryExpression;
                expr = u.Operand as MemberExpression;
            }

            return expr.ToString().Substring(expr.ToString().IndexOf(".") + 1);
        }
    }
}
