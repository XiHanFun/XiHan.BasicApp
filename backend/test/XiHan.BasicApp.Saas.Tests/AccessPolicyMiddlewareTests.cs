// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using XiHan.BasicApp.Saas.Application.AccessPolicies;
using XiHan.BasicApp.Saas.Application.Conditions;
using XiHan.BasicApp.Saas.Domain.Conditions;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Infrastructure.Security;
using static XiHan.BasicApp.Saas.Tests.AccessPolicyTestKit;
using ConditionOperator = XiHan.BasicApp.Saas.Domain.Entities.ConditionOperator;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 访问策略认证前一段：管线最前按平台级、只看请求本身属性的策略放行或拒绝，并拦下自动封禁中的 IP
/// </summary>
public sealed class AccessPolicyMiddlewareTests
{
    /// <summary>
    /// 客户端 IP 命中拒绝策略：403、统一失败响应、记一条拦下的命中
    /// </summary>
    [Fact]
    public async Task DenyPolicy_Returns403AndRecordsBlockedHit()
    {
        var harness = new Harness(Policy(1, 0, AccessPolicyAction.Deny, Clause(SaasConditionAttributes.EnvironmentIp, ConditionOperator.In, "203.0.113.0/24")));
        var context = harness.CreateContext("203.0.113.9");

        await harness.Middleware.InvokeAsync(context);

        Assert.False(harness.NextCalled);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        using var body = JsonDocument.Parse(ReadBody(context));
        Assert.Equal(403, body.RootElement.GetProperty("code").GetInt32());
        Assert.Equal(AccessPolicyMiddleware.ForbiddenMessage, body.RootElement.GetProperty("data").GetString());
        var hit = Assert.Single(harness.DrainHits()).First;
        Assert.Equal(("policy-1", AccessPolicyAction.Deny, AccessPolicyPhase.BeforeAuthentication, true), (hit.PolicyCode, hit.Action, hit.Phase, hit.IsBlocked));
        Assert.Equal("203.0.113.9", hit.Request.Ip);
    }

    /// <summary>
    /// 只记录的策略命中照常放行，并记一条不拦下的命中
    /// </summary>
    [Fact]
    public async Task LogOnlyPolicy_PassesAndRecordsHit()
    {
        var harness = new Harness(Policy(1, 0, AccessPolicyAction.LogOnly, Clause(SaasConditionAttributes.ResourcePath, ConditionOperator.StartsWith, "/api/Demo")));

        await harness.Middleware.InvokeAsync(harness.CreateContext("198.51.100.7"));

        Assert.True(harness.NextCalled);
        var hit = Assert.Single(harness.DrainHits()).First;
        Assert.Equal((AccessPolicyAction.LogOnly, false), (hit.Action, hit.IsBlocked));
    }

    /// <summary>
    /// 不受信的来源自己带的 X-Forwarded-For 被忽略，按连接地址判断
    /// </summary>
    [Theory]
    [InlineData("203.0.113.9", "10.1.2.3", true)]
    [InlineData("10.1.2.3", "203.0.113.9", false)]
    public async Task UntrustedPeer_ForwardedForIsIgnored(string remoteIp, string forwardedFor, bool blocked)
    {
        var harness = new Harness(Policy(1, 0, AccessPolicyAction.Deny, Clause(SaasConditionAttributes.EnvironmentIp, ConditionOperator.In, "203.0.113.0/24")));
        var context = harness.CreateContext(remoteIp);
        context.Request.Headers["X-Forwarded-For"] = forwardedFor;

        await harness.Middleware.InvokeAsync(context);

        Assert.Equal(!blocked, harness.NextCalled);
    }

    /// <summary>
    /// 本机回环上的代理受信：取它转发来的客户端地址；放行的请求本身不被改写
    /// </summary>
    [Theory]
    [InlineData("203.0.113.9", true)]
    [InlineData("198.51.100.7", false)]
    public async Task TrustedProxy_UsesForwardedClient(string forwardedFor, bool blocked)
    {
        var harness = new Harness(Policy(1, 0, AccessPolicyAction.Deny, Clause(SaasConditionAttributes.EnvironmentIp, ConditionOperator.In, "203.0.113.0/24")));
        var context = harness.CreateContext("127.0.0.1");
        context.Request.Headers["X-Forwarded-For"] = forwardedFor;

        await harness.Middleware.InvokeAsync(context);

        Assert.Equal(!blocked, harness.NextCalled);
        Assert.Equal(IPAddress.Loopback, context.Connection.RemoteIpAddress);
        Assert.Equal(forwardedFor, context.Request.Headers["X-Forwarded-For"].ToString());
    }

    /// <summary>
    /// 平台放行策略命中：同层拒绝失效，记下标记，封禁也不再查
    /// </summary>
    [Fact]
    public async Task AllowPolicy_SuppressesDenyAndSkipsBanCheck()
    {
        var harness = new Harness(
            Policy(1, 0, AccessPolicyAction.Deny, Clause(SaasConditionAttributes.EnvironmentIp, ConditionOperator.In, "203.0.113.0/24")),
            Policy(2, 0, AccessPolicyAction.Allow, Clause(SaasConditionAttributes.EnvironmentIp, ConditionOperator.Equals, "203.0.113.9")));
        harness.BannedUntil = Now.AddMinutes(30);
        var context = harness.CreateContext("203.0.113.9");

        await harness.Middleware.InvokeAsync(context);

        Assert.True(harness.NextCalled);
        Assert.True(AccessPolicyRequests.IsPlatformAllowed(context));
        harness.Bans.Verify(service => service.GetBanAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.All(harness.DrainHits(), item => Assert.False(item.First.IsBlocked));
    }

    /// <summary>
    /// 封禁中的 IP：429，带剩余秒数的 Retry-After，记一条自动封禁的命中
    /// </summary>
    [Fact]
    public async Task BannedIp_Returns429WithRetryAfter()
    {
        var harness = new Harness
        {
            BannedUntil = Now.AddSeconds(90)
        };
        var context = harness.CreateContext("203.0.113.9");

        await harness.Middleware.InvokeAsync(context);

        Assert.False(harness.NextCalled);
        Assert.Equal(StatusCodes.Status429TooManyRequests, context.Response.StatusCode);
        Assert.Equal("90", context.Response.Headers.RetryAfter.ToString());
        using var body = JsonDocument.Parse(ReadBody(context));
        Assert.Equal(AccessPolicyMiddleware.BannedMessage, body.RootElement.GetProperty("data").GetString());
        var hit = Assert.Single(harness.DrainHits()).First;
        Assert.Equal((AccessPolicyRequests.AutoBanPolicyCode, true), (hit.PolicyCode, hit.IsBlocked));
    }

    /// <summary>
    /// 要等认证才能求的平台策略不在这一段求
    /// </summary>
    [Fact]
    public async Task AfterAuthenticationPolicies_AreNotEvaluatedHere()
    {
        var harness = new Harness(Policy(1, 0, AccessPolicyAction.Deny,
            Clause(SaasConditionAttributes.EnvironmentIp, ConditionOperator.In, "203.0.113.0/24"),
            Clause(SaasConditionAttributes.SubjectRoleCodes, ConditionOperator.In, "admin")));

        await harness.Middleware.InvokeAsync(harness.CreateContext("203.0.113.9"));

        Assert.True(harness.NextCalled);
        Assert.Empty(harness.DrainHits());
    }

    /// <summary>
    /// 总开关关掉：不求值、不查封禁，直接放行
    /// </summary>
    [Fact]
    public async Task Disabled_PassesThroughWithoutEvaluating()
    {
        var harness = new Harness(Policy(1, 0, AccessPolicyAction.Deny, Clause(SaasConditionAttributes.EnvironmentIp, ConditionOperator.In, "203.0.113.0/24")));
        harness.Options.CurrentValue.Enabled = false;
        harness.BannedUntil = Now.AddMinutes(30);

        await harness.Middleware.InvokeAsync(harness.CreateContext("203.0.113.9"));

        Assert.True(harness.NextCalled);
        harness.Bans.Verify(service => service.GetBanAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 求值本身出错：记日志后放行
    /// </summary>
    [Fact]
    public async Task EvaluationFailure_FailsOpen()
    {
        var harness = new Harness();
        harness.Snapshots.Failure = new InvalidOperationException("缓存与数据库都不可用");

        await harness.Middleware.InvokeAsync(harness.CreateContext("203.0.113.9"));

        Assert.True(harness.NextCalled);
    }

    private static string ReadBody(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private sealed class Harness
    {
        private readonly IServiceProvider _services;
        private readonly ManualTimeProvider _time = new(Now);
        private readonly AccessPolicyHitRecorder _hits;

        public Harness(params CompiledAccessPolicy[] policies)
        {
            Snapshots = new FixedSnapshots(policies);
            _hits = Hits(_time);
            Bans
                .Setup(service => service.GetBanAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => BannedUntil);
            _services = new ServiceCollection()
                .AddSingleton<ISaasConditionAttributeSourceFactory>(new Sources())
                .AddSingleton(Bans.Object)
                .BuildServiceProvider();

            // 与框架受信代理配置一致：还原 For / Proto / Host，缺省只信任回环地址
            var forwarded = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
            };
            Middleware = new AccessPolicyMiddleware(
                _ =>
                {
                    NextCalled = true;
                    return Task.CompletedTask;
                },
                NullLoggerFactory.Instance,
                Microsoft.Extensions.Options.Options.Create(forwarded),
                Options,
                Snapshots,
                Evaluator(),
                _hits,
                _time);
        }

        public AccessPolicyMiddleware Middleware { get; }

        public FixedSnapshots Snapshots { get; }

        public PermissionConditionTestKit.FixedOptionsMonitor<AccessPolicyOptions> Options { get; } = PolicyOptions();

        public Mock<ILoginBanService> Bans { get; } = new();

        public DateTimeOffset? BannedUntil { get; set; }

        public bool NextCalled { get; private set; }

        public DefaultHttpContext CreateContext(string remoteIp)
        {
            var context = Request(remoteIp);
            context.RequestServices = _services;
            return context;
        }

        public IReadOnlyList<AccessPolicyHitBatchItem> DrainHits()
        {
            return _hits.Drain(_time.Now, includeCurrentMinute: true);
        }
    }
}
