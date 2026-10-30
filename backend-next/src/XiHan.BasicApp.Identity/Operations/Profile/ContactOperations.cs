// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.RegularExpressions;
using SqlSugar;
using XiHan.BasicApp.Contracts.Notification;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.BasicApp.Identity.Operations.Users;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Localization.Abstractions.Messages;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Profile;

/// <summary>
/// 发验证码到本人当前的邮箱或手机号。
/// </summary>
/// <remarks>
/// <c>POST /api/profile:send-verification-code</c>。自助操作，只要求已认证（4.x 同，不要密码）；没有绑定即 <c>IDENTITY_VALIDATION_019</c>。
/// 码 6 位、10 分钟有效，限频与每日上限见 <see cref="VerificationCodes"/>；收到后 <c>profile.verify-contact</c> 确认。
/// </remarks>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="contacts">发码。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class SendVerificationCode(IRepository<SysUser, Guid> users, UserAccountService accounts, ContactCodes contacts) : IOperation<SendVerificationCodeRequest, CodeSentResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "profile.send-verification-code";

    /// <inheritdoc />
    public async ValueTask<Result<CodeSentResponse>> HandleAsync(SendVerificationCodeRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = await ProfileSubject.ResolveAsync(users, accounts, context, rejectImpersonation: false).ConfigureAwait(false);
        if (!subject.TryGetValue(out var resolved))
        {
            return subject.Error!;
        }

        var user = resolved.User;
        if (Contacts.Current(user, request.Channel) is not { } current)
        {
            return Error.Validation(IdentityErrors.ContactMissing, IdentityMessages.ContactMissing, MessageArguments.Of(("channel", request.Channel)));
        }

        var title = context.Localize(request.Channel == ContactChannel.Email ? IdentityMessages.CodeTitleVerifyEmail : IdentityMessages.CodeTitleVerifyPhone);
        var sent = await contacts.SendAsync(context, request.Channel, current, new CodeRequest(Contacts.VerifyPurpose(request.Channel), user.Id.ToString("N"), current), (Contacts.VerificationTemplate(request.Channel), title), user.Id).ConfigureAwait(false);
        return sent.TryGetValue(out var seconds) ? new CodeSentResponse(seconds) : sent.Error!;
    }
}

/// <summary>
/// 用收到的验证码确认本人当前的邮箱或手机号。
/// </summary>
/// <remarks>
/// <c>POST /api/profile:verify-contact</c>。码对且发码之后联系方式没变即记为已验证（4.x 同）；码错计次，5 次锁定 10 分钟。
/// </remarks>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="codes">验证码。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class VerifyContact(IRepository<SysUser, Guid> users, UserAccountService accounts, VerificationCodes codes) : IOperation<ContactCodeRequest, ProfileResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "profile.verify-contact";

    /// <inheritdoc />
    public async ValueTask<Result<ProfileResponse>> HandleAsync(ContactCodeRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = await ProfileSubject.ResolveAsync(users, accounts, context, rejectImpersonation: false).ConfigureAwait(false);
        if (!subject.TryGetValue(out var resolved))
        {
            return subject.Error!;
        }

        var (user, security) = resolved;
        var consumed = await codes.ConsumeAsync(Contacts.VerifyPurpose(request.Channel), user.Id.ToString("N"), request.Code, context.CancellationToken).ConfigureAwait(false);
        if (!consumed.TryGetValue(out var verified))
        {
            return consumed.Error!;
        }

        // 发码之后联系方式被改过（管理员改了）：码对应的不是现在这个，当作码无效
        if (!Contacts.SameAs(request.Channel, Contacts.Current(user, request.Channel), verified))
        {
            return Error.Validation(IdentityErrors.CodeInvalid, IdentityMessages.CodeInvalid);
        }

        Contacts.MarkVerified(user, request.Channel);
        var updated = await users.UpdateAsync(user, context.CancellationToken).ConfigureAwait(false);
        return updated.IsFailure ? updated.Error! : ProfileResponse.From(user, security);
    }
}

/// <summary>
/// 换绑邮箱或手机号的第一步：验密码，发验证码到新的。
/// </summary>
/// <remarks>
/// <c>POST /api/profile:send-change-contact-code</c>。模仿会话不能用（4.x 同）。新的要合格式、与现在的不同，
/// 同一注册地租户里不能已被别人使用（<c>IDENTITY_CONFLICT_010</c>）；收到后 <c>profile.change-contact</c> 确认。
/// 与 4.x 不同：手机号也查重（4.x 不查，手机号登录时再要求恰好一个），查重按注册地租户（6.0 的账号属于注册地租户，4.x 的邮箱全局唯一）。
/// </remarks>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="contacts">发码。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class SendChangeContactCode(IRepository<SysUser, Guid> users, UserAccountService accounts, ContactCodes contacts) : IOperation<SendChangeContactCodeRequest, CodeSentResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "profile.send-change-contact-code";

    /// <inheritdoc />
    public async ValueTask<Result<CodeSentResponse>> HandleAsync(SendChangeContactCodeRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = await ProfileSubject.ResolveAsync(users, accounts, context, rejectImpersonation: true).ConfigureAwait(false);
        if (!subject.TryGetValue(out var resolved))
        {
            return subject.Error!;
        }

        var (user, security) = resolved;
        if (!PasswordHasher.Verify(request.Password, security.PasswordHash))
        {
            return Error.Denied(IdentityErrors.InvalidCredentials, IdentityMessages.InvalidCredentials);
        }

        var accepted = await Contacts.AcceptAsync(users, user, request.Channel, request.NewValue, context.CancellationToken).ConfigureAwait(false);
        if (!accepted.TryGetValue(out var value))
        {
            return accepted.Error!;
        }

        var title = context.Localize(request.Channel == ContactChannel.Email ? IdentityMessages.CodeTitleChangeEmail : IdentityMessages.CodeTitleChangePhone);
        var sent = await contacts.SendAsync(context, request.Channel, value, new CodeRequest(Contacts.ChangePurpose(request.Channel), user.Id.ToString("N"), value), (Contacts.VerificationTemplate(request.Channel), title), user.Id).ConfigureAwait(false);
        return sent.TryGetValue(out var seconds) ? new CodeSentResponse(seconds) : sent.Error!;
    }
}

/// <summary>
/// 换绑邮箱或手机号的第二步：用新的收到的码确认。
/// </summary>
/// <remarks>
/// <c>POST /api/profile:change-contact</c>。码对即换成第一步给的新值、记为已验证（4.x 同）；确认时再查一次重。模仿会话不能用。
/// </remarks>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="codes">验证码。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class ChangeContact(IRepository<SysUser, Guid> users, UserAccountService accounts, VerificationCodes codes) : IOperation<ContactCodeRequest, ProfileResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "profile.change-contact";

    /// <inheritdoc />
    public async ValueTask<Result<ProfileResponse>> HandleAsync(ContactCodeRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var subject = await ProfileSubject.ResolveAsync(users, accounts, context, rejectImpersonation: true).ConfigureAwait(false);
        if (!subject.TryGetValue(out var resolved))
        {
            return subject.Error!;
        }

        var (user, security) = resolved;
        var consumed = await codes.ConsumeAsync(Contacts.ChangePurpose(request.Channel), user.Id.ToString("N"), request.Code, context.CancellationToken).ConfigureAwait(false);
        if (!consumed.TryGetValue(out var value))
        {
            return consumed.Error!;
        }

        var accepted = await Contacts.AcceptAsync(users, user, request.Channel, value, context.CancellationToken).ConfigureAwait(false);
        if (accepted.IsFailure)
        {
            return accepted.Error!;
        }

        if (request.Channel == ContactChannel.Email)
        {
            user.Email = value;
        }
        else
        {
            user.PhoneNumber = value;
        }

        Contacts.MarkVerified(user, request.Channel);
        var updated = await users.UpdateAsync(user, context.CancellationToken).ConfigureAwait(false);
        return updated.IsFailure ? updated.Error! : ProfileResponse.From(user, security);
    }
}

/// <summary>
/// 联系方式的用途、模板、格式与查重。
/// </summary>
internal static partial class Contacts
{
    /// <summary>
    /// 验证当前联系方式的码的用途。
    /// </summary>
    public static string VerifyPurpose(ContactChannel channel) => "contact-verify:" + Name(channel);

    /// <summary>
    /// 换绑联系方式的码的用途。
    /// </summary>
    public static string ChangePurpose(ContactChannel channel) => "contact-change:" + Name(channel);

    /// <summary>
    /// 验证类的消息模板编码。
    /// </summary>
    public static string VerificationTemplate(ContactChannel channel)
        => channel == ContactChannel.Email ? MessageTemplateCodes.EmailVerificationCode : MessageTemplateCodes.SmsVerificationCode;

    /// <summary>
    /// 用户当前的邮箱或手机号。
    /// </summary>
    public static string? Current(SysUser user, ContactChannel channel)
        => channel == ContactChannel.Email ? user.Email : user.PhoneNumber;

    /// <summary>
    /// 两个联系方式是否相同（邮箱不分大小写）。
    /// </summary>
    public static bool SameAs(ContactChannel channel, string? left, string? right)
        => left is not null && right is not null && string.Equals(left.Trim(), right.Trim(), channel == ContactChannel.Email ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    /// <summary>
    /// 记为已验证。
    /// </summary>
    public static void MarkVerified(SysUser user, ContactChannel channel)
    {
        if (channel == ContactChannel.Email)
        {
            user.EmailVerified = true;
        }
        else
        {
            user.PhoneVerified = true;
        }
    }

    /// <summary>
    /// 核对要换成的联系方式：合格式、与现在的不同、同一注册地租户里没被别人用。
    /// </summary>
    /// <returns>规范化后的值（邮箱转小写、去首尾空白）。</returns>
    public static async Task<Result<string>> AcceptAsync(IRepository<SysUser, Guid> users, SysUser user, ContactChannel channel, string value, CancellationToken cancellationToken)
    {
        var normalized = channel == ContactChannel.Email ? value.Trim().ToLowerInvariant() : value.Trim();
        var valid = channel == ContactChannel.Email ? normalized.Length <= 200 && UserEmail.IsValid(normalized) : PhonePattern().IsMatch(normalized);
        if (!valid || SameAs(channel, Current(user, channel), normalized))
        {
            return Error.Validation(IdentityErrors.ContactInvalid, IdentityMessages.ContactInvalid, MessageArguments.Of(("channel", channel)));
        }

        var home = user.HomeTenantId;
        var id = user.Id;
        var taken = channel == ContactChannel.Email
            ? new Specification<SysUser>(u => u.HomeTenantId == home && u.Id != id && u.Email != null && SqlFunc.ToLower(u.Email) == normalized)
            : new Specification<SysUser>(u => u.HomeTenantId == home && u.Id != id && u.PhoneNumber == normalized);
        var others = await users.ListAsync(EntityQuery.All<SysUser>().Where(taken).Page(0, 1), cancellationToken).ConfigureAwait(false);
        return others.Count > 0
            ? Error.Conflict(IdentityErrors.ContactTaken, IdentityMessages.ContactTaken, MessageArguments.Of(("channel", channel)))
            : normalized;
    }

    private static string Name(ContactChannel channel) => channel == ContactChannel.Email ? "email" : "phone";

    [GeneratedRegex(@"^\+?[0-9]{5,20}$", RegexOptions.CultureInvariant)]
    private static partial Regex PhonePattern();
}
