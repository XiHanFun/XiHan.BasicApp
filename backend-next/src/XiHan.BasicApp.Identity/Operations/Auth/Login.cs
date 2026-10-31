// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.MultiTenancy.Abstractions.Tenants;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Auth;

/// <summary>
/// 密码登录。
/// </summary>
/// <remarks>
/// 匿名操作，登记在架构测试的匿名白名单。按注册地租户与登录名找账号，校验状态、锁定期与密码。
/// 依次过三道关：登录节流（<see cref="LoginThrottle"/>，超限直接拒绝）、图形验证码（开启时，一次性）、账号与密码；
/// 账号开了双因素即签发两步验证票据、不开会话（日志记为 <see cref="LoginEvent.TwoFactorChallenge"/>），否则开会话并签发令牌对。
/// 失败计数在独立作用域里提交（<see cref="LoginAttempts"/>），否则随本次失败调用一起回滚。用户名或密码错误不区分哪一个。
/// </remarks>
/// <param name="accounts">账号域服务。</param>
/// <param name="attempts">账号的失败计数与锁定。</param>
/// <param name="throttle">登录节流。</param>
/// <param name="captchas">图形验证码。</param>
/// <param name="tickets">两步验证票据。</param>
/// <param name="logins">登录完成（开会话、签令牌）。</param>
/// <param name="options">身份配置。</param>
/// <param name="clock">时间源。</param>
/// <param name="tenants">框架租户目录：租户账号登录前先确认租户存在且启用。</param>
/// <param name="guards">登录守卫（认证失败计数；签发前的判定在 <see cref="LoginSessions"/>）。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http)]
public sealed class Login(
    UserAccountService accounts,
    LoginAttempts attempts,
    LoginThrottle throttle,
    Captchas captchas,
    TwoFactorTickets tickets,
    LoginSessions logins,
    IdentityOptions options,
    TimeProvider clock,
    ITenantStore tenants,
    IEnumerable<ILoginGuard> guards) : IOperation<LoginRequest, LoginResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "auth.login";

    /// <inheritdoc />
    public async ValueTask<Result<LoginResponse>> HandleAsync(LoginRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var tenant = string.IsNullOrWhiteSpace(request.Tenant) ? TenantIds.Platform : request.Tenant.Trim();
        var userName = request.UserName.Trim().ToLowerInvariant();
        // 登录日志成败都记（成败取调用结果，节流与验证码的拒绝也在内）：租户核实之前落在平台一侧，账号所在租户留原值供排查
        var log = new LoginLogEntry(LoginEvent.Login) { AccountTenantId = tenant, UserName = userName };
        context.AddAuditLog(log);
        var account = ThrottleKeyOf(tenant, userName);
        var address = SessionClient.AddressOf(context);
        var allowed = await throttle.CheckAsync(account, address, context.CancellationToken).ConfigureAwait(false);
        if (allowed.IsFailure)
        {
            return allowed.Error!;
        }

        if (options.CaptchaEnabled && !await captchas.ConsumeAsync(request.CaptchaId, request.CaptchaCode, context.CancellationToken).ConfigureAwait(false))
        {
            await throttle.RecordFailureAsync(account, address, context.CancellationToken).ConfigureAwait(false);
            return Error.Validation(IdentityErrors.CaptchaInvalid, IdentityMessages.CaptchaInvalid);
        }

        string? tenantName = null;
        // 租户账号：租户须在目录里且启用（停用的租户连用户名都不核对，直接拒绝）
        if (!string.Equals(tenant, TenantIds.Platform, StringComparison.Ordinal))
        {
            var directory = await tenants.FindAsync(tenant, context.CancellationToken).ConfigureAwait(false);
            if (!directory.TryGetValue(out var info) || !info.IsEnabled)
            {
                return Error.Denied(IdentityErrors.TenantUnavailable, IdentityMessages.TenantUnavailable, MessageArguments.Of(("tenantId", tenant)));
            }

            tenantName = info.DisplayName;
            log.TenantId = tenant;
        }

        var found = await accounts.FindByNameAsync(tenant, userName, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var user))
        {
            await throttle.RecordFailureAsync(account, address, context.CancellationToken).ConfigureAwait(false);
            await guards.RecordFailureAsync(context).ConfigureAwait(false);
            return Error.Denied(IdentityErrors.InvalidCredentials, IdentityMessages.InvalidCredentials);
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
            return LockedOut(lockoutEnd - now);
        }

        if (!PasswordHasher.Verify(request.Password, security.PasswordHash))
        {
            // 调用以失败结束时工作单元会回滚，计数在独立作用域里提交
            await throttle.RecordFailureAsync(account, address, context.CancellationToken).ConfigureAwait(false);
            await guards.RecordFailureAsync(context).ConfigureAwait(false);
            var lockedOut = await attempts.RecordFailureAsync(security.Id, context.CancellationToken).ConfigureAwait(false);
            return lockedOut ? LockedOut(options.LockoutDuration) : Error.Denied(IdentityErrors.InvalidCredentials, IdentityMessages.InvalidCredentials);
        }

        var sessionTenant = string.Equals(tenant, TenantIds.Platform, StringComparison.Ordinal) ? null : tenant;
        if (security.TwoFactorEnabled)
        {
            // 失败计数与锁定等第二步通过才清零：只知道密码的人不能靠反复登录把计数清掉
            var methods = TwoFactorSettings.UsableAt(user, security);
            if (methods.Count == 0)
            {
                return Error.Denied(IdentityErrors.TwoFactorUnavailable, IdentityMessages.TwoFactorUnavailable);
            }

            var (ticket, expiresAt) = await tickets.IssueAsync(user.Id, sessionTenant, methods, context.CancellationToken).ConfigureAwait(false);
            log.Event = LoginEvent.TwoFactorChallenge;
            return new LoginResponse(null, new TwoFactorChallengeResponse(ticket, methods, TwoFactorSettings.PreselectedOf(methods), CodeSent: false, expiresAt));
        }

        var token = await logins.StartAsync(user, security, sessionTenant, tenantName, log, context).ConfigureAwait(false);
        return token.TryGetValue(out var issued) ? new LoginResponse(issued, null) : token.Error!;
    }

    /// <summary>
    /// 节流的账号键：注册地租户与登录名（第二步按同一个键计）。
    /// </summary>
    internal static string ThrottleKeyOf(string tenant, string userName) => tenant + "/" + userName;

    private static Error LockedOut(TimeSpan remaining)
        => Error.Denied(IdentityErrors.AccountUnavailable, IdentityMessages.LockedOut, MessageArguments.Of(("seconds", (int)Math.Ceiling(remaining.TotalSeconds))));
}
