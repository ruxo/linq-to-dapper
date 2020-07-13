using System;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using Dapper.Contrib.Linq2Dapper.Exceptions;
using Dapper.Contrib.Linq2Dapper.Helpers;
using System.Collections;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Reflection;
using Dapper.Contrib.Linq2Dapper.ContextBuilders;

namespace Dapper.Contrib.Linq2Dapper
{
    class QueryProvider<TData> : IQueryProvider
    {
        readonly IDbConnection connection;

        public QueryProvider(IDbConnection connection)
        {
            this.connection = connection;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            Type elementType = TypeHelper.GetElementType(expression.Type);
            try
            {
                return (IQueryable)Activator.CreateInstance(typeof(Linq2Dapper<TData>).MakeGenericType(elementType), this, expression);
            }
            catch (TargetInvocationException tie)
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

        public object Execute(Expression expression) => Query<TData>(expression);

        /// <summary>
        /// Queryable's "single value" standard query operators call this method.
        /// </summary>
        /// <param name="expression"></param>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        public TResult Execute<TResult>(Expression expression) => (TResult)Query<TResult>(expression);

        // Executes the expression tree that is passed to it.
        object Query<T>(Expression expression)
        {
            var (sql, parameters) = new SelectQueryBuilder<TData>().Evaluate(expression);
            try {
                var expected = typeof(T);
                var isEnumerable = typeof(IEnumerable).IsAssignableFrom(expected);
                return isEnumerable ? QueryUtils.GetQueryMethod(expected.GenericTypeArguments[0])
                                                .Invoke(null, new object?[] {connection, sql, parameters, null, true, null, null})
                                    : connection.ExecuteScalar(sql, parameters);
            }
            catch (DbException ex)
            {
                throw new InvalidQueryException(sql, ex);
            }
        }
    }

    static class QueryUtils
    {
        static readonly MethodInfo DapperQueryMethod =
            typeof(SqlMapper).GetMethods()
                             .Where(m => m.Name == nameof(SqlMapper.Query) && m.IsGenericMethod && m.GetParameters().Length == 7)
                             .Single(m => m.IsGenericMethod);
        static readonly ConcurrentDictionary<Type,MethodInfo> QueryCache = new ConcurrentDictionary<Type, MethodInfo>();
        public static MethodInfo GetQueryMethod(Type target) =>
            QueryCache.TryGetValue(target, out var v) ? v : QueryCache[target] = DapperQueryMethod.MakeGenericMethod(target);
    }
}
