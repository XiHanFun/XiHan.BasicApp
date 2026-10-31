// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.BasicApp.Identity.Infrastructure.External;
using XiHan.Framework.Caching.Abstractions.Caches;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.MultiTenancy.Abstractions.Tenants;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Auth;

/// <summary>
/// <c>auth.external-login</c> 的请求。
/// </summary>
/// <param name="Ticket">第三方登录回调发给前端的一次性票据。</param>
public sealed record ExternalLoginRequest([Required, StringLength(128)] string Ticket);

/// <summary>
/// 凭第三方身份票据登录。
/// </summary>
/// <remarks>
/// 匿名操作，登记在架构测试的匿名白名单。凭回调发的一次性票据，在登录页选的注册地里按（提供商, 第三方标识）找绑定的账号。
/// 没绑定即 <c>IDENTITY_DENIED_015</c>（拍板：不自动建号，先用账号密码登录、到个人中心绑定；4.x 在租户 1 自动建号，建出的账号没有可用的密码与邮箱）；
/// 之后与密码登录同样过租户、账号状态、锁定期与双因素（4.x 第三方登录全跳过），开会话前过登录守卫，并刷新绑定上的显示名、邮箱、头像与最近登录时间。
/// </remarks>
/// <param name="cache">共享缓存（票据）。</param>
/// <param name="bindings">第三方绑定仓储。</param>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="tickets">两步验证票据。</param>
/// <param name="logins">登录完成（开会话、签令牌）。</param>
/// <param name="tenants">框架租户目录。</param>
/// <param name="clock">时间源。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http)]
public sealed class ExternalLogin(
    ISharedCache cache,
    IRepository<SysUserExternalLogin, Guid> bindings,
    IRepository<SysUser, Guid> users,
    UserAccountService accounts,
    TwoFactorTickets tickets,
    LoginSessions logins,
    ITenantStore tenants,
    TimeProvider clock) : IOperation<ExternalLoginRequest, LoginResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "auth.external-login";

    /// <inheritdoc />
    public async ValueTask<Result<LoginResponse>> HandleAsync(ExternalLoginRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var log = new LoginLogEntry(LoginEvent.Login);
        context.AddAuditLog(log);
        var ticket = await ExternalLoginFlow.ConsumeTicketAsync(cache, request.Ticket, ExternalLoginFlow.LoginMode, context.CancellationToken).ConfigureAwait(false);
        if (ticket is null)
        {
            return Error.Validation(IdentityErrors.ExternalTicketInvalid, IdentityMessages.ExternalTicketInvalid);
        }

        var tenant = ticket.Tenant;
        log.AccountTenantId = tenant;
        log.Detail = "external:" + ticket.ProviderType;
        string? tenantName = null;
        if (!string.Equals(tenant, TenantIds.Platform, StringComparison.Ordinal))
        {
            var directory = await TenantDirectoryGate.CheckAsync(tenants, tenant, clock.GetUtcNow(), context.CancellationToken).ConfigureAwait(false);
            if (!directory.TryGetValue(out var info))
            {
                return directory.Error!;
            }

            tenantName = info.DisplayName;
            log.TenantId = tenant;
        }

        var providerType = ticket.ProviderType;
        var providerKey = ticket.Identity.ProviderKey;
        var rows = await bindings.ListAsync(EntityQuery.All<SysUserExternalLogin>().Where(new Specification<SysUserExternalLogin>(b => b.HomeTenantId == tenant && b.Provider == providerType && b.ProviderKey == providerKey)).Page(0, 1), context.CancellationToken).ConfigureAwait(false);
        if (rows.Count == 0)
        {
            return Error.Denied(IdentityErrors.ExternalLoginNotBound, IdentityMessages.ExternalLoginNotBound, MessageArguments.Of(("provider", ticket.Provider)));
        }

        var binding = rows[0];
        var found = await users.FindAsync(binding.UserId, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var user))
        {
            return Error.Denied(IdentityErrors.ExternalLoginNotBound, IdentityMessages.ExternalLoginNotBound, MessageArguments.Of(("provider", ticket.Provider)));
        }

        log.UserId = user.Id;
        log.UserName = user.UserName;
        if (user.Status == UserStatus.Disabled)
        {
            return Error.Denied(IdentityErrors.AccountDisabled, IdentityMessages.AccountDisabled);
        }

        if (user.Status != UserStatus.Active)
        {
            return Error.Denied(IdentityErrors.AccountUnavailable, IdentityMessages.AccountUnavailable, MessageArguments.Of(("status", user.Status.ToString())));
        }

        var securityResult = await accounts.SecurityOfAsync(user.Id, context.CancellationToken).ConfigureAwait(false);
        if (!securityResult.TryGetValue(out var security))
        {
            return securityResult.Error!;
        }

        var now = clock.GetUtcNow();
        if (security.LockoutEndAt is { } lockoutEnd && lockoutEnd > now)
        {
            return Error.Denied(IdentityErrors.AccountUnavailable, IdentityMessages.LockedOut, MessageArguments.Of(("seconds", (int)Math.Ceiling((lockoutEnd - now).TotalSeconds))));
        }

        // 第三方给的资料每次登录都刷新（4.x 只在建绑定时写一次）
        binding.DisplayName = Clip(ticket.Identity.DisplayName, 200);
        binding.Email = Clip(ticket.Identity.Email, 200);
        binding.AvatarUrl = Clip(ticket.Identity.AvatarUrl, 500);
        binding.LastLoginAt = now;
        var refreshed = await bindings.UpdateAsync(binding, context.CancellationToken).ConfigureAwait(false);
        if (refreshed.IsFailure)
        {
            return refreshed.Error!;
        }

        var sessionTenant = string.Equals(tenant, TenantIds.Platform, StringComparison.Ordinal) ? null : tenant;
        if (security.TwoFactorEnabled)
        {
            var methods = TwoFactorSettings.UsableAt(user, security);
            if (methods.Count == 0)
            {
                return Error.Denied(IdentityErrors.TwoFactorUnavailable, IdentityMessages.TwoFactorUnavailable);
            }

            var (twoFactorTicket, expiresAt) = await tickets.IssueAsync(user.Id, sessionTenant, methods, context.CancellationToken).ConfigureAwait(false);
            log.Event = LoginEvent.TwoFactorChallenge;
            return new LoginResponse(null, new TwoFactorChallengeResponse(twoFactorTicket, methods, TwoFactorSettings.PreselectedOf(methods), CodeSent: false, expiresAt));
        }

        var token = await logins.StartAsync(user, security, sessionTenant, tenantName, log, context).ConfigureAwait(false);
        return token.TryGetValue(out var issued) ? new LoginResponse(issued, null) : token.Error!;
    }

    private static string? Clip(string? value, int max) => value is null || value.Length <= max ? value : value[..max];
}
