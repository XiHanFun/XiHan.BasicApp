// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Domain.Entities;

namespace XiHan.BasicApp.Contracts.Entities;

/// <summary>
/// 租户库聚合根基类。
/// </summary>
/// <remarks>
/// <see cref="AppAggregateRoot"/> + <c>IMultiTenant</c>。
/// </remarks>
public abstract class TenantScopedAggregateRoot : AppAggregateRoot, IMultiTenant
{
    /// <inheritdoc />
    public string TenantId { get; set; } = string.Empty;
}
