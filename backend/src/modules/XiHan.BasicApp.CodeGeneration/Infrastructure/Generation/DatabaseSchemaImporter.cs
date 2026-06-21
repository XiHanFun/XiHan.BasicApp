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
/// </remarks>
public sealed class DatabaseSchemaImporter(IDatabaseMetadataProvider metadataProvider) : IDatabaseSchemaImporter
{
    private readonly IDatabaseMetadataProvider _metadataProvider = metadataProvider;

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ListTablesAsync(string? connectionConfigId = null, CancellationToken cancellationToken = default)
    {
        var tables = await _metadataProvider.GetTablesAsync(connectionConfigId, cancellationToken);
        return [.. tables.Select(table => table.TableName)];
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

        var columns = table.Columns.Select(MapColumn).ToList();
        return new TableSchema
        {
            TableName = table.TableName,
            TableComment = table.TableDescription,
            PrimaryKeyColumn = columns.FirstOrDefault(column => column.IsPrimaryKey)?.ColumnName,
            Columns = columns
        };
    }

    private static ColumnSchema MapColumn(DatabaseColumnMetadata column) => new()
    {
        ColumnName = column.ColumnName,
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
