// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Domain.Entities;

namespace XiHan.BasicApp.Runtime.Persistence;

/// <summary>
/// 软删除实体的部分唯一索引
/// </summary>
/// <remarks>
/// 按实体上的 <see cref="ActiveUniqueIndexAttribute"/> 以它写明的名字建只约束未删除行的唯一索引，已有同名索引即跳过：
/// PostgreSQL、SQLite、SQL Server 是带条件的索引（<c>WHERE is_deleted = …</c>）；MySQL（8.0.13 起）与 Oracle 没有带条件的索引，每列包成 <c>CASE WHEN is_deleted = 0 THEN 列 END</c> 的函数索引，删除的行整键为空、不参与唯一
/// 表名与列名经 SqlSugar 的方言转义，与它建表时的写法一致
/// 在建表之后调用：开发与测试由 <see cref="RuntimeFeature"/> 跟在框架的 <c>InitTables</c> 之后，正式库与租户独立库由表结构升级器按实体建表之后调用
/// 没写索引名或超出 PostgreSQL 的 63 字符上限即抛出 <see cref="InvalidOperationException"/>（结构测试先行拦下）
/// </remarks>
public static class ActiveUniqueIndexes
{
    /// <summary>
    /// 索引名的长度上限
    /// </summary>
    public const int MaxNameLength = 63;

    /// <summary>
    /// 在客户端当前的连接上补建这些实体的部分唯一索引
    /// </summary>
    public static void Ensure(ISqlSugarClient client, IEnumerable<Type> entityTypes)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(entityTypes);
        foreach (var type in entityTypes)
        {
            var attributes = type.GetCustomAttributes<ActiveUniqueIndexAttribute>(inherit: false).ToArray();
            if (attributes.Length == 0)
            {
                continue;
            }

            var entity = client.EntityMaintenance.GetEntityInfo(type);
            var deleted = ColumnOf(entity, nameof(ISoftDeletable.IsDeleted)).DbColumnName;
            foreach (var attribute in attributes)
            {
                var columns = attribute.Columns.Select(property => ColumnOf(entity, property)).ToArray();
                var name = attribute.Name ?? throw new InvalidOperationException($"[ActiveUniqueIndex] of {type.Name} on ({string.Join(", ", attribute.Columns)}) has no Name.");
                if (name.Length > MaxNameLength)
                {
                    throw new InvalidOperationException($"Index name '{name}' of {type.Name} is longer than {MaxNameLength} characters.");
                }

                var dbType = client.CurrentConnectionConfig.DbType;
                if (dbType is not (DbType.PostgreSQL or DbType.Sqlite) && Exists(client, dbType, name))
                {
                    continue;
                }

                // SqlSugar 只在查询对象上给出方言的转义器；这个查询不执行
                var builder = client.SqlQueryable<object>("SELECT 1").QueryBuilder.Builder;
                client.Ado.ExecuteCommand(SqlOf(builder, dbType, name, entity.DbTableName, columns, deleted));
            }
        }
    }

    private static EntityColumnInfo ColumnOf(EntityInfo entity, string property)
        => entity.Columns.FirstOrDefault(column => column.PropertyName == property)
            ?? throw new InvalidOperationException($"{entity.Type.Name} has no column for property '{property}'.");

    /// <summary>
    /// 同名索引是否已在（没有 <c>IF NOT EXISTS</c> 的库先查目录）
    /// </summary>
    private static bool Exists(ISqlSugarClient client, DbType dbType, string name) => dbType switch
    {
        DbType.SqlServer => client.Ado.GetInt("SELECT COUNT(1) FROM sys.indexes WHERE name = @name", new SugarParameter("@name", name)) > 0,
        DbType.MySql => client.Ado.GetInt("SELECT COUNT(1) FROM information_schema.statistics WHERE table_schema = DATABASE() AND LOWER(index_name) = LOWER(@name)", new SugarParameter("@name", name)) > 0,
        DbType.Oracle => client.Ado.GetInt("SELECT COUNT(1) FROM user_indexes WHERE UPPER(index_name) = UPPER(:name)", new SugarParameter(":name", name)) > 0,
        _ => throw new NotSupportedException($"Partial unique indexes are not supported on {dbType}."),
    };

    private static string SqlOf(ISqlBuilder builder, DbType dbType, string name, string table, EntityColumnInfo[] columns, string deleted)
    {
        var quotedTable = builder.GetTranslationTableName(table);
        var quotedDeleted = builder.GetTranslationColumnName(deleted);
        var quoted = columns.Select(column => builder.GetTranslationColumnName(column.DbColumnName)).ToArray();

        // PostgreSQL 的唯一索引里含空值的键互不冲突；SQL Server 只容一个空值、Oracle 的组合键按非空部分比较，
        // 所以可空列也写进条件：任一可空列为空的行不进索引，与 PostgreSQL 一致
        var active = string.Join(" AND ", columns.Where(static c => c.IsNullable).Select(c => builder.GetTranslationColumnName(c.DbColumnName) + " IS NOT NULL").Prepend($"{quotedDeleted} = 0"));
        return dbType switch
        {
            DbType.PostgreSQL => $"CREATE UNIQUE INDEX IF NOT EXISTS \"{name}\" ON {quotedTable} ({string.Join(", ", quoted)}) WHERE {quotedDeleted} = false",
            DbType.Sqlite => $"CREATE UNIQUE INDEX IF NOT EXISTS \"{name}\" ON {quotedTable} ({string.Join(", ", quoted)}) WHERE {quotedDeleted} = 0",
            DbType.SqlServer => $"CREATE UNIQUE INDEX [{name}] ON {quotedTable} ({string.Join(", ", quoted)}) WHERE {active}",
            DbType.MySql => $"CREATE UNIQUE INDEX `{name}` ON {quotedTable} ({string.Join(", ", quoted.Select(column => $"(CASE WHEN {active} THEN {column} END)"))})",
            DbType.Oracle => $"CREATE UNIQUE INDEX {name} ON {quotedTable} ({string.Join(", ", quoted.Select(column => $"CASE WHEN {active} THEN {column} END"))})",
            _ => throw new NotSupportedException($"Partial unique indexes are not supported on {dbType}."),
        };
    }
}
