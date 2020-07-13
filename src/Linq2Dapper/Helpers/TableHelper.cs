using System.Collections.Immutable;

namespace Dapper.Contrib.Linq2Dapper.Helpers
{
    public sealed class TableHelper
    {
        public string Name { get; set; } = string.Empty;
        public ImmutableDictionary<string, string> Columns { get; set; } = ImmutableDictionary<string, string>.Empty;
        public string Identifier { get; set; } = string.Empty;
    }
}
