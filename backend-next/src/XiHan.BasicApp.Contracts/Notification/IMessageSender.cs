// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Contracts.Notification;

/// <summary>
/// 邮件或短信。
/// </summary>
public enum MessageKind
{
    /// <summary>
    /// 邮件。
    /// </summary>
    Email = 0,

    /// <summary>
    /// 短信。
    /// </summary>
    Sms = 1,
}

/// <summary>
/// 一条要发的邮件或短信：按模板编码取当前一侧的模板（没有取平台的）渲染，参数名即模板里的变量名；品牌名 <c>brand</c> 不传时由发送方补上。
/// </summary>
/// <param name="Kind">邮件或短信。</param>
/// <param name="To">收件地址或手机号。</param>
/// <param name="TemplateCode">模板编码（如 <c>auth-email-login-code</c>）。</param>
/// <param name="Parameters">模板参数（按写入顺序交给短信服务商）。</param>
/// <param name="ReceiverId">接收人（不是本应用用户时为空）。</param>
/// <param name="BusinessType">关联的业务类型。</param>
/// <param name="BusinessId">关联的业务 ID。</param>
public sealed record OutgoingMessage(
    MessageKind Kind,
    string To,
    string TemplateCode,
    IReadOnlyList<KeyValuePair<string, string?>> Parameters,
    Guid? ReceiverId = null,
    string? BusinessType = null,
    string? BusinessId = null);

/// <summary>
/// 邮件与短信的发送（Notification 实现）：写一条排队中的发送记录，随调用的事务一起提交，由后台的发送循环发出（失败重试）。
/// 没装 Notification 即没有实现，调用方按「不能发」处理。
/// </summary>
public interface IMessageSender
{
    /// <summary>
    /// 当前一侧能不能发这类消息：有启用的缺省配置（租户没有看平台的）且有启用的模板。
    /// </summary>
    /// <param name="context">调用上下文。</param>
    /// <param name="kind">邮件或短信。</param>
    /// <param name="templateCode">要用的模板编码。</param>
    /// <returns>能发即真。</returns>
    ValueTask<bool> CanSendAsync(InvocationContext context, MessageKind kind, string templateCode);

    /// <summary>
    /// 排队一条消息；模板不存在、渲染失败或没有可用的配置即失败（不写记录）。
    /// </summary>
    /// <param name="context">调用上下文。</param>
    /// <param name="message">消息。</param>
    /// <returns>发送记录 ID。</returns>
    ValueTask<Result<Guid>> EnqueueAsync(InvocationContext context, OutgoingMessage message);
}
