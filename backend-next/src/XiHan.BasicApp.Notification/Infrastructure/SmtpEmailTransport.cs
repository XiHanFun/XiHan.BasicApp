// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Net.Sockets;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using XiHan.Framework.Kernel.Security;

namespace XiHan.BasicApp.Notification.Infrastructure;

/// <summary>
/// SMTP 发送（MailKit）
/// </summary>
/// <remarks>
/// 465 端口一连上即 TLS，其余端口在开了加密时用 STARTTLS；有登录名才认证。每封邮件一条连接（发送循环逐封发，量不大）
/// 地址是租户能填的，连接过出站地址策略：SMTP 不走 HTTP 处理器，用不上策略的连接回调，这里照它的规则按解析出的实际 IP 判、只连检查过的地址
/// </remarks>
/// <param name="outbound">出站地址策略</param>
/// <param name="outboundOptions">放行的主机名在这里</param>
public sealed class SmtpEmailTransport(OutboundAddressPolicy outbound, OutboundAddressOptions outboundOptions) : IEmailTransport
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
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(_timeout);
            var connected = await ConnectAsync(server.Host, server.Port, timeout.Token).ConfigureAwait(false);
            await client.ConnectAsync(connected, server.Host, server.Port, socket, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(server.UserName))
            {
                await client.AuthenticateAsync(server.UserName, server.Password ?? string.Empty, cancellationToken).ConfigureAwait(false);
            }

            await client.SendAsync(message, cancellationToken).ConfigureAwait(false);
            await client.DisconnectAsync(quit: true, cancellationToken).ConfigureAwait(false);
            return TransportResult.Sent(message.MessageId);
        }
        catch (OutboundAddressDeniedException exception)
        {
            // 只有租户自己填的主机与端口，不是回显
            return TransportResult.Failed(exception.Message);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            return TransportResult.Failed(new TimeoutException(exception.Message, exception));
        }
#pragma warning disable CA1031 // 网络、认证与服务器拒绝都记成这条记录的失败原因，由发送循环决定是否重试
        catch (Exception exception) when (exception is not OperationCanceledException)
#pragma warning restore CA1031
        {
            return TransportResult.Failed(exception);
        }
    }

    /// <summary>
    /// 解析主机名，只连策略允许的地址
    /// </summary>
    /// <remarks>
    /// 连的就是刚检查过的地址，不会再解析一次，DNS 重绑定没有窗口；一部分地址被拒绝时只连其余的
    /// </remarks>
    private async Task<Socket> ConnectAsync(string host, int port, CancellationToken cancellationToken)
    {
        var resolved = await Dns.GetHostAddressesAsync(host, cancellationToken).ConfigureAwait(false);
        if (resolved.Length == 0)
        {
            throw new SocketException((int)SocketError.HostNotFound);
        }

        var allowed = outboundOptions.AllowedHosts.Contains(host, StringComparer.OrdinalIgnoreCase) ? resolved : Array.FindAll(resolved, outbound.IsAllowed);
        if (allowed.Length == 0)
        {
            throw new OutboundAddressDeniedException(host, port);
        }

        for (var index = 0; ; index++)
        {
            Socket? socket = null;
            try
            {
                socket = new Socket(allowed[index].AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                await socket.ConnectAsync(new IPEndPoint(allowed[index], port), cancellationToken).ConfigureAwait(false);
                return socket;
            }
            catch (SocketException) when (index < allowed.Length - 1)
            {
                socket?.Dispose();
            }
            catch
            {
                socket?.Dispose();
                throw;
            }
        }
    }
}
