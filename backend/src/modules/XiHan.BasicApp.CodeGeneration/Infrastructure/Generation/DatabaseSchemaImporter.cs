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
using XiHan.Framework.Domain.Entities.Abstracts;

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
/// 驼峰信息丢失。导入器把扫描到的名称对照已注册的 <c>[SugarTable]</c> 实体还原为真实大小写
/// （如 syscodegendatasource → SysCodeGenDataSource）；未注册的外部表保持原样。
/// </para>
/// <para>
/// 分表折叠：带 <c>[SplitTable]</c> 的日志类实体物理表按时间分片（如 sysdifflog_20260601）。
/// 列表时把同一实体的所有分片折叠为基础逻辑名（SysDiffLog）并去重；导入时若传入基础名（无物理表），
/// 自动扫描最近一个分片取列结构。
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

        // 还原真实大小写 + 把分表分片折叠为基础逻辑名，并按出现顺序去重
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();
        foreach (var table in tables)
        {
            var logicalName = catalog.ResolveLogical(table.TableName);
            if (seen.Add(logicalName))
            {
                result.Add(logicalName);
            }
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<TableSchema?> ImportTableAsync(string tableName, string? connectionConfigId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        var catalog = CatalogAccessor.Value;
        var realTableName = catalog.ResolveTable(tableName);

        var table = await _metadataProvider.GetTableAsync(tableName, connectionConfigId, cancellationToken);

        // 分表基础名无物理表：扫描最近一个分片取列结构
        if (table is null && catalog.IsSplitBase(realTableName))
        {
            var shard = await FindLatestShardAsync(realTableName, connectionConfigId, catalog, cancellationToken);
            if (shard is not null)
            {
                table = await _metadataProvider.GetTableAsync(shard, connectionConfigId, cancellationToken);
            }
        }

        if (table is null)
        {
            return null;
        }

        var columns = table.Columns.Select(column => MapColumn(column, realTableName, catalog)).ToList();
        return new TableSchema
        {
            TableName = realTableName,
            TableComment = table.TableDescription,
            PrimaryKeyColumn = columns.FirstOrDefault(column => column.IsPrimaryKey)?.ColumnName,
            Columns = columns
        };
    }

    /// <summary>
    /// 查找指定分表基础名下最近的一个物理分片
    /// </summary>
    private async Task<string?> FindLatestShardAsync(string realBaseName, string? connectionConfigId, EntityNameCatalog catalog, CancellationToken cancellationToken)
    {
        var tables = await _metadataProvider.GetTablesAsync(connectionConfigId, cancellationToken);
        return tables
            .Select(table => table.TableName)
            .Where(name => catalog.TryResolveSplitShard(name, out var baseName)
                && string.Equals(baseName, realBaseName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(name => name, StringComparer.Ordinal)
            .FirstOrDefault();
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
    /// 已注册 SugarTable 实体的名称目录：小写名 → 真实大小写名（表 + 列），并标记分表基础名。
    /// 反射一次、进程内缓存；用于还原 DB 返回的全小写名与折叠分表分片。
    /// </summary>
    private sealed class EntityNameCatalog
    {
        private readonly IReadOnlyDictionary<string, string> _tables;
        private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _columns;
        private readonly IReadOnlySet<string> _splitBases;

        private EntityNameCatalog(
            IReadOnlyDictionary<string, string> tables,
            IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> columns,
            IReadOnlySet<string> splitBases)
        {
            _tables = tables;
            _columns = columns;
            _splitBases = splitBases;
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
        /// 还原为逻辑表名：精确匹配优先；分表分片折叠为基础逻辑名；否则原样
        /// </summary>
        public string ResolveLogical(string dbTableName)
        {
            if (string.IsNullOrWhiteSpace(dbTableName))
            {
                return dbTableName;
            }

            if (_tables.TryGetValue(dbTableName.ToLowerInvariant(), out var exact))
            {
                return exact;
            }

            return TryResolveSplitShard(dbTableName, out var baseName) ? baseName : dbTableName;
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
        /// 判断是否为分表基础名
        /// </summary>
        public bool IsSplitBase(string tableName)
        {
            return !string.IsNullOrWhiteSpace(tableName) && _splitBases.Contains(tableName.ToLowerInvariant());
        }

        /// <summary>
        /// 判断是否为某分表实体的物理分片（如 sysdifflog_20260601 → SysDiffLog）
        /// </summary>
        public bool TryResolveSplitShard(string dbTableName, out string baseRealName)
        {
            baseRealName = string.Empty;
            if (string.IsNullOrWhiteSpace(dbTableName))
            {
                return false;
            }

            var lower = dbTableName.ToLowerInvariant();
            foreach (var splitBase in _splitBases)
            {
                // 分片命名约定：基础名_后缀（SqlSugar 默认 {table}_{yyyyMMdd}）
                if (lower.Length > splitBase.Length + 1
                    && lower.StartsWith(splitBase, StringComparison.Ordinal)
                    && lower[splitBase.Length] == '_'
                    && _tables.TryGetValue(splitBase, out var real))
                {
                    baseRealName = real;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 反射构建目录（扫描 XiHan* 程序集中带 [SugarTable] 的实体）
        /// </summary>
        public static EntityNameCatalog Build()
        {
            var tables = new Dictionary<string, string>(StringComparer.Ordinal);
            var columns = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);
            var splitBases = new HashSet<string>(StringComparer.Ordinal);

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

                    // 分表实体的 SugarTable 名是模板（如 SysDiffLog_{year}{month}{day}），取占位符 { 之前的基础名
                    var rawTableName = string.IsNullOrWhiteSpace(tableAttribute.TableName) ? type.Name : tableAttribute.TableName;
                    var realTable = ExtractBaseName(rawTableName);
                    if (string.IsNullOrWhiteSpace(realTable))
                    {
                        realTable = type.Name;
                    }

                    var lowerTable = realTable.ToLowerInvariant();
                    tables[lowerTable] = realTable;

                    // 分表标记：ISplitTableEntity 接口 或 [SplitTable] 特性（与框架自身判定一致）
                    var isSplitTable = typeof(ISplitTableEntity).IsAssignableFrom(type)
                        || type.GetCustomAttribute<SplitTableAttribute>() is not null;
                    if (isSplitTable)
                    {
                        splitBases.Add(lowerTable);
                    }

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

            return new EntityNameCatalog(tables, columns, splitBases);
        }

        /// <summary>
        /// 取表名模板占位符 { 之前的基础名（如 SysDiffLog_{year}{month}{day} → SysDiffLog）；无占位符则原样
        /// </summary>
        private static string ExtractBaseName(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                return tableName;
            }

            var placeholderIndex = tableName.IndexOf('{');
            return placeholderIndex < 0
                ? tableName
                : tableName[..placeholderIndex].TrimEnd('_', '-', ' ');
        }
    }
}
