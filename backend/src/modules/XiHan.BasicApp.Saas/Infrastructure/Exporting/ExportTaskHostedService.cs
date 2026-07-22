// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;
using XiHan.BasicApp.Saas.Application.Exporting;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Caching.Distributed.Abstracts;
using XiHan.Framework.Tasks.BackgroundServices;

namespace XiHan.BasicApp.Saas.Infrastructure.Exporting;

/// <summary>
/// 导出任务消费消息：提交时入 <see cref="IRedisDelayQueue{T}"/>，后台拉取后执行。
/// </summary>
public sealed class ExportTaskMessage : IBackgroundTaskItem
{
    /// <summary>
    /// 导出任务主键（SysExportTask.BasicId）。
    /// </summary>
    public long ExportTaskId { get; init; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; init; }

    /// <inheritdoc />
    public int RetryCount { get; set; }

    /// <inheritdoc />
    [JsonIgnore]
    public string TaskId => ExportTaskId.ToString();

    /// <inheritdoc />
    [JsonIgnore]
    public object? Data => null;
}

/// <summary>
/// 导出任务后台执行服务：从延迟队列拉取任务并执行（拉不到等待、拉到消费、可并发）。
/// </summary>
/// <remarks>
/// 提交侧把 <see cref="ExportTaskMessage"/> 推入延迟队列；本服务（基于 <see cref="XiHanBackgroundServiceBase{T}"/>）
/// 拉取后原子领取（<c>ClaimByIdAsync</c> Pending→Processing，去重 + 状态机）并交 <see cref="IExportExecutor"/> 执行。
/// 启动时复位崩溃残留的 Processing→Pending 并重投所有 Pending（队列项随 Redis 持久，仅覆盖在途丢失/Redis 数据丢失）。
/// 导出占内存，并发上限设为 2。
/// </remarks>
public sealed class ExportTaskHostedService : XiHanBackgroundServiceBase<ExportTaskHostedService>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRedisDelayQueue<ExportTaskMessage> _queue;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ExportTaskHostedService(
        IServiceScopeFactory scopeFactory,
        IRedisDelayQueue<ExportTaskMessage> queue,
        IOptions<XiHanBackgroundServiceOptions> options,
        ILogger<ExportTaskHostedService> logger)
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
        var message = (ExportTaskMessage)item;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IExportTaskRepository>();

            // 原子领取：仅 Pending 才置 Processing（去重 + 跳过已取消/已执行/重复投递）
            var task = await repository.ClaimByIdAsync(message.ExportTaskId, DateTimeOffset.UtcNow, cancellationToken);
            if (task is null)
            {
                return;
            }

            var executor = scope.ServiceProvider.GetRequiredService<IExportExecutor>();
            await executor.ExecuteAsync(task, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "导出任务处理异常：{TaskId}", message.ExportTaskId);
        }
    }

    /// <summary>
    /// 导出占内存，限并发 2；队列空时 3s 再查。
    /// </summary>
    private static IDynamicServiceConfig BuildConfig(IOptions<XiHanBackgroundServiceOptions> options)
    {
        var config = new DynamicServiceConfig(options);
        config.UpdateMaxConcurrentTasks(2);
        config.UpdateIdleDelay(3000);
        return config;
    }

    /// <summary>
    /// 启动恢复：复位崩溃残留的 Processing→Pending，并把所有 Pending 重投队列（ClaimByIdAsync 保证不重复执行）。
    /// </summary>
    private async Task RecoverPendingAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IExportTaskRepository>();

            await repository.ResetOrphanedProcessingAsync(cancellationToken);

            var ids = await repository.GetPendingIdsAsync(cancellationToken);
            foreach (var id in ids)
            {
                await _queue.EnqueueAsync(new ExportTaskMessage { ExportTaskId = id, CreatedAt = DateTimeOffset.UtcNow }, TimeSpan.Zero, cancellationToken);
            }

            if (ids.Count > 0)
            {
                Logger.LogInformation("导出启动恢复：重投 {Count} 个待执行任务", ids.Count);
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "导出启动恢复失败（忽略，继续启动）");
        }
    }
}
