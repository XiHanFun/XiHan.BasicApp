// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.Framework.Domain.Entities;

namespace XiHan.BasicApp.Contracts.Entities;

/// <summary>
/// 应用实体基类：UUID v7 主键 + 并发戳
/// </summary>
/// <remarks>
/// 用于关联表与无审计需求的表
/// 主键由 <c>IIdGenerator&lt;Guid&gt;</c> 在创建时分配，并发戳由框架在每次写入时更新
/// </remarks>
public abstract class AppEntity : IEntity<Guid>, IHasConcurrencyStamp
{
    /// <inheritdoc />
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true, IsNullable = false, ColumnDescription = "主键")]
    public Guid Id { get; set; }

    /// <inheritdoc />
    [SugarColumn(ColumnName = "concurrency_stamp", Length = 32, IsNullable = false, ColumnDescription = "并发戳")]
    public string ConcurrencyStamp { get; set; } = string.Empty;
}
