// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma warning disable CS1591

using XiHan.BasicApp.Core.Dtos;
using XiHan.Framework.Workflow.Abstractions.Runtime;

namespace XiHan.BasicApp.Workflow.Application.Dtos;

/// <summary>
/// 工作流实例发起 DTO
/// </summary>
public sealed class WorkflowInstanceStartDto
{
    public string DefinitionCode { get; set; } = string.Empty;

    /// <summary>定义版本（为空取最新已发布版本）</summary>
    public int? DefinitionVersion { get; set; }

    public string? Name { get; set; }
    public string? CorrelationId { get; set; }

    /// <summary>启动变量（JSON 对象文本）</summary>
    public string? VariablesJson { get; set; }
}

/// <summary>
/// 工作流实例标识 DTO
/// </summary>
public sealed class WorkflowInstanceIdDto : BasicAppDto
{
}

/// <summary>
/// 工作流实例操作 DTO（取消/终止/挂起附原因）
/// </summary>
public sealed class WorkflowInstanceOperationDto : BasicAppDto
{
    public string? Reason { get; set; }
}

/// <summary>
/// 工作流信号发布 DTO
/// </summary>
public sealed class WorkflowSignalPublishDto
{
    public string SignalName { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }

    /// <summary>信号载荷（JSON 对象文本）</summary>
    public string? PayloadJson { get; set; }
}

/// <summary>
/// 工作流信号发布结果 DTO
/// </summary>
public sealed class WorkflowSignalPublishResultDto
{
    public int ResumedCount { get; set; }
}

/// <summary>
/// 工作流实例分页查询 DTO
/// </summary>
public sealed class WorkflowInstancePageQueryDto : BasicAppPRDto
{
    public string? Keyword { get; set; }
    public WorkflowInstanceStatus? Status { get; set; }
    public string? DefinitionCode { get; set; }
    public string? CorrelationId { get; set; }
}

/// <summary>
/// 工作流实例列表项 DTO
/// </summary>
public class WorkflowInstanceListItemDto : BasicAppDto
{
    public string DefinitionCode { get; set; } = string.Empty;
    public int DefinitionVersion { get; set; }
    public string Name { get; set; } = string.Empty;
    public WorkflowInstanceStatus Status { get; set; }
    public string? CorrelationId { get; set; }
    public string? StarterId { get; set; }
    public long? ParentInstanceId { get; set; }
    public int Depth { get; set; }
    public DateTime CreationTime { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? FaultNodeId { get; set; }
    public string? FaultMessage { get; set; }
}

/// <summary>
/// 工作流实例详情 DTO（含变量、执行历史与待恢复等待点）
/// </summary>
public sealed class WorkflowInstanceDetailDto : WorkflowInstanceListItemDto
{
    /// <summary>实例变量（JSON 对象文本）</summary>
    public string VariablesJson { get; set; } = string.Empty;

    public string? CancellationReason { get; set; }
    public List<WorkflowNodeInstanceDto> NodeInstances { get; set; } = [];
    public List<WorkflowBookmarkDto> PendingBookmarks { get; set; } = [];
}

/// <summary>
/// 工作流节点实例 DTO（执行历史行）
/// </summary>
public sealed class WorkflowNodeInstanceDto
{
    public string Id { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ActivityType { get; set; } = string.Empty;
    public WorkflowNodeInstanceStatus Status { get; set; }
    public int TryCount { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? FaultMessage { get; set; }

    /// <summary>节点输出（JSON 对象文本）</summary>
    public string? OutputsJson { get; set; }
}

/// <summary>
/// 工作流书签 DTO（待恢复等待点）
/// </summary>
public sealed class WorkflowBookmarkDto
{
    public string Id { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string? Key { get; set; }
    public DateTime? DueTime { get; set; }
    public DateTime CreationTime { get; set; }
}
