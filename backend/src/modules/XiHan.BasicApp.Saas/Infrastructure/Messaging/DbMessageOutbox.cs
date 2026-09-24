// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Messaging;
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
/// - 恢复：<see cref="ResetInFlightAndCollectPendingAsync"/> 启动时在每个数据作用域内复位崩溃残留的 Sending→Pending 并收集待发送以重投。
/// 邮件/短信行带落库时的租户戳：消息携带该租户，领取与恢复都在该租户作用域内执行（由调用方切入）。
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
    /// <param name="channel">渠道</param>
    /// <param name="entityId">邮件/短信行主键</param>
    /// <param name="tenantId">邮件/短信行所属租户（行上的 TenantId，平台为 0）</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task EnqueueAsync(string channel, long entityId, long tenantId, CancellationToken cancellationToken = default)
    {
        if (entityId <= 0)
        {
            return Task.CompletedTask;
        }

        var normalized = string.IsNullOrWhiteSpace(channel) ? SaasMessageChannelNames.Email : channel.Trim();
        var message = new MessageOutboxMessage { Channel = normalized, EntityId = entityId, TenantId = tenantId, CreatedAt = DateTimeOffset.UtcNow };

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

        if (string.Equals(channel, SaasMessageChannelNames.Sms, StringComparison.OrdinalIgnoreCase))
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
    /// 启动恢复：在当前数据作用域内复位崩溃残留的 Sending→Pending，并返回该作用域所有「待发送」（Pending + 可重试 Failed、且已到计划发送时间）以便重投队列。
    /// </summary>
    /// <param name="scopeTenantId">当前数据作用域的租户（平台为 0），写进重投消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<IReadOnlyList<MessageOutboxMessage>> ResetInFlightAndCollectPendingAsync(long scopeTenantId, CancellationToken cancellationToken = default)
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
        result.AddRange(emailIds.Select(id => new MessageOutboxMessage { Channel = SaasMessageChannelNames.Email, EntityId = id, TenantId = scopeTenantId, CreatedAt = now }));

        var smsIds = await client.Queryable<SysSms>()
            .Where(s => !s.IsDeleted
                && (s.ScheduledTime == null || s.ScheduledTime <= now)
                && (s.SmsStatus == SmsStatus.Pending || (s.SmsStatus == SmsStatus.Failed && s.RetryCount < s.MaxRetryCount)))
            .Select(s => s.BasicId)
            .ToListAsync(cancellationToken);
        result.AddRange(smsIds.Select(id => new MessageOutboxMessage { Channel = SaasMessageChannelNames.Sms, EntityId = id, TenantId = scopeTenantId, CreatedAt = now }));

        return result;
    }
}
