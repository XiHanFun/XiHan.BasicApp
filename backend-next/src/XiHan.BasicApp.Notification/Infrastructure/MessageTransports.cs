// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Notification.Domain.Entities;

namespace XiHan.BasicApp.Notification.Infrastructure;

/// <summary>
/// 一次发送的结果：成功带服务商的消息 ID，失败带原因（记进发送记录）。
/// </summary>
/// <param name="Succeeded">是否发出。</param>
/// <param name="MessageId">服务商的消息 ID。</param>
/// <param name="Error">失败原因。</param>
public sealed record TransportResult(bool Succeeded, string? MessageId, string? Error)
{
    /// <summary>
    /// 发出。
    /// </summary>
    /// <param name="messageId">服务商的消息 ID。</param>
    /// <returns>结果。</returns>
    public static TransportResult Sent(string? messageId) => new(true, messageId, null);

    /// <summary>
    /// 失败。
    /// </summary>
    /// <param name="error">原因。</param>
    /// <returns>结果。</returns>
    public static TransportResult Failed(string error) => new(false, null, error);
}

/// <summary>
/// SMTP 服务器（密码已解密）。
/// </summary>
/// <param name="Host">主机。</param>
/// <param name="Port">端口。</param>
/// <param name="UseSsl">加密连接。</param>
/// <param name="AcceptInvalidCertificate">接受无效证书。</param>
/// <param name="FromEmail">发件地址。</param>
/// <param name="FromName">发件人名称。</param>
/// <param name="UserName">登录名。</param>
/// <param name="Password">密码。</param>
public sealed record EmailServer(string Host, int Port, bool UseSsl, bool AcceptInvalidCertificate, string FromEmail, string? FromName, string? UserName, string? Password);

/// <summary>
/// 一封邮件。
/// </summary>
/// <param name="To">收件地址。</param>
/// <param name="Subject">主题。</param>
/// <param name="Body">正文。</param>
/// <param name="IsHtml">正文是 HTML。</param>
public sealed record EmailEnvelope(string To, string Subject, string? Body, bool IsHtml);

/// <summary>
/// 邮件的发送通道（缺省 SMTP）。
/// </summary>
/// <remarks>
/// 测试换成假的。
/// </remarks>
public interface IEmailTransport
{
    /// <summary>
    /// 发一封邮件。
    /// </summary>
    /// <param name="server">服务器。</param>
    /// <param name="envelope">邮件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>结果。</returns>
    Task<TransportResult> SendAsync(EmailServer server, EmailEnvelope envelope, CancellationToken cancellationToken);
}

/// <summary>
/// 短信服务商账号（访问密钥已解密）。
/// </summary>
/// <param name="Provider">服务商。</param>
/// <param name="AccessKeyId">访问密钥 ID。</param>
/// <param name="AccessKeySecret">访问密钥。</param>
/// <param name="SdkAppId">应用 ID。</param>
/// <param name="SignName">签名。</param>
/// <param name="Region">地域。</param>
public sealed record SmsAccount(SmsProvider Provider, string AccessKeyId, string AccessKeySecret, string? SdkAppId, string SignName, string? Region);

/// <summary>
/// 一条短信。
/// </summary>
/// <param name="Phone">手机号。</param>
/// <param name="ProviderTemplateId">服务商的模板 ID。</param>
/// <param name="Parameters">模板参数（按顺序；腾讯云只认顺序）。</param>
public sealed record SmsEnvelope(string Phone, string ProviderTemplateId, IReadOnlyList<KeyValuePair<string, string?>> Parameters);

/// <summary>
/// 短信的发送通道（每个服务商一个实现）。
/// </summary>
/// <remarks>
/// 测试换成假的。
/// </remarks>
public interface ISmsTransport
{
    /// <summary>
    /// 服务商。
    /// </summary>
    SmsProvider Provider { get; }

    /// <summary>
    /// 发一条短信。
    /// </summary>
    /// <param name="account">账号。</param>
    /// <param name="envelope">短信。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>结果。</returns>
    Task<TransportResult> SendAsync(SmsAccount account, SmsEnvelope envelope, CancellationToken cancellationToken);
}
