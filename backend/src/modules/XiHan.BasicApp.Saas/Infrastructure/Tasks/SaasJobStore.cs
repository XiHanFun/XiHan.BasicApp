#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasJobStore
// Guid:3b7c8d2e-9f1a-4e56-b8d3-c7a0e5f2b918
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/12 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlSugar;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Tasks.ScheduledJobs.Abstractions;
using XiHan.Framework.Tasks.ScheduledJobs.Models;

namespace XiHan.BasicApp.Saas.Infrastructure.Tasks;

/// <summary>
/// SaaS 任务存储实现，将框架调度状态持久化到 SysTask / SysTaskLog
/// </summary>
/// <remarks>
/// 映射关系：
/// - JobName → SysTask.TaskCode（全局唯一编码）
/// - JobInfo.TenantId → SysTask.TenantId
/// - InstanceId → 内部 ConcurrentDictionary 追踪（GUID → TaskCode）
/// - JobStatus → RunTaskStatus（Pending→Pending, Running→Running, Succeeded→Success, Failed→Failed, Canceled→Stopped, Paused→Paused）
/// </remarks>
public sealed class SaasJobStore : IJobStore
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SaasJobStore> _logger;

    /// <summary>
    /// InstanceId（框架 GUID）到 TaskCode 的运行时映射
    /// </summary>
    private readonly ConcurrentDictionary<string, InstanceMapping> _instanceMappings = new();

    public SaasJobStore(IServiceScopeFactory scopeFactory, ILogger<SaasJobStore> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task SaveJobInstanceAsync(JobInstance jobInstance)
    {
        ArgumentNullException.ThrowIfNull(jobInstance);

        _logger.LogDebug("保存任务实例: {JobName} ({InstanceId})", jobInstance.JobName, jobInstance.InstanceId);

        _instanceMappings[jobInstance.InstanceId] = new InstanceMapping(jobInstance.JobName, jobInstance.TenantId);

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ITaskRepository>();

        var task = await FindTaskByCodeAsync(repository, jobInstance.JobName, jobInstance.TenantId);
        if (task is null)
        {
            _logger.LogWarning("未找到对应 SysTask: {JobName}, TenantId={TenantId}", jobInstance.JobName, jobInstance.TenantId);
            return;
        }

        task.RunTaskStatus = RunTaskStatus.Running;
        task.LastRunTime = jobInstance.StartedAt;
        await repository.UpdateAsync(task);
    }

    /// <inheritdoc />
    public async Task UpdateJobStatusAsync(string instanceId, JobStatus status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);

        _logger.LogDebug("更新任务状态: InstanceId={InstanceId}, Status={Status}", instanceId, status);

        if (!_instanceMappings.TryGetValue(instanceId, out var mapping))
        {
            _logger.LogWarning("未找到实例映射: {InstanceId}", instanceId);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ITaskRepository>();

        var task = await FindTaskByCodeAsync(repository, mapping.TaskCode, mapping.TenantId);
        if (task is null)
        {
            _logger.LogWarning("未找到对应 SysTask: {TaskCode}, TenantId={TenantId}", mapping.TaskCode, mapping.TenantId);
            return;
        }

        task.RunTaskStatus = MapToRunTaskStatus(status);
        await repository.UpdateAsync(task);
    }

    /// <inheritdoc />
    public async Task SaveJobHistoryAsync(JobHistory history)
    {
        ArgumentNullException.ThrowIfNull(history);

        _logger.LogDebug("保存任务历史: {JobName} ({InstanceId}), Status={Status}", history.JobName, history.InstanceId, history.Status);

        using var scope = _scopeFactory.CreateScope();
        var clientResolver = scope.ServiceProvider.GetRequiredService<ISqlSugarClientResolver>();
        var db = clientResolver.GetCurrentClient();

        var log = new SysTaskLog
        {
            TaskId = 0, // 由 TaskCode 反向查找填充（历史记录冗余 TaskId 信息）
            TaskCode = history.JobName,
            TaskName = history.JobName, // 历史记录冗余任务名
            BatchNumber = history.InstanceId,
            TaskStatus = MapToRunTaskStatus(history.Status),
            StartTime = history.StartedAt,
            EndTime = history.CompletedAt,
            ExecutionTime = history.DurationMilliseconds ?? 0,
            ExecutionResult = history.IsSuccess ? "Success" : null,
            ExceptionMessage = history.ErrorMessage,
            ExceptionStackTrace = history.StackTrace,
            RetryCount = history.RetryCount,
            TriggerMode = history.TriggerType.ToString(),
            ExtendData = history.ParametersJson,
            Remark = history.Remarks,
            CreatedTime = history.StartedAt,
            TenantId = history.TenantId ?? 0
        };

        // 通过 TaskCode 回填 TaskId
        if (!string.IsNullOrWhiteSpace(history.JobName))
        {
            var taskRepository = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
            var task = await FindTaskByCodeAsync(taskRepository, history.JobName, history.TenantId);
            if (task is not null)
            {
                log.TaskId = task.BasicId;
                log.TaskName = task.TaskName;
            }
        }

        await db.Insertable(log).SplitTable().ExecuteCommandAsync();
    }

    /// <inheritdoc />
    public async Task<JobInstance?> GetJobInstanceAsync(string instanceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);

        if (!_instanceMappings.TryGetValue(instanceId, out var mapping))
        {
            return null;
        }

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ITaskRepository>();

        var task = await FindTaskByCodeAsync(repository, mapping.TaskCode, mapping.TenantId);
        if (task is null)
        {
            return null;
        }

        return new JobInstance
        {
            InstanceId = instanceId,
            JobName = task.TaskCode,
            Status = MapToJobStatus(task.RunTaskStatus),
            ScheduledAt = task.NextRunTime ?? task.CreatedTime,
            StartedAt = task.LastRunTime,
            TriggerType = MapToJobTriggerType(task.TriggerType),
            TenantId = task.TenantId,
            ErrorMessage = task.Remark,
            RetryCount = task.RetryCount
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<JobHistory>> GetJobHistoryAsync(string jobName, int pageIndex = 1, int pageSize = 20)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobName);

        using var scope = _scopeFactory.CreateScope();
        var clientResolver = scope.ServiceProvider.GetRequiredService<ISqlSugarClientResolver>();
        var db = clientResolver.GetCurrentClient();

        var logs = await db.Queryable<SysTaskLog>()
            .SplitTable()
            .Where(log => log.TaskCode == jobName)
            .OrderBy(log => log.StartTime, OrderByType.Desc)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return logs.Select(MapToJobHistory).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<JobInstance>> GetRunningInstancesAsync(string jobName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobName);

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ITaskRepository>();

        var tasks = await repository.GetListAsync(
            task => task.TaskCode == jobName && task.RunTaskStatus == RunTaskStatus.Running);

        return tasks.Select(task => new JobInstance
        {
            InstanceId = task.BasicId.ToString(),
            JobName = task.TaskCode,
            Status = JobStatus.Running,
            ScheduledAt = task.NextRunTime ?? task.CreatedTime,
            StartedAt = task.LastRunTime,
            TriggerType = MapToJobTriggerType(task.TriggerType),
            TenantId = task.TenantId,
            ErrorMessage = task.Remark,
            RetryCount = task.RetryCount
        }).ToList();
    }

    /// <inheritdoc />
    public async Task CleanupHistoryAsync(int retentionDays)
    {
        if (retentionDays < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(retentionDays), retentionDays, "保留天数不能小于 0。");
        }

        _logger.LogInformation("清理过期任务历史: 保留 {RetentionDays} 天", retentionDays);

        var cutoffDate = DateTimeOffset.UtcNow.AddDays(-retentionDays);

        using var scope = _scopeFactory.CreateScope();
        var clientResolver = scope.ServiceProvider.GetRequiredService<ISqlSugarClientResolver>();
        var db = clientResolver.GetCurrentClient();

        // SqlSugar 分表删除：先跨分表查询待删除记录，再按记录逐一删除（分表删除不支持 Where 条件直接批删）
        var expiredLogs = await db.Queryable<SysTaskLog>()
            .SplitTable()
            .Where(log => log.StartTime < cutoffDate)
            .ToListAsync();

        if (expiredLogs.Count > 0)
        {
            // 逐条删除以兼容分表
            foreach (var log in expiredLogs)
            {
                await db.Deleteable(log).SplitTable().ExecuteCommandAsync();
            }

            _logger.LogInformation("已清理 {Count} 条过期的任务历史记录", expiredLogs.Count);
        }
    }

    #region Helper Methods

    private static async Task<SysTask?> FindTaskByCodeAsync(ITaskRepository repository, string taskCode, long? tenantId)
    {
        // TaskCode 为全局唯一，按 TaskCode 精确查找
        var tasks = await repository.GetListAsync(task =>
            task.TaskCode == taskCode &&
            (tenantId.HasValue ? task.TenantId == tenantId.Value : true));

        return tasks.FirstOrDefault();
    }

    /// <summary>
    /// 将 JobStatus 映射到 RunTaskStatus
    /// </summary>
    private static RunTaskStatus MapToRunTaskStatus(JobStatus status) => status switch
    {
        JobStatus.Pending => RunTaskStatus.Pending,
        JobStatus.Running => RunTaskStatus.Running,
        JobStatus.Succeeded => RunTaskStatus.Success,
        JobStatus.Failed => RunTaskStatus.Failed,
        JobStatus.Canceled => RunTaskStatus.Stopped,
        JobStatus.Paused => RunTaskStatus.Paused,
        _ => RunTaskStatus.Pending
    };

    /// <summary>
    /// 将 RunTaskStatus 映射到 JobStatus
    /// </summary>
    private static JobStatus MapToJobStatus(RunTaskStatus status) => status switch
    {
        RunTaskStatus.Pending => JobStatus.Pending,
        RunTaskStatus.Running => JobStatus.Running,
        RunTaskStatus.Success => JobStatus.Succeeded,
        RunTaskStatus.Failed => JobStatus.Failed,
        RunTaskStatus.Stopped => JobStatus.Canceled,
        RunTaskStatus.Paused => JobStatus.Paused,
        _ => JobStatus.Pending
    };

    /// <summary>
    /// 将 TriggerType 映射到 JobTriggerType
    /// </summary>
    private static JobTriggerType MapToJobTriggerType(TriggerType triggerType) => triggerType switch
    {
        TriggerType.Immediate => JobTriggerType.Manual,
        TriggerType.Schedule => JobTriggerType.Delay,
        TriggerType.Recurring => JobTriggerType.Interval,
        TriggerType.Cron => JobTriggerType.Cron,
        _ => JobTriggerType.Manual
    };

    /// <summary>
    /// 将 SysTaskLog 映射到 JobHistory
    /// </summary>
    private static JobHistory MapToJobHistory(SysTaskLog log) => new()
    {
        HistoryId = log.BasicId.ToString(),
        InstanceId = log.BatchNumber ?? string.Empty,
        JobName = log.TaskCode,
        Status = MapToJobStatus(log.TaskStatus),
        StartedAt = log.StartTime,
        CompletedAt = log.EndTime,
        DurationMilliseconds = log.ExecutionTime,
        TenantId = log.TenantId,
        TriggerType = Enum.TryParse<JobTriggerType>(log.TriggerMode, out var triggerType) ? triggerType : JobTriggerType.Manual,
        IsSuccess = log.TaskStatus == RunTaskStatus.Success,
        ErrorMessage = log.ExceptionMessage,
        StackTrace = log.ExceptionStackTrace,
        RetryCount = log.RetryCount,
        ExecutionNode = null,
        TraceId = null,
        ParametersJson = log.ExtendData,
        Remarks = log.Remark
    };

    /// <summary>
    /// InstanceId → (TaskCode, TenantId) 映射
    /// </summary>
    private sealed record InstanceMapping(string TaskCode, long? TenantId);

    #endregion
}
