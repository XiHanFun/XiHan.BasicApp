// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma warning disable CS1591

using XiHan.BasicApp.Core.Dtos;
using XiHan.Framework.Workflow.Abstractions.Definitions;

namespace XiHan.BasicApp.Workflow.Application.Dtos;

/// <summary>
/// 工作流定义创建 DTO（定义内容为设计器 JSON，编码/名称/节点/连线均在其中）
/// </summary>
public sealed class WorkflowDefinitionCreateDto
{
    public string DefinitionJson { get; set; } = string.Empty;
}

/// <summary>
/// 工作流定义草稿更新 DTO
/// </summary>
public sealed class WorkflowDefinitionUpdateDraftDto : BasicAppUDto
{
    public string DefinitionJson { get; set; } = string.Empty;
}

/// <summary>
/// 工作流定义标识 DTO（发布/停用/归档等生命周期操作）
/// </summary>
public sealed class WorkflowDefinitionIdDto : BasicAppDto
{
}

/// <summary>
/// 工作流定义新版本 DTO
/// </summary>
public sealed class WorkflowDefinitionNewVersionDto
{
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// 工作流定义分页查询 DTO
/// </summary>
public sealed class WorkflowDefinitionPageQueryDto : BasicAppPRDto
{
    public string? Keyword { get; set; }
    public WorkflowDefinitionStatus? Status { get; set; }
    public string? Category { get; set; }
}

/// <summary>
/// 工作流定义列表项 DTO
/// </summary>
public class WorkflowDefinitionListItemDto : BasicAppDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public WorkflowDefinitionStatus Status { get; set; }
    public bool EnableCompensation { get; set; }
    public DateTime? PublishTime { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
}

/// <summary>
/// 工作流定义详情 DTO
/// </summary>
public sealed class WorkflowDefinitionDetailDto : WorkflowDefinitionListItemDto
{
    public string DefinitionJson { get; set; } = string.Empty;
}
