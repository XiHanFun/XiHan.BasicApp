// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using XiHan.BasicApp.Integration.Tests.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证租户配额与订阅。
/// </summary>
/// <remarks>
/// 套餐的价格、周期与配额（免费套餐不能标价），租户自己的上限覆盖套餐的；租户一侧建用户占席位、上传与秒传占存储，超额即拒绝，
/// 平台代租户建用户不受限（超额只在配额审计里报告）；我的订阅只在租户侧，配额审计只在平台侧。
/// </remarks>
public sealed class TenantQuotaTests : IDisposable
{
    private readonly SqliteDatabase _database = new();
    private readonly string _root = Path.Combine(Path.GetTempPath(), "xihan-quota-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        _database.Dispose();
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, string? json, string? token)
    {
        var (response, body) = await server.SendAsync(BasicAppTestServer.Request(method, path, json, token));
        return (response.StatusCode, body);
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> UploadAsync(BasicAppTestServer server, string token, string name, byte[] content)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(content), "file", name);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/files/upload") { Content = form };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var (response, body) = await server.SendAsync(request);
        return (response.StatusCode, body);
    }

    private static async Task<JsonElement> QuotaOfAsync(BasicAppTestServer server, string platform, bool overOnly = false)
        => (await SendAsync(server, HttpMethod.Get, $"/api/tenants:quotas?overOnly={(overOnly ? "true" : "false")}", null, platform)).Body.GetProperty("items");

    [Fact]
    public async Task Quotas_LimitTenantSeatsAndStorage_AndSubscriptionShowsThem()
    {
        var (server, platform, tenant) = await SettingsTestHost.StartAsync(_database.ConnectionString, configure: builder => builder.Configuration.AddInMemoryCollection([new("BasicApp:Files:LocalRootPath", _root)]));
        await using var running = server;

        // 套餐：免费套餐不能标价；价格保留两位小数
        Assert.Equal("TENANCY_VALIDATION_004", (await SendAsync(server, HttpMethod.Post, "/api/editions", """{"code":"gift","name":"Gift","isFree":true,"price":1}""", platform)).Body.GetProperty("code").GetString());
        var permissions = new[] { "identity.users.read", "identity.users.create", "files.files.read", "files.files.create", "tenancy.subscription.read" };
        var (created, tiny) = await SendAsync(server, HttpMethod.Post, "/api/editions", JsonSerializer.Serialize(new { code = "tiny", name = "Tiny", userLimit = 2, storageLimitMb = 1, price = 9.999m, billingPeriodMonths = 1, permissions }), platform);
        Assert.Equal(HttpStatusCode.Created, created);
        tiny = tiny.GetProperty("edition");
        Assert.Equal((2, 1L, 10.00m, 1), (tiny.GetProperty("userLimit").GetInt32(), tiny.GetProperty("storageLimitMb").GetInt64(), tiny.GetProperty("price").GetDecimal(), tiny.GetProperty("billingPeriodMonths").GetInt32()));
        var contoso = (await SendAsync(server, HttpMethod.Post, "/api/tenants:list", """{"page":{"index":1,"size":10}}""", platform)).Body.GetProperty("items").EnumerateArray().Single(t => t.GetProperty("tenantId").GetString() == "contoso").GetProperty("id").GetString()!;
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, $"/api/tenants/{contoso}:change-edition", JsonSerializer.Serialize(new { editionId = tiny.GetProperty("id").GetString() }), platform)).Status);

        // 我的订阅：只在租户侧
        var (subscribed, subscription) = await SendAsync(server, HttpMethod.Get, "/api/subscription", null, tenant);
        Assert.Equal(HttpStatusCode.OK, subscribed);
        Assert.Equal(("tiny", 2, 1L, 1L, 0L, false), (subscription.GetProperty("editionCode").GetString(), subscription.GetProperty("userLimit").GetInt32(), subscription.GetProperty("usedSeats").GetInt64(), subscription.GetProperty("storageLimitMb").GetInt64(), subscription.GetProperty("usedStorageBytes").GetInt64(), subscription.GetProperty("isExpired").GetBoolean()));
        var (platformSide, platformSideBody) = await SendAsync(server, HttpMethod.Get, "/api/subscription", null, platform);
        Assert.Equal((HttpStatusCode.Forbidden, "TENANCY_DENIED_002"), (platformSide, platformSideBody.GetProperty("code").GetString()));

        // 席位：租户一侧第二个用户可以，第三个超额；平台代租户建用户不受限，超额进审计
        Assert.Equal(HttpStatusCode.Created, (await SendAsync(server, HttpMethod.Post, "/api/users", """{"userName":"ann","displayName":"Ann","password":"Pass@123456"}""", tenant)).Status);
        var (full, fullBody) = await SendAsync(server, HttpMethod.Post, "/api/users", """{"userName":"bob","displayName":"Bob","password":"Pass@123456"}""", tenant);
        Assert.Equal((HttpStatusCode.Conflict, "TENANCY_CONFLICT_009"), (full, fullBody.GetProperty("code").GetString()));
        Assert.Equal(HttpStatusCode.Created, (await SendAsync(server, HttpMethod.Post, "/api/users", """{"userName":"cat","displayName":"Cat","password":"Pass@123456","homeTenantId":"contoso"}""", platform)).Status);
        var over = Assert.Single((await QuotaOfAsync(server, platform, overOnly: true)).EnumerateArray());
        Assert.Equal(("contoso", 3L, true, false), (over.GetProperty("tenantId").GetString(), over.GetProperty("usedSeats").GetInt64(), over.GetProperty("isOverSeats").GetBoolean(), over.GetProperty("isOverStorage").GetBoolean()));

        // 租户自己的上限覆盖套餐的
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Put, $"/api/tenants/{contoso}", """{"name":"Contoso","userLimit":10}""", platform)).Status);
        Assert.Empty((await QuotaOfAsync(server, platform, overOnly: true)).EnumerateArray());
        Assert.Equal(HttpStatusCode.Created, (await SendAsync(server, HttpMethod.Post, "/api/users", """{"userName":"bob","displayName":"Bob","password":"Pass@123456"}""", tenant)).Status);
        var detail = (await SendAsync(server, HttpMethod.Get, $"/api/tenants/{contoso}", null, platform)).Body;
        Assert.Equal((10, 4L), (detail.GetProperty("quota").GetProperty("userLimit").GetInt32(), detail.GetProperty("quota").GetProperty("usedSeats").GetInt64()));

        // 存储：1 MB 上限，600 KB 可以；秒传同样内容也计入，超额拒绝；再上传也拒绝
        var content = RandomNumberGenerator.GetBytes(600 * 1024);
        Assert.Equal(HttpStatusCode.OK, (await UploadAsync(server, tenant, "a.bin", content)).Status);
        var (fast, fastBody) = await SendAsync(server, HttpMethod.Post, "/api/files:fast-upload", JsonSerializer.Serialize(new { hash = Convert.ToHexStringLower(SHA256.HashData(content)), size = content.Length, originalName = "b.bin" }), tenant);
        Assert.Equal((HttpStatusCode.Conflict, "TENANCY_CONFLICT_008"), (fast, fastBody.GetProperty("code").GetString()));
        Assert.Equal("TENANCY_CONFLICT_008", (await UploadAsync(server, tenant, "c.bin", RandomNumberGenerator.GetBytes(600 * 1024))).Body.GetProperty("code").GetString());
        Assert.Equal(600L * 1024, (await SendAsync(server, HttpMethod.Get, "/api/subscription", null, tenant)).Body.GetProperty("usedStorageBytes").GetInt64());
        var usage = (await QuotaOfAsync(server, platform)).EnumerateArray().Single(q => q.GetProperty("tenantId").GetString() == "contoso");
        Assert.Equal((600L * 1024, 1L, "tiny"), (usage.GetProperty("usedStorageBytes").GetInt64(), usage.GetProperty("storageLimitMb").GetInt64(), usage.GetProperty("editionCode").GetString()));

        // 平台的上传不受租户配额约束
        Assert.Equal(HttpStatusCode.OK, (await UploadAsync(server, platform, "p.bin", RandomNumberGenerator.GetBytes(700 * 1024))).Status);
    }
}
