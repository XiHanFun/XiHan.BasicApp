// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Domain.Entities;

namespace XiHan.BasicApp.Contracts.Entities;

/// <summary>
/// 只增记录基类：<see cref="AppEntity"/> + 创建时间 + 创建人。用于日志、流水、投递记录。
/// </summary>
public abstract class AppRecord : AppEntity, IHasCreationTime, IHasCreator
{
    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public string? CreatedBy { get; set; }
}
