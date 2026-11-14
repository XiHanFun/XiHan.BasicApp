// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.Framework.Domain.Entities;

namespace XiHan.BasicApp.Contracts.Entities;

/// <summary>
/// 租户库全审计实体基类
/// </summary>
/// <remarks>
/// <see cref="AppAuditedEntity"/> + <c>IMultiTenant</c>。租户库实体的默认选择
/// </remarks>
public abstract class TenantScopedAuditedEntity : AppAuditedEntity, IMultiTenant
{
    /// <inheritdoc />
    [SugarColumn(ColumnName = "tenant_id", Length = 512, IsNullable = false, ColumnDescription = "租户 ID")]
    public string TenantId { get; set; } = string.Empty;
}
