// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Domain.Entities;

namespace XiHan.BasicApp.Contracts.Entities;

/// <summary>
/// 租户库实体基类：<see cref="AppEntity"/> + <c>IMultiTenant</c>。
/// </summary>
/// <remarks>
/// 只用于租户库实体：由框架仓储按当前租户严格过滤（无租户的调用看不到任何行），并随租户分库落到 <c>shard</c> 连接。
/// </remarks>
public abstract class TenantScopedEntity : AppEntity, IMultiTenant
{
    /// <inheritdoc />
    public string TenantId { get; set; } = string.Empty;
}
