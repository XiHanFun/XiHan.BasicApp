// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.SignalR.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Caching;
using XiHan.BasicApp.Contracts.Realtime;
using XiHan.BasicApp.Realtime.Handlers;
using XiHan.BasicApp.Realtime.Infrastructure;
using XiHan.BasicApp.Realtime.Middleware;
using XiHan.BasicApp.Runtime;
using XiHan.Framework.Caching.Abstractions;
using XiHan.Framework.Caching.Redis;
using XiHan.Framework.Hosting.AspNetCore.Authentication;
using XiHan.Framework.Kernel;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.Messaging.Abstractions;
using XiHan.Framework.Messaging.Abstractions.Handlers;
using XiHan.Framework.Primitives;
using XiHan.Framework.Primitives.Manifests;
using XiHan.Framework.Security.Abstractions;

namespace XiHan.BasicApp.Realtime;

/// <summary>
/// 实时能力包（Tier 2）。
/// </summary>
/// <remarks>
/// SignalR Hub 的登记与映射（通知 Hub <see cref="RealtimeHubs.Notification"/> 与各包经 <see cref="RealtimeHubServiceCollectionExtensions.AddRealtimeHub{THub}"/> 登记的 Hub，由宿主经 <see cref="RealtimeEndpoints.MapRealtimeHubs"/> 映射）、
/// 连接登记与在线状态（<see cref="IRealtimePresence"/>）、推送（<see cref="IRealtimePush"/>，调用成功提交后才发出）。没有操作与权限：推送由各包在自己的操作里发起。
/// 宿主配了 Redis 时以 <c>scaleOut</c> 装配（方案 N3 第二批，用户定「配置了就跨实例」）：SignalR 接 Redis 背板（连接配置取宿主的 Redis 缓存配置，频道带同一键前缀），
/// 在线状态的连接登记存进共享缓存、按实例心跳续期，断开连接经消息代理广播到各实例；没配即全部在本实例内。
/// </remarks>
public sealed class RealtimeFeature : IHostedFeature, IDisposable
{
    private CancellationTokenSource? _stopping;
    private Task? _heartbeat;
    private PresenceBoard? _board;

    /// <summary>
    /// 单实例装配。
    /// </summary>
    public RealtimeFeature()
        : this(scaleOut: false)
    {
    }

    /// <summary>
    /// 按是否跨实例装配。
    /// </summary>
    /// <param name="scaleOut">跨实例：要求宿主同时装配 Redis 缓存（<c>caching.distributed</c>）。</param>
    public RealtimeFeature(bool scaleOut)
    {
        ScaleOut = scaleOut;
        List<CapabilityRequirement> requires =
        [
            new CapabilityRequirement(KernelCapabilities.Hosting, KernelCapabilities.Version),
            new CapabilityRequirement(SecurityCapabilities.Authentication, SecurityCapabilities.Version),
            new CapabilityRequirement(MessagingCapabilities.Queue, MessagingCapabilities.Version),
            new CapabilityRequirement(RuntimeCapabilities.Runtime, RuntimeCapabilities.Version),
        ];
        if (scaleOut)
        {
            requires.Add(new CapabilityRequirement(CachingCapabilities.Distributed, CachingCapabilities.Version));
        }

        Manifest = new Manifest
        {
            Id = AppInfo.ManifestIdPrefix + "realtime",
            Package = "XiHan.BasicApp.Realtime",
            Version = AppInfo.Version,
            Primitives = 1,
            Stability = Stability.Preview,
            Kind = ManifestKind.Capability,
            Provides = [new CapabilityDeclaration(RealtimeCapabilities.Realtime, RealtimeCapabilities.Version)],
            Requires = requires,
            Contributes = new Contributions
            {
                Pipeline = [new PipelineContribution(typeof(RealtimeDispatchMiddleware).FullName!, PipelineStage.Transaction)],
            },
            Entry = typeof(RealtimeFeature).FullName,
        };
    }

    /// <summary>
    /// 是否跨实例。
    /// </summary>
    public bool ScaleOut { get; }

    /// <inheritdoc />
    public Manifest Manifest { get; }

    /// <inheritdoc />
    public void Configure(FeatureContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var signalR = context.Services.AddSignalR();
        // Hub 绕过内核管道：认证交给框架的 XiHan 方案（与内核同一套令牌校验，ADR-0051），浏览器的 WebSocket / EventSource 只在 Hub 路径下认查询串令牌；会话门禁由过滤器做
        context.Services.AddAuthentication().AddXiHan(static options => options.QueryTokenPaths.Add(RealtimeHubs.Prefix));
        context.Services.AddSingleton<IInvocationAuthenticationFilter, SessionAuthenticationFilter>();
        context.Services.AddSingleton(RealtimeNode.Create());
        context.Services.AddSingleton<ConnectionRegistry>();
        context.Services.AddSingleton<ConnectionCloser>();
        context.Services.AddSingleton(static services => new RealtimeHubServices(services.GetRequiredService<ConnectionRegistry>(), services.GetRequiredService<RealtimeHubCatalog>()));
        context.Services.AddRealtimeHub<NotificationHub>(RealtimeHubs.Notification);
        context.Services.AddScoped<RealtimeOutbox>();
        context.Services.AddScoped<IRealtimePush>(static services => services.GetRequiredService<RealtimeOutbox>());
        // 断开连接：每个实例都要断自己的连接，以广播方式订阅（发布的实例自己那份按来源跳过）
        context.Services.AddMessageHandler<DisconnectHandler>(DisconnectHandler.Topic, SubscriptionMode.Broadcast);
        if (ScaleOut)
        {
            signalR.AddStackExchangeRedis();
            // 背板用自己的连接（SignalR 管理它的订阅与释放），配置、账号与键前缀取宿主的 Redis 缓存配置：同一个 Redis 上不同前缀的部署互不串扰
            context.Services.AddOptions<RedisOptions>().Configure<RedisCachingOptions>(static (options, redis) =>
            {
                var configuration = ConfigurationOptions.Parse(redis.Configuration);
                configuration.User = redis.User ?? configuration.User;
                configuration.Password = redis.Password ?? configuration.Password;
                configuration.ChannelPrefix = RedisChannel.Literal(redis.KeyPrefix + "realtime:");
                options.Configuration = configuration;
            });
            context.Services.AddSingleton<PresenceBoard>();
            context.Services.AddSingleton<IRealtimePresence>(static services => new RealtimePresence(services.GetRequiredService<ConnectionRegistry>(), services.GetRequiredService<PresenceBoard>()));
            context.Services.AddCacheNamespace("realtime", editable: false);
        }
        else
        {
            context.Services.AddSingleton<IRealtimePresence>(static services => new RealtimePresence(services.GetRequiredService<ConnectionRegistry>(), null));
        }

        // 排在 Transaction 阶段开头、工作单元之外：工作单元提交之后它才拿到结果
        context.AddMiddleware<RealtimeDispatchMiddleware>(PipelineStage.Transaction, Placement.Beginning);
    }

    /// <inheritdoc />
    public async Task StartAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(services);
        if (!ScaleOut)
        {
            return;
        }

        // 连接变化即重写本实例的快照；心跳续期并把本实例续进名册
        _board = services.GetRequiredService<PresenceBoard>();
        services.GetRequiredService<ConnectionRegistry>().Changed += _board.PublishAsync;
        await _board.HeartbeatAsync(cancellationToken).ConfigureAwait(false);
        _stopping = new CancellationTokenSource();
        _heartbeat = BeatAsync(_board, _stopping.Token);
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_stopping is null || _heartbeat is null || _board is null)
        {
            return;
        }

        await _stopping.CancelAsync().ConfigureAwait(false);
        await _heartbeat.ConfigureAwait(false);
        await _board.LeaveAsync(cancellationToken).ConfigureAwait(false);
        _stopping.Dispose();
        _stopping = null;
        _heartbeat = null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _stopping?.Dispose();
        _stopping = null;
    }

    private static async Task BeatAsync(PresenceBoard board, CancellationToken stopping)
    {
        using var timer = new PeriodicTimer(PresenceBoard.HeartbeatInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(stopping).ConfigureAwait(false))
            {
                await board.HeartbeatAsync(stopping).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stopping.IsCancellationRequested)
        {
            // 停机
        }
    }
}
