// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Domain.Entities;

namespace XiHan.BasicApp.Contracts.Entities;

/// <summary>
/// 租户库只增记录基类：<see cref="AppRecord"/> + <c>IMultiTenant</c>。用于租户库里的日志、流水、投递记录。
/// </summary>
public abstract class TenantScopedRecord : AppRecord, IMultiTenant
{
    /// <inheritdoc />
    public string TenantId { get; set; } = string.Empty;
}
