using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Dapper.Contrib.Linq2Dapper.ContextBuilders.SelectQuery;
using Dapper.Contrib.Linq2Dapper.Helpers;

namespace Dapper.Contrib.Linq2Dapper.Writers
{
    /// <summary>
    /// Microsoft SQL SQL writer
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    sealed class MsSqlWriter<TData>
    {
        readonly StringBuilder _joinTable;
        readonly StringBuilder _whereClause;
        readonly StringBuilder _orderBy;

        int _nextParameter;

        string GetParameter() => $"ld__{_nextParameter += 1}";

        internal bool NotOperater;

        internal DynamicParameters Parameters { get; }

        internal MsSqlWriter()
        {
            Parameters = new DynamicParameters();
            _joinTable = new StringBuilder();
            _whereClause = new StringBuilder();
            _orderBy = new StringBuilder();
        }

        public string SelectStatement(SelectBuilder selectContext)
        {
            var primaryTable = QueryHelper.GetTableHelper(typeof(TData));
            var selectTable = selectContext.GetTable();

            var sb = new StringBuilder();

            sb.Append("SELECT ");

            if (selectContext.NumberOfTake != null)
                sb.Append("TOP(" + selectContext.NumberOfTake + ") ");

            if (selectContext.IsDistinct)
                sb.Append("DISTINCT ");

            sb.AppendJoin(',', selectTable.Columns.Values.Select(column => $"{selectTable.Identifier}.[{column}]"));

            sb.Append($"FROM [{primaryTable.Name}] {primaryTable.Identifier}");
            sb.Append(WriteClause());

            return sb.ToString();
        }

        string WriteClause()
        {
            var clause = string.Empty;

            // JOIN
            if (!string.IsNullOrEmpty(_joinTable.ToString()))
                clause += _joinTable;

            // WHERE
            if (!string.IsNullOrEmpty(_whereClause.ToString()))
                clause += " WHERE " + _whereClause;

            //ORDER BY
            if (!string.IsNullOrEmpty(_orderBy.ToString()))
                clause += " ORDER BY " + _orderBy;

            return clause;
        }

        internal void WriteOrder(string name, bool descending)
        {
            var order = new StringBuilder();
            order.Append(name);
            if (descending) order.Append(" DESC");
            if (!string.IsNullOrEmpty(_orderBy.ToString())) order.Append(", ");
            _orderBy.Insert(0, order);
        }

        internal void WriteJoin(string joinToTableName, string joinToTableIdentifier, string primaryJoinColumn, string secondaryJoinColumn)
        {
            _joinTable.Append($" JOIN [{joinToTableName}] {joinToTableIdentifier} ON {primaryJoinColumn} = {secondaryJoinColumn}");
        }

        internal void Write(object value)
        {
            _whereClause.Append(value);
        }

        internal void Parameter(object val)
        {
            if (val == null)
            {
                Write("NULL");
                return;
            }

            var param = GetParameter();
            Parameters.Add(param, val);

            Write("@" + param);
        }

        internal void AliasName(string aliasName)
        {
            Write(aliasName);
        }

        internal void ColumnName(string columnName)
        {
            Write(columnName);
        }

        internal void IsNull()
        {
            Write(" IS");
            if (!NotOperater)
                Write(" NOT");
            Write(" NULL");
            NotOperater = false;
        }

        internal void IsNullFunction()
        {
            Write("ISNULL");
        }

        internal void Like()
        {
            if (NotOperater)
                Write(" NOT");
            Write(" LIKE ");
            NotOperater = false;
        }

        internal void In()
        {
            if (NotOperater)
                Write(" NOT");
            Write(" IN ");
            NotOperater = false;
        }

        internal void Operator()
        {
            Write(QueryHelper.GetOperator((NotOperater) ? ExpressionType.NotEqual : ExpressionType.Equal));
            NotOperater = false;
        }

        internal void Boolean(bool op)
        {
            Write((op ? " <> " : " = ") + "0");
        }

        internal void OpenBrace()
        {
            Write("(");
        }

        internal void CloseBrace()
        {
            Write(")");
        }

        internal void WhiteSpace()
        {
            Write(" ");
        }

        internal void Delimiter()
        {
            Write(", ");
        }

        internal void LikePrefix()
        {
            Write("'%' + ");
        }

        internal void LikeSuffix()
        {
            Write("+ '%'");
        }

        internal void EmptyString()
        {
            Write("''");
        }
    }
}
