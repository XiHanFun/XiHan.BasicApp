// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Services;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 审批进度应用层映射器
/// </summary>
public static class WorkflowProgressApplicationMapper
{
    /// <summary>
    /// 审批进度转 DTO
    /// </summary>
    /// <param name="progress">审批进度</param>
    /// <returns>DTO</returns>
    public static WorkflowProgressDto ToDto(WorkflowBusinessProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress);

        return new WorkflowProgressDto
        {
            InstanceId = progress.InstanceId,
            Status = progress.Status,
            Outcome = progress.Outcome,
            PendingNodeNames = [.. progress.PendingNodeNames],
            PendingAssigneeNames = [.. progress.PendingAssignees.Select(assignee => assignee.DisplayName)],
            FaultMessage = progress.FaultMessage,
            StartTime = progress.StartTime,
            EndTime = progress.EndTime
        };
    }
}
