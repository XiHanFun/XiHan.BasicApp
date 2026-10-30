// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using SqlSugar;

namespace XiHan.BasicApp.Integration.Tests.Infrastructure;

/// <summary>
/// 一个 PostgreSQL 库 public 模式的表结构快照。
/// </summary>
/// <remarks>
/// 表、列（类型、可空、缺省、自增）、约束（PostgreSQL 18 起列的 NOT NULL 也记成约束，已在列定义里，不重复）与不属于约束的索引；按月分表的表（名字以 <c>_yyyyMMdd</c> 结尾）不算，它们由写入时按需建出。
/// 用来比对 InitTables 建的库与基线脚本建的库，也用来把前者渲染成基线脚本。
/// </remarks>
public sealed partial record PostgresSchemaSnapshot(IReadOnlyList<PostgresTable> Tables)
{
    /// <summary>
    /// 读一个库的快照。
    /// </summary>
    public static async Task<PostgresSchemaSnapshot> ReadAsync(string connectionString)
    {
        using var client = new SqlSugarClient(new ConnectionConfig { DbType = SqlSugar.DbType.PostgreSQL, ConnectionString = connectionString, IsAutoCloseConnection = true });
        var columns = await client.Ado.GetDataTableAsync("""
            SELECT c.relname AS table_name, a.attname AS column_name, format_type(a.atttypid, a.atttypmod) AS column_type,
                   a.attnotnull AS not_null, pg_get_expr(d.adbin, d.adrelid) AS column_default, a.attidentity::text AS identity, a.attnum AS position
            FROM pg_attribute a
            JOIN pg_class c ON c.oid = a.attrelid
            JOIN pg_namespace n ON n.oid = c.relnamespace
            LEFT JOIN pg_attrdef d ON d.adrelid = a.attrelid AND d.adnum = a.attnum
            WHERE n.nspname = 'public' AND c.relkind = 'r' AND a.attnum > 0 AND NOT a.attisdropped
            ORDER BY c.relname, a.attnum
            """);
        var constraints = await client.Ado.GetDataTableAsync("""
            SELECT c.relname AS table_name, con.conname AS name, con.contype::text AS kind, pg_get_constraintdef(con.oid) AS definition
            FROM pg_constraint con
            JOIN pg_class c ON c.oid = con.conrelid
            JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE n.nspname = 'public' AND con.contype <> 'n'
            ORDER BY c.relname, con.conname
            """);
        var indexes = await client.Ado.GetDataTableAsync("""
            SELECT i.tablename AS table_name, i.indexname AS name, i.indexdef AS definition
            FROM pg_indexes i
            WHERE i.schemaname = 'public' AND NOT EXISTS (SELECT 1 FROM pg_constraint con WHERE con.conname = i.indexname)
            ORDER BY i.tablename, i.indexname
            """);

        var tables = Rows(columns).Select(static r => (string)r["table_name"]).Distinct(StringComparer.Ordinal)
            .Where(static t => !SplitTable().IsMatch(t))
            .Order(StringComparer.Ordinal)
            .Select(table => new PostgresTable(
                table,
                [.. Rows(columns).Where(r => (string)r["table_name"] == table).Select(static r => new PostgresColumn(
                    (string)r["column_name"],
                    (string)r["column_type"],
                    (bool)r["not_null"],
                    r["column_default"] as string,
                    (string)r["identity"]))],
                [.. Rows(constraints).Where(r => (string)r["table_name"] == table).Select(static r => new PostgresConstraint((string)r["name"], (string)r["kind"], (string)r["definition"]))],
                [.. Rows(indexes).Where(r => (string)r["table_name"] == table).Select(static r => new PostgresIndex((string)r["name"], (string)r["definition"]))]))
            .ToList();
        return new PostgresSchemaSnapshot(tables);
    }

    /// <summary>
    /// 渲染成可重复执行的建表脚本。
    /// </summary>
    /// <remarks>
    /// <c>CREATE TABLE IF NOT EXISTS</c>（列按原顺序，<c>serial</c> 列还原成 <c>serial</c> / <c>bigserial</c>，约束写在表内）与 <c>CREATE INDEX IF NOT EXISTS</c>。
    /// </remarks>
    public string ToSql(string header)
    {
        var sql = new StringBuilder();
        sql.Append(header).Append('\n');
        foreach (var table in Tables)
        {
            sql.Append('\n').Append("CREATE TABLE IF NOT EXISTS \"").Append(table.Name).Append("\" (\n");
            var lines = table.Columns.Select(static c => "    " + c.ToDefinition())
                .Concat(table.Constraints.Select(static c => $"    CONSTRAINT \"{c.Name}\" {c.Definition}"))
                .ToList();
            sql.Append(string.Join(",\n", lines)).Append("\n);\n");
            foreach (var index in table.Indexes)
            {
                sql.Append(IndexHead().Replace(index.Definition, "$1 IF NOT EXISTS ", 1)).Append(";\n");
            }
        }

        return sql.ToString();
    }

    /// <summary>
    /// 列出与另一个快照的差别。
    /// </summary>
    /// <remarks>
    /// 每条一行，前缀 <c>-</c> 是只在这一边的，<c>+</c> 是只在另一边的；没有差别为空。
    /// </remarks>
    public IReadOnlyList<string> Differences(PostgresSchemaSnapshot other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var mine = Lines(this);
        var theirs = Lines(other);
        return [.. mine.Except(theirs).Select(static l => "- " + l).Concat(theirs.Except(mine).Select(static l => "+ " + l)).Order(StringComparer.Ordinal)];
    }

    private static HashSet<string> Lines(PostgresSchemaSnapshot snapshot)
        => snapshot.Tables.SelectMany(static t => t.Columns.Select(c => $"{t.Name}.{c.Name} {c.ToDefinition()}")
            .Concat(t.Constraints.Select(c => $"{t.Name} constraint {c.Name} {c.Definition}"))
            .Concat(t.Indexes.Select(i => $"{t.Name} index {i.Definition}"))
            .Append($"{t.Name} table"))
            .ToHashSet(StringComparer.Ordinal);

    private static IEnumerable<DataRow> Rows(DataTable table) => table.Rows.Cast<DataRow>();

    [GeneratedRegex(@"_\d{8}$", RegexOptions.CultureInvariant)]
    private static partial Regex SplitTable();

    [GeneratedRegex(@"^(CREATE (?:UNIQUE )?INDEX) ", RegexOptions.CultureInvariant)]
    private static partial Regex IndexHead();
}

/// <summary>
/// 一张表。
/// </summary>
public sealed record PostgresTable(string Name, IReadOnlyList<PostgresColumn> Columns, IReadOnlyList<PostgresConstraint> Constraints, IReadOnlyList<PostgresIndex> Indexes);

/// <summary>
/// 一列。
/// </summary>
public sealed partial record PostgresColumn(string Name, string Type, bool NotNull, string? Default, string Identity)
{
    /// <summary>
    /// 建表语句里的列定义。
    /// </summary>
    public string ToDefinition()
    {
        var type = Type;
        var defaultValue = Default;
        if (defaultValue is not null && Serial().IsMatch(defaultValue) && type is "integer" or "bigint" or "smallint")
        {
            type = type switch { "bigint" => "bigserial", "smallint" => "smallserial", _ => "serial" };
            defaultValue = null;
        }

        var definition = $"\"{Name}\" {type}";
        if (Identity is "a" or "d")
        {
            definition += Identity == "a" ? " GENERATED ALWAYS AS IDENTITY" : " GENERATED BY DEFAULT AS IDENTITY";
        }

        if (defaultValue is not null)
        {
            definition += " DEFAULT " + defaultValue;
        }

        return definition + (NotNull ? " NOT NULL" : " NULL");
    }

    [GeneratedRegex(@"^nextval\('[^']+'::regclass\)$", RegexOptions.CultureInvariant)]
    private static partial Regex Serial();
}

/// <summary>
/// 一个约束。
/// </summary>
public sealed record PostgresConstraint(string Name, string Kind, string Definition);

/// <summary>
/// 一个不属于约束的索引。
/// </summary>
public sealed record PostgresIndex(string Name, string Definition);
