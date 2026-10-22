// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using XiHan.BasicApp.Saas.Application.AccessPolicies;
using XiHan.BasicApp.Saas.Application.EventHandlers;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Configurations;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Events;
using static XiHan.BasicApp.Saas.Tests.AccessPolicyTestKit;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 登录失败自动封禁：同一 IP 窗口内失败到阈值即封，再犯翻倍且有上限，放行的 IP 不计数，手动解封清计数
/// </summary>
public sealed class LoginBanServiceTests
{
    private const string Ip = "203.0.113.9";

    /// <summary>
    /// 到阈值才封；封禁到期自动解开
    /// </summary>
    [Fact]
    public async Task Failures_BanAtThresholdUntilExpiry()
    {
        var harness = new Harness();

        await harness.FailAsync(Ip, 2);
        Assert.Null(await harness.Service.GetBanAsync(Ip));

        await harness.FailAsync(Ip, 1);
        Assert.Equal(Now.AddMinutes(30), await harness.Service.GetBanAsync(Ip));

        harness.Time.Now = Now.AddMinutes(30);
        Assert.Null(await harness.Service.GetBanAsync(Ip));
    }

    /// <summary>
    /// 24 小时内再次被封时长翻倍，不超过上限
    /// </summary>
    [Fact]
    public async Task RepeatedBans_DoubleUpToMaximum()
    {
        var harness = new Harness();
        harness.Settings.MaxBanMinutes = 90;
        var expected = new[] { 30, 60, 90, 90 };

        foreach (var minutes in expected)
        {
            await harness.FailAsync(Ip, 3);
            var until = await harness.Service.GetBanAsync(Ip);
            Assert.Equal(harness.Time.Now.AddMinutes(minutes), until);
            harness.Time.Now = until!.Value;
        }
    }

    /// <summary>
    /// 计数窗口过了重新计
    /// </summary>
    [Fact]
    public async Task FailuresOutsideWindow_StartOver()
    {
        var harness = new Harness();

        await harness.FailAsync(Ip, 2);
        harness.Time.Now = Now.AddMinutes(10);
        await harness.FailAsync(Ip, 2);

        Assert.Null(await harness.Service.GetBanAsync(Ip));
    }

    /// <summary>
    /// 参数关着、或认证前已命中平台放行策略的请求，都不计数
    /// </summary>
    [Fact]
    public async Task DisabledOrAllowedRequests_AreNotCounted()
    {
        var harness = new Harness();
        harness.Settings.IsEnabled = false;
        await harness.FailAsync(Ip, 5);
        Assert.Null(await harness.Service.GetBanAsync(Ip));

        harness.Settings.IsEnabled = true;
        harness.HttpContextAccessor.HttpContext = new DefaultHttpContext();
        harness.HttpContextAccessor.HttpContext.Items[AccessPolicyRequests.PlatformAllowedItemKey] = true;
        await harness.FailAsync(Ip, 5);
        Assert.Null(await harness.Service.GetBanAsync(Ip));
    }

    /// <summary>
    /// 手动解封：立刻生效，并清掉计数与历史（再被封从首次时长算）
    /// </summary>
    [Fact]
    public async Task Unban_ClearsBanCounterAndHistory()
    {
        var harness = new Harness();
        await harness.FailAsync(Ip, 3);
        Assert.Single(await harness.Service.ListAsync());

        Assert.True(await harness.Service.UnbanAsync(Ip));
        Assert.Null(await harness.Service.GetBanAsync(Ip));
        Assert.Empty(await harness.Service.ListAsync());

        await harness.FailAsync(Ip, 2);
        Assert.Null(await harness.Service.GetBanAsync(Ip));
        await harness.FailAsync(Ip, 1);
        Assert.Equal(Now.AddMinutes(30), await harness.Service.GetBanAsync(Ip));
    }

    /// <summary>
    /// 「没被封」的本地缓存：同一个服务里封禁后立刻可见
    /// </summary>
    [Fact]
    public async Task NotBannedCache_IsForgottenWhenBanned()
    {
        var harness = new Harness();
        Assert.Null(await harness.Service.GetBanAsync(Ip));

        await harness.FailAsync(Ip, 3);

        Assert.NotNull(await harness.Service.GetBanAsync(Ip));
    }

    /// <summary>
    /// 失败事件计数：访问策略拒绝的登录不算；计数出错不影响登录失败本身
    /// </summary>
    [Fact]
    public async Task EventHandler_SkipsAccessDeniedAndSwallowsErrors()
    {
        var bans = new Mock<ILoginBanService>();
        var handler = new LoginAutoBanEventHandler(bans.Object, NullLogger<LoginAutoBanEventHandler>.Instance);

        await handler.HandleEventAsync(Failed(LoginResult.AccessDenied));
        await handler.HandleEventAsync(Failed(LoginResult.InvalidCredentials));
        bans.Setup(service => service.RecordFailureAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Redis 不可用"));
        await handler.HandleEventAsync(Failed(LoginResult.TwoFactorFailed));

        bans.Verify(service => service.RecordFailureAsync(Ip, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    /// <summary>
    /// 进程内存储有容量上限：满了先清过期项，再淘汰最早到期的
    /// </summary>
    [Fact]
    public async Task MemoryStore_IsBounded()
    {
        var time = new ManualTimeProvider(Now);
        var store = new MemoryLoginBanStore(time);
        for (var index = 0; index < MemoryLoginBanStore.Capacity; index++)
        {
            await store.BanAsync($"ip-{index}", Now.AddMinutes(10 + index), TimeSpan.FromMinutes(10));
        }

        await store.BanAsync("newcomer", Now.AddDays(1), TimeSpan.FromDays(1));

        var bans = await store.ListAsync(Now);
        Assert.Equal(MemoryLoginBanStore.Capacity, bans.Count);
        Assert.DoesNotContain(bans, ban => ban.Ip == "ip-0");
        Assert.Contains(bans, ban => ban.Ip == "newcomer");
    }

    private static AuthLoginFailedDomainEvent Failed(LoginResult result)
    {
        return new AuthLoginFailedDomainEvent(null, 1, "admin", result, null, Now, null, Ip, null);
    }

    private sealed class Harness
    {
        public Harness()
        {
            var configuration = new Mock<ISaasConfigurationService>();
            configuration
                .Setup(service => service.GetJsonAsync(SaasConfigKeys.Security.LoginAutoBan, It.IsAny<SaasLoginAutoBanSettings>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Settings);
            Service = new LoginBanService(
                new MemoryLoginBanStore(Time),
                new LoginBanCheckCache(),
                configuration.Object,
                new TestCurrentTenant(),
                HttpContextAccessor,
                PolicyOptions(),
                Time,
                NullLogger<LoginBanService>.Instance);
        }

        public ManualTimeProvider Time { get; } = new(Now);

        public HttpContextAccessor HttpContextAccessor { get; } = new();

        public SaasLoginAutoBanSettings Settings { get; } = new() { IsEnabled = true, WindowMinutes = 10, MaxFailures = 3, BanMinutes = 30, MaxBanMinutes = 1440 };

        public LoginBanService Service { get; }

        public async Task FailAsync(string ip, int times)
        {
            for (var index = 0; index < times; index++)
            {
                await Service.RecordFailureAsync(ip);
            }
        }
    }
}
