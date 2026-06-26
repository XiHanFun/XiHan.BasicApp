#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DbMessageOutbox
// Guid:9b2c4d6e-8f01-4a23-b5c6-7d8e9f0a1b2c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/16 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Caching.Distributed.Abstracts;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Saas.Infrastructure.Messaging;

/// <summary>
/// 业务层邮件/短信发件箱：SysEmail / SysSms 表为事实源（状态/重试/定时/审计），Redis 延迟队列承载待发送工作。
/// </summary>
/// <remarks>
/// 框架 Messaging 仅负责路由（IMessageDispatcher + Sender）；「后台异步发送」由本类 + <see cref="MessageOutboxHostedService"/> 在业务层承载。
/// - 入队：<see cref="EnqueueAsync"/> 在「事务提交后」把 <see cref="MessageOutboxMessage"/> 推入延迟队列（延迟 0），由后台拉取后经 IMessageDispatcher + Sender（EntityId 重放）发送。
/// - 领取：<see cref="TryClaimForSendingAsync"/> 原子置 Sending（去重 + 按 MaxRetryCount 自限重试）。
/// - 恢复：<see cref="ResetInFlightAndCollectPendingAsync"/> 启动时复位崩溃残留的 Sending→Pending 并收集待发送以重投。
/// 注册为 Singleton，内部用 IServiceScopeFactory 取 Scoped 的 ISqlSugarClientResolver。
/// </remarks>
public sealed class DbMessageOutbox
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRedisDelayQueue<MessageOutboxMessage> _queue;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DbMessageOutbox(IServiceScopeFactory scopeFactory, IRedisDelayQueue<MessageOutboxMessage> queue, IUnitOfWorkManager unitOfWorkManager)
    {
        _scopeFactory = scopeFactory;
        _queue = queue;
        _unitOfWorkManager = unitOfWorkManager;
    }

    /// <summary>
    /// 入队待发送（SysEmail/SysSms 行已落库为 Pending）。在「事务提交后」入队，保证后台拉到时业务行已可见；无环境 UoW 时直接入队。
    /// </summary>
    public Task EnqueueAsync(string channel, long entityId, CancellationToken cancellationToken = default)
    {
        if (entityId <= 0)
        {
            return Task.CompletedTask;
        }

        var normalized = string.IsNullOrWhiteSpace(channel) ? "email" : channel.Trim();
        var message = new MessageOutboxMessage { Channel = normalized, EntityId = entityId, CreatedAt = DateTimeOffset.UtcNow };

        var uow = _unitOfWorkManager.Current;
        if (uow is not null)
        {
            uow.OnCompleted(() => _queue.EnqueueAsync(message, TimeSpan.Zero));
            return Task.CompletedTask;
        }

        return _queue.EnqueueAsync(message, TimeSpan.Zero, cancellationToken);
    }

    /// <summary>
    /// 原子领取待发送：仅 Pending / 可重试 Failed（RetryCount &lt; MaxRetryCount）才置 Sending，返回是否领取成功。
    /// </summary>
    /// <remarks>消费者发送前调用：去重（同一行只一个消费者领取）+ 按 MaxRetryCount 自限重试（超限领取失败 → 丢弃消息，行保持 Failed）。</remarks>
    public async Task<bool> TryClaimForSendingAsync(string channel, long entityId, CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var client = scope.ServiceProvider.GetRequiredService<ISqlSugarClientResolver>().GetCurrentClient();

        if (string.Equals(channel, "sms", StringComparison.OrdinalIgnoreCase))
        {
            return await client.Updateable<SysSms>()
                .SetColumns(s => s.SmsStatus == SmsStatus.Sending)
                .Where(s => s.BasicId == entityId && !s.IsDeleted
                    && (s.SmsStatus == SmsStatus.Pending || (s.SmsStatus == SmsStatus.Failed && s.RetryCount < s.MaxRetryCount)))
                .ExecuteCommandAsync(cancellationToken) > 0;
        }

        return await client.Updateable<SysEmail>()
            .SetColumns(e => e.EmailStatus == EmailStatus.Sending)
            .Where(e => e.BasicId == entityId && !e.IsDeleted
                && (e.EmailStatus == EmailStatus.Pending || (e.EmailStatus == EmailStatus.Failed && e.RetryCount < e.MaxRetryCount)))
            .ExecuteCommandAsync(cancellationToken) > 0;
    }

    /// <summary>
    /// 启动恢复：复位崩溃残留的 Sending→Pending，并返回当前所有「待发送」（Pending + 可重试 Failed、且已到计划发送时间）以便重投队列。
    /// </summary>
    public async Task<IReadOnlyList<MessageOutboxMessage>> ResetInFlightAndCollectPendingAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var client = scope.ServiceProvider.GetRequiredService<ISqlSugarClientResolver>().GetCurrentClient();
        var now = DateTimeOffset.UtcNow;

        // 复位崩溃残留的 Sending（在途发送中途崩溃）→ Pending
        await client.Updateable<SysEmail>()
            .SetColumns(e => e.EmailStatus == EmailStatus.Pending)
            .Where(e => e.EmailStatus == EmailStatus.Sending && !e.IsDeleted)
            .ExecuteCommandAsync(cancellationToken);
        await client.Updateable<SysSms>()
            .SetColumns(s => s.SmsStatus == SmsStatus.Pending)
            .Where(s => s.SmsStatus == SmsStatus.Sending && !s.IsDeleted)
            .ExecuteCommandAsync(cancellationToken);

        var result = new List<MessageOutboxMessage>();

        var emailIds = await client.Queryable<SysEmail>()
            .Where(e => !e.IsDeleted
                && (e.ScheduledTime == null || e.ScheduledTime <= now)
                && (e.EmailStatus == EmailStatus.Pending || (e.EmailStatus == EmailStatus.Failed && e.RetryCount < e.MaxRetryCount)))
            .Select(e => e.BasicId)
            .ToListAsync(cancellationToken);
        result.AddRange(emailIds.Select(id => new MessageOutboxMessage { Channel = "email", EntityId = id, CreatedAt = now }));

        var smsIds = await client.Queryable<SysSms>()
            .Where(s => !s.IsDeleted
                && (s.ScheduledTime == null || s.ScheduledTime <= now)
                && (s.SmsStatus == SmsStatus.Pending || (s.SmsStatus == SmsStatus.Failed && s.RetryCount < s.MaxRetryCount)))
            .Select(s => s.BasicId)
            .ToListAsync(cancellationToken);
        result.AddRange(smsIds.Select(id => new MessageOutboxMessage { Channel = "sms", EntityId = id, CreatedAt = now }));

        return result;
    }
}
