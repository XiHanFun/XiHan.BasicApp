// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using XiHan.BasicApp.Integration.Tests.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证运行时作业的新建与删除。
/// </summary>
/// <remarks>
/// 按已登记的作业类型运行时登记（来源 <c>Runtime</c>），同名、名不合规、参数不是 JSON 对象、类型不认识各自报错；
/// 只删运行时登记的，代码登记的逐项拒绝；只在平台一侧。
/// </remarks>
public sealed class MaintenanceJobRegistrationTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, object? body, string? token)
    {
        var (response, json) = await server.SendAsync(BasicAppTestServer.Request(method, path, body is null ? null : JsonSerializer.Serialize(body), token));
        return (response.StatusCode, json);
    }

    [Fact]
    public async Task RuntimeJobs_AreCreatedFromRegisteredTypes_AndOnlyTheyCanBeDeleted()
    {
        var (server, platform, tenant) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        var (_, types) = await SendAsync(server, HttpMethod.Get, "/api/jobs:types", null, platform);
        var jobType = types.GetProperty("items")[0].GetString()!;

        var (created, job) = await SendAsync(server, HttpMethod.Post, "/api/jobs", new { name = "reports.daily", jobType, expression = "0 30 2 * * ?", timeZone = "Asia/Shanghai", data = """{"days":7}""", description = "日报" }, platform);
        Assert.Equal(HttpStatusCode.Created, created);
        Assert.Equal(("reports.daily", "Runtime", jobType), (job.GetProperty("name").GetString(), job.GetProperty("source").GetString(), job.GetProperty("jobType").GetString()));
        var trigger = Assert.Single(job.GetProperty("triggers").EnumerateArray());
        Assert.Equal(("Cron", "Asia/Shanghai"), (trigger.GetProperty("kind").GetString(), trigger.GetProperty("timeZone").GetString()));
        Assert.Equal(JsonValueKind.Null, job.GetProperty("retry").ValueKind);
        Assert.Equal(JsonValueKind.Null, job.GetProperty("timeoutSeconds").ValueKind);

        // 重试与执行时限：新建带上；修改计划按请求整体替换（不填即不重试、不限时），只给次数时间隔取缺省 60 秒
        var (_, retried) = await SendAsync(server, HttpMethod.Post, "/api/jobs", new { name = "reports.retried", jobType, expression = "every 01:00:00", timeoutSeconds = 120, maxRetries = 3, retryDelaySeconds = 30, retryBackoff = "Exponential", retryMaxDelaySeconds = 600 }, platform);
        Assert.Equal(120, retried.GetProperty("timeoutSeconds").GetInt64());
        var policy = retried.GetProperty("retry");
        Assert.Equal((3, 30L, "Exponential", 600L), (policy.GetProperty("maxRetries").GetInt32(), policy.GetProperty("delaySeconds").GetInt64(), policy.GetProperty("backoff").GetString(), policy.GetProperty("maxDelaySeconds").GetInt64()));
        var (_, cleared) = await SendAsync(server, HttpMethod.Post, "/api/jobs/reports.retried:reschedule", new { expression = "every 02:00:00" }, platform);
        Assert.Equal((JsonValueKind.Null, JsonValueKind.Null), (cleared.GetProperty("retry").ValueKind, cleared.GetProperty("timeoutSeconds").ValueKind));
        var (_, fixedRetry) = await SendAsync(server, HttpMethod.Post, "/api/jobs/reports.retried:reschedule", new { expression = "every 02:00:00", maxRetries = 2 }, platform);
        var fixedPolicy = fixedRetry.GetProperty("retry");
        Assert.Equal((2, 60L, "Fixed", JsonValueKind.Null), (fixedPolicy.GetProperty("maxRetries").GetInt32(), fixedPolicy.GetProperty("delaySeconds").GetInt64(), fixedPolicy.GetProperty("backoff").GetString(), fixedPolicy.GetProperty("maxDelaySeconds").ValueKind));
        var (_, reread) = await SendAsync(server, HttpMethod.Get, "/api/jobs/reports.retried", null, platform);
        Assert.Equal(2, reread.GetProperty("retry").GetProperty("maxRetries").GetInt32());
        Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(server, HttpMethod.Post, "/api/jobs", new { name = "reports.bad", jobType, expression = "every 01:00:00", maxRetries = 0 }, platform)).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(server, HttpMethod.Post, "/api/jobs", new { name = "reports.bad", jobType, expression = "every 01:00:00", timeoutSeconds = 2_592_001 }, platform)).Status);

        // 各种不合规
        Assert.Equal("MAINTENANCE_CONFLICT_003", (await SendAsync(server, HttpMethod.Post, "/api/jobs", new { name = "reports.daily", jobType, expression = "every 01:00:00" }, platform)).Body.GetProperty("code").GetString());
        Assert.Equal("MAINTENANCE_VALIDATION_004", (await SendAsync(server, HttpMethod.Post, "/api/jobs", new { name = "Bad Name", jobType, expression = "every 01:00:00" }, platform)).Body.GetProperty("code").GetString());
        Assert.Equal("MAINTENANCE_VALIDATION_005", (await SendAsync(server, HttpMethod.Post, "/api/jobs", new { name = "reports.weekly", jobType, expression = "every 01:00:00", data = "[1,2]" }, platform)).Body.GetProperty("code").GetString());
        Assert.Equal("MAINTENANCE_VALIDATION_002", (await SendAsync(server, HttpMethod.Post, "/api/jobs", new { name = "reports.weekly", jobType, expression = "not a schedule" }, platform)).Body.GetProperty("code").GetString());
        Assert.Equal("MAINTENANCE_CONFLICT_001", (await SendAsync(server, HttpMethod.Post, "/api/jobs", new { name = "reports.weekly", jobType = "nope", expression = "every 01:00:00" }, platform)).Body.GetProperty("code").GetString());

        // 删除：运行时的删掉，代码登记的与不存在的逐项拒绝
        var (deleted, result) = await SendAsync(server, HttpMethod.Delete, "/api/jobs?ids=reports.daily&ids=reports.retried&ids=auditing.retention&ids=nope.job", null, platform);
        Assert.Equal(HttpStatusCode.OK, deleted);
        Assert.Equal(["reports.daily", "reports.retried"], result.GetProperty("succeeded").EnumerateArray().Select(static e => e.GetString()));
        var failed = result.GetProperty("failed").EnumerateArray().ToDictionary(static f => f.GetProperty("id").GetString()!, static f => f.GetProperty("code").GetString());
        Assert.Equal("MAINTENANCE_CONFLICT_004", failed["auditing.retention"]);
        Assert.Equal("MAINTENANCE_NOTFOUND_001", failed["nope.job"]);
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(server, HttpMethod.Get, "/api/jobs/reports.daily", null, platform)).Status);

        // 只在平台一侧
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(server, HttpMethod.Post, "/api/jobs", new { name = "tenant.job", jobType, expression = "every 01:00:00" }, tenant)).Status);
    }
}
