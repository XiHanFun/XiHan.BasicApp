#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FileUploadedEventHandler
// Guid:4d5e6f7a-8b9c-0123-defa-234567890123
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/12 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.EventBus.Abstractions.Local;

namespace XiHan.BasicApp.Saas.Application.EventHandlers;

/// <summary>
/// 文件上传完成事件处理器
/// </summary>
/// <remarks>
/// 当文件上传完成后，创建主存储记录并触发病毒扫描（桩实现）。
/// </remarks>
public sealed class FileUploadedEventHandler : ILocalEventHandler<FileUploadedDomainEvent>
{
    private readonly ISqlSugarClientResolver _clientResolver;
    private readonly ILogger<FileUploadedEventHandler> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public FileUploadedEventHandler(
        ISqlSugarClientResolver clientResolver,
        ILogger<FileUploadedEventHandler> logger)
    {
        _clientResolver = clientResolver ?? throw new ArgumentNullException(nameof(clientResolver));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 处理文件上传完成事件
    /// </summary>
    /// <param name="eventData">事件数据</param>
    public async Task HandleEventAsync(FileUploadedDomainEvent eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        _logger.LogInformation(
            "[FileUploaded] File uploaded: FileId={FileId}, StorageId={StorageId}, FileName={FileName}, Size={FileSize}",
            eventData.FileId, eventData.StorageId, eventData.FileName, eventData.FileSize);

        // 确保主存储记录已创建（大部分场景下上传服务已完成此操作，此处为幂等保护）
        await EnsurePrimaryStorageAsync(eventData);

        // 触发病毒扫描桩
        await TriggerVirusScanStubAsync(eventData);
    }

    /// <summary>
    /// 确保主存储记录存在（幂等）
    /// </summary>
    private async Task EnsurePrimaryStorageAsync(FileUploadedDomainEvent eventData)
    {
        try
        {
            var db = _clientResolver.GetCurrentClient();

            var existing = await db.Queryable<SysFileStorage>()
                .Where(s => s.BasicId == eventData.StorageId && !s.IsDeleted)
                .FirstAsync();

            if (existing is not null)
            {
                // 存储记录已存在，标记为主存储
                if (!existing.IsPrimary)
                {
                    existing.IsPrimary = true;
                    existing.Status = FileStorageStatus.Normal;
                    await db.Updateable(existing)
                        .UpdateColumns(s => new { s.IsPrimary, s.Status })
                        .ExecuteCommandAsync();
                }

                _logger.LogDebug(
                    "[FileUploaded] Primary storage record verified: StorageId={StorageId}", eventData.StorageId);
                return;
            }

            // 存储记录不存在，创建新记录
            var now = DateTimeOffset.UtcNow;
            var storage = new SysFileStorage
            {
                FileId = eventData.FileId,
                StorageType = FileStorageType.Local,
                StoragePath = eventData.FileName,
                IsPrimary = true,
                Status = FileStorageStatus.Normal,
                UploadedAt = now,
                TenantId = eventData.TenantId
            };

            await db.Insertable(storage).ExecuteCommandAsync();

            _logger.LogInformation(
                "[FileUploaded] Created primary storage record: StorageId={StorageId}, FileId={FileId}",
                eventData.StorageId, eventData.FileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[FileUploaded] Failed to ensure primary storage for FileId={FileId}, StorageId={StorageId}",
                eventData.FileId, eventData.StorageId);
        }
    }

    /// <summary>
    /// 触发病毒扫描（桩实现）
    /// </summary>
    /// <remarks>
    /// 当前为桩实现，后续可接入 ClamAV 或其他病毒扫描服务。
    /// </remarks>
    private Task TriggerVirusScanStubAsync(FileUploadedDomainEvent eventData)
    {
        _logger.LogInformation(
            "[FileUploaded][VirusScan] Virus scan stub triggered for FileId={FileId}, FileName={FileName}",
            eventData.FileId, eventData.FileName);

        // TODO: 接入真实病毒扫描服务 (ClamAV / 第三方安全服务)
        // 1. 将文件推送至病毒扫描队列
        // 2. 扫描完成后通过回调或事件更新 SysFile.Status 和 SysFileStorage.Status
        // 3. 若发现病毒，标记文件为已损坏/已隔离，并通知上传者

        return Task.CompletedTask;
    }
}
