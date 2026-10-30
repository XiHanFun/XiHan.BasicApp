// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Tenancy.Domain.Entities;

/// <summary>
/// 租户成员的类型。
/// </summary>
/// <remarks>
/// 成员类型不带任何权限，权限仍由 Access 的角色决定。租户的所有者不是成员类型，是 <see cref="SysTenant.AdminUserId"/>（每租户唯一）。
/// </remarks>
public enum TenantMemberType
{
    /// <summary>
    /// 普通成员。
    /// </summary>
    /// <remarks>
    /// 注册地在别处的租户内用户：能切换进去、能在其中被模仿、占席位、可接任所有者。
    /// </remarks>
    Member = 0,

    /// <summary>
    /// 运维支持成员。
    /// </summary>
    /// <remarks>
    /// 对应 4.x 的 <c>PlatformAdmin</c>（平台运营方跨租户进入做运维、客服与风控）：只能是平台账号，平台账号也只能以这种身份加入；
    /// 能切换进去，但不占席位、不能成为所有者，也不算租户内的用户（不进模仿候选、不收租户的群发）。
    /// </remarks>
    Support = 1,
}
