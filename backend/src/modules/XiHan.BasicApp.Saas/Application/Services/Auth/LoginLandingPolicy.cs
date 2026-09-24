// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.QueryServices;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 租户账号的登录落点
/// </summary>
/// <remarks>
/// 平台只对平台账号开放，租户账号总是落进一个可进入的租户：最近进入过的 → 归属租户 → 第一个可进入的。
/// </remarks>
public static class LoginLandingPolicy
{
    /// <summary>
    /// 从可进入的租户里选出登录落点
    /// </summary>
    /// <param name="accessible">可进入的租户（不能为空）</param>
    /// <param name="homeTenantId">账号的归属租户</param>
    /// <returns>落点租户</returns>
    public static AccessibleTenant Choose(IReadOnlyList<AccessibleTenant> accessible, long homeTenantId)
    {
        ArgumentNullException.ThrowIfNull(accessible);
        if (accessible.Count == 0)
        {
            throw new ArgumentException("可进入的租户不能为空。", nameof(accessible));
        }

        return accessible
                   .Where(item => item.Membership.LastActiveTime.HasValue)
                   .MaxBy(item => item.Membership.LastActiveTime)
               ?? accessible.FirstOrDefault(item => item.Tenant.BasicId == homeTenantId)
               ?? accessible[0];
    }
}
