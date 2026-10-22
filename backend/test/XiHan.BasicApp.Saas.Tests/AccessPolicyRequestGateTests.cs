// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using XiHan.BasicApp.Saas.Application.AccessPolicies;
using XiHan.BasicApp.Saas.Domain.Conditions;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.BasicApp.Saas.Infrastructure.Security;
using XiHan.Framework.Domain.Entities.Abstracts;
using XiHan.Framework.EventBus.Abstractions.Local;
using XiHan.Framework.Web.Core.Clients;
using static XiHan.BasicApp.Saas.Tests.AccessPolicyTestKit;
using ConditionOperator = XiHan.BasicApp.Saas.Domain.Entities.ConditionOperator;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 访问策略认证后一段（请求访问闸门）：平台级要等认证的策略，加上已登录请求所在租户的策略
/// </summary>
public sealed class AccessPolicyRequestGateTests
{
    private const long AdminId = 11;
    private const long ClerkId = 12;
    private const long TenantId = 7;

    private static readonly AccessPolicyConditionCommand Admin = Clause(SaasConditionAttributes.SubjectRoleCodes, ConditionOperator.In, "admin");
    private static readonly AccessPolicyConditionCommand OutsideOffice = Clause(SaasConditionAttributes.EnvironmentIp, ConditionOperator.NotIn, "10.0.0.0/8");

    /// <summary>
    /// 平台级「管理员只能在办公网访问」：办公网外的管理员被拒并记下用户，普通成员与办公网内的管理员放行
    /// </summary>
    [Theory]
    [InlineData(AdminId, "203.0.113.9", true)]
    [InlineData(AdminId, "10.1.2.3", false)]
    [InlineData(ClerkId, "203.0.113.9", false)]
    public async Task PlatformRolePolicy_BlocksAdminsOutsideOffice(long userId, string ip, bool blocked)
    {
        var harness = new Harness(Policy(1, 0, AccessPolicyAction.Deny, Admin, OutsideOffice));
        harness.SignIn(userId, TenantId, ip);

        var decision = await harness.Gate.EvaluateAsync(harness.Context);

        Assert.Equal(!blocked, decision.IsAllowed);
        if (blocked)
        {
            Assert.Equal(StatusCodes.Status403Forbidden, decision.StatusCode);
            var hit = Assert.Single(harness.DrainHits()).First;
            Assert.Equal((AccessPolicyPhase.AfterAuthentication, true, (long?)AdminId), (hit.Phase, hit.IsBlocked, hit.Request.UserId));
            Assert.NotNull(hit.Request.ClientKind);
        }
    }

    /// <summary>
    /// 租户策略只作用于这个租户里已登录的请求：匿名请求与其他租户不受影响
    /// </summary>
    [Fact]
    public async Task TenantPolicy_AppliesToSignedInMembersOfThatTenantOnly()
    {
        var harness = new Harness(Policy(1, TenantId, AccessPolicyAction.Deny, OutsideOffice));

        harness.SignIn(ClerkId, TenantId, "203.0.113.9");
        Assert.False((await harness.Gate.EvaluateAsync(harness.Context)).IsAllowed);

        harness.SignIn(null, TenantId, "203.0.113.9");
        Assert.True((await harness.Gate.EvaluateAsync(harness.Context)).IsAllowed);

        harness.SignIn(ClerkId, 8, "203.0.113.9");
        Assert.True((await harness.Gate.EvaluateAsync(harness.Context)).IsAllowed);
    }

    /// <summary>
    /// 认证前命中的平台放行压过这里的平台拒绝，但租户自己的拒绝照样拦
    /// </summary>
    [Fact]
    public async Task EarlierPlatformAllow_SuppressesPlatformDenyButNotTenantDeny()
    {
        var harness = new Harness(
            Policy(1, 0, AccessPolicyAction.Deny, Admin),
            Policy(2, TenantId, AccessPolicyAction.Deny, Clause(SaasConditionAttributes.SubjectUserId, ConditionOperator.Equals, AdminId.ToString(System.Globalization.CultureInfo.InvariantCulture))));
        harness.SignIn(AdminId, TenantId, "10.1.2.3");
        harness.Context.Items[AccessPolicyRequests.PlatformAllowedItemKey] = true;

        var decision = await harness.Gate.EvaluateAsync(harness.Context);

        Assert.False(decision.IsAllowed);
        var hits = harness.DrainHits().Select(item => item.First).ToList();
        Assert.False(hits.Single(hit => hit.PolicyId == 1).IsBlocked);
        Assert.True(hits.Single(hit => hit.PolicyId == 2).IsBlocked);
    }

    /// <summary>
    /// 总开关关掉或求值出错时放行
    /// </summary>
    [Fact]
    public async Task DisabledOrFailure_Allows()
    {
        var harness = new Harness(Policy(1, 0, AccessPolicyAction.Deny, Admin));
        harness.SignIn(AdminId, TenantId, "203.0.113.9");

        harness.Options.CurrentValue.Enabled = false;
        Assert.True((await harness.Gate.EvaluateAsync(harness.Context)).IsAllowed);

        harness.Options.CurrentValue.Enabled = true;
        harness.Snapshots.Failure = new InvalidOperationException("快照重建失败");
        Assert.True((await harness.Gate.EvaluateAsync(harness.Context)).IsAllowed);
    }

    /// <summary>
    /// 登录时：目标租户的策略按这个用户求，被拒时写一条「访问策略拒绝」的登录失败并拒绝登录
    /// </summary>
    [Fact]
    public async Task LoginGuard_RejectsLoginIntoTenantOutsideOffice()
    {
        var harness = new Harness(Policy(1, TenantId, AccessPolicyAction.Deny, Admin, OutsideOffice));
        harness.SignIn(null, null, "203.0.113.9");

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => harness.LoginGuard.EnsureLoginAllowedAsync(AdminId, "admin", TenantId));

        Assert.Equal(AccessPolicyLoginGuard.DeniedMessage, error.Message);
        harness.EventBus.Verify(bus => bus.PublishAsync(
            It.Is<AuthLoginFailedDomainEvent>(eventData => eventData.LoginResult == LoginResult.AccessDenied && eventData.UserId == AdminId && eventData.TenantId == TenantId),
            It.IsAny<bool>()), Times.Once);
        var hit = Assert.Single(harness.DrainHits()).First;
        Assert.Equal((AccessPolicyPhase.Login, true), (hit.Phase, hit.IsBlocked));
    }

    /// <summary>
    /// 登录时：进平台不看租户策略；办公网内照常登录；总开关关掉时不求值
    /// </summary>
    [Fact]
    public async Task LoginGuard_AllowsOtherCases()
    {
        var harness = new Harness(Policy(1, TenantId, AccessPolicyAction.Deny, Admin, OutsideOffice));

        harness.SignIn(null, null, "203.0.113.9");
        await harness.LoginGuard.EnsureLoginAllowedAsync(AdminId, "admin", null);

        harness.SignIn(null, null, "10.1.2.3");
        await harness.LoginGuard.EnsureLoginAllowedAsync(AdminId, "admin", TenantId);

        harness.SignIn(null, null, "203.0.113.9");
        harness.Options.CurrentValue.Enabled = false;
        await harness.LoginGuard.EnsureLoginAllowedAsync(AdminId, "admin", TenantId);

        harness.EventBus.Verify(bus => bus.PublishAsync(It.IsAny<AuthLoginFailedDomainEvent>(), It.IsAny<bool>()), Times.Never);
    }

    private sealed class Harness
    {
        private readonly ManualTimeProvider _time = new(Now);
        private readonly AccessPolicyHitRecorder _hits;
        private readonly Sources _sources = new();

        public Harness(params CompiledAccessPolicy[] policies)
        {
            Snapshots = new FixedSnapshots(policies);
            _hits = Hits(_time);
            _sources.Roles[AdminId] = ["admin"];
            _sources.Roles[ClerkId] = ["clerk"];
            var evaluator = new AccessPolicyAuthenticatedEvaluator(Snapshots, Evaluator(), _sources);
            Gate = new AccessPolicyRequestGate(Options, evaluator, _sources, _hits, _time, NullLogger<AccessPolicyRequestGate>.Instance);
            var clientInfo = new Mock<IClientInfoProvider>();
            clientInfo.Setup(provider => provider.GetCurrent()).Returns(() => new ClientInfo { IpAddress = Context.Connection.RemoteIpAddress?.ToString() });
            LoginGuard = new AccessPolicyLoginGuard(
                Options,
                evaluator,
                _hits,
                _sources.HttpContextAccessor,
                EventBus.Object,
                clientInfo.Object,
                new Mock<ITraceIdProvider>().Object,
                _time,
                NullLogger<AccessPolicyLoginGuard>.Instance);
        }

        public AccessPolicyRequestGate Gate { get; }

        public AccessPolicyLoginGuard LoginGuard { get; }

        public FixedSnapshots Snapshots { get; }

        public PermissionConditionTestKit.FixedOptionsMonitor<AccessPolicyOptions> Options { get; } = PolicyOptions();

        public Mock<ILocalEventBus> EventBus { get; } = new();

        public DefaultHttpContext Context { get; private set; } = Request("127.0.0.1");

        public void SignIn(long? userId, long? tenantId, string ip)
        {
            Context = Request(ip);
            _sources.HttpContextAccessor.HttpContext = Context;
            _sources.UserId = userId;
            _ = _sources.CurrentTenant.Change(tenantId);
        }

        public IReadOnlyList<AccessPolicyHitBatchItem> DrainHits()
        {
            return _hits.Drain(_time.Now, includeCurrentMinute: true);
        }
    }
}
