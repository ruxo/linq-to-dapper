using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Dapper.Contrib.Linq2Dapper.Helpers
{
    static class CacheHelper
    {
        internal static int Size => _typeList.Count;

        static readonly ConcurrentDictionary<Type, TableHelper> _typeList = new ConcurrentDictionary<Type, TableHelper>();

        internal static bool HasCache<T>() => HasCache(typeof (T));

        internal static bool HasCache(Type type) => _typeList.TryGetValue(type, out TableHelper _);

        internal static bool TryAddTable<T>(TableHelper table) => TryAddTable(typeof(T), table);

        internal static bool TryAddTable(Type type, TableHelper table) => _typeList.TryAdd(type, table);

        internal static TableHelper? TryGetTable<T>() => TryGetTable(typeof(T));
        internal static TableHelper? TryGetTable(Type type) => _typeList.TryGetValue(type, out var table) ? table : null;

        internal static string? TryGetIdentifier<T>() => TryGetIdentifier(typeof(T));

        internal static string? TryGetIdentifier(Type type) => TryGetTable(type)?.Identifier;

        internal static ImmutableDictionary<string, string>? TryGetPropertyList<T>() => TryGetPropertyList(typeof(T));

        internal static ImmutableDictionary<string, string>? TryGetPropertyList(Type type) => TryGetTable(type)?.Columns;

        internal static string? TryGetTableName<T>() => TryGetTableName(typeof(T));

        internal static string? TryGetTableName(Type type) => TryGetTable(type)?.Name;
    }
}
