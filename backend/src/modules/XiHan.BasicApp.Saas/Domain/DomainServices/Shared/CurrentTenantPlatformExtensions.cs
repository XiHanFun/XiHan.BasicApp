// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 当前租户平台态判定扩展
/// </summary>
/// <remarks>
/// 平台运维态（Platform Operation）= 当前请求无租户上下文（<see cref="ICurrentTenant.Id"/> 为 null 或 0）。
/// 约定见 <c>BasicAppEntity</c>：业务租户 Id 从 1 开始，0 号为平台保留，登录未指定租户时 <see cref="ICurrentTenant.Id"/> 为 null。
/// 仅在平台态下允许维护平台级全局模板（菜单/权限/角色/操作/资源/版本等 TenantId=0 的记录）；
/// 租户态（Id&gt;0）下对全局模板一律拒绝写入，避免某租户改动影响所有租户。
/// </remarks>
public static class CurrentTenantPlatformExtensions
{
    /// <summary>
    /// 当前是否处于平台运维态（无租户上下文）
    /// </summary>
    /// <param name="currentTenant">当前租户</param>
    /// <returns>true 表示平台运维态，可维护全局模板</returns>
    public static bool IsPlatformOperation(this ICurrentTenant currentTenant)
    {
        ArgumentNullException.ThrowIfNull(currentTenant);
        return currentTenant.Id is null or 0;
    }
}
