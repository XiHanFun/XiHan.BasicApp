// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;

namespace XiHan.BasicApp.Notification.Infrastructure;

/// <summary>
/// SMTP 发送（MailKit）。
/// </summary>
/// <remarks>
/// 465 端口一连上即 TLS，其余端口在开了加密时用 STARTTLS；有登录名才认证。每封邮件一条连接（发送循环逐封发，量不大）。
/// </remarks>
public sealed class SmtpEmailTransport : IEmailTransport
{
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);

    /// <inheritdoc />
    public async Task<TransportResult> SendAsync(EmailServer server, EmailEnvelope envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(server);
        ArgumentNullException.ThrowIfNull(envelope);
        using var message = new MimeMessage();
        message.From.Add(new MailboxAddress(string.IsNullOrWhiteSpace(server.FromName) ? server.FromEmail : server.FromName, server.FromEmail));
        if (!MailboxAddress.TryParse(envelope.To, out var to))
        {
            return TransportResult.Failed($"Invalid recipient address '{envelope.To}'.");
        }

        message.To.Add(to);
        message.Subject = envelope.Subject;
        message.Body = new TextPart(envelope.IsHtml ? TextFormat.Html : TextFormat.Plain) { Text = envelope.Body ?? string.Empty };

        using var client = new SmtpClient { Timeout = (int)_timeout.TotalMilliseconds };
        if (server.AcceptInvalidCertificate)
        {
            // 配置上明确打开、只用于内网测试的服务器
#pragma warning disable CA5359
            client.ServerCertificateValidationCallback = static (_, _, _, _) => true;
#pragma warning restore CA5359
        }

        var socket = !server.UseSsl ? SecureSocketOptions.None : server.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
        try
        {
            await client.ConnectAsync(server.Host, server.Port, socket, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(server.UserName))
            {
                await client.AuthenticateAsync(server.UserName, server.Password ?? string.Empty, cancellationToken).ConfigureAwait(false);
            }

            await client.SendAsync(message, cancellationToken).ConfigureAwait(false);
            await client.DisconnectAsync(quit: true, cancellationToken).ConfigureAwait(false);
            return TransportResult.Sent(message.MessageId);
        }
#pragma warning disable CA1031 // 网络、认证与服务器拒绝都记成这条记录的失败原因，由发送循环决定是否重试
        catch (Exception exception) when (exception is not OperationCanceledException)
#pragma warning restore CA1031
        {
            return TransportResult.Failed(exception.Message);
        }
    }
}
