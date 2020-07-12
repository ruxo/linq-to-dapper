using System;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using Dapper.Contrib.Linq2Dapper.Exceptions;
using Dapper.Contrib.Linq2Dapper.Helpers;
using System.Collections;
using System.Data.Common;

namespace Dapper.Contrib.Linq2Dapper
{
    class QueryProvider<TData> : IQueryProvider
    {
        readonly IDbConnection connection;
        readonly QueryBuilder<TData> qb;

        public QueryProvider(IDbConnection connection)
        {
            this.connection = connection;
            qb = new QueryBuilder<TData>();
        }

        public IQueryable CreateQuery(Expression expression)
        {
            Type elementType = TypeHelper.GetElementType(expression.Type);
            try
            {
                return (IQueryable)Activator.CreateInstance(typeof(Linq2Dapper<TData>).MakeGenericType(elementType), this, expression);
            }
            catch (System.Reflection.TargetInvocationException tie)
            {
                throw tie.InnerException;
            }
        }

        /// <summary>
        /// Queryable's collection-returning standard query operators call this method.
        /// </summary>
        /// <param name="expression"></param>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        public IQueryable<TResult> CreateQuery<TResult>(Expression expression)
        {
            return new Linq2Dapper<TResult>(this, expression);
        }

        public object Execute(Expression expression) => Query(expression);

        /// <summary>
        /// Queryable's "single value" standard query operators call this method.
        /// </summary>
        /// <param name="expression"></param>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        public TResult Execute<TResult>(Expression expression) =>
            (TResult)Query(expression, typeof(IEnumerable).IsAssignableFrom(typeof(TResult)));

        // Executes the expression tree that is passed to it.
        object Query(Expression expression, bool isEnumerable = false)
        {
            try
            {
                qb.Evaluate(expression);
                return isEnumerable ? connection.Query<TData>(qb.Sql, qb.Parameters)
                                    : connection.ExecuteScalar(qb.Sql, qb.Parameters);
            }
            catch (DbException ex)
            {
                throw new InvalidQueryException(qb.Sql, ex);
            }
        }

    }
}
