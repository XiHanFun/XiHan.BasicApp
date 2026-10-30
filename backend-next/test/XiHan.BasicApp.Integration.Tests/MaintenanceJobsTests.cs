// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using XiHan.BasicApp.Integration.Tests.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证作业的管理面。
/// </summary>
/// <remarks>
/// 宿主用数据库调度：代码登记的作业都列得出，带按区域的显示名与计划；暂停与恢复按作业名批量、认不出的算失败；
/// 立即执行排进一次性触发器、返回触发器名；计划校验接受 Cron 表达式或计划写法，写错即报错。
/// </remarks>
public sealed class MaintenanceJobsTests : IDisposable
{
    private static readonly string[] _pauseTargets = ["auditing.retention", "nope.job"];
    private static readonly string[] _retention = ["auditing.retention"];

    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, object? body, string? token)
    {
        var (response, json) = await server.SendAsync(BasicAppTestServer.Request(method, path, body is null ? null : JsonSerializer.Serialize(body), token));
        return (response.StatusCode, json);
    }

    private static JsonElement JobOf(JsonElement page, string name)
        => page.GetProperty("items").EnumerateArray().Single(j => j.GetProperty("name").GetString() == name);

    [Fact]
    public async Task Jobs_AreListed_PausedResumedTriggered_AndSchedulesValidated()
    {
        var (server, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;

        // 三个包代码登记的作业都在，显示名取登记包的文案
        var (listed, page) = await SendAsync(server, HttpMethod.Post, "/api/jobs:list", new { page = new { index = 1, size = 50 } }, platform);
        Assert.Equal(HttpStatusCode.OK, listed);
        var retention = JobOf(page, "auditing.retention");
        Assert.Equal("Log retention cleanup", retention.GetProperty("title").GetString());
        Assert.Equal("Code", retention.GetProperty("source").GetString());
        Assert.False(retention.GetProperty("paused").GetBoolean());
        var trigger = Assert.Single(retention.GetProperty("triggers").EnumerateArray());
        Assert.Equal("Cron", trigger.GetProperty("kind").GetString());
        Assert.NotEqual(JsonValueKind.Null, retention.GetProperty("nextRunAt").ValueKind);
        Assert.Equal("Expire multipart uploads", JobOf(page, "files.uploads.expire").GetProperty("title").GetString());
        Assert.Equal("Clean up expired OAuth grants", JobOf(page, "openplatform.oauth-cleanup").GetProperty("title").GetString());

        // 暂停：认不出的作业算失败；按 paused 筛选；取单个；恢复
        var (_, paused) = await SendAsync(server, HttpMethod.Post, "/api/jobs:pause", new { ids = _pauseTargets }, platform);
        Assert.Equal(["auditing.retention"], paused.GetProperty("succeeded").EnumerateArray().Select(static e => e.GetString()));
        Assert.Equal("MAINTENANCE_NOTFOUND_001", Assert.Single(paused.GetProperty("failed").EnumerateArray()).GetProperty("code").GetString());
        var (_, pausedPage) = await SendAsync(server, HttpMethod.Post, "/api/jobs:list", new { filters = new[] { new { field = "paused", @operator = "eq", value = "true" } } }, platform);
        Assert.Equal("auditing.retention", Assert.Single(pausedPage.GetProperty("items").EnumerateArray()).GetProperty("name").GetString());
        var (got, job) = await SendAsync(server, HttpMethod.Get, "/api/jobs/auditing.retention", null, platform);
        Assert.Equal(HttpStatusCode.OK, got);
        Assert.True(job.GetProperty("paused").GetBoolean());
        Assert.Equal(JsonValueKind.Null, job.GetProperty("nextRunAt").ValueKind);
        await SendAsync(server, HttpMethod.Post, "/api/jobs:resume", new { ids = _retention }, platform);
        Assert.False((await SendAsync(server, HttpMethod.Get, "/api/jobs/auditing.retention", null, platform)).Body.GetProperty("paused").GetBoolean());
        Assert.Equal("MAINTENANCE_NOTFOUND_001", (await SendAsync(server, HttpMethod.Get, "/api/jobs/nope.job", null, platform)).Body.GetProperty("code").GetString());
        Assert.Equal("MAINTENANCE_VALIDATION_001", (await SendAsync(server, HttpMethod.Post, "/api/jobs:list", new { filters = new[] { new { field = "title", @operator = "eq", value = "x" } } }, platform)).Body.GetProperty("code").GetString());

        // 立即执行：返回一次性触发器名
        var (triggered, run) = await SendAsync(server, HttpMethod.Post, "/api/jobs/openplatform.oauth-cleanup:trigger", new { }, platform);
        Assert.Equal(HttpStatusCode.OK, triggered);
        Assert.False(string.IsNullOrEmpty(run.GetProperty("trigger").GetString()));
        Assert.Equal("MAINTENANCE_NOTFOUND_001", (await SendAsync(server, HttpMethod.Post, "/api/jobs/nope.job:trigger", new { }, platform)).Body.GetProperty("code").GetString());

        // 计划校验：Cron 表达式与时区、计划写法；写错即报错
        var (checkedCron, check) = await SendAsync(server, HttpMethod.Get, "/api/jobs:validate-schedule?expression=0%203%20*%20*%20*&timeZone=Asia/Shanghai&count=3", null, platform);
        Assert.Equal(HttpStatusCode.OK, checkedCron);
        Assert.Equal("Cron", check.GetProperty("kind").GetString());
        Assert.Equal(3, check.GetProperty("nextRuns").GetArrayLength());
        Assert.Contains("Asia/Shanghai", check.GetProperty("schedule").GetString(), StringComparison.Ordinal);
        var (_, every) = await SendAsync(server, HttpMethod.Get, "/api/jobs:validate-schedule?expression=every%2001:00:00", null, platform);
        Assert.Equal("Interval", every.GetProperty("kind").GetString());
        Assert.Equal(5, every.GetProperty("nextRuns").GetArrayLength());
        Assert.Equal("MAINTENANCE_VALIDATION_002", (await SendAsync(server, HttpMethod.Get, "/api/jobs:validate-schedule?expression=61%20*%20*%20*%20*", null, platform)).Body.GetProperty("code").GetString());
        Assert.Equal("MAINTENANCE_VALIDATION_002", (await SendAsync(server, HttpMethod.Get, "/api/jobs:validate-schedule?expression=0%203%20*%20*%20*&timeZone=Nowhere/Zone", null, platform)).Body.GetProperty("code").GetString());

        // 没有权限
        Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(server, HttpMethod.Post, "/api/jobs:list", new { }, null)).Status);
    }
}
