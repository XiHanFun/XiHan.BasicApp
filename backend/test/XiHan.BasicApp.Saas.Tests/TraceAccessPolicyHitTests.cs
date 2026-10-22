// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 链路追踪里的条件命中记录：时间线条目与字段安全来源
/// </summary>
public sealed class TraceAccessPolicyHitTests
{
    private static readonly DateTimeOffset HitTime = new(2026, 10, 23, 3, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// 标题取策略名（没有名字取编码），摘要是方法与路径，拦下的标警告、放行的标成功、其余标信息
    /// </summary>
    [Fact]
    public void FromAccessPolicyHit_MapsTimelineItem()
    {
        var hit = Hit(AccessPolicyAction.Deny, isBlocked: true);

        var item = TraceApplicationMapper.FromAccessPolicyHit(hit);

        Assert.Equal(TraceLogType.AccessPolicyHit, item.LogType);
        Assert.Equal(HitTime, item.Time);
        Assert.Equal("办公网之外", item.Title);
        Assert.Equal("POST /api/User/User", item.Summary);
        Assert.Equal("warning", item.Status);
        Assert.Equal(("203.0.113.9", "admin", 7L, "6b3882cbba0883fb9e2d75386a00b0cf"), (item.Ip, item.UserName, item.UserId, item.TraceId));

        hit.PolicyName = null;
        Assert.Equal("office-only", TraceApplicationMapper.FromAccessPolicyHit(hit).Title);
        Assert.Equal("success", TraceApplicationMapper.FromAccessPolicyHit(Hit(AccessPolicyAction.Allow, isBlocked: false)).Status);
        Assert.Equal("info", TraceApplicationMapper.FromAccessPolicyHit(Hit(AccessPolicyAction.LogOnly, isBlocked: false)).Status);
    }

    /// <summary>
    /// 字段安全按命中记录的规则给条目打码：标题、摘要与状态分别来自策略名、路径与是否拦下
    /// </summary>
    [Fact]
    public void AccessPolicyHitItem_UsesHitFieldSecurityRules()
    {
        IFieldSecurityMultiSourceDto item = TraceApplicationMapper.FromAccessPolicyHit(Hit(AccessPolicyAction.Deny, isBlocked: true));

        Assert.Equal(typeof(SysAccessPolicyHit), item.FieldSecurityEntity);
        Assert.Equal(nameof(SysAccessPolicyHit.HitTime), item.FieldSecuritySourceOf(nameof(TraceTimelineItemDto.Time)));
        Assert.Equal(nameof(SysAccessPolicyHit.PolicyName), item.FieldSecuritySourceOf(nameof(TraceTimelineItemDto.Title)));
        Assert.Equal(nameof(SysAccessPolicyHit.Path), item.FieldSecuritySourceOf(nameof(TraceTimelineItemDto.Summary)));
        Assert.Equal(nameof(SysAccessPolicyHit.IsBlocked), item.FieldSecuritySourceOf(nameof(TraceTimelineItemDto.Status)));
        Assert.Equal(nameof(SysAccessPolicyHit.Ip), item.FieldSecuritySourceOf(nameof(TraceTimelineItemDto.Ip)));
    }

    private static SysAccessPolicyHit Hit(AccessPolicyAction action, bool isBlocked)
    {
        return new SysAccessPolicyHit
        {
            PolicyCode = "office-only",
            PolicyName = "办公网之外",
            Action = action,
            Phase = AccessPolicyPhase.AfterAuthentication,
            IsBlocked = isBlocked,
            TraceId = "6b3882cbba0883fb9e2d75386a00b0cf",
            Ip = "203.0.113.9",
            UserId = 7,
            UserName = "admin",
            Method = "POST",
            Path = "/api/User/User",
            HitTime = HitTime
        };
    }
}
