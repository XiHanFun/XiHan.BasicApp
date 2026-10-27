// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Domain.Entities;

namespace XiHan.BasicApp.Contracts.Entities;

/// <summary>
/// 租户库全审计实体基类：<see cref="AppAuditedEntity"/> + <c>IMultiTenant</c>。租户库实体的默认选择。
/// </summary>
public abstract class TenantScopedAuditedEntity : AppAuditedEntity, IMultiTenant
{
    /// <inheritdoc />
    public string TenantId { get; set; } = string.Empty;
}
