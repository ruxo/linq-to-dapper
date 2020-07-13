using System;
using System.Linq.Expressions;
using Dapper.Contrib.Linq2Dapper.Helpers;

namespace Dapper.Contrib.Linq2Dapper.ContextBuilders.SelectQuery
{
    public sealed class SelectBuilder : ExpressionVisitor
    {
        Type queryType;

        public SelectBuilder(Type starterType) {
            queryType = starterType;
        }

        public bool IsDistinct { get; private set; }
        public int? NumberOfTake { get; private set; }
        public int? NumberOfSkip { get; private set; }

        public void Take(int n) {
            NumberOfTake = n;
        }

        public void Skip(int n) {
            if (NumberOfTake != null)
                throw new InvalidOperationException("Skip must be called before take. Take before Skip is not supported.");
            NumberOfSkip = n;
        }

        public void RequireDistinct() {
            IsDistinct = true;
        }

        public TableHelper GetTable() => QueryHelper.GetTableHelper(queryType);

        protected override Expression VisitUnary(UnaryExpression node) {
            queryType = ((LambdaExpression) node.Operand).Body.Type;
            return node;
        }
    }
}