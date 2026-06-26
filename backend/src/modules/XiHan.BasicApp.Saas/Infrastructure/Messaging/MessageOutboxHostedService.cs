#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MessageOutboxHostedService
// Guid:7e9a1b23-4d5f-4071-ac2b-3d4e5f6a7b8c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/17 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;
using XiHan.Framework.Caching.Distributed.Abstracts;
using XiHan.Framework.Messaging.Abstractions;
using XiHan.Framework.Messaging.Models;
using XiHan.Framework.Tasks.BackgroundServices;

namespace XiHan.BasicApp.Saas.Infrastructure.Messaging;

/// <summary>
/// 发件箱消费消息：标识「某条 SysEmail/SysSms 可发送了」，由后台拉取后发送。
/// </summary>
public sealed class MessageOutboxMessage : IBackgroundTaskItem
{
    /// <summary>
    /// 渠道：email / sms。
    /// </summary>
    public string Channel { get; init; } = "email";

    /// <summary>
    /// 业务实体主键（SysEmail.BasicId / SysSms.BasicId）。
    /// </summary>
    public long EntityId { get; init; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; init; }

    /// <inheritdoc />
    public int RetryCount { get; set; }

    /// <inheritdoc />
    [JsonIgnore]
    public string TaskId => $"{Channel}:{EntityId}";

    /// <inheritdoc />
    [JsonIgnore]
    public object? Data => null;
}

/// <summary>
/// 发件箱后台发送服务：从延迟队列拉取后发送（拉不到等待、拉到消费、可并发）。框架 Messaging 仅负责路由，发件箱（后台发送编排）在业务层。
/// </summary>
/// <remarks>
/// 提交侧（<see cref="DbMessageOutbox"/>）落库后入队 <see cref="MessageOutboxMessage"/>；本服务（基于 <see cref="XiHanBackgroundServiceBase{T}"/>）
/// 拉取后原子领取（<c>TryClaimForSendingAsync</c> Pending/可重试Failed→Sending，去重 + 按 MaxRetryCount 自限），经 <see cref="IMessageDispatcher"/>
/// 走既有 Sender（EntityId 重放，加载行→发送→更新状态）。发送失败延迟重投。
/// 启动时复位崩溃残留的 Sending→Pending 并重投所有待发送。
/// </remarks>
public sealed class MessageOutboxHostedService : XiHanBackgroundServiceBase<MessageOutboxHostedService>
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRedisDelayQueue<MessageOutboxMessage> _queue;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MessageOutboxHostedService(
        IServiceScopeFactory scopeFactory,
        IRedisDelayQueue<MessageOutboxMessage> queue,
        IOptions<XiHanBackgroundServiceOptions> options,
        ILogger<MessageOutboxHostedService> logger)
        : base(logger, options, BuildConfig(options))
    {
        _scopeFactory = scopeFactory;
        _queue = queue;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RecoverPendingAsync(stoppingToken);
        await base.ExecuteAsync(stoppingToken);
    }

    /// <inheritdoc />
    protected override async Task<IEnumerable<IBackgroundTaskItem>> FetchWorkItemsAsync(int maxCount, CancellationToken cancellationToken)
    {
        return await _queue.DequeueDueAsync(maxCount, cancellationToken);
    }

    /// <inheritdoc />
    protected override async Task ProcessItemAsync(IBackgroundTaskItem item, CancellationToken cancellationToken)
    {
        var message = (MessageOutboxMessage)item;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var outbox = scope.ServiceProvider.GetRequiredService<DbMessageOutbox>();

            // 原子领取：仅 Pending / 可重试 Failed 才置 Sending（去重 + 按 MaxRetryCount 自限，超限直接丢弃该消息）
            if (!await outbox.TryClaimForSendingAsync(message.Channel, message.EntityId, cancellationToken))
            {
                return;
            }

            // 经既有调度器 + Sender（EntityId 重放）发送：加载行 → 发送 → 更新状态/重试计数
            var dispatcher = scope.ServiceProvider.GetRequiredService<IMessageDispatcher>();
            var envelope = new MessageEnvelope
            {
                Channel = message.Channel,
                Recipients = [new MessageRecipient { Address = "outbox", DisplayName = "outbox" }],
                Metadata = new Dictionary<string, string?> { ["EntityId"] = message.EntityId.ToString() }
            };
            var results = await dispatcher.DispatchAsync(envelope, cancellationToken);

            if (results.Any(result => !result.IsSuccess))
            {
                // 失败（Sender 已置 Failed + RetryCount++）：延迟重投，下次领取据 MaxRetryCount 自限；超限领取失败即丢弃
                var error = string.Join("; ", results
                    .Where(result => !result.IsSuccess)
                    .Select(result => result.ErrorMessage)
                    .Where(msg => !string.IsNullOrWhiteSpace(msg)));
                await _queue.EnqueueAsync(
                    new MessageOutboxMessage { Channel = message.Channel, EntityId = message.EntityId, RetryCount = message.RetryCount + 1, CreatedAt = DateTimeOffset.UtcNow },
                    RetryDelay,
                    cancellationToken);
                Logger.LogWarning("发件箱发送失败，将延迟重投：{Channel}:{Id}，原因：{Error}", message.Channel, message.EntityId, error);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "发件箱消息处理异常：{Channel}:{Id}", message.Channel, message.EntityId);
        }
    }

    /// <summary>
    /// 邮件/短信发送较轻，并发上限 5；队列空时 2s 再查。
    /// </summary>
    private static IDynamicServiceConfig BuildConfig(IOptions<XiHanBackgroundServiceOptions> options)
    {
        var config = new DynamicServiceConfig(options);
        config.UpdateMaxConcurrentTasks(5);
        config.UpdateIdleDelay(2000);
        return config;
    }

    /// <summary>
    /// 启动恢复：复位崩溃残留的 Sending→Pending，并把所有待发送重投队列（TryClaimForSendingAsync 保证不重复发送）。
    /// </summary>
    private async Task RecoverPendingAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var outbox = scope.ServiceProvider.GetRequiredService<DbMessageOutbox>();

            var pending = await outbox.ResetInFlightAndCollectPendingAsync(cancellationToken);
            foreach (var message in pending)
            {
                await _queue.EnqueueAsync(message, TimeSpan.Zero, cancellationToken);
            }

            if (pending.Count > 0)
            {
                Logger.LogInformation("发件箱启动恢复：重投 {Count} 条待发送", pending.Count);
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "发件箱启动恢复失败（忽略，继续启动）");
        }
    }
}
