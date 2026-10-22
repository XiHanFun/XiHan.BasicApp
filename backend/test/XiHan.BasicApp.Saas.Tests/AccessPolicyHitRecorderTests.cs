// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.AccessPolicies;
using XiHan.BasicApp.Saas.Application.Conditions;
using XiHan.BasicApp.Saas.Domain.Entities;
using static XiHan.BasicApp.Saas.Tests.AccessPolicyTestKit;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 命中记录：同一键每分钟合并成一项带次数，只取已结束的分钟，满了只计数；权限条件的拒绝也记进来
/// </summary>
public sealed class AccessPolicyHitRecorderTests
{
    /// <summary>
    /// 同一分钟里同一键合并，第一次的追踪号保留；不同 IP 或不同分钟分开
    /// </summary>
    [Fact]
    public void Record_MergesSameKeyWithinMinute()
    {
        var time = new ManualTimeProvider(Now);
        var hits = Hits(time);
        hits.Record(Entry(Now, "203.0.113.9", "trace-1"));
        hits.Record(Entry(Now.AddSeconds(30), "203.0.113.9", "trace-2"));
        hits.Record(Entry(Now.AddSeconds(40), "198.51.100.7", "trace-3"));
        hits.Record(Entry(Now.AddMinutes(1), "203.0.113.9", "trace-4"));

        var items = hits.Drain(Now.AddMinutes(2));

        Assert.Equal(3, items.Count);
        var merged = items.Single(item => item.First.Request.TraceId == "trace-1");
        Assert.Equal(2, merged.Count);
        Assert.Equal(1, items.Single(item => item.First.Request.TraceId == "trace-4").Count);
    }

    /// <summary>
    /// 只取已经结束的分钟；停机时连当前分钟一起取
    /// </summary>
    [Fact]
    public void Drain_TakesClosedMinutesOnlyUnlessStopping()
    {
        var time = new ManualTimeProvider(Now);
        var hits = Hits(time);
        hits.Record(Entry(Now, "203.0.113.9", "trace-1"));

        Assert.Empty(hits.Drain(Now.AddSeconds(59)));
        Assert.Single(hits.Drain(Now.AddSeconds(59), includeCurrentMinute: true));
        Assert.Empty(hits.Drain(Now.AddMinutes(5), includeCurrentMinute: true));
    }

    /// <summary>
    /// 合并键满了：已有键照常累加，新键只计数不记
    /// </summary>
    [Fact]
    public void Record_CountsDropsWhenFull()
    {
        var time = new ManualTimeProvider(Now);
        var hits = Hits(time, capacity: 2);
        hits.Record(Entry(Now, "203.0.113.1", "trace-1"));
        hits.Record(Entry(Now, "203.0.113.2", "trace-2"));
        hits.Record(Entry(Now, "203.0.113.3", "trace-3"));
        hits.Record(Entry(Now, "203.0.113.1", "trace-4"));

        var items = hits.Drain(Now.AddMinutes(1));

        Assert.Equal(1, hits.Dropped);
        Assert.Equal(2, items.Count);
        Assert.Equal(2, items.Single(item => item.First.Request.Ip == "203.0.113.1").Count);
    }

    /// <summary>
    /// 权限条件的拒绝记进命中记录：拦截模式记作拒绝并拦下，只记录模式记作只记录，落在该租户
    /// </summary>
    [Theory]
    [InlineData(PermissionConditionMode.Enforce, AccessPolicyAction.Deny, true)]
    [InlineData(PermissionConditionMode.LogOnly, AccessPolicyAction.LogOnly, false)]
    public async Task PermissionConditionHits_AreRecorded(PermissionConditionMode mode, AccessPolicyAction action, bool blocked)
    {
        var time = new ManualTimeProvider(Now);
        var hits = Hits(time);
        var recorder = new AccessPolicyPermissionConditionHitRecorder(hits, time);

        await recorder.RecordAsync(new PermissionConditionHit(11, 7, "saas:user:export", mode, "/api/User/Export", "trace-1", "POST", "203.0.113.9", "curl/8"));

        var hit = Assert.Single(hits.Drain(Now, includeCurrentMinute: true)).First;
        Assert.Equal((7L, AccessPolicyHitSource.PermissionCondition, "saas:user:export", action, AccessPolicyPhase.Authorization, blocked),
            (hit.TenantId, hit.Source, hit.PolicyCode, hit.Action, hit.Phase, hit.IsBlocked));
        Assert.Equal(("203.0.113.9", "POST", (long?)11), (hit.Request.Ip, hit.Request.Method, hit.Request.UserId));
    }

    private static AccessPolicyHitEntry Entry(DateTimeOffset time, string ip, string traceId)
    {
        return new AccessPolicyHitEntry(
            0,
            AccessPolicyHitSource.AccessPolicy,
            1,
            "policy-1",
            "策略 1",
            AccessPolicyAction.Deny,
            AccessPolicyPhase.BeforeAuthentication,
            true,
            time,
            new AccessPolicyRequestInfo(traceId, ip, null, null, "GET", "/api/Demo", null, null, null));
    }
}
