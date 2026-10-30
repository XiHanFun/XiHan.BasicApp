// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Localization.Abstractions.Messages;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Profile;

/// <summary>
/// 发开关邮箱或短信双因素用的验证码。
/// </summary>
/// <remarks>
/// <c>POST /api/profile:send-two-factor-code</c>。发到本人已验证的邮箱或手机号（没有绑定 <c>IDENTITY_VALIDATION_019</c>、
/// 没验证 <c>IDENTITY_VALIDATION_023</c>），收到后交给 <c>profile.enable-two-factor</c> 或 <c>profile.disable-two-factor</c>；认证器不发码。
/// 模仿会话被拒。限频同其他验证码。
/// </remarks>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="contacts">发码。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class SendTwoFactorSetupCode(IRepository<SysUser, Guid> users, UserAccountService accounts, ContactCodes contacts) : IOperation<TwoFactorMethodRequest, CodeSentResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "profile.send-two-factor-code";

    /// <inheritdoc />
    public async ValueTask<Result<CodeSentResponse>> HandleAsync(TwoFactorMethodRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = await ProfileSubject.ResolveAsync(users, accounts, context, rejectImpersonation: true).ConfigureAwait(false);
        if (!subject.TryGetValue(out var resolved))
        {
            return subject.Error!;
        }

        var user = resolved.User;
        if (TwoFactorSettings.ChannelOf(request.Method) is not { } channel)
        {
            return Error.Validation(IdentityErrors.TwoFactorMethodUnavailable, IdentityMessages.TwoFactorMethodUnavailable, MessageArguments.Of(("method", request.Method.ToString())));
        }

        var contact = TwoFactorSetup.VerifiedContactOf(user, channel);
        if (!contact.TryGetValue(out var to))
        {
            return contact.Error!;
        }

        var title = context.Localize(channel == ContactChannel.Email ? IdentityMessages.CodeTitleTwoFactorEmail : IdentityMessages.CodeTitleTwoFactorPhone);
        var sent = await contacts.SendAsync(context, channel, to, new CodeRequest(TwoFactorSetup.Purpose(channel), user.Id.ToString("N"), to), (Contacts.VerificationTemplate(channel), title), user.Id).ConfigureAwait(false);
        return sent.TryGetValue(out var seconds) ? new CodeSentResponse(seconds) : sent.Error!;
    }
}

/// <summary>
/// 开关邮箱与短信双因素的验证码。
/// </summary>
internal static class TwoFactorSetup
{
    /// <summary>
    /// 码的用途。
    /// </summary>
    public static string Purpose(ContactChannel channel) => "two-factor-setup:" + LoginMethods.Of(channel);

    /// <summary>
    /// 本人已验证的邮箱或手机号。
    /// </summary>
    public static Result<string> VerifiedContactOf(SysUser user, ContactChannel channel)
    {
        if (Contacts.Current(user, channel) is not { } current || string.IsNullOrWhiteSpace(current))
        {
            return Error.Validation(IdentityErrors.ContactMissing, IdentityMessages.ContactMissing, MessageArguments.Of(("channel", channel)));
        }

        var verified = channel == ContactChannel.Email ? user.EmailVerified : user.PhoneVerified;
        return verified ? current : Error.Validation(IdentityErrors.ContactUnverified, IdentityMessages.ContactUnverified, MessageArguments.Of(("channel", channel)));
    }

    /// <summary>
    /// 核对并作废收到的码。
    /// </summary>
    /// <remarks>
    /// 联系方式须仍已验证，且与发码时的相同。
    /// </remarks>
    public static async Task<Result> ConfirmAsync(VerificationCodes codes, SysUser user, ContactChannel channel, string code, CancellationToken cancellationToken)
    {
        var contact = VerifiedContactOf(user, channel);
        if (!contact.TryGetValue(out var current))
        {
            return contact.Error!;
        }

        var consumed = await codes.ConsumeAsync(Purpose(channel), user.Id.ToString("N"), code, cancellationToken).ConfigureAwait(false);
        if (!consumed.TryGetValue(out var target))
        {
            return consumed.Error!;
        }

        return Contacts.SameAs(channel, current, target) ? Result.Success() : Error.Validation(IdentityErrors.CodeInvalid, IdentityMessages.CodeInvalid);
    }
}
