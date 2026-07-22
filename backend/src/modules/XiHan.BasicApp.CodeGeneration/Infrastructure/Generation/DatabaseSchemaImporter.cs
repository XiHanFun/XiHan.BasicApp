// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using SqlSugar;
using XiHan.BasicApp.CodeGeneration.Domain.DomainServices;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using XiHan.Framework.Data.SqlSugar.Connections;
using XiHan.Framework.Data.SqlSugar.Metadata;
using XiHan.Framework.Domain.Entities.Abstracts;

namespace XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

/// <summary>
/// 数据库结构导入器（接通框架 IDatabaseMetadataProvider，DbFirst）
/// </summary>
/// <remarks>
/// 仅产出数据库层结构（列名/类型/可空/主键等）。C#/TS 类型与表单语义由引擎结合
/// <see cref="ITypeMappingProvider"/> 二次填充。
/// <para>
/// 多数据源：入参 <c>dataSourceId</c> 为 <c>SysCodeGenDataSource</c> 主键，为空表示本系统主库。
/// 首次使用某数据源时按需解密其连接信息并注册进 SqlSugar（见 <see cref="IDynamicConnectionRegistrar"/>），
/// 此后框架元数据提供器即可按同一 ConfigId 解析到该外部库。
/// </para>
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
public sealed class DatabaseSchemaImporter(
    IDatabaseMetadataProvider metadataProvider,
    IDynamicConnectionRegistrar connectionRegistrar,
    ICodeGenDataSourceDomainService dataSourceDomainService,
    IEntityMetadataCatalog catalog) : IDatabaseSchemaImporter
{
    private readonly IDatabaseMetadataProvider _metadataProvider = metadataProvider;
    private readonly IDynamicConnectionRegistrar _connectionRegistrar = connectionRegistrar;
    private readonly ICodeGenDataSourceDomainService _dataSourceDomainService = dataSourceDomainService;
    private readonly IEntityMetadataCatalog _catalog = catalog;

    /// <summary>
    /// 确保数据源连接已注册，返回框架可解析的 ConfigId（null 表示走本系统主库）
    /// </summary>
    /// <remarks>
    /// 数据源不存在或已停用时由领域服务抛出明确异常（fail-closed），
    /// 不静默回落到主库——否则用户以为在导外部库、实际导的是本系统的表。
    /// </remarks>
    private async Task<string?> EnsureConnectionAsync(string? dataSourceId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dataSourceId))
        {
            return null;
        }

        var configId = dataSourceId.Trim();
        if (_connectionRegistrar.IsRegistered(configId))
        {
            return configId;
        }

        if (!long.TryParse(configId, out var parsedId))
        {
            throw new ArgumentException($"数据源标识非法：{dataSourceId}", nameof(dataSourceId));
        }

        var info = await _dataSourceDomainService.GetConnectionInfoAsync(parsedId, cancellationToken);
        _connectionRegistrar.Register(new DynamicConnectionDescriptor(info.ConfigId, info.DbType, info.ConnectionString));
        return info.ConfigId;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ListTablesAsync(string? connectionConfigId = null, CancellationToken cancellationToken = default)
    {
        var configId = await EnsureConnectionAsync(connectionConfigId, cancellationToken);
        var tables = await _metadataProvider.GetTablesAsync(configId, cancellationToken);

        // 还原真实大小写 + 把分表分片折叠为基础逻辑名，并按出现顺序去重
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();
        foreach (var table in tables)
        {
            var logicalName = _catalog.ResolveLogical(table.TableName);
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

        var configId = await EnsureConnectionAsync(connectionConfigId, cancellationToken);
        var realTableName = _catalog.ResolveTable(tableName);

        var table = await _metadataProvider.GetTableAsync(tableName, configId, cancellationToken);

        // 分表基础名无物理表：扫描最近一个分片取列结构
        if (table is null && _catalog.IsSplitBase(realTableName))
        {
            var shard = await FindLatestShardAsync(realTableName, configId, cancellationToken);
            if (shard is not null)
            {
                table = await _metadataProvider.GetTableAsync(shard, configId, cancellationToken);
            }
        }

        if (table is null)
        {
            return null;
        }

        var columns = table.Columns.Select(column => MapColumn(column, realTableName)).ToList();
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
    private async Task<string?> FindLatestShardAsync(string realBaseName, string? connectionConfigId, CancellationToken cancellationToken)
    {
        var tables = await _metadataProvider.GetTablesAsync(connectionConfigId, cancellationToken);
        return tables
            .Select(table => table.TableName)
            .Where(name => _catalog.TryResolveSplitShard(name, out var baseName)
                && string.Equals(baseName, realBaseName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(name => name, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private ColumnSchema MapColumn(DatabaseColumnMetadata column, string realTableName) => new()
    {
        ColumnName = _catalog.ResolveColumn(realTableName, column.ColumnName),
        ColumnComment = column.Description,
        DbType = column.DataType,
        IsPrimaryKey = column.IsPrimaryKey,
        IsIdentity = column.IsIdentity,
        IsNullable = column.IsNullable,
        IsRequired = !column.IsNullable && !column.IsIdentity,
        Length = column.Length,
        DecimalDigits = column.Scale
    };
}
