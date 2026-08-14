// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
    /// <summary>
    /// 获取当前用户的导出任务分页（按创建时间倒序）
    /// </summary>
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

    /// <summary>
    /// 按主键获取当前用户的导出任务（自鉴权：仅返回本人创建的）
    /// </summary>
    public async Task<SysExportTask?> GetByIdForUserAsync(long id, long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rows = await CreateQueryable()
            .Where(task => task.BasicId == id && task.CreatedId == userId)
            .Take(1)
            .ToListAsync(cancellationToken);
        return rows.FirstOrDefault();
    }

    /// <summary>
    /// 按主键原子领取指定任务（仅当仍为 Pending 才置 Processing）；领取失败（已执行/取消/重复投递）返回 null。
    /// 队列消费者据队列项的任务 id 调用，跨租户。
    /// </summary>
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

    /// <summary>
    /// 获取所有待执行（Pending）任务的主键（按创建时间升序）；后台启动恢复时用于重投队列，跨租户。
    /// </summary>
    public async Task<IReadOnlyList<long>> GetPendingIdsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(task => task.Status == ExportTaskStatus.Pending)
            .OrderBy(task => task.CreatedTime)
            .Select(task => task.BasicId)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 回写进度（已处理行数 + 百分比）
    /// </summary>
    public async Task UpdateProgressAsync(long id, int processedCount, int progress, CancellationToken cancellationToken = default)
    {
        await UpdateAsync(
            task => new SysExportTask { ProcessedCount = processedCount, Progress = progress },
            task => task.BasicId == id,
            cancellationToken);
    }

    /// <summary>
    /// 标记成功（关联产物文件 + 完成时间 + 进度 100）
    /// </summary>
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

    /// <summary>
    /// 标记失败（错误信息 + 完成时间）
    /// </summary>
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

    /// <summary>
    /// 尝试取消待执行任务（自鉴权 + 仅 Pending 可取消）；成功返回 true。
    /// </summary>
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

    /// <summary>
    /// 复位崩溃残留的执行中任务（Processing → Pending）；worker 启动时调用，跨租户。
    /// </summary>
    public async Task<int> ResetOrphanedProcessingAsync(CancellationToken cancellationToken = default)
    {
        var reset = await UpdateAsync(
            task => new SysExportTask { Status = ExportTaskStatus.Pending, StartedTime = null, Progress = 0 },
            task => task.Status == ExportTaskStatus.Processing,
            cancellationToken);
        return reset ? 1 : 0;
    }
}
