using System;
using System.Data;
using System.Linq;
using Dapper.Contrib.Linq2Dapper.Test.POCO;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Xunit;
using Dapper.Contrib.Linq2Dapper.Extensions;

namespace Dapper.Contrib.Linq2Dapper.Test
{
    public sealed class Linq2DapperShould
    {
        [Fact]
        public void SelectAllRecords() {
            using var cn = CreateNewDatabase();

            var results = cn.Query<DataType>().ToArray();

            results.Length.Should().Be(5);
        }

        [Fact]
        public void SelectAllRecords2() {
            using var cntx = new DataContext(CreateNewDatabase());

            var results = (from x in cntx.DataTypes
                           where x.Name == "text"
                           select x).ToArray();

            results.Length.Should().Be(1);
            var item = results[0];

            item.DataTypeId.Should().Be(1);
            item.Name.Should().Be("text");
            item.IsActive.Should().BeTrue();
            item.Created.Should().Be(new DateTime(2020, 7, 15, 16, 0, 0));
        }

        [Fact]
        public void SelectAllRecordsWithSpecificColumn() {
            using var cntx = new DataContext(CreateNewDatabase());

            var results = (from x in cntx.DataTypes
                           where x.Name == "text"
                           select x.Name).ToArray();

            results.Length.Should().Be(1);
            var item = results[0];

            item.Should().Be("text");
        }

        [Fact]
        public void JoinWhere() {
            using var cntx = new DataContext(CreateNewDatabase());

            var results = (from d in cntx.DataTypes
                           join a in cntx.Fields on d.DataTypeId equals a.DataTypeId
                           where a.DataTypeId == 1
                           select d).ToArray();
            results.Length.Should().Be(3);
        }

        [Fact]
        public void JoinWhereProjection()
        {
            var cntx = new DataContext(CreateNewDatabase());

            var results = (from d in cntx.DataTypes
                           join a in cntx.Fields on d.DataTypeId equals a.DataTypeId
                           where a.DataTypeId == 1
                           select a).ToArray();

            results.Length.Should().Be(3);
        }

        [Fact]
        public void MultiJoinWhere()
        {
            var cntx = new DataContext(CreateNewDatabase());

            var results = (from d in cntx.DataTypes
                           join a in cntx.Fields on d.DataTypeId equals a.DataTypeId
                           join b in cntx.Documents on a.FieldId equals b.FieldId
                           where a.DataTypeId == 1 && b.FieldId == 1
                           select d).ToArray();

            results.Length.Should().Be(1);
        }

        [Fact]
        public void WhereContains() {
            using var cn = CreateNewDatabase();
            var r = (from a in cn.Query<DataType>()
                     where new[] { "text", "int", "random" }.Contains(a.Name)
                     orderby a.Name
                     select a).ToArray();

            r.Length.Should().Be(2);
        }

        [Fact]
        public void WhereEquals() {
            using var cn = CreateNewDatabase();
            foreach (var item in new[] { "text", "int" })
            {
                var results = cn.Query<DataType>(x => x.Name == item).ToArray();
                results.Length.Should().Be(1);
            }
        }

                [Fact]
        public void Top1Statement() {
            using var cn = CreateNewDatabase();
            var results = cn.Query<DataType>().FirstOrDefault(m => !m.IsActive);
            results.Should().NotBeNull();
        }


        [Fact]
        public void Top10A() {
            using var cn = CreateNewDatabase();
            var results = cn.Query<DataType>().Take(5).ToArray();
            results.Length.Should().Be(5);
        }

        [Fact]
        public void Top10B()
        {
            const int TopCount = 10;
            using var cn = CreateNewDatabase();
            var results = cn.Query<DataType>().Take(TopCount).ToArray();
            results.Length.Should().Be(5);
        }

        [Fact]
        public void Top10C() {
            using var cn = CreateNewDatabase();
            for (var topCount = 1; topCount < 5; topCount++)
            {
                var results = cn.Query<DataType>().Take(topCount).ToArray();
                results.Length.Should().Be(topCount);
            }
        }

        [Fact]
        public void Distinct() {
            using var cn = CreateNewDatabase();
            var results = cn.Query<DataType>().Distinct().ToArray();
            results.Length.Should().Be(5);
        }

        [Fact]
        public void OrderBy() {
            using var cn = CreateNewDatabase();
            var results = cn.Query<DataType>().OrderBy(m => m.Name).ToArray();
            results.Length.Should().Be(5);
        }

        [Fact]
        public void OrderByAndThenBy() {
            using var cn = CreateNewDatabase();
            var results = cn.Query<DataType>().OrderBy(m => m.Name).ThenBy(m => m.DataTypeId).ToArray();
            results.Length.Should().Be(5);
        }

        [Fact]
        public void OrderByWithTop() {
            using var cn = CreateNewDatabase();
            var results = cn.Query<DataType>().OrderBy(m => m.Name).ThenBy(m => m.DataTypeId).Take(5).ToArray();
            results.Length.Should().Be(5);
        }

        [Fact]
        public void WhereSimpleEqual() {
            using var cn = CreateNewDatabase();
            var results = cn.Query<DataType>(m => m.Name == "text").ToArray();
            results.Length.Should().Be(1);
            results[0].Name.Should().Be("text");
        }

        [Fact]
        public void WhereSimpleEqualWithoutParameter() {
            using var cn = CreateNewDatabase();
            // ReSharper disable once EqualExpressionComparison
            var results = cn.Query<DataType>(m => m.DataTypeId == m.DataTypeId).ToArray();
            results.Length.Should().Be(5);
        }

        [Fact]
        public void WhereIsNullOrEmpty() {
            using var cn = CreateNewDatabase();
            var results = cn.Query<DataType>(m => String.IsNullOrEmpty(m.Name)).ToArray();
            results.Length.Should().Be(0);
        }

        [Fact]
        public void WhereNotIsNullOrEmpty() {
            using var cn = CreateNewDatabase();
            var results = cn.Query<DataType>(m => !String.IsNullOrEmpty(m.Name)).ToArray();
            results.Length.Should().Be(5);
        }

        [Fact]
        public void WhereHasValue() {
            using var cn = CreateNewDatabase();
            var results = cn.Query<DataType>(m => m.Created.HasValue).ToArray();
            results.Length.Should().Be(5);
        }

        [Fact]
        public void WhereNotHasValue() {
            using var cn = CreateNewDatabase();
            var results = cn.Query<DataType>(m => !m.Created.HasValue).ToArray();
            results.Length.Should().Be(0);
        }

        [Fact]
        public void WhereLike() {
            using var cn = CreateNewDatabase();
            var results = cn.Query<DataType>(m => m.Name.Contains("te")).ToArray();
            results.Length.Should().Be(2);
        }

        [Fact]
        public void WhereEndsWith() {
            using var cn = CreateNewDatabase();
            var results = cn.Query<DataType>(m => m.Name.StartsWith("te")).ToArray();
            results.Length.Should().Be(1);
        }

        [Fact]
        public void WhereStartsWith() {
            using var cn = CreateNewDatabase();
            var results = cn.Query<DataType>(m => m.Name.EndsWith("xt")).ToArray();
            results.Length.Should().Be(1);
        }

        [Fact]
        public void WhereEndsWithAndComparison() {
            using var cn = CreateNewDatabase();
            var results = cn.Query<DataType>(m => m.Name.StartsWith("te", StringComparison.OrdinalIgnoreCase)).ToArray();
            results.Length.Should().Be(1);
        }

        [Fact]
        public void WhereStartsWithAndComparison() {
            using var cn = CreateNewDatabase();
            var results = cn.Query<DataType>(m => m.Name.EndsWith("xt", StringComparison.OrdinalIgnoreCase)).ToArray();
            results.Length.Should().Be(1);
        }

        [Fact]
        public void WhereNotLike() {
            using var cn = CreateNewDatabase();
            var results = cn.Query<DataType>(m => !m.Name.Contains("te")).ToArray();
            results.Length.Should().Be(3);
        }

        [Fact]
        public void WhereNotEndsWith() {
            using var cn = CreateNewDatabase();
            var results = cn.Query<DataType>(m => !m.Name.StartsWith("te")).ToArray();
            results.Length.Should().Be(4);
        }

        [Fact]
        public void WhereNotStartsWith() {
            using var cn = CreateNewDatabase();
            var results = cn.Query<DataType>(m => !m.Name.EndsWith("xt")).ToArray();
            results.Length.Should().Be(4);
        }

        [Fact]
        public void TwoPartWhereAnd() {
            using var cn = CreateNewDatabase();
            var results = cn.Query<DataType>(m => m.Name == "text" && m.Created.HasValue).ToArray();
            results.Length.Should().Be(1);
        }

        [Fact]
        public void TwoPartWhereOr() {
            using var cn = CreateNewDatabase();
            var results = cn.Query<DataType>(m => m.Name == "text" || m.Name == "int").ToArray();
            results.Length.Should().Be(2);
        }

        [Fact]
        public void MultiPartWhereAndOr() {
            using var cn = CreateNewDatabase();
            var results = cn.Query<DataType>(m => m.Name == "text" && (m.Name == "int" || m.Created.HasValue)).ToArray();
            results.Length.Should().Be(1);
        }

        [Fact]
        public void MultiPartWhereAndOr2() {
            using var cn = CreateNewDatabase();
            var results = cn.Query<DataType>(m => m.Name != "text" && (m.Name == "int" || m.Created.HasValue)).ToArray();
            results.Length.Should().Be(4);
        }

        [Fact]
        public void Single() {
            using var cn = CreateNewDatabase();
            var result = cn.Query<DataType>().Single(m => m.Name == "text");
            result.Name.Should().Be("text");
        }

        static SqliteConnection CreateNewDatabase() {
            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            connection.Execute(TestDatabase);

            return connection;
        }

        const string TestDatabase =
@"CREATE TABLE DataType (DataTypeId INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT(100) NOT NULL, IsActive INTEGER NOT NULL, Created TEXT NOT NULL);
CREATE TABLE Field (FieldId INTEGER PRIMARY KEY AUTOINCREMENT, DataTypeId INTEGER NOT NULL, Name TEXT(100) NOT NULL, Created TEXT NOT NULL);
CREATE TABLE Document (DocumentId INTEGER PRIMARY KEY AUTOINCREMENT, FieldId INTEGER NOT NULL, Name TEXT(100) NOT NULL, Created TEXT NOT NULL);
INSERT INTO DataType (Name, IsActive, Created) VALUES ('text', 1, '2020-07-15 16:00:00.0000000');
INSERT INTO DataType (Name, IsActive, Created) VALUES ('int', 0, '2020-07-15 16:00:01.0000000');
INSERT INTO DataType (Name, IsActive, Created) VALUES ('number', 0, '2020-07-15 16:00:02.0000000');
INSERT INTO DataType (Name, IsActive, Created) VALUES ('date', 1, '2020-07-15 16:00:03.0000000');
INSERT INTO DataType (Name, IsActive, Created) VALUES ('checkbox', 0, '2020-07-15 16:00:04.0000000');
INSERT INTO Field (DataTypeId, Name, Created) VALUES (1, 'TextField1', '2017-02-27 11:43:22.390');
INSERT INTO Field (DataTypeId, Name, Created) VALUES (1, 'TextField2', '2017-02-27 11:43:29.900');
INSERT INTO Field (DataTypeId, Name, Created) VALUES (1, 'TextField3', '2017-02-27 11:43:38.510');
INSERT INTO Field (DataTypeId, Name, Created) VALUES (4, 'TextField4', '2017-02-27 11:43:53.157');
INSERT INTO Field (DataTypeId, Name, Created) VALUES (4, 'TextField5', '2017-02-27 11:44:02.367');
INSERT INTO Document (FieldId, Name, Created) VALUES (1, 'Document1', '2020-07-15 16:01:00')
";
    }

    public sealed class DataContext : IDisposable
    {
        readonly IDbConnection connection;

        Linq2Dapper<DataType>? dataTypes;
        public Linq2Dapper<DataType> DataTypes => dataTypes ??= CreateObject<DataType>();

        Linq2Dapper<Field>? fields;
        public Linq2Dapper<Field> Fields => fields ??= CreateObject<Field>();

        Linq2Dapper<Document>? documents;
        public Linq2Dapper<Document> Documents => documents ??= CreateObject<Document>();

        public DataContext(IDbConnection connection)
        {
            this.connection = connection;
        }

        Linq2Dapper<T> CreateObject<T>() => new Linq2Dapper<T>(connection);

        public void Dispose()
        {
            connection.Dispose();
        }
    }
}