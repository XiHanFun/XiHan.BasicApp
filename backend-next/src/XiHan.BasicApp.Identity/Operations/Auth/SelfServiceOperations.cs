// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Notification;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.BasicApp.Identity.Operations.Profile;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Localization.Abstractions.Messages;
using XiHan.Framework.MultiTenancy.Abstractions.Tenants;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Auth;

/// <summary>
/// <c>auth.register</c> 的请求。
/// </summary>
/// <param name="UserName">登录名：3 到 64 个字符，小写字母开头，只含小写字母、数字、<c>.</c>、<c>_</c>、<c>-</c>。</param>
/// <param name="Password">密码，按密码策略校验。</param>
/// <param name="Email">邮箱：找回密码用，同一侧不能与正常账号重复。</param>
/// <param name="DisplayName">显示名；不填即登录名。</param>
/// <param name="CaptchaId">图形验证码标识（开了图形验证码时必填）。</param>
/// <param name="CaptchaCode">图形验证码。</param>
public sealed record RegisterRequest(
    [Required, StringLength(64, MinimumLength = 3), RegularExpression("^[a-z][a-z0-9._-]*$")] string UserName,
    [Required, StringLength(128, MinimumLength = 8)] string Password,
    [Required, StringLength(256, MinimumLength = 3)] string Email,
    [StringLength(64)] string? DisplayName = null,
    [StringLength(64)] string? CaptchaId = null,
    [StringLength(16)] string? CaptchaCode = null);

/// <summary>
/// 注册成功的账号。
/// </summary>
/// <param name="UserId">用户主键。</param>
/// <param name="UserName">登录名。</param>
public sealed record RegisterResponse(Guid UserId, string UserName);

/// <summary>
/// <c>auth.request-password-reset</c> 的请求。
/// </summary>
/// <param name="Channel">发到邮箱还是手机号。</param>
/// <param name="Target">账号的邮箱或已验证的手机号。</param>
/// <param name="Tenant">注册地租户；平台账号留空。</param>
public sealed record RequestPasswordResetRequest(ContactChannel Channel, [Required, StringLength(200, MinimumLength = 1)] string Target, [StringLength(64)] string? Tenant = null);

/// <summary>
/// <c>auth.reset-password</c> 的请求。
/// </summary>
/// <param name="Channel">收码的是邮箱还是手机号。</param>
/// <param name="Target">收码的邮箱或手机号。</param>
/// <param name="Code">收到的验证码。</param>
/// <param name="NewPassword">新密码，按密码策略与历史校验。</param>
/// <param name="Tenant">注册地租户；平台账号留空。</param>
public sealed record ResetPasswordWithCodeRequest(
    ContactChannel Channel,
    [Required, StringLength(200, MinimumLength = 1)] string Target,
    [Required, StringLength(10, MinimumLength = 4)] string Code,
    [Required, StringLength(128, MinimumLength = 8)] string NewPassword,
    [StringLength(64)] string? Tenant = null);

/// <summary>
/// 密码已重置。
/// </summary>
/// <param name="UserName">账号的登录名（回到登录页时预填）。</param>
public sealed record PasswordResetResponse(string UserName);

/// <summary>
/// 自助注册平台账号。
/// </summary>
/// <remarks>
/// 匿名操作，登记在架构测试的匿名白名单。<c>POST /api/auth:register</c>。要在配置里打开（<c>BasicApp:Auth:SelfRegistrationEnabled</c>，缺省关），否则 <see cref="IdentityErrors.RegistrationClosed"/>；
/// 开了图形验证码时与密码登录一样要先过验证码。注册的是平台账号、立即可用、不带任何角色（4.x 同，权限由管理员之后授予），要进租户由租户加为成员。
/// 登录名在平台一侧唯一，邮箱不能与平台一侧的正常账号重复（找回密码按它找人）。配好邮件且有 <c>auth-welcome</c> 模板时随工作单元入发件箱一封欢迎邮件，没有就不发、不影响注册。
/// 与 4.x 不同：4.x 没有开关、总是开放，并且注册进固定的缺省租户。
/// </remarks>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="ids">标识生成器。</param>
/// <param name="captchas">图形验证码。</param>
/// <param name="options">身份配置。</param>
/// <param name="senders">消息发送（装了 Notification 才有）。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http)]
public sealed class Register(
    IRepository<SysUser, Guid> users,
    UserAccountService accounts,
    IIdGenerator<Guid> ids,
    Captchas captchas,
    IdentityOptions options,
    IEnumerable<IMessageSender> senders) : IOperation<RegisterRequest, RegisterResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "auth.register";

    /// <summary>
    /// 欢迎邮件在发件箱里的业务类型。
    /// </summary>
    public const string WelcomeBusinessType = "identity.welcome";

    /// <inheritdoc />
    public async ValueTask<Result<RegisterResponse>> HandleAsync(RegisterRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (!options.SelfRegistrationEnabled)
        {
            return Error.Denied(IdentityErrors.RegistrationClosed, IdentityMessages.RegistrationClosed);
        }

        if (options.CaptchaEnabled && !await captchas.ConsumeAsync(request.CaptchaId, request.CaptchaCode, context.CancellationToken).ConfigureAwait(false))
        {
            return Error.Validation(IdentityErrors.CaptchaInvalid, IdentityMessages.CaptchaInvalid);
        }

        var email = LoginCodes.Normalize(ContactChannel.Email, request.Email);
        if (!email.TryGetValue(out var address))
        {
            return email.Error!;
        }

        var userName = request.UserName.Trim();
        if ((await accounts.FindByNameAsync(TenantIds.Platform, userName, context.CancellationToken).ConfigureAwait(false)).HasValue)
        {
            return Error.Conflict(IdentityErrors.UserNameTaken, IdentityMessages.UserNameTaken, MessageArguments.Of(("userName", userName), ("tenant", TenantIds.Platform)));
        }

        if ((await accounts.FindByContactAsync(TenantIds.Platform, ContactChannel.Email, address, context.CancellationToken).ConfigureAwait(false)).Count > 0)
        {
            return Error.Conflict(IdentityErrors.EmailTaken, IdentityMessages.EmailTaken, MessageArguments.Of(("email", address)));
        }

        var user = new SysUser
        {
            Id = ids.NewId(),
            UserName = userName,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? userName : request.DisplayName.Trim(),
            Email = address,
            HomeTenantId = TenantIds.Platform,
            Status = UserStatus.Active,
        };
        var added = await users.AddAsync(user, context.CancellationToken).ConfigureAwait(false);
        if (added.IsFailure)
        {
            return added.Error!;
        }

        var securityResult = await accounts.SecurityOfAsync(user.Id, context.CancellationToken).ConfigureAwait(false);
        if (!securityResult.TryGetValue(out var security))
        {
            return securityResult.Error!;
        }

        var set = await accounts.SetPasswordAsync(security, request.Password, mustChange: false, context.CancellationToken).ConfigureAwait(false);
        if (set.IsFailure)
        {
            return set.Error!;
        }

        // 欢迎邮件（4.x 同）：发不了就不发，注册照常
        if (senders.FirstOrDefault() is { } sender && await sender.CanSendAsync(context, MessageKind.Email, MessageTemplateCodes.Welcome).ConfigureAwait(false))
        {
            var queued = await sender.EnqueueAsync(context, new OutgoingMessage(MessageKind.Email, address, MessageTemplateCodes.Welcome, [new("user_name", user.DisplayName)], user.Id, WelcomeBusinessType, null)).ConfigureAwait(false);
            if (queued.IsFailure)
            {
                return queued.Error!;
            }
        }

        return new RegisterResponse(user.Id, user.UserName);
    }
}

/// <summary>
/// 找回密码：发验证码到账号的邮箱或手机号。
/// </summary>
/// <remarks>
/// 匿名操作，登记在架构测试的匿名白名单。<c>POST /api/auth:request-password-reset</c>。租户须存在、启用且没到期；
/// 账号在租户里按邮箱（不分大小写）或已验证的手机号找，恰好一个正常的才真发，否则照样回答「已发送」、签发并计入限频，只是不发（与验证码登录同一套，不让人探测账号）。
/// 与 4.x 不同：4.x 只按邮箱发一次性链接，6.0 发验证码（邮箱或手机，限频与输错作废同验证码登录），不依赖前端地址配置；用的是验证类模板，标题「重置密码」。
/// </remarks>
/// <param name="accounts">账号域服务。</param>
/// <param name="contacts">发码。</param>
/// <param name="tenants">框架租户目录。</param>
/// <param name="clock">时间源（租户到期）。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http)]
public sealed class RequestPasswordReset(UserAccountService accounts, ContactCodes contacts, ITenantStore tenants, TimeProvider clock) : IOperation<RequestPasswordResetRequest, CodeSentResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "auth.request-password-reset";

    /// <inheritdoc />
    public async ValueTask<Result<CodeSentResponse>> HandleAsync(RequestPasswordResetRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var tenant = LoginCodes.TenantOf(request.Tenant);
        var available = await LoginCodes.EnsureTenantAsync(tenants, tenant, clock.GetUtcNow(), context.CancellationToken).ConfigureAwait(false);
        if (available.IsFailure)
        {
            return available.Error!;
        }

        var normalized = LoginCodes.Normalize(request.Channel, request.Target);
        if (!normalized.TryGetValue(out var target))
        {
            return normalized.Error!;
        }

        var matches = await accounts.FindByContactAsync(tenant, request.Channel, target, context.CancellationToken).ConfigureAwait(false);
        var user = matches.Count == 1 ? matches[0] : null;
        var title = context.Localize(request.Channel == ContactChannel.Email ? IdentityMessages.CodeTitleResetPasswordEmail : IdentityMessages.CodeTitleResetPasswordPhone);
        var code = new CodeRequest(PasswordResetCodes.Purpose(request.Channel), LoginCodes.SubjectOf(tenant, target), user?.Id.ToString("N") ?? string.Empty);
        var sent = await contacts.SendAsync(context, request.Channel, target, code, (Contacts.VerificationTemplate(request.Channel), title), user?.Id, deliver: user is not null).ConfigureAwait(false);
        return sent.TryGetValue(out var seconds) ? new CodeSentResponse(seconds) : sent.Error!;
    }
}

/// <summary>
/// 用找回密码收到的验证码设新密码。
/// </summary>
/// <remarks>
/// 匿名操作，登记在架构测试的匿名白名单。<c>POST /api/auth:reset-password</c>。码对、发码之后联系方式没变、账号正常才认，码输错 5 次作废并锁定；
/// 新密码先按密码策略校验（不合格不消费验证码），设的时候再核对历史；设好后清掉登录失败锁定、不要求再改，并吊销这个账号的全部会话（4.x 只换安全戳）。停用的账号 <see cref="IdentityErrors.AccountDisabled"/>，被锁定的账号 <see cref="IdentityErrors.AccountUnavailable"/>。
/// </remarks>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="codes">验证码。</param>
/// <param name="tenants">框架租户目录。</param>
/// <param name="clock">时间源（租户到期）。</param>
/// <param name="options">身份配置（密码策略）。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http)]
public sealed class ResetPasswordWithCode(IRepository<SysUser, Guid> users, UserAccountService accounts, VerificationCodes codes, ITenantStore tenants, TimeProvider clock, IdentityOptions options) : IOperation<ResetPasswordWithCodeRequest, PasswordResetResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "auth.reset-password";

    /// <summary>
    /// 吊销会话时记的原因。
    /// </summary>
    public const string RevokeReason = "password-reset";

    /// <inheritdoc />
    public async ValueTask<Result<PasswordResetResponse>> HandleAsync(ResetPasswordWithCodeRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var tenant = LoginCodes.TenantOf(request.Tenant);
        var available = await LoginCodes.EnsureTenantAsync(tenants, tenant, clock.GetUtcNow(), context.CancellationToken).ConfigureAwait(false);
        if (available.IsFailure)
        {
            return available.Error!;
        }

        var normalized = LoginCodes.Normalize(request.Channel, request.Target);
        if (!normalized.TryGetValue(out var target))
        {
            return normalized.Error!;
        }

        var policy = PasswordPolicy.Validate(request.NewPassword, options);
        if (policy.IsFailure)
        {
            return policy.Error!;
        }

        var consumed = await codes.ConsumeAsync(PasswordResetCodes.Purpose(request.Channel), LoginCodes.SubjectOf(tenant, target), request.Code, context.CancellationToken).ConfigureAwait(false);
        if (!consumed.TryGetValue(out var payload))
        {
            return consumed.Error!;
        }

        var user = Guid.TryParseExact(payload, "N", out var userId) && (await users.FindAsync(userId, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var found)
            && Contacts.SameAs(request.Channel, Contacts.Current(found, request.Channel), target) && (request.Channel == ContactChannel.Email || found.PhoneVerified)
            ? found
            : null;
        if (user is null)
        {
            return Error.Validation(IdentityErrors.CodeInvalid, IdentityMessages.CodeInvalid);
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

        var set = await accounts.SetPasswordAsync(security, request.NewPassword, mustChange: false, context.CancellationToken).ConfigureAwait(false);
        if (set.IsFailure)
        {
            return set.Error!;
        }

        var revoked = await accounts.RevokeSessionsAsync(user.Id, RevokeReason, context.CancellationToken).ConfigureAwait(false);
        return revoked.IsFailure ? revoked.Error! : new PasswordResetResponse(user.UserName);
    }
}

/// <summary>
/// 找回密码的验证码用途。
/// </summary>
internal static class PasswordResetCodes
{
    /// <summary>
    /// 码的用途：与登录码分开计数与作废。
    /// </summary>
    public static string Purpose(ContactChannel channel) => "password-reset:" + LoginMethods.Of(channel);
}
