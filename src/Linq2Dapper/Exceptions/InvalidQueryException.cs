using System;

namespace Dapper.Contrib.Linq2Dapper.Exceptions
{
    public class InvalidQueryException : Exception
    {
        public InvalidQueryException(string sql) : base(PrefixMessage(sql)) {}
        public InvalidQueryException(string sql, Exception ex) : base(PrefixMessage(sql), ex){}

        static string PrefixMessage(string sql) => $"The client query is invalid: {sql}";
    }
}
