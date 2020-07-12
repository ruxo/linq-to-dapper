using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;

namespace Dapper.Contrib.Linq2Dapper
{
    public class Linq2Dapper<TData> : IOrderedQueryable<TData>
    {
        #region Constructors

        /// <summary>
        /// This constructor is called by Provider.CreateQuery().
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="expression"></param>
        public Linq2Dapper(IQueryProvider provider, Expression? expression = null)
        {
            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Expression = expression ?? Expression.Constant(this);
        }

        /// <summary>
        /// This constructor is called by Provider.CreateQuery().
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="provider"></param>
        /// <param name="expression"></param>
        public Linq2Dapper(IDbConnection connection, IQueryProvider? provider = null, Expression<Func<TData, bool>>? expression = null)
            : this(provider ?? new QueryProvider<TData>(connection), expression)
        { }

        #endregion

        #region Properties

        public IQueryProvider Provider { get; }
        public Expression Expression { get; }

        public Type ElementType => typeof(TData);

        #endregion

        #region Enumerators
        public IEnumerator<TData> GetEnumerator() =>
            Provider.Execute<IEnumerable<TData>>(Expression).GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() =>
            Provider.Execute<System.Collections.IEnumerable>(Expression).GetEnumerator();

        #endregion
    }
}
