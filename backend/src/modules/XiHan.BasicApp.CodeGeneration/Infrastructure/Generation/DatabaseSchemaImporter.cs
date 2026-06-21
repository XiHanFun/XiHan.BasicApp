#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DatabaseSchemaImporter
// Guid:c0de9e00-0304-4a00-9000-000000000304
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Reflection;
using SqlSugar;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using XiHan.Framework.Data.SqlSugar.Metadata;

namespace XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

/// <summary>
/// 数据库结构导入器（接通框架 IDatabaseMetadataProvider，DbFirst）
/// </summary>
/// <remarks>
/// 仅产出数据库层结构（列名/类型/可空/主键等）。C#/TS 类型与表单语义由引擎结合
/// <see cref="ITypeMappingProvider"/> 二次填充。多数据源连接（SysCodeGenDataSource）通过
/// connectionConfigId 透传给框架元数据提供器。
/// <para>
/// 大小写还原：部分数据库（如 MySQL lower_case_table_names=1）返回的表名/列名为全小写，
/// 驼峰信息丢失。导入器会把扫描到的名称对照已注册的 <c>[SugarTable]</c> 实体还原为真实大小写
/// （如 syscodegendatasource → SysCodeGenDataSource），从而让生成的类名/属性名正确；
/// 未注册的外部表保持原样。
/// </para>
/// </remarks>
public sealed class DatabaseSchemaImporter(IDatabaseMetadataProvider metadataProvider) : IDatabaseSchemaImporter
{
    private static readonly Lazy<EntityNameCatalog> CatalogAccessor = new(EntityNameCatalog.Build);

    private readonly IDatabaseMetadataProvider _metadataProvider = metadataProvider;

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ListTablesAsync(string? connectionConfigId = null, CancellationToken cancellationToken = default)
    {
        var tables = await _metadataProvider.GetTablesAsync(connectionConfigId, cancellationToken);
        var catalog = CatalogAccessor.Value;
        return [.. tables.Select(table => catalog.ResolveTable(table.TableName))];
    }

    /// <inheritdoc />
    public async Task<TableSchema?> ImportTableAsync(string tableName, string? connectionConfigId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        var table = await _metadataProvider.GetTableAsync(tableName, connectionConfigId, cancellationToken);
        if (table is null)
        {
            return null;
        }

        var catalog = CatalogAccessor.Value;
        // 优先用调用方传入的（已还原）表名，再兜底用元数据返回名做目录匹配
        var realTableName = catalog.ResolveTable(string.IsNullOrWhiteSpace(tableName) ? table.TableName : tableName);

        var columns = table.Columns.Select(column => MapColumn(column, realTableName, catalog)).ToList();
        return new TableSchema
        {
            TableName = realTableName,
            TableComment = table.TableDescription,
            PrimaryKeyColumn = columns.FirstOrDefault(column => column.IsPrimaryKey)?.ColumnName,
            Columns = columns
        };
    }

    private static ColumnSchema MapColumn(DatabaseColumnMetadata column, string realTableName, EntityNameCatalog catalog) => new()
    {
        ColumnName = catalog.ResolveColumn(realTableName, column.ColumnName),
        ColumnComment = column.Description,
        DbType = column.DataType,
        IsPrimaryKey = column.IsPrimaryKey,
        IsIdentity = column.IsIdentity,
        IsNullable = column.IsNullable,
        IsRequired = !column.IsNullable && !column.IsIdentity,
        Length = column.Length,
        DecimalDigits = column.Scale
    };

    /// <summary>
    /// 已注册 SugarTable 实体的名称目录：小写名 → 真实大小写名（表 + 列）。
    /// 反射一次、进程内缓存；用于还原 DB 返回的全小写名。
    /// </summary>
    private sealed class EntityNameCatalog
    {
        private readonly IReadOnlyDictionary<string, string> _tables;
        private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _columns;

        private EntityNameCatalog(
            IReadOnlyDictionary<string, string> tables,
            IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> columns)
        {
            _tables = tables;
            _columns = columns;
        }

        /// <summary>
        /// 还原表名真实大小写（未注册则原样返回）
        /// </summary>
        public string ResolveTable(string dbTableName)
        {
            if (string.IsNullOrWhiteSpace(dbTableName))
            {
                return dbTableName;
            }

            return _tables.TryGetValue(dbTableName.ToLowerInvariant(), out var real) ? real : dbTableName;
        }

        /// <summary>
        /// 还原列名真实大小写（未注册则原样返回）
        /// </summary>
        public string ResolveColumn(string realTableName, string dbColumnName)
        {
            if (string.IsNullOrWhiteSpace(realTableName) || string.IsNullOrWhiteSpace(dbColumnName))
            {
                return dbColumnName;
            }

            return _columns.TryGetValue(realTableName.ToLowerInvariant(), out var map)
                && map.TryGetValue(dbColumnName.ToLowerInvariant(), out var real)
                ? real
                : dbColumnName;
        }

        /// <summary>
        /// 反射构建目录（扫描 XiHan* 程序集中带 [SugarTable] 的实体）
        /// </summary>
        public static EntityNameCatalog Build()
        {
            var tables = new Dictionary<string, string>(StringComparer.Ordinal);
            var columns = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic)
                {
                    continue;
                }

                var assemblyName = assembly.GetName().Name;
                if (assemblyName is null || !assemblyName.StartsWith("XiHan", StringComparison.Ordinal))
                {
                    continue;
                }

                Type?[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types;
                }
                catch
                {
                    continue;
                }

                foreach (var type in types)
                {
                    if (type is null || !type.IsClass)
                    {
                        continue;
                    }

                    var tableAttribute = type.GetCustomAttribute<SugarTable>();
                    if (tableAttribute is null)
                    {
                        continue;
                    }

                    var realTable = string.IsNullOrWhiteSpace(tableAttribute.TableName) ? type.Name : tableAttribute.TableName;
                    var lowerTable = realTable.ToLowerInvariant();
                    tables[lowerTable] = realTable;

                    var columnMap = new Dictionary<string, string>(StringComparer.Ordinal);
                    foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        var columnAttribute = property.GetCustomAttribute<SugarColumn>();
                        if (columnAttribute is not null && columnAttribute.IsIgnore)
                        {
                            continue;
                        }

                        var realColumn = columnAttribute is not null && !string.IsNullOrWhiteSpace(columnAttribute.ColumnName)
                            ? columnAttribute.ColumnName
                            : property.Name;
                        columnMap[realColumn.ToLowerInvariant()] = realColumn;
                    }

                    columns[lowerTable] = columnMap;
                }
            }

            return new EntityNameCatalog(tables, columns);
        }
    }
}
