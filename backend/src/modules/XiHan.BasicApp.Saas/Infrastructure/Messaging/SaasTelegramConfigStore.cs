// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.Framework.Bot.Telegram.Abstractions;
using XiHan.Framework.Bot.Telegram.Options;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Infrastructure.Messaging;

/// <summary>
/// Telegram 广播通道配置：只在平台上下文生效
/// </summary>
/// <remarks>
/// 机器人通知广播到全部提供者时，Telegram 用的是 appsettings 里平台的机器人与会话。
/// 租户没有自己的 Telegram 通道，租户的通知不能借平台的机器人发进平台的会话，在租户上下文里按未配置处理。
/// </remarks>
public sealed class SaasTelegramConfigStore : ITelegramConfigStore
{
    private readonly IOptionsMonitor<TelegramOptions> _options;
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SaasTelegramConfigStore(IOptionsMonitor<TelegramOptions> options, IServiceScopeFactory scopeFactory)
    {
        _options = options;
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// 获取当前生效配置：平台里是 appsettings 的配置，租户里没有
    /// </summary>
    public async Task<TelegramOptions?> GetAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var currentTenant = scope.ServiceProvider.GetRequiredService<ICurrentTenant>();
        return currentTenant.IsPlatformOperation() ? _options.CurrentValue : null;
    }
}
