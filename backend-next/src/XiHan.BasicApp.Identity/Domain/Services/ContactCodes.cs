// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using XiHan.BasicApp.Contracts.Notification;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Domain.Services;

/// <summary>
/// 联系方式的渠道。
/// </summary>
public enum ContactChannel
{
    /// <summary>
    /// 邮箱。
    /// </summary>
    Email = 0,

    /// <summary>
    /// 手机号（短信）。
    /// </summary>
    Phone = 1,
}

/// <summary>
/// 发验证码到邮箱或手机号。
/// </summary>
/// <remarks>
/// 码由 <see cref="VerificationCodes"/> 签发并限频，经 Notification 的 <see cref="IMessageSender"/> 在本次调用的工作单元里入发件箱（调用失败即不发）；
/// 邮件 / 短信配置与模板按调用所在的一侧取，租户没有自己启用的缺省配置时由平台代发（4.x 同，修订（五十二）拍板保留）。
/// 没有可用的配置或模板（或没装 Notification）即 <c>IDENTITY_UNAVAILABLE_006</c>，不签发码。
/// </remarks>
/// <param name="codes">验证码。</param>
/// <param name="senders">消息发送（装了 Notification 才有）。</param>
public sealed class ContactCodes(VerificationCodes codes, IEnumerable<IMessageSender> senders)
{
    /// <summary>
    /// 发件箱记录的业务类型。
    /// </summary>
    public const string BusinessType = "identity.verification";

    /// <summary>
    /// 签发一个验证码并发出去。
    /// </summary>
    /// <param name="context">调用上下文。</param>
    /// <param name="channel">渠道。</param>
    /// <param name="to">收码的邮箱或手机号。</param>
    /// <param name="code">要发的码的用途、主体与随码保存的内容。</param>
    /// <param name="template">模板编码与用途标题（登录码的模板没有标题）。</param>
    /// <param name="receiverId">收码的用户；还不知道是谁时为空。</param>
    /// <param name="deliver">是否真的发出去；为假时照样签发并计入限频，只是不发（验证码登录找不到账号时，不让人从回答或限频上看出账号在不在）。</param>
    /// <returns>码的有效秒数。</returns>
    public async Task<Result<int>> SendAsync(InvocationContext context, ContactChannel channel, string to, CodeRequest code, (string Code, string? Title) template, Guid? receiverId, bool deliver = true)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(to);
        ArgumentNullException.ThrowIfNull(code);
        var kind = channel == ContactChannel.Email ? MessageKind.Email : MessageKind.Sms;
        var sender = senders.FirstOrDefault();
        if (sender is null || !await sender.CanSendAsync(context, kind, template.Code).ConfigureAwait(false))
        {
            return Error.Unavailable(IdentityErrors.CodeChannelUnavailable, IdentityMessages.CodeChannelUnavailable, MessageArguments.Of(("channel", channel)));
        }

        var issued = await codes.IssueAsync(code.Purpose, code.Subject, to, code.Payload, SessionClient.AddressOf(context), context.CancellationToken).ConfigureAwait(false);
        if (!issued.TryGetValue(out var sent))
        {
            return issued.Error!;
        }

        if (!deliver)
        {
            return sent.ExpiresInSeconds;
        }

        var parameters = new List<KeyValuePair<string, string?>>
        {
            new("code", sent.Code),
            new("minutes", (sent.ExpiresInSeconds / 60).ToString(CultureInfo.InvariantCulture)),
        };
        if (template.Title is { } title)
        {
            parameters.Add(new("title", title));
        }

        var queued = await sender.EnqueueAsync(context, new OutgoingMessage(kind, to, template.Code, parameters, receiverId, BusinessType, null)).ConfigureAwait(false);
        return queued.IsFailure ? queued.Error! : sent.ExpiresInSeconds;
    }
}

/// <summary>
/// 要发的验证码：用途、主体与随码保存的内容。
/// </summary>
/// <param name="Purpose">用途。</param>
/// <param name="Subject">码归属的主体。</param>
/// <param name="Payload">校验成功时取回的内容。</param>
public sealed record CodeRequest(string Purpose, string Subject, string Payload);
