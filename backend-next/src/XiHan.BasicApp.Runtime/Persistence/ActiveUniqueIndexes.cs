// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Domain.Entities;

namespace XiHan.BasicApp.Runtime.Persistence;

/// <summary>
/// 软删除实体的部分唯一索引。
/// </summary>
/// <remarks>
/// 按实体上的 <see cref="ActiveUniqueIndexAttribute"/> 建 <c>ux_&lt;表&gt;_&lt;列&gt;… WHERE is_deleted = false</c>，已有即跳过。
/// 在建表之后调用：开发与测试由 <see cref="RuntimeFeature"/> 跟在框架的 <c>InitTables</c> 之后，租户独立库由 <c>TenantSchemas</c> 初始化时调用，生产库随基线脚本建出。
/// 只支持 PostgreSQL 与 SQLite；索引名超出 PostgreSQL 的 63 字符上限即抛出 <see cref="InvalidOperationException"/>（结构测试先行拦下）。
/// </remarks>
public static class ActiveUniqueIndexes
{
    /// <summary>
    /// 索引名的长度上限。
    /// </summary>
    public const int MaxNameLength = 63;

    /// <summary>
    /// 索引名。
    /// </summary>
    public static string NameOf(string table, IEnumerable<string> columns)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(table);
        ArgumentNullException.ThrowIfNull(columns);
        return $"ux_{table}_{string.Join('_', columns)}";
    }

    /// <summary>
    /// 在客户端当前的连接上补建这些实体的部分唯一索引。
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
            var deleted = ColumnOf(entity, nameof(ISoftDeletable.IsDeleted));
            foreach (var attribute in attributes)
            {
                var columns = attribute.Columns.Select(property => ColumnOf(entity, property)).ToArray();
                var name = attribute.Name ?? NameOf(entity.DbTableName, columns);
                if (name.Length > MaxNameLength)
                {
                    throw new InvalidOperationException($"Index name '{name}' of {type.Name} is longer than {MaxNameLength} characters.");
                }

                client.Ado.ExecuteCommand(SqlOf(client.CurrentConnectionConfig.DbType, name, entity.DbTableName, columns, deleted));
            }
        }
    }

    private static string ColumnOf(EntityInfo entity, string property)
        => entity.Columns.FirstOrDefault(column => column.PropertyName == property)?.DbColumnName
            ?? throw new InvalidOperationException($"{entity.Type.Name} has no column for property '{property}'.");

    private static string SqlOf(DbType dbType, string name, string table, string[] columns, string deleted)
    {
        var list = string.Join(", ", columns.Select(static column => $"\"{column}\""));
        return dbType switch
        {
            DbType.PostgreSQL => $"CREATE UNIQUE INDEX IF NOT EXISTS \"{name}\" ON \"{table}\" ({list}) WHERE \"{deleted}\" = false",
            DbType.Sqlite => $"CREATE UNIQUE INDEX IF NOT EXISTS \"{name}\" ON \"{table}\" ({list}) WHERE \"{deleted}\" = 0",
            _ => throw new NotSupportedException($"Partial unique indexes are created on PostgreSQL and SQLite only, not on {dbType}."),
        };
    }
}
