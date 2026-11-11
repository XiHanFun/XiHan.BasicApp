// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.MultiTenancy.Abstractions.Tenants;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Infrastructure;

/// <summary>
/// 从框架租户目录取租户显示名
/// </summary>
/// <remarks>
/// 登录 / 刷新 / 切换 / 模仿的响应与 <c>auth.me</c> 都带它，顶栏按它显示当前上下文
/// </remarks>
public static class TenantNames
{
    /// <summary>
    /// 取 <paramref name="tenantId"/> 的显示名
    /// </summary>
    /// <returns>显示名；平台（<see langword="null"/>）、目录里没有或没起显示名即无值，目录读不到即失败</returns>
    public static async ValueTask<Result<Maybe<string>>> ResolveAsync(ITenantStore tenants, string? tenantId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tenants);
        if (tenantId is null)
        {
            return Maybe.None<string>();
        }

        var found = await tenants.FindAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var info))
        {
            return found.Error!;
        }

        return info.TryGetValue(out var tenant) ? Maybe.From(tenant.DisplayName) : Maybe.None<string>();
    }
}
