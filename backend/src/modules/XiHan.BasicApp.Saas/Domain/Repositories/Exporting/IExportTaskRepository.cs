#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IExportTaskRepository
// Guid:9e3f6a4c-7d0b-4c5e-bf81-3b2c4d5e6f70
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/14 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 导出任务仓储接口
/// </summary>
public interface IExportTaskRepository : ISaasRepository<SysExportTask>
{
    /// <summary>
    /// 获取当前用户的导出任务分页（按创建时间倒序）
    /// </summary>
    Task<(List<SysExportTask> Items, int Total)> GetMineAsync(long userId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按主键获取当前用户的导出任务（自鉴权：仅返回本人创建的）
    /// </summary>
    Task<SysExportTask?> GetByIdForUserAsync(long id, long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按主键原子领取指定任务（仅当仍为 Pending 才置 Processing）；领取失败（已执行/取消/重复投递）返回 null。
    /// 队列消费者据队列项的任务 id 调用，跨租户。
    /// </summary>
    Task<SysExportTask?> ClaimByIdAsync(long id, DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有待执行（Pending）任务的主键（按创建时间升序）；后台启动恢复时用于重投队列，跨租户。
    /// </summary>
    Task<IReadOnlyList<long>> GetPendingIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 回写进度（已处理行数 + 百分比）
    /// </summary>
    Task UpdateProgressAsync(long id, int processedCount, int progress, CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记成功（关联产物文件 + 完成时间 + 进度 100）
    /// </summary>
    Task MarkSuccessAsync(long id, long fileId, string fileName, long fileSize, int totalCount, DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记失败（错误信息 + 完成时间）
    /// </summary>
    Task MarkFailedAsync(long id, string errorMessage, DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>
    /// 尝试取消待执行任务（自鉴权 + 仅 Pending 可取消）；成功返回 true。
    /// </summary>
    Task<bool> TryCancelPendingAsync(long id, long userId, DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>
    /// 复位崩溃残留的执行中任务（Processing → Pending）；worker 启动时调用，跨租户。
    /// </summary>
    Task<int> ResetOrphanedProcessingAsync(CancellationToken cancellationToken = default);
}
