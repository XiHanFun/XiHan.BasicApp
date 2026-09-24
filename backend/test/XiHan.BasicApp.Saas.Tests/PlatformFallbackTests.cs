// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Infrastructure.Messaging;
using XiHan.Framework.Bot.Telegram.Options;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 平台为租户提供的默认服务：租户只看得见自己的配置，回退平台是显式的一步；
/// 平台级通道（Telegram 广播）在租户里按未配置处理。
/// </summary>
public sealed class PlatformFallbackTests
{
    /// <summary>
    /// 租户有自己的配置就用自己的，不看平台
    /// </summary>
    [Fact]
    public async Task CurrentThenPlatform_OwnConfig_WinsWithoutFallback()
    {
        var currentTenant = new TestCurrentTenant(7);
        var contexts = new List<long?>();

        var result = await currentTenant.CurrentThenPlatformAsync(() =>
        {
            contexts.Add(currentTenant.Id);
            return Task.FromResult<string?>("tenant");
        });

        Assert.Equal("tenant", result);
        Assert.Equal([7L], contexts);
    }

    /// <summary>
    /// 租户没有时切到平台再取，取完回到租户
    /// </summary>
    [Fact]
    public async Task CurrentThenPlatform_Missing_FallsBackToPlatformAndRestores()
    {
        var currentTenant = new TestCurrentTenant(7);
        var contexts = new List<long?>();

        var result = await currentTenant.CurrentThenPlatformAsync(() =>
        {
            contexts.Add(currentTenant.Id);
            return Task.FromResult<string?>(currentTenant.Id is null ? "platform" : null);
        });

        Assert.Equal("platform", result);
        Assert.Equal([7L, null], contexts);
        Assert.Equal(7L, currentTenant.Id);
    }

    /// <summary>
    /// 平台自己取不到就是没有，不再回退
    /// </summary>
    [Fact]
    public async Task CurrentThenPlatform_InPlatform_DoesNotRetry()
    {
        var currentTenant = new TestCurrentTenant();
        var calls = 0;

        var result = await currentTenant.CurrentThenPlatformAsync(() =>
        {
            calls++;
            return Task.FromResult<string?>(null);
        });

        Assert.Null(result);
        Assert.Equal(1, calls);
    }

    /// <summary>
    /// Telegram 广播通道只在平台生效：租户的通知不借平台的机器人
    /// </summary>
    [Fact]
    public async Task TelegramConfigStore_OnlyInPlatform()
    {
        var currentTenant = new TestCurrentTenant();
        var services = new ServiceCollection();
        services.AddSingleton<ICurrentTenant>(currentTenant);
        using var provider = services.BuildServiceProvider();
        var options = new TelegramOptions { Enabled = true, Token = "platform-token" };
        var store = new SaasTelegramConfigStore(new StaticOptionsMonitor(options), provider.GetRequiredService<IServiceScopeFactory>());

        var platform = await store.GetAsync();
        TelegramOptions? tenant;
        using (currentTenant.Change(7))
        {
            tenant = await store.GetAsync();
        }

        Assert.Same(options, platform);
        Assert.Null(tenant);
    }

    private sealed class StaticOptionsMonitor(TelegramOptions value) : IOptionsMonitor<TelegramOptions>
    {
        public TelegramOptions CurrentValue => value;

        public TelegramOptions Get(string? name) => value;

        public IDisposable? OnChange(Action<TelegramOptions, string?> listener) => null;
    }
}
