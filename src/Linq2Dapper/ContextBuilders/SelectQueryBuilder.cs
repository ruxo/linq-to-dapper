using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Dapper.Contrib.Linq2Dapper.ContextBuilders.SelectQuery;
using Dapper.Contrib.Linq2Dapper.Helpers;
using Dapper.Contrib.Linq2Dapper.Writers;

namespace Dapper.Contrib.Linq2Dapper.ContextBuilders
{
    sealed class SelectQueryBuilder<TData> : ExpressionVisitor
    {
        readonly MsSqlWriter<TData> writer = new MsSqlWriter<TData>();
        readonly SelectBuilder selectContext = new SelectBuilder(typeof(TData));

        #region Visitors

        public (string SQL, DynamicParameters Bindings) Evaluate(Expression node)
        {
            if (!(node is ConstantExpression) || node.Type != typeof(Linq2Dapper<TData>))
                Visit(node);
            return (writer.SelectStatement(selectContext), writer.Parameters);
        }

        /// <summary>
        /// Visits the children of the <see cref="T:System.Linq.Expressions.MethodCallExpression"/>.
        /// </summary>
        /// <returns>
        /// The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.
        /// </returns>
        /// <param name="node">The expression to visit.</param>
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case MethodCall.Select:
                    selectContext.Visit(node.Arguments[1]);
                    return Visit(node.Arguments[0]);

                case MethodCall.Where:
                    return node.Arguments[0] is ConstantExpression? VisitUnary((UnaryExpression) node.Arguments[1]) : base.VisitMethodCall(node);

                case MethodCall.OrderBy:
                case MethodCall.ThenBy:
                case MethodCall.OrderByDescending:
                case MethodCall.ThenByDescending:
                    // ORDER BY ...
                    writer.WriteOrder(QueryHelper.GetPropertyNameWithIdentifierFromExpression(node.Arguments[1]), node.Method.Name.Contains("Descending"));
                    return node;

                case MethodCall.EndsWith:
                case MethodCall.StartsWith:
                case MethodCall.Contains:
                    // LIKE '(%)xyz(%)'
                    // LIKE IN (x, y, s)
                    return LikeInMethod(node);
                case MethodCall.IsNullOrEmpty:
                    // ISNULL(x, '') (!)= ''
                    if (IsNullMethod(node)) return node;
                    break;
                case MethodCall.Join:
                    return JoinMethod(node);
                case MethodCall.Take:
                    selectContext.Take((int)QueryHelper.GetValueFromExpression(node.Arguments[1]));
                    return node;
                case MethodCall.Skip:
                    selectContext.Skip((int)QueryHelper.GetValueFromExpression(node.Arguments[1]));
                    return node;
                case MethodCall.Single:
                case MethodCall.First:
                case MethodCall.FirstOrDefault:
                    selectContext.Take(1);
                    return Visit(node.Arguments[1]);
                case MethodCall.Distinct:
                    selectContext.RequireDistinct();
                    return node;
            }
            return base.VisitMethodCall(node);
        }

        /// <summary>
        /// Visits the children of the <see cref="T:System.Linq.Expressions.UnaryExpression"/>.
        /// </summary>
        /// <returns>
        /// The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.
        /// </returns>
        /// <param name="node">The expression to visit.</param>
        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType == ExpressionType.Not)
                writer.NotOperater = true;

            //if ((node.Operand is LambdaExpression) && (((LambdaExpression)node.Operand).Body is MemberExpression))
            //    base.Visit(node.Operand);

            if (!(node.Operand is MemberExpression))
                return base.VisitUnary(node);

            Visit(node.Operand);

            if (QueryHelper.IsBoolean(node.Operand.Type) && !QueryHelper.IsHasValue(node.Operand))
                writer.Boolean(!QueryHelper.IsPredicate(node));

            return node;
        }

        /// <summary>
        /// Visits the children of the <see cref="T:System.Linq.Expressions.MemberExpression"/>.
        /// </summary>
        /// <returns>
        /// The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.
        /// </returns>
        /// <param name="node">The expression to visit.</param>
        protected override Expression VisitMember(MemberExpression node)
        {
            if (QueryHelper.IsSpecificMemberExpression(node, node.Expression.Type, CacheHelper.TryGetPropertyList(node.Expression.Type)))
            {
                writer.ColumnName(QueryHelper.GetPropertyNameWithIdentifierFromExpression(node));
                return node;
            }
            else if (QueryHelper.IsVariable(node))
            {
                writer.Parameter(QueryHelper.GetValueFromExpression(node));
                return node;
            }
            else if (QueryHelper.IsHasValue(node))
            {
                var me = base.VisitMember(node);
                writer.IsNull();
                return me;
            }
            return base.VisitMember(node); ;
        }

        /// <summary>
        /// Visits the <see cref="T:System.Linq.Expressions.ConstantExpression"/>.
        /// </summary>
        /// <returns>
        /// The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.
        /// </returns>
        /// <param name="node">The expression to visit.</param>
        protected override Expression VisitConstant(ConstantExpression node)
        {
            writer.Parameter(node.Value);

            return base.VisitConstant(node);
        }

        /// <summary>
        /// Visits the children of the <see cref="T:System.Linq.Expressions.BinaryExpression"/>.
        /// </summary>
        /// <returns>
        /// The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.
        /// </returns>
        /// <param name="node">The expression to visit.</param>
        protected override Expression VisitBinary(BinaryExpression node)
        {
            var op = QueryHelper.GetOperator(node);
            Expression left = node.Left;
            Expression right = node.Right;

            writer.OpenBrace();

            if (QueryHelper.IsBoolean(left.Type))
            {
                Visit(left);
                writer.WhiteSpace();
                writer.Write(op);
                writer.WhiteSpace();
                Visit(right);
            }
            else
            {
                VisitValue(left);
                writer.WhiteSpace();
                writer.Write(op);
                writer.WhiteSpace();
                VisitValue(right);
            }

            writer.CloseBrace();

            return node;
        }

        Expression VisitValue(Expression expr)
        {
            return Visit(expr);
        }

        Expression VisitPredicate(Expression expr)
        {
            if (!QueryHelper.IsPredicate(expr) && !QueryHelper.IsHasValue(expr))
            {
                writer.Boolean(true);
            }
            return expr;
        }

        Expression VisitQuote(Expression expr)
        {
            return expr;
        }

        #endregion

        Expression JoinMethod(MethodCallExpression expression)
        {
            // first argument is another join or method call
            if (expression.Arguments[0] is MethodCallExpression) VisitMethodCall((MethodCallExpression)expression.Arguments[0]);

            var joinFromType = ((LambdaExpression)((UnaryExpression)expression.Arguments[4]).Operand).Parameters[0].Type;

            // from type if generic, possbily another join
            if (joinFromType.IsGenericType) joinFromType = joinFromType.GenericTypeArguments[1];
            var joinToType = ((LambdaExpression)((UnaryExpression)expression.Arguments[4]).Operand).Parameters[1].Type;

            QueryHelper.GetTableHelper(joinFromType);
            var joinToTable = QueryHelper.GetTableHelper(joinToType);

            var primaryJoinColumn = QueryHelper.GetPropertyNameWithIdentifierFromExpression(expression.Arguments[2]);
            var secondaryJoinColumn = QueryHelper.GetPropertyNameWithIdentifierFromExpression(expression.Arguments[3]);

            writer.WriteJoin(joinToTable.Name, joinToTable.Identifier, primaryJoinColumn, secondaryJoinColumn);

            return expression;
        }

        bool IsNullMethod(MethodCallExpression node)
        {
            if (!QueryHelper.IsSpecificMemberExpression(node.Arguments[0], typeof (TData), CacheHelper.TryGetPropertyList<TData>())) return false;

            writer.IsNullFunction();
            writer.OpenBrace();
            Visit(node.Arguments[0]);
            writer.Delimiter();
            writer.WhiteSpace();
            writer.EmptyString();
            writer.CloseBrace();
            writer.WhiteSpace();
            writer.Operator();
            writer.WhiteSpace();
            writer.EmptyString();
            return true;
        }

        Expression LikeInMethod(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(string))
            {
                // LIKE '..'
                if (!QueryHelper.IsSpecificMemberExpression(node.Object, typeof(TData), CacheHelper.TryGetPropertyList<TData>()))
                    return node;

                Visit(node.Object);
                writer.Like();
                if (node.Method.Name == MethodCall.EndsWith || node.Method.Name == MethodCall.Contains) writer.LikePrefix();
                Visit(node.Arguments[0]);
                if (node.Method.Name == MethodCall.StartsWith || node.Method.Name == MethodCall.Contains) writer.LikeSuffix();
                return node;
            }

            // IN (...)
            object ev;

            if (node.Method.DeclaringType == typeof (List<string>))
            {
                if (
                    !QueryHelper.IsSpecificMemberExpression(node.Arguments[0], typeof (TData),
                        CacheHelper.TryGetPropertyList<TData>()))
                    return node;


                Visit(node.Arguments[0]);
                ev = QueryHelper.GetValueFromExpression(node.Object);

            }
            else if (node.Method.DeclaringType == typeof (Enumerable))
            {
                if (
                    !QueryHelper.IsSpecificMemberExpression(node.Arguments[1], typeof (TData),
                        CacheHelper.TryGetPropertyList<TData>()))
                    return node;

                Visit(node.Arguments[1]);
                ev = QueryHelper.GetValueFromExpression(node.Arguments[0]);

            }
            else
            {
                return node;
            }

            writer.In();

            // Add each string in the collection to the list of locations to obtain data about.
            var queryStrings = (IList<object>)ev;
            var count = queryStrings.Count;
            writer.OpenBrace();
            for (var i = 0; i < count; i++)
            {
                writer.Parameter(queryStrings.ElementAt(i));

                if (i + 1 < count)
                    writer.Delimiter();
            }
            writer.CloseBrace();

            return node;
        }
    }
}