// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using XiHan.BasicApp.Notification.Domain.Entities;
using XiHan.BasicApp.Notification.Infrastructure;
using XiHan.Framework.Kernel.Security;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证发送通道的失败原因与出站地址
/// </summary>
/// <remarks>
/// 发送记录租户看得到，地址也是租户能填的：SMTP 过出站地址策略，失败原因不带异常原文，也不回显对方的响应正文
/// </remarks>
public sealed class MessageTransportTests
{
    private static readonly EmailEnvelope _envelope = new("someone@example.test", "subject", "body", false);

    private static SmtpEmailTransport Smtp(OutboundAddressOptions options)
        => new(new OutboundAddressPolicy(options, NullLoggerFactory.Instance));

    private static EmailServer Server(string host, int port) => new(host, port, false, false, "noreply@example.test", null, null, null);

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.0.0.8")]
    [InlineData("192.168.1.8")]
    [InlineData("169.254.169.254")]
    public async Task Smtp_DeniedAddresses_AreNotConnected(string host)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var result = await Smtp(new OutboundAddressOptions()).SendAsync(Server(host, port), _envelope, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains(host, result.Error, StringComparison.Ordinal);
        Assert.False(listener.Pending());
    }

    [Fact]
    public async Task Smtp_ConnectionFailure_RecordsOnlyTheExceptionType()
    {
        // 拿一个刚放开的端口，连上去被拒绝
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        var result = await Smtp(new OutboundAddressOptions { AllowLoopback = true }).SendAsync(Server("127.0.0.1", port), _envelope, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(typeof(SocketException).FullName, result.Error);
        Assert.DoesNotContain(port.ToString(System.Globalization.CultureInfo.InvariantCulture), result.Error, StringComparison.Ordinal);
        Assert.IsType<SocketException>(result.Exception);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Smtp_AllowedAddresses_SendOverTheCheckedSocket(bool allowLoopback, bool allowHost)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var options = new OutboundAddressOptions { AllowLoopback = allowLoopback, AllowedHosts = allowHost ? ["127.0.0.1"] : [] };
        var server = FakeSmtpAsync(listener, TestContext.Current.CancellationToken);

        var result = await Smtp(options).SendAsync(Server("127.0.0.1", port), _envelope, TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded, result.Error);
        Assert.Contains("MAIL FROM", await server, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Bot_ErrorResponse_IsNotEchoed()
    {
        using var handler = new Stub(new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("internal-secret-page") });
        var transport = new WeComBotTransport(new Factory(handler));

        var result = await transport.SendAsync(new BotEndpoint(BotChannel.WeCom, "https://internal.test/hook", null, null), "text", TestContext.Current.CancellationToken);

        Assert.Equal((false, "HTTP 500"), (result.Succeeded, result.Error));
    }

    [Fact]
    public async Task Bot_NetworkFailure_RecordsOnlyTheExceptionType()
    {
        using var handler = new Stub(new HttpRequestException("Connection refused (10.0.0.8:8080)"));
        var transport = new WeComBotTransport(new Factory(handler));

        var result = await transport.SendAsync(new BotEndpoint(BotChannel.WeCom, "https://internal.test/hook", null, null), "text", TestContext.Current.CancellationToken);

        Assert.Equal(typeof(HttpRequestException).FullName, result.Error);
        Assert.Contains("10.0.0.8", result.Exception!.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 只会收一封信的 SMTP 服务器
    /// </summary>
    private static async Task<string> FakeSmtpAsync(TcpListener listener, CancellationToken cancellationToken)
    {
        using var client = await listener.AcceptTcpClientAsync(cancellationToken);
        await using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII);
        await using var writer = new StreamWriter(stream, Encoding.ASCII) { NewLine = "\r\n", AutoFlush = true };
        var seen = new StringBuilder();
        await writer.WriteLineAsync("220 fake");
        var data = false;
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            seen.AppendLine(line);
            if (data)
            {
                if (line == ".")
                {
                    data = false;
                    await writer.WriteLineAsync("250 queued");
                }

                continue;
            }

            if (line.StartsWith("QUIT", StringComparison.OrdinalIgnoreCase))
            {
                await writer.WriteLineAsync("221 bye");
                break;
            }

            if (line.StartsWith("DATA", StringComparison.OrdinalIgnoreCase))
            {
                data = true;
                await writer.WriteLineAsync("354 go");
                continue;
            }

            await writer.WriteLineAsync("250 ok");
        }

        return seen.ToString();
    }

    private sealed class Stub : HttpMessageHandler
    {
        private readonly HttpResponseMessage? _response;

        private readonly Exception? _failure;

        public Stub(HttpResponseMessage response) => _response = response;

        public Stub(Exception failure) => _failure = failure;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => _failure is not null ? throw _failure : Task.FromResult(_response!);
    }

    private sealed class Factory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }
}
