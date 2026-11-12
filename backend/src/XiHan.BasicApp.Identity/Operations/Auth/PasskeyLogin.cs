// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.MultiTenancy.Abstractions.Tenants;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.WebAuthn;

namespace XiHan.BasicApp.Identity.Operations.Auth;

/// <summary>
/// <c>auth.begin-passkey-login</c> 的请求
/// </summary>
public sealed record BeginPasskeyLoginRequest;

/// <summary>
/// 通行密钥登录的请求选项
/// </summary>
/// <param name="Ticket">一次性票据，登录时带回</param>
/// <param name="Options">交给浏览器 <c>navigator.credentials.get</c> 的选项（WebAuthn JSON 形态）</param>
public sealed record BeginPasskeyLoginResponse(string Ticket, WebAuthnRequestOptions Options);

/// <summary>
/// <c>auth.login-by-passkey</c> 的请求
/// </summary>
/// <param name="Ticket">发起登录时给的票据</param>
/// <param name="Credential">浏览器给的断言（<c>PublicKeyCredential.toJSON()</c>）</param>
public sealed record LoginByPasskeyRequest([Required, StringLength(128)] string Ticket, [Required] WebAuthnAuthenticationResponse Credential);

/// <summary>
/// 发起通行密钥登录
/// </summary>
/// <remarks>
/// 匿名操作，登记在架构测试的匿名白名单。<c>POST /api/auth:begin-passkey-login</c>。给请求选项与一次性票据（按选项的超时过期）
/// 不列允许的凭据，由认证器列出本站的通行密钥让人选，不用先填账号与注册地。宿主没配或系统参数没开放时不可用
/// </remarks>
/// <param name="passkeys">通行密钥的仪式</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http)]
public sealed class BeginPasskeyLogin(PasskeyCeremonies passkeys) : IOperation<BeginPasskeyLoginRequest, BeginPasskeyLoginResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "auth.begin-passkey-login";

    /// <inheritdoc />
    public async ValueTask<Result<BeginPasskeyLoginResponse>> HandleAsync(BeginPasskeyLoginRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var open = await passkeys.EnsureOpenAsync(context).ConfigureAwait(false);
        if (!open.TryGetValue(out var ceremonies))
        {
            return open.Error!;
        }

        var (ticket, options) = await passkeys.BeginAuthenticationAsync(ceremonies, context.CancellationToken).ConfigureAwait(false);
        return new BeginPasskeyLoginResponse(ticket, options);
    }
}

/// <summary>
/// 用通行密钥登录
/// </summary>
/// <remarks>
/// 匿名操作，登记在架构测试的匿名白名单。<c>POST /api/auth:login-by-passkey</c>。票据换出挑战（否则 <c>IDENTITY_VALIDATION_028</c>），按凭据 ID 找登记的通行密钥（没有即 <c>IDENTITY_DENIED_022</c>），
/// 登录进它的注册地；过登录节流，由框架核对来源、挑战、签名、签名计数与凭据归属（断言的用户句柄必须等于凭据的所属用户），核对失败计入节流与登录守卫的认证失败
/// 之后与密码登录同样过租户、账号状态与锁定期，开会话前过登录守卫。认证器做了用户验证（指纹、面容或设备密码）即顶替双因素；没做而账号开了双因素照样要第二步
/// 登录日志记为登录、附注 <c>passkey</c>
/// </remarks>
/// <param name="users">用户仓储</param>
/// <param name="accounts">账号域服务</param>
/// <param name="passkeys">通行密钥的仪式</param>
/// <param name="rows">通行密钥仓储</param>
/// <param name="throttle">登录节流</param>
/// <param name="tickets">两步验证票据</param>
/// <param name="logins">登录完成（开会话、签令牌）</param>
/// <param name="clock">时间源</param>
/// <param name="tenants">框架租户目录</param>
/// <param name="guards">登录守卫</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http)]
public sealed class LoginByPasskey(
    IRepository<SysUser, Guid> users,
    UserAccountService accounts,
    PasskeyCeremonies passkeys,
    IRepository<SysUserPasskey, Guid> rows,
    LoginThrottle throttle,
    TwoFactorTickets tickets,
    LoginSessions logins,
    TimeProvider clock,
    ITenantStore tenants,
    IEnumerable<ILoginGuard> guards) : IOperation<LoginByPasskeyRequest, LoginResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "auth.login-by-passkey";

    /// <inheritdoc />
    public async ValueTask<Result<LoginResponse>> HandleAsync(LoginByPasskeyRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var log = new LoginLogEntry(LoginEvent.Login) { Detail = LoginMethods.Passkey };
        context.AddAuditLog(log);
        var open = await passkeys.EnsureOpenAsync(context).ConfigureAwait(false);
        if (!open.TryGetValue(out var ceremonies))
        {
            return open.Error!;
        }

        if (await passkeys.ConsumeAuthenticationAsync(request.Ticket, context.CancellationToken).ConfigureAwait(false) is not { } challenge)
        {
            return Error.Validation(IdentityErrors.PasskeyTicketInvalid, IdentityMessages.PasskeyTicketInvalid);
        }

        var address = SessionClient.AddressOf(context);
        var passkey = await passkeys.FindAsync(request.Credential.Id, context.CancellationToken).ConfigureAwait(false);
        var user = passkey is null ? null : (await users.FindAsync(passkey.UserId, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var owner) ? owner : null;
        if (passkey is null || user is null)
        {
            await guards.RecordFailureAsync(context).ConfigureAwait(false);
            return Error.Denied(IdentityErrors.PasskeyNotRegistered, IdentityMessages.PasskeyNotRegistered);
        }

        var tenant = user.HomeTenantId;
        log.AccountTenantId = tenant;
        log.UserId = user.Id;
        log.UserName = user.UserName;
        var account = Login.ThrottleKeyOf(tenant, user.UserName);
        var allowed = await throttle.CheckAsync(account, address, context.CancellationToken).ConfigureAwait(false);
        if (allowed.IsFailure)
        {
            return allowed.Error!;
        }

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

        // 免用户名登录，登录前没认出任何人：不传期望用户，由框架要求断言的用户句柄等于凭据的所属用户
        var verified = ceremonies.VerifyAuthentication(request.Credential, challenge, PasskeyCeremonies.StoredOf(passkey), expectedUserHandle: null);
        if (!verified.TryGetValue(out var assertion))
        {
            await throttle.RecordFailureAsync(account, address, context.CancellationToken).ConfigureAwait(false);
            await guards.RecordFailureAsync(context).ConfigureAwait(false);
            return verified.Error!;
        }

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

        passkey.SignCount = assertion.SignCount;
        passkey.BackedUp = assertion.BackedUp;
        passkey.LastUsedAt = now;
        var used = await rows.UpdateAsync(passkey, context.CancellationToken).ConfigureAwait(false);
        if (used.IsFailure)
        {
            return used.Error!;
        }

        var sessionTenant = string.Equals(tenant, TenantIds.Platform, StringComparison.Ordinal) ? null : tenant;
        if (security.TwoFactorEnabled && !assertion.UserVerified)
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
}
