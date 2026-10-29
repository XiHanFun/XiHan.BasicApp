// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.BasicApp.Runtime.Security;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.MultiTenancy.Abstractions.Tenants;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Auth;

/// <summary>
/// 两步验证的第二步（匿名操作，登记在架构测试的匿名白名单）：凭票据与验证码完成登录，成功即开会话、签令牌对，票据作废。
/// 票据不存在、过期或已作废一律 <see cref="IdentityErrors.TwoFactorTicketInvalid"/>（须重新登录）；
/// 码错计入票据的输错次数（用完即作废）、账号的失败计数（可触发锁定）与登录节流。认证器的码按时间步防重放。
/// 账号状态、锁定期与租户在第二步重新核对：签发票据之后被锁定或停用的账号同样进不来。
/// </summary>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="attempts">账号的失败计数与锁定。</param>
/// <param name="throttle">登录节流。</param>
/// <param name="tickets">两步验证票据。</param>
/// <param name="logins">登录完成（开会话、签令牌）。</param>
/// <param name="protector">密钥解密。</param>
/// <param name="options">身份配置。</param>
/// <param name="clock">时间源。</param>
/// <param name="tenants">框架租户目录。</param>
/// <param name="guards">登录守卫（认证失败计数；签发前的判定在 <see cref="LoginSessions"/>）。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http)]
public sealed class VerifyTwoFactor(
    IRepository<SysUser, Guid> users,
    UserAccountService accounts,
    LoginAttempts attempts,
    LoginThrottle throttle,
    TwoFactorTickets tickets,
    LoginSessions logins,
    ISecretProtector protector,
    IdentityOptions options,
    TimeProvider clock,
    ITenantStore tenants,
    IEnumerable<ILoginGuard> guards) : IOperation<VerifyTwoFactorRequest, TokenResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "auth.verify-two-factor";

    /// <summary>
    /// 认证器密钥加密的用途名。
    /// </summary>
    internal const string SecretPurpose = "identity.totp";

    /// <inheritdoc />
    public async ValueTask<Result<TokenResponse>> HandleAsync(VerifyTwoFactorRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        // 第二步成功才是真正的登录：日志记为登录（失败带错误码），认出票据之前没有账号信息
        var log = new LoginLogEntry(LoginEvent.Login);
        context.AddAuditLog(log);
        var found = await tickets.FindAsync(request.Ticket, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var ticket) || !(await users.FindAsync(ticket.UserId, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var user))
        {
            return Error.Denied(IdentityErrors.TwoFactorTicketInvalid, IdentityMessages.TwoFactorTicketInvalid);
        }

        log.TenantId = ticket.TenantId;
        log.AccountTenantId = user.HomeTenantId;
        log.UserId = user.Id;
        log.UserName = user.UserName;
        var account = Login.ThrottleKeyOf(user.HomeTenantId, user.UserName);
        var address = SessionClient.AddressOf(context);
        var allowed = await throttle.CheckAsync(account, address, context.CancellationToken).ConfigureAwait(false);
        if (allowed.IsFailure)
        {
            return allowed.Error!;
        }

        if (user.Status != UserStatus.Active)
        {
            await tickets.RevokeAsync(request.Ticket, context.CancellationToken).ConfigureAwait(false);
            return Error.Denied(IdentityErrors.AccountUnavailable, IdentityMessages.AccountUnavailable, MessageArguments.Of(("status", user.Status.ToString())));
        }

        string? tenantName = null;
        if (ticket.TenantId is { } tenantId)
        {
            var directory = await tenants.FindAsync(tenantId, context.CancellationToken).ConfigureAwait(false);
            if (!directory.TryGetValue(out var info) || !info.IsEnabled)
            {
                await tickets.RevokeAsync(request.Ticket, context.CancellationToken).ConfigureAwait(false);
                return Error.Denied(IdentityErrors.TenantUnavailable, IdentityMessages.TenantUnavailable, MessageArguments.Of(("tenantId", tenantId)));
            }

            tenantName = info.DisplayName;
        }

        var securityResult = await accounts.SecurityOfAsync(user.Id, context.CancellationToken).ConfigureAwait(false);
        if (!securityResult.TryGetValue(out var security))
        {
            return securityResult.Error!;
        }

        var now = clock.GetUtcNow();
        if (security.LockoutEndAt is { } lockoutEnd && lockoutEnd > now)
        {
            await tickets.RevokeAsync(request.Ticket, context.CancellationToken).ConfigureAwait(false);
            return Error.Denied(IdentityErrors.AccountUnavailable, IdentityMessages.LockedOut, MessageArguments.Of(("seconds", (int)Math.Ceiling((lockoutEnd - now).TotalSeconds))));
        }

        if (!TwoFactorSettings.UsableAt(security).Contains(request.Method))
        {
            return Error.Validation(IdentityErrors.TwoFactorMethodUnavailable, IdentityMessages.TwoFactorMethodUnavailable, MessageArguments.Of(("method", request.Method.ToString())));
        }

        var step = protector.TryUnprotect(SecretPurpose, security.TwoFactorSecret!, out var secret) ? Totp.Verify(secret, request.Code, now, security.TwoFactorLastStep) : null;
        if (step is null)
        {
            // 三处计数各自独立：票据用完即作废、账号到上限即锁定、地址与账号按窗口节流
            var revoked = await tickets.RecordFailureAsync(request.Ticket, ticket, context.CancellationToken).ConfigureAwait(false);
            var lockedOut = await attempts.RecordFailureAsync(security.Id, context.CancellationToken).ConfigureAwait(false);
            await throttle.RecordFailureAsync(account, address, context.CancellationToken).ConfigureAwait(false);
            await guards.RecordFailureAsync(context).ConfigureAwait(false);
            if (lockedOut)
            {
                await tickets.RevokeAsync(request.Ticket, context.CancellationToken).ConfigureAwait(false);
                return Error.Denied(IdentityErrors.AccountUnavailable, IdentityMessages.LockedOut, MessageArguments.Of(("seconds", (int)Math.Ceiling(options.LockoutDuration.TotalSeconds))));
            }

            return revoked
                ? Error.Denied(IdentityErrors.TwoFactorTicketInvalid, IdentityMessages.TwoFactorTicketInvalid)
                : Error.Denied(IdentityErrors.TwoFactorCodeInvalid, IdentityMessages.TwoFactorCodeInvalid);
        }

        security.TwoFactorLastStep = step;
        await tickets.RevokeAsync(request.Ticket, context.CancellationToken).ConfigureAwait(false);
        return await logins.StartAsync(user, security, ticket.TenantId, tenantName, log, context).ConfigureAwait(false);
    }
}
