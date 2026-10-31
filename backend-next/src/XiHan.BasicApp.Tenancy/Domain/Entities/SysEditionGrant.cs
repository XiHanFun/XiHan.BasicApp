// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Tenancy.Domain.Entities;

/// <summary>
/// 套餐白名单的一行（平台库，表 <c>sys_edition_grant</c>）。
/// </summary>
/// <remarks>
/// 套餐放行一个权限码。只能是租户能生效的权限（作用侧 Tenant / Both），由 <c>editions.set-permissions</c> 整体替换。
/// </remarks>
[SugarIndex("ux_{table}_edition_id_permission_code", nameof(EditionId), OrderByType.Asc, nameof(PermissionCode), OrderByType.Asc, true)]
public sealed class SysEditionGrant : AppRecord
{
    /// <summary>
    /// 套餐主键。
    /// </summary>
    public Guid EditionId { get; set; }

    /// <summary>
    /// 权限码。
    /// </summary>
    public string PermissionCode { get; set; } = string.Empty;
}
