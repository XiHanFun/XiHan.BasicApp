// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Notification;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.BasicApp.Identity.Operations.Profile;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Auth;

/// <summary>
/// <c>auth.send-two-factor-code</c> 的请求。
/// </summary>
/// <param name="Ticket">两步验证票据。</param>
/// <param name="Method">邮箱还是短信。</param>
public sealed record SendTwoFactorCodeRequest(
    [Required, StringLength(128, MinimumLength = 1)] string Ticket,
    TwoFactorMethod Method);

/// <summary>
/// 两步验证的第二步：发验证码到邮箱或手机。
/// </summary>
/// <remarks>
/// 匿名操作，登记在架构测试的匿名白名单。<c>POST /api/auth:send-two-factor-code</c>。凭票据发到账号已验证的邮箱或手机号，
/// 方式须是这张票据能用的、且现在仍可用（<see cref="TwoFactorSettings.UsableAt"/>）；认证器不发码。
/// 过登录节流（只看不计），停用与锁定期内的账号不发；限频同其他验证码（同一目标 60 秒、每天的目标与地址上限）。
/// 第一步没登录，按平台的发送配置发。码交给 <c>auth.verify-two-factor</c>。
/// </remarks>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="throttle">登录节流。</param>
/// <param name="tickets">两步验证票据。</param>
/// <param name="contacts">发码。</param>
/// <param name="clock">时间源。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http)]
public sealed class SendTwoFactorCode(
    IRepository<SysUser, Guid> users,
    UserAccountService accounts,
    LoginThrottle throttle,
    TwoFactorTickets tickets,
    ContactCodes contacts,
    TimeProvider clock) : IOperation<SendTwoFactorCodeRequest, CodeSentResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "auth.send-two-factor-code";

    /// <inheritdoc />
    public async ValueTask<Result<CodeSentResponse>> HandleAsync(SendTwoFactorCodeRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await tickets.FindAsync(request.Ticket, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var ticket) || !(await users.FindAsync(ticket.UserId, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var user))
        {
            return Error.Denied(IdentityErrors.TwoFactorTicketInvalid, IdentityMessages.TwoFactorTicketInvalid);
        }

        var allowed = await throttle.CheckAsync(Login.ThrottleKeyOf(user.HomeTenantId, user.UserName), SessionClient.AddressOf(context), context.CancellationToken).ConfigureAwait(false);
        if (allowed.IsFailure)
        {
            return allowed.Error!;
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

        if (TwoFactorSettings.ChannelOf(request.Method) is not { } channel || !ticket.Allows(request.Method) || !TwoFactorSettings.UsableAt(user, security).Contains(request.Method))
        {
            return Error.Validation(IdentityErrors.TwoFactorMethodUnavailable, IdentityMessages.TwoFactorMethodUnavailable, MessageArguments.Of(("method", request.Method.ToString())));
        }

        var to = Contacts.Current(user, channel)!;
        var template = channel == ContactChannel.Email ? MessageTemplateCodes.EmailLoginCode : MessageTemplateCodes.SmsLoginCode;
        var sent = await contacts.SendAsync(context, channel, to, new CodeRequest(TwoFactorCodes.Purpose(channel), user.Id.ToString("N"), to), (template, null), user.Id).ConfigureAwait(false);
        return sent.TryGetValue(out var seconds) ? new CodeSentResponse(seconds) : sent.Error!;
    }
}

/// <summary>
/// 登录第二步的邮件与短信验证码。
/// </summary>
internal static class TwoFactorCodes
{
    /// <summary>
    /// 码的用途。
    /// </summary>
    public static string Purpose(ContactChannel channel) => "two-factor:" + LoginMethods.Of(channel);

    /// <summary>
    /// 核对并作废收到的码。
    /// </summary>
    /// <remarks>
    /// 码记的是发到哪里：发码之后联系方式被改过，同样当作码错。
    /// </remarks>
    public static async Task<bool> ConsumeAsync(VerificationCodes codes, SysUser user, ContactChannel channel, string code, CancellationToken cancellationToken)
    {
        var consumed = await codes.ConsumeAsync(Purpose(channel), user.Id.ToString("N"), code, cancellationToken).ConfigureAwait(false);
        return consumed.TryGetValue(out var target) && Contacts.SameAs(channel, Contacts.Current(user, channel), target);
    }
}
