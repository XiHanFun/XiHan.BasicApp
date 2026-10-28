// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts.Settings;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.BasicApp.Settings.Domain.Services;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 业务编号：格式计算（与 4.x 同规则）、规则的维护（日期格式与周期的安全组合、时区、发号后格式冻结、只有没发过号的能删）、
/// 发号（连续区间、幂等重放与同键异参冲突、新周期从 1 开始、时钟落后拒绝、流水用尽、停用）、安全重置、预览、租户与平台规则的找法、发号记录的可见范围。
/// </summary>
public sealed class SettingsNumberingTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public void Formatter_JoinsNonEmptySegments_AndGuardsCapacityAndCycles()
    {
        Assert.Equal("ORD-20261028-0007", NumberingFormatter.Format("ORD", "-", "20261028", 4, 7));
        Assert.Equal("0007", NumberingFormatter.Format(null, "-", null, 4, 7));
        Assert.Equal("ORD20261028007", NumberingFormatter.Format("ORD", string.Empty, "20261028", 3, 7));
        Assert.Equal(999_999_999_999_999_999L, NumberingFormatter.MaxValue(18));
        Assert.Throws<ArgumentOutOfRangeException>(() => NumberingFormatter.Format(null, "-", null, 2, 100));
        Assert.Throws<ArgumentOutOfRangeException>(() => NumberingFormatter.FormatRange(null, "-", null, 4, 1, 1001));
        Assert.True(NumberingFormatter.IsSafe(NumberingDateFormat.YyyyMMdd, NumberingResetCycle.Daily));
        Assert.True(NumberingFormatter.IsSafe(NumberingDateFormat.YyMM, NumberingResetCycle.Yearly));
        Assert.True(NumberingFormatter.IsSafe(NumberingDateFormat.None, NumberingResetCycle.Never));
        Assert.False(NumberingFormatter.IsSafe(NumberingDateFormat.YyyyMM, NumberingResetCycle.Daily));
        Assert.False(NumberingFormatter.IsSafe(NumberingDateFormat.MMdd, NumberingResetCycle.Yearly));
        Assert.True(NumberingFormatter.TryResolveTimeZone("Asia/Shanghai", out var shanghai));
        Assert.False(NumberingFormatter.TryResolveTimeZone("Mars/Olympus", out _));
        // 规则时区决定日期与周期：UTC 前一天 16:30 在上海已是第二天
        var local = NumberingFormatter.ToRuleTime(new DateTimeOffset(2026, 10, 27, 16, 30, 0, TimeSpan.Zero), shanghai);
        Assert.Equal("20261028", NumberingFormatter.PeriodKey(local, NumberingResetCycle.Daily));
        Assert.Equal(20261028L, NumberingFormatter.PeriodOrdinal(local, NumberingResetCycle.Daily));
        Assert.Equal("261028", NumberingFormatter.DateText(local, NumberingDateFormat.YyMMdd));
        Assert.Equal("never", NumberingFormatter.PeriodKey(local, NumberingResetCycle.Never));
        Assert.Equal(0L, NumberingFormatter.PeriodOrdinal(local, NumberingResetCycle.Never));
        Assert.Equal("INV-yyyyMM-000000", NumberingFormatter.Template(new SysNumberingRule { Prefix = "INV", Separator = "-", DateFormat = NumberingDateFormat.YyyyMM, SerialLength = 6 }));
    }

    [Fact]
    public async Task Rules_IssueReplayResetAndFreeze_OverHttp()
    {
        var (server, platform, tenant) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;

        // 维护：不安全的格式与无效时区被拒
        var (unsafeFormat, unsafeBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/numbering-rules", """{"code":"bad","name":"Bad","dateFormat":"YyyyMM","resetCycle":"Daily"}""", platform));
        Assert.Equal(HttpStatusCode.BadRequest, unsafeFormat.StatusCode);
        Assert.Equal("SETTINGS_VALIDATION_005", unsafeBody.GetProperty("code").GetString());
        var (badZone, badZoneBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/numbering-rules", """{"code":"bad","name":"Bad","timeZoneId":"Mars/Olympus"}""", platform));
        Assert.Equal(HttpStatusCode.BadRequest, badZone.StatusCode);
        Assert.Equal("SETTINGS_VALIDATION_006", badZoneBody.GetProperty("code").GetString());

        // 平台规则，供租户使用
        var (created, createdBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/numbering-rules", """{"code":"order","name":"Order","prefix":"ORD","dateFormat":"None","resetCycle":"Never","serialLength":4,"allowTenantUse":true}""", platform));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal("ORD-0000", createdBody.GetProperty("template").GetString());
        var ruleId = createdBody.GetProperty("id").GetString()!;

        // 发号：连续区间；同键同参重放，同键异参冲突
        var (first, firstBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/numbering-rules:generate", """{"code":"order","idempotencyKey":"k1","count":3,"businessType":"order"}""", platform));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(["ORD-0001", "ORD-0002", "ORD-0003"], firstBody.GetProperty("numbers").EnumerateArray().Select(static n => n.GetString()).ToList());
        Assert.False(firstBody.GetProperty("isReplay").GetBoolean());
        Assert.Equal("Platform", firstBody.GetProperty("scope").GetString());
        var (replay, replayBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/numbering-rules:generate", """{"code":"order","idempotencyKey":"k1","count":3,"businessType":"order"}""", platform));
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.True(replayBody.GetProperty("isReplay").GetBoolean());
        Assert.Equal("ORD-0001", replayBody.GetProperty("numbers")[0].GetString());
        var (mismatch, mismatchBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/numbering-rules:generate", """{"code":"order","idempotencyKey":"k1","count":2}""", platform));
        Assert.Equal(HttpStatusCode.Conflict, mismatch.StatusCode);
        Assert.Equal("SETTINGS_CONFLICT_013", mismatchBody.GetProperty("code").GetString());

        // 租户 Auto 用供租户使用的平台规则，共用一条流水；租户建同码规则后 Auto 改用自己的
        var (shared, sharedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/numbering-rules:generate", """{"code":"order","idempotencyKey":"k1"}""", tenant));
        Assert.Equal(HttpStatusCode.OK, shared.StatusCode);
        Assert.Equal("ORD-0004", sharedBody.GetProperty("numbers")[0].GetString());
        Assert.Equal("contoso", sharedBody.GetProperty("requestTenantId").GetString());
        Assert.Equal("Platform", sharedBody.GetProperty("scope").GetString());
        var (own, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/numbering-rules", """{"code":"order","name":"Tenant order","prefix":"T","dateFormat":"None","resetCycle":"Never","serialLength":3}""", tenant));
        Assert.Equal(HttpStatusCode.Created, own.StatusCode);
        var (mine, mineBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/numbering-rules:generate", """{"code":"order","idempotencyKey":"k2"}""", tenant));
        Assert.Equal(HttpStatusCode.OK, mine.StatusCode);
        Assert.Equal("T-001", mineBody.GetProperty("numbers")[0].GetString());
        Assert.Equal("Tenant", mineBody.GetProperty("scope").GetString());
        var (platformScope, platformScopeBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/numbering-rules:generate", """{"code":"order","idempotencyKey":"k3","scope":"Platform"}""", tenant));
        Assert.Equal(HttpStatusCode.OK, platformScope.StatusCode);
        Assert.Equal("ORD-0005", platformScopeBody.GetProperty("numbers")[0].GetString());
        var (tenantScope, tenantScopeBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/numbering-rules:generate", """{"code":"order","idempotencyKey":"k9","scope":"Tenant"}""", platform));
        Assert.Equal(HttpStatusCode.BadRequest, tenantScope.StatusCode);
        Assert.Equal("SETTINGS_VALIDATION_009", tenantScopeBody.GetProperty("code").GetString());

        // 发号记录：平台看全部，租户对平台规则只看自己发起的
        var (allocations, allocationsBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/numbering-rules/{ruleId}:allocations", """{"page":{"index":1,"size":20}}""", platform));
        Assert.Equal(HttpStatusCode.OK, allocations.StatusCode);
        Assert.Equal(3, allocationsBody.GetProperty("total").GetInt64());
        var (tenantAllocations, tenantAllocationsBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/numbering-rules/{ruleId}:allocations", """{"page":{"index":1,"size":20}}""", tenant));
        Assert.Equal(HttpStatusCode.OK, tenantAllocations.StatusCode);
        Assert.Equal(2, tenantAllocationsBody.GetProperty("total").GetInt64());
        Assert.All(tenantAllocationsBody.GetProperty("items").EnumerateArray(), static a => Assert.Equal("contoso", a.GetProperty("requestTenantId").GetString()));

        // 发过号即格式冻结、不能删除；名称照改；另一侧的规则改不了
        var (frozen, frozenBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/numbering-rules/{ruleId}", """{"name":"Order","prefix":"SO","dateFormat":"None","resetCycle":"Never","serialLength":4,"allowTenantUse":true}""", platform));
        Assert.Equal(HttpStatusCode.Conflict, frozen.StatusCode);
        Assert.Equal("SETTINGS_CONFLICT_009", frozenBody.GetProperty("code").GetString());
        var (renamed, renamedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/numbering-rules/{ruleId}", """{"name":"Sales order","prefix":"ORD","dateFormat":"None","resetCycle":"Never","serialLength":4,"allowTenantUse":true}""", platform));
        Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);
        Assert.Equal("Sales order", renamedBody.GetProperty("name").GetString());
        var (deleteFrozen, deleteFrozenBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/numbering-rules?ids={ruleId}", token: platform));
        Assert.Equal("SETTINGS_CONFLICT_009", deleteFrozenBody.GetProperty("failed")[0].GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.OK, deleteFrozen.StatusCode);
        var (foreign, foreignBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/numbering-rules/{ruleId}", """{"name":"Mine","prefix":"ORD","dateFormat":"None","resetCycle":"Never","serialLength":4}""", tenant));
        Assert.Equal(HttpStatusCode.Forbidden, foreign.StatusCode);
        Assert.Equal("SETTINGS_DENIED_001", foreignBody.GetProperty("code").GetString());

        // 安全重置：平台规则要回填编码；不能回到已发区间；向前跳之后从新值发
        var (unconfirmed, unconfirmedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/numbering-rules/{ruleId}:reset", """{"nextValue":"100","reason":"migration"}""", platform));
        Assert.Equal(HttpStatusCode.BadRequest, unconfirmed.StatusCode);
        Assert.Equal("SETTINGS_VALIDATION_008", unconfirmedBody.GetProperty("code").GetString());
        var (backwards, backwardsBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/numbering-rules/{ruleId}:reset", """{"nextValue":"5","reason":"oops","confirmCode":"order"}""", platform));
        Assert.Equal(HttpStatusCode.BadRequest, backwards.StatusCode);
        Assert.Equal("SETTINGS_VALIDATION_010", backwardsBody.GetProperty("code").GetString());
        var (overflow, overflowBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/numbering-rules/{ruleId}:reset", """{"nextValue":"10000","reason":"too far","confirmCode":"order"}""", platform));
        Assert.Equal(HttpStatusCode.BadRequest, overflow.StatusCode);
        Assert.Equal("SETTINGS_VALIDATION_007", overflowBody.GetProperty("code").GetString());
        var (reset, resetBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/numbering-rules/{ruleId}:reset", """{"nextValue":"100","reason":"migration","confirmCode":"order"}""", platform));
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
        Assert.Equal(99, resetBody.GetProperty("currentValue").GetInt64());
        var (afterReset, afterResetBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/numbering-rules:generate", """{"code":"order","idempotencyKey":"k4"}""", platform));
        Assert.Equal(HttpStatusCode.OK, afterReset.StatusCode);
        Assert.Equal("ORD-0100", afterResetBody.GetProperty("numbers")[0].GetString());

        // 流水用尽与停用
        var (tiny, tinyBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/numbering-rules", """{"code":"tiny","name":"Tiny","dateFormat":"None","resetCycle":"Never","serialLength":1}""", platform));
        Assert.Equal(HttpStatusCode.Created, tiny.StatusCode);
        var tinyId = tinyBody.GetProperty("id").GetString()!;
        var (nine, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/numbering-rules:generate", """{"code":"tiny","idempotencyKey":"t1","count":9}""", platform));
        Assert.Equal(HttpStatusCode.OK, nine.StatusCode);
        var (exhausted, exhaustedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/numbering-rules:generate", """{"code":"tiny","idempotencyKey":"t2"}""", platform));
        Assert.Equal(HttpStatusCode.Conflict, exhausted.StatusCode);
        Assert.Equal("SETTINGS_CONFLICT_011", exhaustedBody.GetProperty("code").GetString());
        var (off, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/numbering-rules/{tinyId}", """{"name":"Tiny","dateFormat":"None","resetCycle":"Never","serialLength":1,"status":"Disabled"}""", platform));
        Assert.Equal(HttpStatusCode.OK, off.StatusCode);
        var (disabled, disabledBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/numbering-rules:generate", """{"code":"tiny","idempotencyKey":"t3"}""", platform));
        Assert.Equal(HttpStatusCode.Conflict, disabled.StatusCode);
        Assert.Equal("SETTINGS_CONFLICT_010", disabledBody.GetProperty("code").GetString());

        // 没发过号的能删；预览不发号、越出位数整体被拒
        var (spare, spareBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/numbering-rules", """{"code":"spare","name":"Spare"}""", platform));
        Assert.Equal(HttpStatusCode.Created, spare.StatusCode);
        var (deleted, deletedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/numbering-rules?ids={spareBody.GetProperty("id").GetString()}", token: platform));
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        Assert.Equal(1, deletedBody.GetProperty("succeeded").GetArrayLength());
        var (preview, previewBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/numbering-rules:preview?prefix=PV&dateFormat=None&resetCycle=Never&serialLength=3&startValue=8&count=3", token: platform));
        Assert.Equal(HttpStatusCode.OK, preview.StatusCode);
        Assert.Equal(["PV-008", "PV-009", "PV-010"], previewBody.GetProperty("numbers").EnumerateArray().Select(static n => n.GetString()).ToList());
        var (previewOverflow, previewOverflowBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/numbering-rules:preview?dateFormat=None&resetCycle=Never&serialLength=2&startValue=98&count=3", token: platform));
        Assert.Equal(HttpStatusCode.BadRequest, previewOverflow.StatusCode);
        Assert.Equal("SETTINGS_VALIDATION_007", previewOverflowBody.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Generator_IssuesContiguousUniqueRanges_UnderConcurrency_OnPostgres()
    {
        await using var database = await PostgresDatabase.CreateAsync(PostgresDatabase.RequireServer(), "xihan_basicapp_numbering");
        var (server, platform, _) = await SettingsTestHost.StartAsync(database.ConnectionString, DatabaseProviders.PostgreSql);
        await using var running = server;
        var (created, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/numbering-rules", """{"code":"batch","name":"Batch","dateFormat":"None","resetCycle":"Never","serialLength":6}""", platform));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        Task<Result<NumberAllocation>> GenerateAsync(string key, int count)
            => SettingsTestHost.InvokeAsync(server, null, async ctx => await ctx.Services.GetRequiredService<INumberGenerator>().GenerateAsync(new NumberRequest("batch", key, count), ctx));

        // 20 个不同键各要 5 个号，另有 5 个调用争同一个键：同一规则的发号在规则行锁上排队
        var distinct = Enumerable.Range(0, 20).Select(i => GenerateAsync($"k{i}", 5)).ToList();
        var same = Enumerable.Range(0, 5).Select(_ => GenerateAsync("same", 3)).ToList();
        var results = await Task.WhenAll(distinct.Concat(same));
        Assert.All(results, static r => Assert.True(r.IsSuccess, r.Error?.Code));
        var allocations = results.Select(static r => { Assert.True(r.TryGetValue(out var a)); return a; }).ToList();

        // 同一个键只发一次：其余都是重放
        var sameAllocations = allocations.Skip(20).ToList();
        Assert.Single(sameAllocations.Select(static a => a.StartValue).Distinct());
        Assert.Single(sameAllocations, static a => !a.IsReplay);

        // 发出的区间两两不交、拼起来正好是 1..103
        var issued = allocations.Take(20).Append(sameAllocations.Single(static a => !a.IsReplay))
            .SelectMany(static a => Enumerable.Range((int)a.StartValue, (int)(a.EndValue - a.StartValue + 1)))
            .Order()
            .ToList();
        Assert.Equal(Enumerable.Range(1, 103).ToList(), issued);
    }

    [Fact]
    public async Task Generator_StartsEachPeriodAtOne_AndRefusesAClockThatIsBehind()
    {
        // 登录与建规则走 HTTP，时钟先停在真实时间；之后只在进程内拨动（令牌校验不跟着拨）
        var clock = new ManualClock(DateTimeOffset.UtcNow);
        var (server, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString, configure: builder => builder.Services.AddSingleton<TimeProvider>(clock));
        await using var running = server;
        var (created, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/numbering-rules", """{"code":"daily","name":"Daily","prefix":"D","dateFormat":"YyMMdd","resetCycle":"Daily","serialLength":3,"timeZoneId":"Asia/Shanghai"}""", platform));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        clock.Set(new DateTimeOffset(2026, 10, 28, 15, 59, 0, TimeSpan.Zero));

        async Task<NumberAllocation> GenerateAsync(string key)
            => await SettingsTestHost.InvokeAsync(server, null, async ctx =>
            {
                var result = await ctx.Services.GetRequiredService<INumberGenerator>().GenerateAsync(new NumberRequest("daily", key, 2), ctx);
                Assert.True(result.TryGetValue(out var allocation), result.Error?.Code);
                return allocation;
            });

        // 上海时间 23:59：当天的号；跨过上海的午夜即新周期从 1 开始
        var today = await GenerateAsync("a");
        Assert.Equal(["D-261028-001", "D-261028-002"], today.Numbers);
        Assert.Equal("20261028", today.PeriodKey);
        clock.Set(new DateTimeOffset(2026, 10, 28, 16, 1, 0, TimeSpan.Zero));
        var tomorrow = await GenerateAsync("b");
        Assert.Equal(["D-261029-001", "D-261029-002"], tomorrow.Numbers);

        // 时钟拨回前一天：拒绝发号，规则不被拉回旧周期
        clock.Set(new DateTimeOffset(2026, 10, 28, 15, 0, 0, TimeSpan.Zero));
        var behind = await SettingsTestHost.InvokeAsync(server, null, async ctx => await ctx.Services.GetRequiredService<INumberGenerator>().GenerateAsync(new NumberRequest("daily", "c"), ctx));
        Assert.True(behind.IsFailure);
        Assert.Equal("SETTINGS_CONFLICT_012", behind.Error!.Code);
    }
}
