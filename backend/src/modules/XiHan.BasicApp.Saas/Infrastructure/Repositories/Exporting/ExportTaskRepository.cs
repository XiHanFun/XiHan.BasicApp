#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ExportTaskRepository
// Guid:a0f7b5d3-8e1c-4d6f-92a4-4c3d5e6f7081
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/14 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 导出任务仓储实现
/// </summary>
public sealed class ExportTaskRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysExportTask>(clientResolver), IExportTaskRepository
{
    /// <inheritdoc />
    public async Task<(List<SysExportTask> Items, int Total)> GetMineAsync(long userId, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        RefAsync<int> total = 0;
        var items = await CreateQueryable()
            .Where(task => task.CreatedId == userId)
            .OrderByDescending(task => task.CreatedTime)
            .ToPageListAsync(pageIndex, pageSize, total, cancellationToken);
        return (items, total);
    }

    /// <inheritdoc />
    public async Task<SysExportTask?> GetByIdForUserAsync(long id, long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rows = await CreateQueryable()
            .Where(task => task.BasicId == id && task.CreatedId == userId)
            .Take(1)
            .ToListAsync(cancellationToken);
        return rows.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<SysExportTask?> ClaimByIdAsync(long id, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 条件更新抢占：仅当仍为 Pending 才置 Processing，防重复投递/多 worker 重复领取
        var claimed = await UpdateAsync(
            task => new SysExportTask { Status = ExportTaskStatus.Processing, StartedTime = now, Progress = 0 },
            task => task.BasicId == id && task.Status == ExportTaskStatus.Pending,
            cancellationToken);
        if (!claimed)
        {
            return null;
        }

        var rows = await CreateQueryable()
            .Where(task => task.BasicId == id)
            .Take(1)
            .ToListAsync(cancellationToken);
        var candidate = rows.FirstOrDefault();
        if (candidate is not null)
        {
            candidate.Status = ExportTaskStatus.Processing;
            candidate.StartedTime = now;
            candidate.Progress = 0;
        }

        return candidate;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<long>> GetPendingIdsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(task => task.Status == ExportTaskStatus.Pending)
            .OrderBy(task => task.CreatedTime)
            .Select(task => task.BasicId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateProgressAsync(long id, int processedCount, int progress, CancellationToken cancellationToken = default)
    {
        await UpdateAsync(
            task => new SysExportTask { ProcessedCount = processedCount, Progress = progress },
            task => task.BasicId == id,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task MarkSuccessAsync(long id, long fileId, string fileName, long fileSize, int totalCount, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        await UpdateAsync(
            task => new SysExportTask
            {
                Status = ExportTaskStatus.Success,
                FileId = fileId,
                FileName = fileName,
                FileSize = fileSize,
                TotalCount = totalCount,
                ProcessedCount = totalCount,
                Progress = 100,
                FinishedTime = now,
                ErrorMessage = null
            },
            task => task.BasicId == id,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task MarkFailedAsync(long id, string errorMessage, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        await UpdateAsync(
            task => new SysExportTask
            {
                Status = ExportTaskStatus.Failed,
                ErrorMessage = errorMessage,
                FinishedTime = now
            },
            task => task.BasicId == id,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> TryCancelPendingAsync(long id, long userId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        return await UpdateAsync(
            task => new SysExportTask
            {
                Status = ExportTaskStatus.Failed,
                ErrorMessage = "已取消",
                FinishedTime = now
            },
            task => task.BasicId == id && task.CreatedId == userId && task.Status == ExportTaskStatus.Pending,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> ResetOrphanedProcessingAsync(CancellationToken cancellationToken = default)
    {
        var reset = await UpdateAsync(
            task => new SysExportTask { Status = ExportTaskStatus.Pending, StartedTime = null, Progress = 0 },
            task => task.Status == ExportTaskStatus.Processing,
            cancellationToken);
        return reset ? 1 : 0;
    }
}
