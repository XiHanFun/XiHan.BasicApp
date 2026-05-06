#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ITaskScheduleDomainService
// Guid:08f2c6b0-40bb-44e8-a697-ea27229dd5c9
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 任务调度领域服务
/// </summary>
public interface ITaskScheduleDomainService
{
    /// <summary>
    /// 校验任务调度配置
    /// </summary>
    void EnsureScheduleConfiguration(
        TriggerType triggerType,
        string? cronExpression,
        DateTimeOffset? startTime,
        DateTimeOffset? endTime,
        int? intervalSeconds,
        int repeatCount,
        int executedCount,
        int timeoutSeconds,
        int retryCount,
        int maxRetryCount);

    /// <summary>
    /// 校验任务允许修改
    /// </summary>
    void EnsureCanModify(SysTask task);

    /// <summary>
    /// 校验任务允许删除
    /// </summary>
    void EnsureCanDelete(SysTask task);

    /// <summary>
    /// 应用启停状态
    /// </summary>
    void ApplyStatus(SysTask task, EnableStatus status, string? remark);

    /// <summary>
    /// 应用运行状态
    /// </summary>
    void ApplyRunStatus(
        SysTask task,
        RunTaskStatus runTaskStatus,
        DateTimeOffset? nextRunTime,
        DateTimeOffset? lastRunTime,
        int? executedCount,
        int? retryCount,
        string? remark);

    /// <summary>
    /// 解析下次执行时间
    /// </summary>
    DateTimeOffset? ResolveNextRunTime(SysTask task, DateTimeOffset now);
}
