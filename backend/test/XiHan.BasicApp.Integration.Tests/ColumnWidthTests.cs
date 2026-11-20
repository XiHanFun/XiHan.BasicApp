// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证字符串列与索引键在各数据库上不超长
/// </summary>
/// <remarks>
/// 按建表回调施加之后的映射，逐个算全部已登记实体（含框架 Provider 的表）：
/// 有长度的字符串列不超过 Oracle <c>nvarchar2</c> 的 2000 个字符，更长的用 <c>[LongText]</c>；
/// 索引键不超过 MySQL（InnoDB、utf8mb4，每字符 4 字节）的 3072 字节与 SQL Server 非聚集索引的 1700 字节（字符串是 nvarchar，每字符 2 字节），长文本列不能进索引
/// 索引含 SqlSugar 的索引（实体的 <c>[SugarIndex]</c> 与框架 <c>[EntityIndex]</c> 转成的）与软删除实体的部分唯一索引
/// </remarks>
public sealed class ColumnWidthTests : IDisposable
{
    private const int OracleTextLimit = 2000;
    private const int MySqlLimit = 3072;
    private const int SqlServerLimit = 1700;

    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task StringColumns_AndIndexKeys_FitEveryDatabase()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, DatabaseProviders.Sqlite);
        await using var scope = server.Services.CreateAsyncScope();
        var client = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var offenders = new List<string>();
        foreach (var type in server.Services.GetRequiredService<EntityTypeCatalog>().All)
        {
            var entity = client.EntityMaintenance.GetEntityInfo(type);
            offenders.AddRange(entity.Columns
                .Where(static c => !c.IsIgnore && Underlying(c) == typeof(string) && c.Length > OracleTextLimit)
                .Select(c => $"{entity.DbTableName}.{c.DbColumnName}: length {c.Length} is over Oracle's {OracleTextLimit}, use [LongText]"));

            var indexes = (entity.Indexs ?? []).Select(static i => (i.IndexName, Fields: (IReadOnlyList<string>)[.. i.IndexFields.Keys]))
                .Concat(EntityIndexes.Resolve(type, entity.DbTableName).Where(static i => i.ExcludesDeleted).Select(static i => (IndexName: i.Name, Fields: i.Properties)));
            foreach (var (name, fields) in indexes)
            {
                var columns = fields.Select(field => entity.Columns.First(c => c.PropertyName == field || string.Equals(c.DbColumnName, field, StringComparison.OrdinalIgnoreCase))).ToList();
                if (columns.FirstOrDefault(IsLongText) is { } longText)
                {
                    offenders.Add($"{entity.DbTableName}.{name}: long text column {longText.DbColumnName} cannot be indexed");
                    continue;
                }

                var mysql = columns.Sum(MySqlBytes);
                var sqlServer = columns.Sum(SqlServerBytes);
                if (mysql > MySqlLimit || sqlServer > SqlServerLimit)
                {
                    offenders.Add($"{entity.DbTableName}.{name}: ({string.Join(", ", columns.Select(static c => $"{c.DbColumnName}:{c.Length}"))}) MySQL {mysql} bytes, SQL Server {sqlServer} bytes");
                }
            }
        }

        Assert.True(offenders.Count == 0, "Columns or index keys too wide:" + Environment.NewLine + string.Join(Environment.NewLine, offenders.Order(StringComparer.Ordinal)));
    }

    private static bool IsLongText(EntityColumnInfo column)
        => Underlying(column) == typeof(string) && (column.Length <= 0 || column.Length > OracleTextLimit);

    private static int MySqlBytes(EntityColumnInfo column) => Underlying(column) switch
    {
        var t when t == typeof(string) => column.Length * 4,
        var t when t == typeof(Guid) => 36 * 4,
        var t when t == typeof(DateTimeOffset) || t == typeof(DateTime) => 8,
        var t when t == typeof(bool) || t == typeof(byte) => 1,
        var t when t == typeof(short) => 2,
        var t when t == typeof(int) || t.IsEnum => 4,
        _ => 8,
    };

    private static int SqlServerBytes(EntityColumnInfo column) => Underlying(column) switch
    {
        var t when t == typeof(string) => column.Length * 2,
        var t when t == typeof(Guid) => 16,
        var t when t == typeof(DateTimeOffset) => 10,
        var t when t == typeof(DateTime) => 8,
        var t when t == typeof(bool) || t == typeof(byte) => 1,
        var t when t == typeof(short) => 2,
        var t when t == typeof(int) || t.IsEnum => 4,
        _ => 8,
    };

    private static Type Underlying(EntityColumnInfo column)
    {
        var type = column.PropertyInfo.PropertyType;
        return Nullable.GetUnderlyingType(type) ?? type;
    }
}
