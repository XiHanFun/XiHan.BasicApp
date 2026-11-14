// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.Framework.Domain.Entities;

namespace XiHan.BasicApp.Contracts.Entities;

/// <summary>
/// 只增记录基类：<see cref="AppEntity"/> + 创建时间 + 创建人
/// </summary>
/// <remarks>
/// 用于日志、流水、投递记录
/// </remarks>
public abstract class AppRecord : AppEntity, IHasCreationTime, IHasCreator
{
    /// <inheritdoc />
    [SugarColumn(ColumnName = "created_at", IsNullable = false, ColumnDescription = "创建时间")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    [SugarColumn(ColumnName = "created_by", Length = 512, IsNullable = true, ColumnDescription = "创建人（主体 ID）")]
    public string? CreatedBy { get; set; }
}
