// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Notification.Domain.Entities;
using XiHan.BasicApp.Notification.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证邮件与短信的模板、渠道配置、排队发送与重试。
/// </summary>
/// <remarks>
/// 平台的 8 个模板种子、模板语法检查与编码唯一、租户的同编码模板覆盖平台的；渠道配置的缺省与密钥不回显；
/// 通知按投递渠道给有邮箱 / 手机号的接收人排队，后台循环用缺省配置（租户没有取平台的）发出、HTML 模板转义参数、短信按映射取服务商模板；
/// 失败按次数退避直到失败、重发后再发；发送中的不能删、只有排队中的能取消；发送记录只看本侧。
/// </remarks>
public sealed class MessagingTests : IDisposable
{
    private static readonly string[] _allChannels = ["InApp", "Email", "Sms"];
    private static readonly string[] _withEmail = ["InApp", "Email"];
    private static readonly string[] _emailOnly = ["Email"];

    private readonly SqliteDatabase _database = new();
    private readonly FakeEmailTransport _email = new();
    private readonly FakeSmsTransport _sms = new();

    public void Dispose() => _database.Dispose();

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, object? body, string token)
    {
        var json = body switch
        {
            null => null,
            string text => text,
            _ => JsonSerializer.Serialize(body),
        };
        var (response, payload) = await server.SendAsync(BasicAppTestServer.Request(method, path, json, token));
        return (response.StatusCode, payload);
    }

    private static async Task<string> CodeAsync(BasicAppTestServer server, HttpMethod method, string path, object? body, string token)
        => (await SendAsync(server, method, path, body, token)).Body.GetProperty("code").GetString()!;

    private static async Task<string> CreatedAsync(BasicAppTestServer server, string path, object body, string token)
    {
        var (status, created) = await SendAsync(server, HttpMethod.Post, path, body, token);
        Assert.True(status == HttpStatusCode.Created, created.ToString());
        return created.GetProperty("id").GetString()!;
    }

    private static async Task<List<JsonElement>> RecordsAsync(BasicAppTestServer server, string token, string kind)
        => (await SendAsync(server, HttpMethod.Post, $"/api/{kind}:list", """{"page":{"index":1,"size":50}}""", token)).Body.GetProperty("items").EnumerateArray().ToList();

    /// <summary>
    /// 等到这一侧的记录都不再排队或发送中。
    /// </summary>
    private static async Task<List<JsonElement>> SettledAsync(BasicAppTestServer server, string token, string kind, int count)
    {
        for (var attempt = 0; attempt < 150; attempt++)
        {
            var records = await RecordsAsync(server, token, kind);
            if (records.Count == count && records.All(static r => r.GetProperty("status").GetString() is not ("Pending" or "Sending")))
            {
                return records;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException($"{kind} did not settle.");
    }

    private static async Task<string> PublishAsync(BasicAppTestServer server, string token, object draft)
    {
        var id = await CreatedAsync(server, "/api/notifications", draft, token);
        var (status, body) = await SendAsync(server, HttpMethod.Post, $"/api/notifications/{id}:publish", "{}", token);
        Assert.True(status == HttpStatusCode.OK, body.ToString());
        return id;
    }

    [Fact]
    public async Task Messages_RenderTemplates_AndTheOutboxSendsRetriesAndCancels()
    {
        var (server, platform, tenant) = await SettingsTestHost.StartAsync(_database.ConnectionString, configure: builder =>
        {
            builder.Configuration.AddInMemoryCollection(
            [
                new("BasicApp:Notification:Brand", "Acme"),
                new("BasicApp:Notification:DispatchInterval", "00:00:00.100"),
                new("BasicApp:Notification:RetryDelay", "00:00:00"),
                new("BasicApp:Notification:MaxAttempts", "2"),
            ]);
            builder.Services.AddSingleton<IEmailTransport>(_email);
            builder.Services.AddSingleton<ISmsTransport>(_sms);
        });
        await using var running = server;

        // 模板：平台种子 8 个；语法错误与同渠道同编码拒绝
        Assert.Equal(8, (await SendAsync(server, HttpMethod.Post, "/api/message-templates:list", "{}", platform)).Body.GetProperty("total").GetInt32());
        Assert.Equal("NOTIFICATION_VALIDATION_007", await CodeAsync(server, HttpMethod.Post, "/api/message-templates", new { code = "broken", channel = "Email", name = "Broken", content = "{{ if }}" }, platform));
        Assert.Equal("NOTIFICATION_CONFLICT_003", await CodeAsync(server, HttpMethod.Post, "/api/message-templates", new { code = "notification-email", channel = "Email", name = "Again", content = "x" }, platform));

        // 没有配置时发布带邮件渠道的通知不可用，通知仍是草稿
        var ann = await CreatedAsync(server, "/api/users", new { userName = "ann", displayName = "Ann", password = "Pass@123456", email = "ann@example.com", phoneNumber = "13800000000" }, platform);
        var bob = await CreatedAsync(server, "/api/users", new { userName = "bob", displayName = "Bob", password = "Pass@123456" }, platform);
        var early = await CreatedAsync(server, "/api/notifications", new { title = "Early", targetType = "User", targetIds = new[] { ann }, deliveryChannels = _withEmail }, platform);
        Assert.Equal("NOTIFICATION_UNAVAILABLE_003", await CodeAsync(server, HttpMethod.Post, $"/api/notifications/{early}:publish", "{}", platform));
        Assert.Equal("NOTIFICATION_VALIDATION_010", await CodeAsync(server, HttpMethod.Post, "/api/notifications", new { title = "Mail only", deliveryChannels = _emailOnly }, platform));

        // 渠道配置：第一个即缺省、密码不回显；地址与模板映射校验；缺省的不能删
        Assert.Equal("NOTIFICATION_VALIDATION_008", await CodeAsync(server, HttpMethod.Post, "/api/email-configs", new { code = "bad", name = "Bad", smtpHost = "smtp.example.com", fromEmail = "nope" }, platform));
        var (smtpStatus, smtp) = await SendAsync(server, HttpMethod.Post, "/api/email-configs", new { code = "primary", name = "Primary", smtpHost = "smtp.example.com", smtpPort = 465, fromEmail = "noreply@example.com", fromName = "Acme", userName = "noreply", password = "s3cret-pass" }, platform);
        Assert.Equal(HttpStatusCode.Created, smtpStatus);
        Assert.True(smtp.GetProperty("isDefault").GetBoolean());
        Assert.True(smtp.GetProperty("hasPassword").GetBoolean());
        Assert.DoesNotContain("s3cret-pass", smtp.ToString(), StringComparison.Ordinal);
        var backup = await CreatedAsync(server, "/api/email-configs", new { code = "backup", name = "Backup", smtpHost = "backup.example.com", fromEmail = "backup@example.com" }, platform);
        Assert.Equal("NOTIFICATION_CONFLICT_005", (await SendAsync(server, HttpMethod.Delete, $"/api/email-configs?ids={smtp.GetProperty("id").GetString()}", null, platform)).Body.GetProperty("failed")[0].GetProperty("code").GetString());
        Assert.Empty((await SendAsync(server, HttpMethod.Delete, $"/api/email-configs?ids={backup}", null, platform)).Body.GetProperty("failed").EnumerateArray());
        Assert.Equal("NOTIFICATION_VALIDATION_009", await CodeAsync(server, HttpMethod.Post, "/api/sms-configs", new { code = "ali", name = "Aliyun", provider = "Aliyun", accessKeyId = "id", accessKeySecret = "key", signName = "Acme", templateMap = "[1]" }, platform));
        var (aliStatus, ali) = await SendAsync(server, HttpMethod.Post, "/api/sms-configs", new { code = "ali", name = "Aliyun", provider = "Aliyun", accessKeyId = "id", accessKeySecret = "ali-secret", signName = "Acme", templateMap = """{"notification-sms":"SMS_0001"}""" }, platform);
        Assert.Equal(HttpStatusCode.Created, aliStatus);
        Assert.DoesNotContain("ali-secret", ali.ToString(), StringComparison.Ordinal);

        // 发布：有邮箱 / 手机号的接收人各排一条，循环用缺省配置发出；HTML 模板里的参数被转义，短信不把品牌名交给服务商
        await PublishAsync(server, platform, new { title = "Release", content = "a <b>bold</b> move", targetType = "User", targetIds = new[] { ann, bob }, deliveryChannels = _allChannels });
        var emails = await SettledAsync(server, platform, "emails", 1);
        Assert.Equal(("Success", "ann@example.com", "【Acme】Release", "notification-email"), (emails[0].GetProperty("status").GetString(), emails[0].GetProperty("toEmail").GetString(), emails[0].GetProperty("subject").GetString(), emails[0].GetProperty("templateCode").GetString()));
        var (server1, mail) = Assert.Single(_email.Sent);
        Assert.Equal(("smtp.example.com", 465, "noreply", "s3cret-pass"), (server1.Host, server1.Port, server1.UserName, server1.Password));
        Assert.True(mail.IsHtml);
        Assert.Contains("a &lt;b&gt;bold&lt;/b&gt; move", mail.Body, StringComparison.Ordinal);
        var sms = await SettledAsync(server, platform, "sms", 1);
        Assert.Equal(("Success", "13800000000", "Aliyun", "【Acme】您有新的通知：Release，详情请登录查看。"), (sms[0].GetProperty("status").GetString(), sms[0].GetProperty("toPhone").GetString(), sms[0].GetProperty("provider").GetString(), sms[0].GetProperty("content").GetString()));
        var (account, text) = Assert.Single(_sms.Sent);
        Assert.Equal(("ali-secret", "SMS_0001", "13800000000"), (account.AccessKeySecret, text.ProviderTemplateId, text.Phone));
        Assert.DoesNotContain(text.Parameters, static p => p.Key == "brand");
        Assert.Contains(text.Parameters, static p => p.Key == "title" && p.Value == "Release");

        // 失败：两次都失败即记为失败；重发后清零再发
        _email.FailNext(2);
        await PublishAsync(server, platform, new { title = "Flaky", targetType = "User", targetIds = new[] { ann }, deliveryChannels = _withEmail });
        var failed = (await SettledAsync(server, platform, "emails", 2)).Single(static e => e.GetProperty("subject").GetString() == "【Acme】Flaky");
        Assert.Equal(("Failed", 2, "smtp down"), (failed.GetProperty("status").GetString(), failed.GetProperty("attempts").GetInt32(), failed.GetProperty("errorMessage").GetString()));
        var failedId = failed.GetProperty("id").GetString()!;
        Assert.Single((await SendAsync(server, HttpMethod.Post, "/api/emails:resend", new { ids = new[] { failedId } }, platform)).Body.GetProperty("succeeded").EnumerateArray());
        var resent = (await SettledAsync(server, platform, "emails", 2)).Single(e => e.GetProperty("id").GetString() == failedId);
        Assert.Equal(("Success", 1), (resent.GetProperty("status").GetString(), resent.GetProperty("attempts").GetInt32()));
        Assert.Equal("NOTIFICATION_CONFLICT_007", (await SendAsync(server, HttpMethod.Post, "/api/emails:cancel", new { ids = new[] { failedId } }, platform)).Body.GetProperty("failed")[0].GetProperty("code").GetString());

        // 发送中的不能删也不能重发，排在后面的能取消
        var carl = await CreatedAsync(server, "/api/users", new { userName = "carl", displayName = "Carl", password = "Pass@123456", email = "carl@example.com" }, platform);
        _email.Hold();
        await PublishAsync(server, platform, new { title = "Queued", targetType = "User", targetIds = new[] { ann, carl }, deliveryChannels = _withEmail });
        List<JsonElement> queued = [];
        for (var attempt = 0; attempt < 150 && !queued.Any(static e => e.GetProperty("status").GetString() == "Sending"); attempt++)
        {
            await Task.Delay(100);
            queued = (await RecordsAsync(server, platform, "emails")).Where(static e => e.GetProperty("subject").GetString() == "【Acme】Queued").ToList();
        }

        var sending = queued.Single(static e => e.GetProperty("status").GetString() == "Sending").GetProperty("id").GetString()!;
        var waiting = queued.Single(static e => e.GetProperty("status").GetString() == "Pending").GetProperty("id").GetString()!;
        Assert.Equal("NOTIFICATION_CONFLICT_006", (await SendAsync(server, HttpMethod.Delete, $"/api/emails?ids={sending}", null, platform)).Body.GetProperty("failed")[0].GetProperty("code").GetString());
        Assert.Equal("NOTIFICATION_CONFLICT_006", (await SendAsync(server, HttpMethod.Post, "/api/emails:resend", new { ids = new[] { sending } }, platform)).Body.GetProperty("failed")[0].GetProperty("code").GetString());
        Assert.Single((await SendAsync(server, HttpMethod.Post, "/api/emails:cancel", new { ids = new[] { waiting } }, platform)).Body.GetProperty("succeeded").EnumerateArray());
        _email.Release();
        var after = await SettledAsync(server, platform, "emails", 4);
        Assert.Equal("Success", after.Single(e => e.GetProperty("id").GetString() == sending).GetProperty("status").GetString());
        Assert.Equal("Cancelled", after.Single(e => e.GetProperty("id").GetString() == waiting).GetProperty("status").GetString());
        Assert.Equal(3, _email.Sent.Count);
        Assert.Empty((await SendAsync(server, HttpMethod.Delete, $"/api/emails?ids={waiting}", null, platform)).Body.GetProperty("failed").EnumerateArray());
        Assert.Equal(3, (await RecordsAsync(server, platform, "emails")).Count);

        // 租户一侧：同编码模板覆盖平台的，没有自己的配置即用平台缺省；记录只看本侧
        Assert.Equal(0, (await SendAsync(server, HttpMethod.Post, "/api/message-templates:list", "{}", tenant)).Body.GetProperty("total").GetInt32());
        await CreatedAsync(server, "/api/message-templates", new { code = "notification-email", channel = "Email", name = "Ours", subject = "[{{brand}}] {{title}}", content = "Hi: {{content}}" }, tenant);
        var dana = await CreatedAsync(server, "/api/users", new { userName = "dana", displayName = "Dana", password = "Pass@123456", email = "dana@example.com" }, tenant);
        await PublishAsync(server, tenant, new { title = "Team", content = "<hello>", targetType = "User", targetIds = new[] { dana }, deliveryChannels = _withEmail });
        var tenantEmails = await SettledAsync(server, tenant, "emails", 1);
        Assert.Equal(("Success", "[Acme] Team"), (tenantEmails[0].GetProperty("status").GetString(), tenantEmails[0].GetProperty("subject").GetString()));
        var (_, tenantMail) = _email.Sent.Last();
        Assert.Equal(("dana@example.com", "Hi: <hello>", false), (tenantMail.To, tenantMail.Body, tenantMail.IsHtml));
        Assert.Equal("NOTIFICATION_NOTFOUND_006", await CodeAsync(server, HttpMethod.Get, $"/api/emails/{sending}", null, tenant));
        Assert.Empty(await RecordsAsync(server, tenant, "sms"));
        Assert.Equal(3, (await RecordsAsync(server, platform, "emails")).Count);
    }

    private sealed class FakeEmailTransport : IEmailTransport
    {
        private int _failures;
        private TaskCompletionSource? _gate;

        public ConcurrentQueue<(EmailServer Server, EmailEnvelope Envelope)> Sent { get; } = new();

        public void FailNext(int count) => Volatile.Write(ref _failures, count);

        public void Hold() => _gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Release() => Interlocked.Exchange(ref _gate, null)?.TrySetResult();

        public async Task<TransportResult> SendAsync(EmailServer server, EmailEnvelope envelope, CancellationToken cancellationToken)
        {
            if (_gate is { } gate)
            {
                await gate.Task.WaitAsync(cancellationToken);
            }

            if (Volatile.Read(ref _failures) > 0 && Interlocked.Decrement(ref _failures) >= 0)
            {
                return TransportResult.Failed("smtp down");
            }

            Sent.Enqueue((server, envelope));
            return TransportResult.Sent(null);
        }
    }

    private sealed class FakeSmsTransport : ISmsTransport
    {
        public SmsProvider Provider => SmsProvider.Aliyun;

        public ConcurrentQueue<(SmsAccount Account, SmsEnvelope Envelope)> Sent { get; } = new();

        public Task<TransportResult> SendAsync(SmsAccount account, SmsEnvelope envelope, CancellationToken cancellationToken)
        {
            Sent.Enqueue((account, envelope));
            return Task.FromResult(TransportResult.Sent("biz-1"));
        }
    }
}
