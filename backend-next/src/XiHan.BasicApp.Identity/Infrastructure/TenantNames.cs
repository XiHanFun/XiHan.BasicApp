// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.MultiTenancy.Abstractions.Tenants;

namespace XiHan.BasicApp.Identity.Infrastructure;

/// <summary>
/// 从框架租户目录取租户显示名（登录 / 刷新 / 切换 / 模仿的响应与 <c>auth.me</c> 都带它，顶栏按它显示当前上下文）。
/// </summary>
public static class TenantNames
{
    /// <summary>
    /// <paramref name="tenantId"/> 的显示名；平台（<see langword="null"/>）或目录里没有即 <see langword="null"/>。
    /// </summary>
    public static async ValueTask<string?> ResolveAsync(ITenantStore tenants, string? tenantId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tenants);
        if (tenantId is null)
        {
            return null;
        }

        var found = await tenants.FindAsync(tenantId, cancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var info) ? info.DisplayName : null;
    }
}
