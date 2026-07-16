#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:WorkflowTodoDtos
// Guid:16f0d8a4-c752-4e91-b3d6-80e29c54f7b1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/17 10:19:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

#pragma warning disable CS1591

using XiHan.BasicApp.Core.Dtos;
using XiHan.Framework.Workflow.Abstractions.Runtime;

namespace XiHan.BasicApp.Workflow.Application.Dtos;

/// <summary>
/// 我的待办分页查询 DTO
/// </summary>
public sealed class WorkflowTodoPageQueryDto : BasicAppPRDto
{
    public string? Keyword { get; set; }
}

/// <summary>
/// 我的待办列表项 DTO（键为任务标识，即书签标识）
/// </summary>
public sealed class WorkflowTodoListItemDto
{
    public string TaskId { get; set; } = string.Empty;
    public string InstanceId { get; set; } = string.Empty;
    public string InstanceName { get; set; } = string.Empty;
    public string DefinitionCode { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public DateTime CreationTime { get; set; }
}

/// <summary>
/// 待办办理 DTO
/// </summary>
public sealed class WorkflowTodoCompleteDto
{
    public string TaskId { get; set; } = string.Empty;

    /// <summary>办理结果（approved/rejected 或业务自定义）</summary>
    public string Outcome { get; set; } = string.Empty;

    public string? Comment { get; set; }

    /// <summary>附加业务变量（JSON 对象文本）</summary>
    public string? VariablesJson { get; set; }
}

/// <summary>
/// 待办办理结果 DTO
/// </summary>
public sealed class WorkflowTodoCompleteResultDto
{
    public string InstanceId { get; set; } = string.Empty;
    public WorkflowInstanceStatus InstanceStatus { get; set; }
}

/// <summary>
/// 待办转办 DTO
/// </summary>
public sealed class WorkflowTodoTransferDto
{
    public string TaskId { get; set; } = string.Empty;
    public string TargetAssigneeId { get; set; } = string.Empty;
    public string? Comment { get; set; }
}

/// <summary>
/// 待办加签 DTO
/// </summary>
public sealed class WorkflowTodoAddAssigneesDto
{
    public string TaskId { get; set; } = string.Empty;
    public List<string> AssigneeIds { get; set; } = [];
    public string? Comment { get; set; }
}
