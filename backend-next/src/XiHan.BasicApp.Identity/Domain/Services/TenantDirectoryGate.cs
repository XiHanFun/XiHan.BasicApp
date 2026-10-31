// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.MultiTenancy;
using XiHan.Framework.MultiTenancy.Abstractions.Tenants;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Domain.Services;

/// <summary>
/// 核对租户能否进入。
/// </summary>
/// <remarks>
/// 登录（密码、验证码、第三方、二次验证）、刷新、切换租户与模仿登录共用，只读框架租户目录：
/// 不在目录里或已停用 <see cref="IdentityErrors.TenantUnavailable"/>；目录属性里的到期时间已过 <see cref="IdentityErrors.TenantExpired"/>
/// （到期作业把租户标为到期、目录随之停用之后仍按到期回答）。
/// </remarks>
internal static class TenantDirectoryGate
{
    /// <summary>
    /// 核对并取出目录里的租户。
    /// </summary>
    /// <param name="tenants">租户目录。</param>
    /// <param name="tenantId">租户 ID。</param>
    /// <param name="now">当前时刻。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public static async ValueTask<Result<TenantInfo>> CheckAsync(ITenantStore tenants, string tenantId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var directory = await tenants.FindAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (!directory.TryGetValue(out var info))
        {
            return Error.Denied(IdentityErrors.TenantUnavailable, IdentityMessages.TenantUnavailable, MessageArguments.Of(("tenantId", tenantId)));
        }

        if (TenantDirectoryProperties.ExpiresAtOf(info.Properties) is { } expiresAt && expiresAt <= now)
        {
            return Error.Denied(IdentityErrors.TenantExpired, IdentityMessages.TenantExpired, MessageArguments.Of(("tenantId", tenantId)));
        }

        return info.IsEnabled
            ? info
            : Error.Denied(IdentityErrors.TenantUnavailable, IdentityMessages.TenantUnavailable, MessageArguments.Of(("tenantId", tenantId)));
    }
}
