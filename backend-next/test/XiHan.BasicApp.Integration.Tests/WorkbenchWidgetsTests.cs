// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Auditing.Domain.Services;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.Framework.Auditing.Database.Trail;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证工作台小组件。
/// </summary>
/// <remarks>
/// 各包登记的小组件按调用方权限列出（平台侧的作业执行租户看不到），取数先核权限再交给登记它的包；
/// 日志类按查看者的本地日统计（偏移由前端带来），构成只列前几项。
/// </remarks>
public sealed class WorkbenchWidgetsTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static async Task<(HttpStatusCode Status, JsonElement Body)> GetAsync(BasicAppTestServer server, string path, string? token)
    {
        var (response, json) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, path, token: token));
        return (response.StatusCode, json);
    }

    private static async Task<JsonElement> DataAsync(BasicAppTestServer server, string key, string token, int days = 7, int offset = 0)
    {
        var (status, body) = await GetAsync(server, $"/api/workbench:widget-data?key={key}&days={days}&offset={offset}", token);
        Assert.Equal(HttpStatusCode.OK, status);
        return body;
    }

    private static List<long> Line(JsonElement data, string key)
        => data.GetProperty("series").GetProperty("lines").EnumerateArray().Single(l => l.GetProperty("key").GetString() == key).GetProperty("values").EnumerateArray().Select(static v => v.ValueKind == JsonValueKind.String ? long.Parse(v.GetString()!, CultureInfo.InvariantCulture) : v.GetInt64()).ToList();

    private static long Number(JsonElement value)
        => value.ValueKind == JsonValueKind.String ? long.Parse(value.GetString()!, CultureInfo.InvariantCulture) : value.GetInt64();

    [Fact]
    public async Task Widgets_AreListedByPermission_AndRejectUnknownOrForbiddenKeys()
    {
        var (server, platform, tenant) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;

        var (_, platformList) = await GetAsync(server, "/api/workbench:widgets", platform);
        var platformKeys = platformList.GetProperty("items").EnumerateArray().ToDictionary(static w => w.GetProperty("key").GetString()!, static w => w.GetProperty("kind").GetString());
        Assert.Equal("Metric", platformKeys["auditing.logins-today"]);
        Assert.Equal("Chart", platformKeys["auditing.login-trend"]);
        Assert.Equal("List", platformKeys["notification.announcements"]);
        Assert.Contains("identity.online-users", platformKeys.Keys);
        Assert.Contains("files.storage-mix", platformKeys.Keys);
        Assert.Contains("maintenance.job-runs", platformKeys.Keys);
        Assert.Equal(platformKeys.Keys.Order(StringComparer.Ordinal), platformKeys.Keys);

        // 作业只在平台一侧：租户看不到也取不到
        var (_, tenantList) = await GetAsync(server, "/api/workbench:widgets", tenant);
        var tenantKeys = tenantList.GetProperty("items").EnumerateArray().Select(static w => w.GetProperty("key").GetString()).ToList();
        Assert.DoesNotContain("maintenance.job-runs", tenantKeys);
        Assert.Contains("auditing.login-trend", tenantKeys);
        var (forbidden, forbiddenBody) = await GetAsync(server, "/api/workbench:widget-data?key=maintenance.job-runs", tenant);
        Assert.Equal((HttpStatusCode.Forbidden, "WORKBENCH_DENIED_001"), (forbidden, forbiddenBody.GetProperty("code").GetString()));
        var (missing, missingBody) = await GetAsync(server, "/api/workbench:widget-data?key=nothing.here", platform);
        Assert.Equal((HttpStatusCode.NotFound, "WORKBENCH_NOTFOUND_001"), (missing, missingBody.GetProperty("code").GetString()));
        Assert.Equal(HttpStatusCode.Unauthorized, (await GetAsync(server, "/api/workbench:widgets", null)).Status);
    }

    [Fact]
    public async Task WidgetData_CountsTheCallersSide_ByTheViewersLocalDay()
    {
        var (server, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        var (wrong, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", """{"userName":"superadmin","password":"nope-nope"}"""));
        Assert.Equal(HttpStatusCode.Forbidden, wrong.StatusCode);
        await server.Services.GetRequiredService<DatabaseAuditTrail>().FlushAsync(CancellationToken.None);
        await server.Services.GetRequiredService<AuditLogWriter>().FlushAsync(CancellationToken.None);

        // 今日登录：成功的登录次数，带昨天的作对比
        var logins = await DataAsync(server, "auditing.logins-today", platform, offset: 480);
        Assert.True(Number(logins.GetProperty("value")) >= 1);
        Assert.Equal(0, Number(logins.GetProperty("previous")));

        // 登录趋势：按东八区的本地日分桶，最后一天是今天
        var trend = await DataAsync(server, "auditing.login-trend", platform, days: 7, offset: 480);
        var days = trend.GetProperty("series").GetProperty("categories").EnumerateArray().Select(static c => c.GetString()).ToList();
        Assert.Equal(7, days.Count);
        Assert.Equal(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), days[^1]);
        Assert.True(Line(trend, "succeeded")[^1] >= 1);
        Assert.True(Line(trend, "failed")[^1] >= 1);

        // 登录失败原因：错误码带上失败原因作显示名
        var failures = (await DataAsync(server, "auditing.login-failures", platform)).GetProperty("slices").EnumerateArray().ToList();
        Assert.Contains(failures, s => s.GetProperty("key").GetString() == "IDENTITY_DENIED_003" && !string.IsNullOrEmpty(s.GetProperty("label").GetString()));

        // 访问：今日访问、逐日概况、星期与小时的热力、访问最多的操作
        Assert.True(Number((await DataAsync(server, "auditing.requests-today", platform)).GetProperty("value")) >= 1);
        var kpi = await DataAsync(server, "auditing.request-kpi", platform);
        Assert.Equal(7, Line(kpi, "total").Count);
        Assert.True(Line(kpi, "total").Sum() >= 1);
        Assert.Equal(7, Line(kpi, "average_ms").Count);
        var heatmap = (await DataAsync(server, "auditing.request-heatmap", platform)).GetProperty("series");
        Assert.Equal(24, heatmap.GetProperty("categories").GetArrayLength());
        Assert.Equal(["1", "2", "3", "4", "5", "6", "7"], heatmap.GetProperty("lines").EnumerateArray().Select(static l => l.GetProperty("key").GetString()));
        Assert.True(heatmap.GetProperty("lines").EnumerateArray().SelectMany(static l => l.GetProperty("values").EnumerateArray()).Sum(Number) >= 1);
        var operations = await DataAsync(server, "auditing.top-operations", platform);
        Assert.InRange(operations.GetProperty("series").GetProperty("categories").GetArrayLength(), 1, 10);
        Assert.Equal(Line(operations, "count").OrderDescending(), Line(operations, "count"));
        Assert.Equal(JsonValueKind.Array, (await DataAsync(server, "auditing.exception-types", platform)).GetProperty("slices").ValueKind);

        // 身份：没有实时连接即在线为 0；有效会话按设备的构成
        Assert.Equal(0, Number((await DataAsync(server, "identity.online-users", platform)).GetProperty("value")));
        Assert.NotEmpty((await DataAsync(server, "identity.session-devices", platform)).GetProperty("slices").EnumerateArray());

        // 文件、作业、通知
        Assert.Equal(JsonValueKind.Array, (await DataAsync(server, "files.storage-mix", platform)).GetProperty("slices").ValueKind);
        var runs = await DataAsync(server, "maintenance.job-runs", platform, days: 14);
        Assert.Equal(14, Line(runs, "succeeded").Count);
        Assert.Equal(14, Line(runs, "failed").Count);
        Assert.Equal(JsonValueKind.Array, (await DataAsync(server, "notification.announcements", platform)).GetProperty("items").ValueKind);
        var deliveries = (await DataAsync(server, "notification.deliveries", platform)).GetProperty("series");
        Assert.Equal(["email", "sms"], deliveries.GetProperty("categories").EnumerateArray().Select(static c => c.GetString()));
    }
}
