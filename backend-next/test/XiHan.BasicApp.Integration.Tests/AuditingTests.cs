// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Auditing.Domain.Entities;
using XiHan.BasicApp.Auditing.Domain.Services;
using XiHan.BasicApp.Auditing.Jobs;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.Framework.Auditing.Database.Trail;
using XiHan.Framework.Kernel.Diagnostics;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;
using XiHan.Framework.Scheduling.Abstractions.Jobs;
using XiHan.Framework.Scheduling.Abstractions.Schedulers;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 审计日志：日志严格按上下文隔离（平台主体看平台一侧，租户主体看本租户）；登录日志成败都记、找不到的租户落平台一侧；
/// 访问日志每次操作调用一条、结果按错误类别归并；操作日志只记标了审计的操作并带上中间件补的细节；权限变更只在真正生效时记；
/// 链路按 trace-id 与会话聚合；未处理异常进异常日志；查询拒绝白名单外的筛选；保留清理删过期的分表日志与操作审计、参数不合法即失败。
/// </summary>
public sealed class AuditingTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    /// <summary>
    /// 把两条后台队列（分表日志、框架操作审计）都刷完，再查。
    /// </summary>
    private static async Task FlushAsync(BasicAppTestServer server)
    {
        await server.Services.GetRequiredService<DatabaseAuditTrail>().FlushAsync(CancellationToken.None);
        await server.Services.GetRequiredService<AuditLogWriter>().FlushAsync(CancellationToken.None);
    }

    private static async Task<List<JsonElement>> ListAsync(BasicAppTestServer server, string resource, string token, string body = """{"page":{"index":1,"size":200}}""")
    {
        var (response, json) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/{resource}:list", body, token));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return json.GetProperty("items").EnumerateArray().ToList();
    }

    [Fact]
    public async Task Logs_StayOnTheCallersSide_AndRecordLoginsAccessAndOperations()
    {
        var (server, platform, tenant) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;

        // 登录失败也记：密码错、找不到的租户（落平台一侧，账号所在租户留原值）
        var (wrong, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", """{"userName":"superadmin","password":"nope-nope"}"""));
        Assert.Equal(HttpStatusCode.Forbidden, wrong.StatusCode);
        var (nowhere, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", """{"userName":"admin","password":"Admin@123456","tenant":"nowhere"}"""));
        Assert.Equal(HttpStatusCode.Forbidden, nowhere.StatusCode);
        // 没带令牌的调用：访问日志记未认证
        var (anonymous, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:list", """{"page":{"index":1,"size":5}}"""));
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        await FlushAsync(server);

        var platformLogins = await ListAsync(server, "login-logs", platform);
        Assert.Contains(platformLogins, l => l.GetProperty("event").GetString() == "Login" && l.GetProperty("succeeded").GetBoolean() && l.GetProperty("userName").GetString() == "superadmin" && l.GetProperty("sessionId").ValueKind == JsonValueKind.String);
        Assert.Contains(platformLogins, l => l.GetProperty("event").GetString() == "Login" && !l.GetProperty("succeeded").GetBoolean() && l.GetProperty("errorCode").GetString() == "IDENTITY_DENIED_003" && l.GetProperty("userId").GetString() is not null);
        Assert.Contains(platformLogins, l => !l.GetProperty("succeeded").GetBoolean() && l.GetProperty("accountTenantId").GetString() == "nowhere");
        Assert.DoesNotContain(platformLogins, l => l.GetProperty("accountTenantId").GetString() == "contoso" && l.GetProperty("succeeded").GetBoolean());

        // 租户管理员的登录在租户一侧，平台的看不到
        var tenantLogins = await ListAsync(server, "login-logs", tenant);
        var adminLogin = Assert.Single(tenantLogins, l => l.GetProperty("event").GetString() == "Login");
        Assert.Equal("admin", adminLogin.GetProperty("userName").GetString());
        Assert.Equal("contoso", adminLogin.GetProperty("accountTenantId").GetString());
        Assert.DoesNotContain(tenantLogins, l => l.GetProperty("userName").GetString() == "superadmin");

        // 访问日志：每次操作调用一条，结果归并
        var platformAccess = await ListAsync(server, "access-logs", platform);
        Assert.Contains(platformAccess, a => a.GetProperty("operationId").GetString() == "tenants.create" && a.GetProperty("result").GetString() == "Success" && a.GetProperty("userName").GetString() == "superadmin");
        Assert.Contains(platformAccess, a => a.GetProperty("operationId").GetString() == "users.list" && a.GetProperty("result").GetString() == "Unauthorized" && a.GetProperty("errorCode").GetString() == "SECURITY_DENIED_001");
        Assert.Contains(platformAccess, a => a.GetProperty("operationId").GetString() == "auth.login" && a.GetProperty("result").GetString() == "Forbidden");
        Assert.DoesNotContain(await ListAsync(server, "access-logs", tenant), a => a.GetProperty("operationId").GetString() == "tenants.create");

        // 操作日志只记标了审计的操作（开通租户进、列用户不进），细节里有登录名与耗时
        var operations = await ListAsync(server, "operation-logs", platform);
        var provision = Assert.Single(operations, o => o.GetProperty("operationId").GetString() == "tenants.create");
        Assert.True(provision.GetProperty("succeeded").GetBoolean());
        Assert.Equal("superadmin", provision.GetProperty("userName").GetString());
        Assert.True(provision.GetProperty("durationMs").GetInt64() >= 0);
        Assert.DoesNotContain(operations, o => o.GetProperty("operationId").GetString() is "users.list" or "editions.list");
        var (detail, detailBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/operation-logs/{provision.GetProperty("id").GetString()}", token: platform));
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        Assert.Contains("durationMs", detailBody.GetProperty("details").GetString(), StringComparison.Ordinal);
        // 租户看不到平台的操作日志：同一条按主键取即找不到
        var (hidden, hiddenBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/operation-logs/{provision.GetProperty("id").GetString()}", token: tenant));
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        Assert.Equal("AUDITING_NOTFOUND_001", hiddenBody.GetProperty("code").GetString());
    }

    [Fact]
    public async Task PermissionChanges_AreLoggedOnlyWhenTheyTakeEffect_AndTracesAggregateOneCall()
    {
        var (server, platform, tenant) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        var (created, createdBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/roles", """{"code":"auditor","name":"Auditor"}""", tenant));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var roleId = createdBody.GetProperty("id").GetString()!;

        // 一个码生效一个被拒：日志只带生效的；全被拒的一次不记
        var (granted, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{roleId}:grant", """{"permissions":["identity.users.read","nope.things.read"]}""", tenant));
        Assert.Equal(HttpStatusCode.OK, granted.StatusCode);
        var (rejected, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{roleId}:grant", """{"permissions":["nope.things.read"]}""", tenant));
        Assert.Equal(HttpStatusCode.OK, rejected.StatusCode);
        var (users, usersBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:list", """{"page":{"index":1,"size":5},"filters":[{"field":"userName","operator":"eq","value":"admin"}]}""", tenant));
        Assert.Equal(HttpStatusCode.OK, users.StatusCode);
        var adminId = usersBody.GetProperty("items")[0].GetProperty("id").GetString()!;
        var (assigned, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{roleId}:assign", $$"""{"userIds":["{{adminId}}"]}""", tenant));
        Assert.Equal(HttpStatusCode.OK, assigned.StatusCode);
        await FlushAsync(server);

        var changes = await ListAsync(server, "permission-change-logs", tenant);
        var grant = Assert.Single(changes, c => c.GetProperty("changeType").GetString() == "RoleGrant");
        Assert.Equal("identity.users.read", grant.GetProperty("permissionCodes").GetString());
        Assert.Equal("Auditor", grant.GetProperty("roleName").GetString());
        Assert.Equal("admin", grant.GetProperty("userName").GetString());
        var assign = Assert.Single(changes, c => c.GetProperty("changeType").GetString() == "RoleAssign");
        Assert.Equal("admin", assign.GetProperty("targetUserName").GetString());
        Assert.DoesNotContain(await ListAsync(server, "permission-change-logs", platform), c => c.GetProperty("changeType").GetString() == "RoleGrant");

        // 按 trace-id 聚合：授予那一次调用的访问日志、操作日志与权限变更串在一起
        var traceId = grant.GetProperty("traceId").GetString()!;
        var (timeline, timelineBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/traces:timeline?dimension=TraceId&value={traceId}", token: tenant));
        Assert.Equal(HttpStatusCode.OK, timeline.StatusCode);
        var types = timelineBody.GetProperty("items").EnumerateArray().Select(static i => i.GetProperty("logType").GetString()).ToHashSet();
        Assert.Equal(["Access", "Operation", "PermissionChange"], types.Order(StringComparer.Ordinal).ToList());
        Assert.False(timelineBody.GetProperty("truncated").GetBoolean());

        // 按会话聚合：租户管理员这次会话的登录与之后的调用
        var sessionId = grant.GetProperty("sessionId").GetString()!;
        var (session, sessionBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/traces:timeline?dimension=SessionId&value={sessionId}", token: tenant));
        Assert.Equal(HttpStatusCode.OK, session.StatusCode);
        var items = sessionBody.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal("Login", items[0].GetProperty("logType").GetString());
        Assert.Contains(items, i => i.GetProperty("logType").GetString() == "Operation" && i.GetProperty("title").GetString() == "roles.grant");
        Assert.True(items.Select(static i => i.GetProperty("occurredAt").GetDateTimeOffset()).SequenceEqual(items.Select(static i => i.GetProperty("occurredAt").GetDateTimeOffset()).Order()));

        // 不合法的链路查询
        var (blank, blankBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/traces:timeline?dimension=TraceId&value=%20%20", token: tenant));
        Assert.Equal(HttpStatusCode.BadRequest, blank.StatusCode);
        Assert.Equal("AUDITING_VALIDATION_003", blankBody.GetProperty("code").GetString());
        var (reversed, reversedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/traces:timeline?dimension=TraceId&value={traceId}&from=2026-02-01T00:00:00Z&to=2026-01-01T00:00:00Z", token: tenant));
        Assert.Equal(HttpStatusCode.BadRequest, reversed.StatusCode);
        Assert.Equal("AUDITING_VALIDATION_003", reversedBody.GetProperty("code").GetString());
        var (badSession, badSessionBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/traces:timeline?dimension=SessionId&value=not-a-guid", token: tenant));
        Assert.Equal(HttpStatusCode.BadRequest, badSession.StatusCode);
        Assert.Equal("AUDITING_VALIDATION_002", badSessionBody.GetProperty("code").GetString());
    }

    [Fact]
    public async Task UnhandledExceptions_AreLogged_AndQueriesRejectUnsupportedFilters()
    {
        var (server, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        var outcome = await server.InvokeAsync(static _ => throw new InvalidOperationException("boom"));
        Assert.Equal("KERNEL_UNEXPECTED_001", outcome.Error?.Code);
        await FlushAsync(server);

        var failures = await ListAsync(server, "exception-logs", platform, """{"page":{"index":1,"size":20},"filters":[{"field":"exceptionType","operator":"contains","value":"InvalidOperation"}]}""");
        var failure = Assert.Single(failures);
        Assert.Equal("System.InvalidOperationException", failure.GetProperty("exceptionType").GetString());
        Assert.Equal("boom", failure.GetProperty("message").GetString());
        Assert.False(failure.TryGetProperty("stackTrace", out _));
        var (detail, detailBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/exception-logs/{failure.GetProperty("id").GetString()}", token: platform));
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        Assert.Contains("boom", detailBody.GetProperty("stackTrace").GetString(), StringComparison.Ordinal);

        // 白名单外的筛选与排序、不合法的值都拒绝；时间范围既是条件也限定扫描的月表（范围里没有月表即空）
        var (unsupported, unsupportedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/access-logs:list", """{"filters":[{"field":"durationMs","operator":"eq","value":"1"}]}""", platform));
        Assert.Equal(HttpStatusCode.BadRequest, unsupported.StatusCode);
        Assert.Equal("AUDITING_VALIDATION_001", unsupportedBody.GetProperty("code").GetString());
        var (badSort, badSortBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/login-logs:list", """{"sorts":[{"field":"userAgent"}]}""", platform));
        Assert.Equal(HttpStatusCode.BadRequest, badSort.StatusCode);
        Assert.Equal("AUDITING_VALIDATION_001", badSortBody.GetProperty("code").GetString());
        var (badDate, badDateBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/login-logs:list", """{"filters":[{"field":"occurredAt","operator":"ge","value":"yesterday"}]}""", platform));
        Assert.Equal(HttpStatusCode.BadRequest, badDate.StatusCode);
        Assert.Equal("AUDITING_VALIDATION_002", badDateBody.GetProperty("code").GetString());
        Assert.Empty(await ListAsync(server, "login-logs", platform, """{"filters":[{"field":"occurredAt","operator":"ge","value":"2001-01-01T00:00:00Z"},{"field":"occurredAt","operator":"le","value":"2001-02-01T00:00:00Z"}]}"""));
        Assert.NotEmpty(await ListAsync(server, "login-logs", platform, $$"""{"filters":[{"field":"occurredAt","operator":"ge","value":"{{DateTimeOffset.UtcNow.AddDays(-1):O}}"}],"sorts":[{"field":"occurredAt","descending":true}]}"""));
        Assert.NotEmpty(await ListAsync(server, "access-logs", platform, """{"filters":[{"field":"result","operator":"in","value":"Success,Unauthorized"}]}"""));
    }

    [Fact]
    public async Task Retention_RemovesExpiredLogs_AndRejectsAnInvalidSetting()
    {
        var (server, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        var old = DateTimeOffset.UtcNow.AddDays(-400);
        var writer = server.Services.GetRequiredService<AuditLogWriter>();
        Assert.True(writer.Enqueue(new SysLoginLog { Id = Guid.CreateVersion7(), TenantId = TenantIds.Platform, OccurredAt = old.UtcDateTime, TraceId = "old-trace", Event = LoginEvent.Login, Succeeded = true, UserName = "ghost" }));
        Assert.True(writer.Enqueue(new SysLoginLog { Id = Guid.CreateVersion7(), TenantId = "contoso", OccurredAt = old.UtcDateTime, TraceId = "old-trace", Event = LoginEvent.Logout, Succeeded = true, UserName = "ghost" }));
        await server.Services.GetRequiredService<DatabaseAuditTrail>().WriteAsync(new AuditEvent(old, "tenants.update", Result.Success(), "old-trace"), CancellationToken.None);
        await FlushAsync(server);
        Assert.Contains(await ListAsync(server, "login-logs", platform), l => l.GetProperty("userName").GetString() == "ghost");

        async Task<Result> RunAsync() => await SettingsTestHost.InvokeAsync(server, null, async ctx =>
        {
            var job = ctx.Services.GetRequiredService<AuditRetentionJob>();
            return await job.RunAsync(JobRun.Create(AuditRetentionJob.Name, DateTimeOffset.UtcNow, JobTrigger.Manual), ctx, ctx.CancellationToken);
        });

        // 缺省保留 180 天：一年多以前的分表日志（平台与租户两侧）与操作审计都被删，近期的留着
        Assert.True((await RunAsync()).IsSuccess);
        var remaining = await ListAsync(server, "login-logs", platform);
        Assert.DoesNotContain(remaining, l => l.GetProperty("userName").GetString() == "ghost");
        Assert.Contains(remaining, l => l.GetProperty("userName").GetString() == "superadmin");
        Assert.DoesNotContain(await ListAsync(server, "operation-logs", platform), o => o.GetProperty("traceId").GetString() == "old-trace");
        var tenantRows = await SettingsTestHost.InvokeAsync(server, "contoso", async ctx => await ctx.Services.GetRequiredService<IRepository<SysLoginLog, Guid>>().ListAsync(EntityQuery.All<SysLoginLog>(), ctx.CancellationToken));
        Assert.DoesNotContain(tenantRows, l => l.UserName == "ghost");

        // 参数建了却不合法：作业失败，不按错误的保留期删
        var (config, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/configs", """{"key":"auditing.retention-days","name":"Log retention","dataType":"Number","value":"0"}""", platform));
        Assert.Equal(HttpStatusCode.Created, config.StatusCode);
        Assert.Equal("AUDITING_VALIDATION_004", (await RunAsync()).Error?.Code);
    }
}
