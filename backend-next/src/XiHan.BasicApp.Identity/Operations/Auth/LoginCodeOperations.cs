// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Contracts.Notification;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.BasicApp.Identity.Operations.Profile;
using XiHan.BasicApp.Identity.Operations.Users;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.MultiTenancy.Abstractions.Tenants;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Auth;

/// <summary>
/// <c>auth.send-login-code</c> 的请求。
/// </summary>
/// <param name="Channel">发到邮箱还是手机号。</param>
/// <param name="Target">邮箱或手机号。</param>
/// <param name="Tenant">注册地租户；平台账号留空。</param>
public sealed record SendLoginCodeRequest(ContactChannel Channel, [Required, StringLength(200, MinimumLength = 1)] string Target, [StringLength(64)] string? Tenant = null);

/// <summary>
/// <c>auth.login-by-code</c> 的请求。
/// </summary>
/// <param name="Channel">邮箱还是手机号。</param>
/// <param name="Target">收码的邮箱或手机号。</param>
/// <param name="Code">收到的验证码。</param>
/// <param name="Tenant">注册地租户；平台账号留空。</param>
public sealed record LoginByCodeRequest(ContactChannel Channel, [Required, StringLength(200, MinimumLength = 1)] string Target, [Required, StringLength(10, MinimumLength = 4)] string Code, [StringLength(64)] string? Tenant = null);

/// <summary>
/// 发登录验证码到邮箱或手机号。
/// </summary>
/// <remarks>
/// 匿名操作，登记在架构测试的匿名白名单。<c>POST /api/auth:send-login-code</c>。这种登录方式要在系统参数里开放（<see cref="LoginMethods"/>），租户须存在且启用。
/// 账号在登录租户里按邮箱（不分大小写）或已验证的手机号找，恰好一个正常的才真发；
/// 与 4.x 不同：账号不存在、不唯一或停用都照样回答「已发送」，签发并计入限频，只是不发（4.x 直接报「未注册」，能用来探测账号）。
/// </remarks>
/// <param name="accounts">账号域服务。</param>
/// <param name="contacts">发码。</param>
/// <param name="tenants">框架租户目录。</param>
/// <param name="clock">时间源（租户到期）。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http)]
public sealed class SendLoginCode(UserAccountService accounts, ContactCodes contacts, ITenantStore tenants, TimeProvider clock) : IOperation<SendLoginCodeRequest, CodeSentResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "auth.send-login-code";

    /// <inheritdoc />
    public async ValueTask<Result<CodeSentResponse>> HandleAsync(SendLoginCodeRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var opened = await LoginCodes.EnsureOpenAsync(context, request.Channel).ConfigureAwait(false);
        if (opened.IsFailure)
        {
            return opened.Error!;
        }

        var tenant = LoginCodes.TenantOf(request.Tenant);
        var available = await LoginCodes.EnsureTenantAsync(tenants, tenant, clock.GetUtcNow(), context.CancellationToken).ConfigureAwait(false);
        if (available.IsFailure)
        {
            return available.Error!;
        }

        var normalized = LoginCodes.Normalize(request.Channel, request.Target);
        if (normalized.IsFailure)
        {
            return normalized.Error!;
        }

        var target = normalized.ValueOrThrow();
        var matches = await accounts.FindByContactAsync(tenant, request.Channel, target, context.CancellationToken).ConfigureAwait(false);
        var user = matches.Count == 1 ? matches[0] : null;
        var template = request.Channel == ContactChannel.Email ? MessageTemplateCodes.EmailLoginCode : MessageTemplateCodes.SmsLoginCode;
        var code = new CodeRequest(LoginCodes.Purpose(request.Channel), LoginCodes.SubjectOf(tenant, target), user?.Id.ToString("N") ?? string.Empty);
        var sent = await contacts.SendAsync(context, request.Channel, target, code, (template, null), user?.Id, deliver: user is not null).ConfigureAwait(false);
        return sent.TryGetValue(out var seconds) ? new CodeSentResponse(seconds) : sent.Error!;
    }
}

/// <summary>
/// 用邮箱或手机收到的验证码登录。
/// </summary>
/// <remarks>
/// 匿名操作，登记在架构测试的匿名白名单。<c>POST /api/auth:login-by-code</c>。过登录节流，码对且发码之后联系方式没变才认账号，再看状态与锁定期；
/// 码错计入节流与登录守卫的认证失败，码本身输错 5 次作废并锁定。登录日志记为登录、附注 <c>code:email</c> / <c>code:phone</c>。
/// 与 4.x 不同：账号开了双因素照样要第二步（返回挑战，与密码登录、第三方登录同一条路；4.x 验证码登录跳过双因素）。
/// 第二步不能用这次收码的那种（邮箱收码登录的不能再用邮箱）；可用的只剩它时 <see cref="IdentityErrors.TwoFactorSameAsLogin"/>，须改用密码登录。
/// </remarks>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="codes">验证码。</param>
/// <param name="throttle">登录节流。</param>
/// <param name="tickets">两步验证票据。</param>
/// <param name="logins">登录完成（开会话、签令牌）。</param>
/// <param name="clock">时间源。</param>
/// <param name="tenants">框架租户目录。</param>
/// <param name="guards">登录守卫。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http)]
public sealed class LoginByCode(
    IRepository<SysUser, Guid> users,
    UserAccountService accounts,
    VerificationCodes codes,
    LoginThrottle throttle,
    TwoFactorTickets tickets,
    LoginSessions logins,
    TimeProvider clock,
    ITenantStore tenants,
    IEnumerable<ILoginGuard> guards) : IOperation<LoginByCodeRequest, LoginResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "auth.login-by-code";

    /// <inheritdoc />
    public async ValueTask<Result<LoginResponse>> HandleAsync(LoginByCodeRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var tenant = LoginCodes.TenantOf(request.Tenant);
        var log = new LoginLogEntry(LoginEvent.Login) { AccountTenantId = tenant, UserName = request.Target.Trim(), Detail = "code:" + LoginMethods.Of(request.Channel) };
        context.AddAuditLog(log);
        var opened = await LoginCodes.EnsureOpenAsync(context, request.Channel).ConfigureAwait(false);
        if (opened.IsFailure)
        {
            return opened.Error!;
        }

        var normalized = LoginCodes.Normalize(request.Channel, request.Target);
        if (normalized.IsFailure)
        {
            return normalized.Error!;
        }

        var target = normalized.ValueOrThrow();
        log.UserName = target;
        var account = Login.ThrottleKeyOf(tenant, target);
        var address = SessionClient.AddressOf(context);
        var allowed = await throttle.CheckAsync(account, address, context.CancellationToken).ConfigureAwait(false);
        if (allowed.IsFailure)
        {
            return allowed.Error!;
        }

        var available = await LoginCodes.EnsureTenantAsync(tenants, tenant, clock.GetUtcNow(), context.CancellationToken).ConfigureAwait(false);
        if (!available.TryGetValue(out var displayName))
        {
            return available.Error!;
        }

        var tenantName = displayName.Length == 0 ? null : displayName;

        if (!string.Equals(tenant, TenantIds.Platform, StringComparison.Ordinal))
        {
            log.TenantId = tenant;
        }

        var consumed = await codes.ConsumeAsync(LoginCodes.Purpose(request.Channel), LoginCodes.SubjectOf(tenant, target), request.Code, context.CancellationToken).ConfigureAwait(false);
        var user = consumed.TryGetValue(out var payload) && Guid.TryParseExact(payload, "N", out var userId) && (await users.FindAsync(userId, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var found)
            && Contacts.SameAs(request.Channel, Contacts.Current(found, request.Channel), target) && (request.Channel == ContactChannel.Email || found.PhoneVerified)
            ? found
            : null;
        if (user is null)
        {
            await throttle.RecordFailureAsync(account, address, context.CancellationToken).ConfigureAwait(false);
            await guards.RecordFailureAsync(context).ConfigureAwait(false);
            return consumed.IsFailure ? consumed.Error! : Error.Validation(IdentityErrors.CodeInvalid, IdentityMessages.CodeInvalid);
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

        var sessionTenant = string.Equals(tenant, TenantIds.Platform, StringComparison.Ordinal) ? null : tenant;
        if (security.TwoFactorEnabled)
        {
            // 第二步换一种：邮箱收码登录的不能再用邮箱做第二步，短信同理
            var usable = TwoFactorSettings.UsableAt(user, security);
            var methods = usable.Where(m => m != TwoFactorSettings.MethodOf(request.Channel)).ToList();
            if (methods.Count == 0)
            {
                return usable.Count == 0
                    ? Error.Denied(IdentityErrors.TwoFactorUnavailable, IdentityMessages.TwoFactorUnavailable)
                    : Error.Denied(IdentityErrors.TwoFactorSameAsLogin, IdentityMessages.TwoFactorSameAsLogin, MessageArguments.Of(("method", TwoFactorSettings.MethodOf(request.Channel).ToString())));
            }

            var (ticket, expiresAt) = await tickets.IssueAsync(user.Id, sessionTenant, methods, context.CancellationToken).ConfigureAwait(false);
            log.Event = LoginEvent.TwoFactorChallenge;
            return new LoginResponse(null, new TwoFactorChallengeResponse(ticket, methods, TwoFactorSettings.PreselectedOf(methods), CodeSent: false, expiresAt));
        }

        var token = await logins.StartAsync(user, security, sessionTenant, tenantName, log, context).ConfigureAwait(false);
        return token.TryGetValue(out var issued) ? new LoginResponse(issued, null) : token.Error!;
    }
}

/// <summary>
/// 验证码登录的开关、租户、格式与码的用途。
/// </summary>
internal static partial class LoginCodes
{
    /// <summary>
    /// 登录的注册地租户（留空即平台）。
    /// </summary>
    public static string TenantOf(string? tenant) => string.IsNullOrWhiteSpace(tenant) ? TenantIds.Platform : tenant.Trim();

    /// <summary>
    /// 登录码的用途。
    /// </summary>
    public static string Purpose(ContactChannel channel) => "login:" + LoginMethods.Of(channel);

    /// <summary>
    /// 登录码的主体：登录租户与联系方式。
    /// </summary>
    public static string SubjectOf(string tenant, string target) => tenant + ":" + target;

    /// <summary>
    /// 这种验证码登录是否开放。
    /// </summary>
    public static async ValueTask<Result> EnsureOpenAsync(InvocationContext context, ContactChannel channel)
    {
        var methods = await LoginMethods.ReadAsync(context).ConfigureAwait(false);
        if (!methods.TryGetValue(out var open))
        {
            return methods.Error!;
        }

        var method = LoginMethods.Of(channel);
        return open.Contains(method) ? Result.Success() : Error.Denied(IdentityErrors.LoginMethodDisabled, IdentityMessages.LoginMethodDisabled, MessageArguments.Of(("method", method)));
    }

    /// <summary>
    /// 租户账号：租户须在目录里、启用且没到期。
    /// </summary>
    /// <returns>租户的显示名；平台或没有显示名为空串。</returns>
    public static async ValueTask<Result<string>> EnsureTenantAsync(ITenantStore tenants, string tenant, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (string.Equals(tenant, TenantIds.Platform, StringComparison.Ordinal))
        {
            return string.Empty;
        }

        var directory = await TenantDirectoryGate.CheckAsync(tenants, tenant, now, cancellationToken).ConfigureAwait(false);
        return directory.TryGetValue(out var info) ? info.DisplayName ?? string.Empty : directory.Error!;
    }

    /// <summary>
    /// 规范并核对邮箱或手机号的格式。
    /// </summary>
    /// <returns>邮箱转小写、去首尾空白；格式不对即 <c>IDENTITY_VALIDATION_022</c>。</returns>
    public static Result<string> Normalize(ContactChannel channel, string target)
    {
        var normalized = channel == ContactChannel.Email ? target.Trim().ToLowerInvariant() : target.Trim();
        var valid = channel == ContactChannel.Email ? UserEmail.IsValid(normalized) : PhonePattern().IsMatch(normalized);
        return valid ? normalized : Error.Validation(IdentityErrors.LoginTargetInvalid, IdentityMessages.LoginTargetInvalid, MessageArguments.Of(("channel", channel)));
    }

    [GeneratedRegex(@"^\+?[0-9]{5,20}$", RegexOptions.CultureInvariant)]
    private static partial Regex PhonePattern();
}
