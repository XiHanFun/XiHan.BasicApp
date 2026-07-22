// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.EventBus.Abstractions.Local;

namespace XiHan.BasicApp.Saas.Application.EventHandlers;

/// <summary>
/// 文件删除事件处理器
/// </summary>
/// <remarks>
/// 当文件被删除时，软删除所有关联的存储记录，并将物理清理任务加入队列。
/// </remarks>
public sealed class FileDeletedEventHandler : ILocalEventHandler<FileDeletedDomainEvent>
{
    private readonly ISqlSugarClientResolver _clientResolver;
    private readonly ILogger<FileDeletedEventHandler> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public FileDeletedEventHandler(
        ISqlSugarClientResolver clientResolver,
        ILogger<FileDeletedEventHandler> logger)
    {
        _clientResolver = clientResolver ?? throw new ArgumentNullException(nameof(clientResolver));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 处理文件删除事件
    /// </summary>
    /// <param name="eventData">事件数据</param>
    public async Task HandleEventAsync(FileDeletedDomainEvent eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        _logger.LogInformation(
            "[FileDeleted] File deleted: FileId={FileId}, FileName={FileName}, PhysicalDeleted={PhysicalDeleted}",
            eventData.FileId, eventData.FileName, eventData.PhysicalDeleted);

        if (eventData.PhysicalDeleted)
        {
            // 物理删除：直接清理存储记录和物理文件
            await PhysicalDeleteStoragesAsync(eventData);
        }
        else
        {
            // 软删除：标记存储记录为已删除
            await SoftDeleteStoragesAsync(eventData);
        }
    }

    /// <summary>
    /// 软删除所有关联的文件存储记录
    /// </summary>
    private async Task SoftDeleteStoragesAsync(FileDeletedDomainEvent eventData)
    {
        try
        {
            var db = _clientResolver.GetCurrentClient();
            var now = DateTimeOffset.UtcNow;

            var storages = await db.Queryable<SysFileStorage>()
                .Where(s => s.FileId == eventData.FileId && !s.IsDeleted)
                .ToListAsync();

            if (storages.Count == 0)
            {
                _logger.LogDebug(
                    "[FileDeleted] No storage records found for FileId={FileId}", eventData.FileId);
                return;
            }

            // 软删除所有存储记录
            foreach (var storage in storages)
            {
                storage.IsDeleted = true;
                storage.Status = FileStorageStatus.Deleted;
            }

            await db.Updateable(storages)
                .UpdateColumns(s => new { s.IsDeleted, s.Status })
                .ExecuteCommandAsync();

            _logger.LogInformation(
                "[FileDeleted] Soft-deleted {Count} storage records for FileId={FileId}",
                storages.Count, eventData.FileId);

            // 将物理清理任务加入队列（桩实现）
            await QueuePhysicalCleanupAsync(eventData.FileId, storages.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[FileDeleted] Failed to soft-delete storage records for FileId={FileId}", eventData.FileId);
        }
    }

    /// <summary>
    /// 物理删除所有关联的文件存储记录
    /// </summary>
    private async Task PhysicalDeleteStoragesAsync(FileDeletedDomainEvent eventData)
    {
        try
        {
            var db = _clientResolver.GetCurrentClient();

            var deletedCount = await db.Deleteable<SysFileStorage>()
                .Where(s => s.FileId == eventData.FileId)
                .ExecuteCommandAsync();

            _logger.LogWarning(
                "[FileDeleted] Physically deleted {Count} storage records for FileId={FileId}",
                deletedCount, eventData.FileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[FileDeleted] Failed to physically delete storage records for FileId={FileId}", eventData.FileId);
        }
    }

    /// <summary>
    /// 将物理文件清理任务加入队列（桩实现）
    /// </summary>
    /// <remarks>
    /// 当前为桩实现。后续可接入 IBackgroundJobQueue 或消息队列进行异步物理文件清理。
    /// 物理清理任务负责：
    /// 1. 删除本地/OSS/CDN 上的实际文件
    /// 2. 清理 SysFileStorage 中的软删记录
    /// 3. 更新租户存储使用量统计
    /// </remarks>
    private Task QueuePhysicalCleanupAsync(long fileId, int storageCount)
    {
        _logger.LogInformation(
            "[FileDeleted][CleanupQueue] Physical cleanup queued for FileId={FileId}, {Count} storage(s) to clean",
            fileId, storageCount);

        // TODO: 接入消息队列或后台作业
        // 1. 将清理任务信息写入 SysBackgroundJob 或直接入队
        // 2. 后台 Worker 消费任务，执行实际文件清理

        return Task.CompletedTask;
    }
}
