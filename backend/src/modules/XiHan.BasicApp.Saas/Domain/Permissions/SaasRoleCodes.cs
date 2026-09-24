// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.Permissions;

/// <summary>
/// 系统角色编码
/// </summary>
/// <remarks>
/// 两个都是系统角色（<c>RoleType.System</c>）：成员由系统流程维护，界面上不可授予、撤销或编辑；
/// 权限不靠授权行，由授权快照按所在上下文整体给出。
/// </remarks>
public static class SaasRoleCodes
{
    /// <summary>
    /// 平台超级管理员：只在平台成立，拥有平台生效的全部权限与通配 *
    /// </summary>
    public const string SuperAdmin = "super_admin";

    /// <summary>
    /// 租户所有者：只在所属租户成立，拥有租户生效的全部权限，再由套餐白名单收窄；
    /// 开通管理员时建角色并绑定，所有权转移时随所有者移交
    /// </summary>
    public const string TenantOwner = "tenant_owner";
}
