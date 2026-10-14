// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Workflow.Application.Dtos;
using XiHan.BasicApp.Workflow.Domain.Entities;
using XiHan.BasicApp.Workflow.Infrastructure.Stores;
using XiHan.Framework.Workflow.Abstractions.Definitions;
using XiHan.Framework.Workflow.Abstractions.Runtime;
using XiHan.Framework.Workflow.Abstractions.UserTasks;
using XiHan.Framework.Workflow.Builders;

namespace XiHan.BasicApp.Workflow.Application.Mappers;

/// <summary>
/// 工作流应用层映射器
/// </summary>
/// <remarks>
/// 引擎时间（定义发布、实例、节点、书签、待办）按框架时钟口径转成带偏移的时间，见 <see cref="WorkflowTimes"/>。
/// </remarks>
public static class WorkflowApplicationMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    /// <summary>
    /// 定义实体转列表项 DTO
    /// </summary>
    public static WorkflowDefinitionListItemDto ToListItemDto(SysWorkflowDefinition entity, DateTimeKind clockKind)
    {
        return new WorkflowDefinitionListItemDto
        {
            BasicId = entity.BasicId,
            Code = entity.Code,
            Name = entity.Name,
            Version = entity.Version,
            Description = entity.Description,
            Category = entity.Category,
            Status = entity.Status,
            EnableCompensation = entity.EnableCompensation,
            PublishTime = WorkflowTimes.ToOffset(entity.PublishTime, clockKind),
            CreatedTime = entity.CreatedTime
        };
    }

    /// <summary>
    /// 定义实体转详情 DTO
    /// </summary>
    public static WorkflowDefinitionDetailDto ToDetailDto(SysWorkflowDefinition entity, DateTimeKind clockKind)
    {
        return new WorkflowDefinitionDetailDto
        {
            BasicId = entity.BasicId,
            Code = entity.Code,
            Name = entity.Name,
            Version = entity.Version,
            Description = entity.Description,
            Category = entity.Category,
            Status = entity.Status,
            EnableCompensation = entity.EnableCompensation,
            PublishTime = WorkflowTimes.ToOffset(entity.PublishTime, clockKind),
            CreatedTime = entity.CreatedTime,
            DefinitionJson = entity.DefinitionJson
        };
    }

    /// <summary>
    /// 定义模型转详情 DTO（生命周期操作的返回）
    /// </summary>
    public static WorkflowDefinitionDetailDto ToDetailDto(WorkflowDefinition definition, DateTimeKind clockKind)
    {
        return new WorkflowDefinitionDetailDto
        {
            BasicId = WorkflowStoreMapper.ParseId(definition.Id),
            Code = definition.Code,
            Name = definition.Name,
            Version = definition.Version,
            Description = definition.Description,
            Category = definition.Category,
            Status = definition.Status,
            EnableCompensation = definition.EnableCompensation,
            PublishTime = WorkflowTimes.ToOffset(definition.PublishTime, clockKind),
            CreatedTime = WorkflowTimes.ToOffset(definition.CreationTime, clockKind),
            DefinitionJson = WorkflowDefinitionJsonSerializer.Serialize(definition)
        };
    }

    /// <summary>
    /// 实例实体转列表项 DTO
    /// </summary>
    public static WorkflowInstanceListItemDto ToListItemDto(SysWorkflowInstance entity, DateTimeKind clockKind)
    {
        return new WorkflowInstanceListItemDto
        {
            BasicId = entity.BasicId,
            DefinitionCode = entity.DefinitionCode,
            DefinitionVersion = entity.DefinitionVersion,
            Name = entity.Name,
            Status = entity.Status,
            Outcome = entity.Outcome,
            CorrelationId = entity.CorrelationId,
            StarterId = entity.StarterId,
            ParentInstanceId = entity.ParentInstanceId,
            Depth = entity.Depth,
            CreationTime = WorkflowTimes.ToOffset(entity.CreationTime, clockKind),
            StartTime = WorkflowTimes.ToOffset(entity.StartTime, clockKind),
            EndTime = WorkflowTimes.ToOffset(entity.EndTime, clockKind),
            FaultNodeId = entity.FaultNodeId,
            FaultMessage = entity.FaultMessage
        };
    }

    /// <summary>
    /// 实例模型转列表项 DTO（实例操作的返回）
    /// </summary>
    public static WorkflowInstanceListItemDto ToListItemDto(WorkflowInstance instance, DateTimeKind clockKind)
    {
        return new WorkflowInstanceListItemDto
        {
            BasicId = WorkflowStoreMapper.ParseId(instance.Id),
            DefinitionCode = instance.DefinitionCode,
            DefinitionVersion = instance.DefinitionVersion,
            Name = instance.Name,
            Status = instance.Status,
            Outcome = instance.Outcome,
            CorrelationId = instance.CorrelationId,
            StarterId = instance.StarterId,
            ParentInstanceId = instance.ParentInstanceId is { } parentId ? WorkflowStoreMapper.ParseId(parentId) : null,
            Depth = instance.Depth,
            CreationTime = WorkflowTimes.ToOffset(instance.CreationTime, clockKind),
            StartTime = WorkflowTimes.ToOffset(instance.StartTime, clockKind),
            EndTime = WorkflowTimes.ToOffset(instance.EndTime, clockKind),
            FaultNodeId = instance.FaultNodeId,
            FaultMessage = instance.FaultMessage
        };
    }

    /// <summary>
    /// 实例模型与执行历史转详情 DTO
    /// </summary>
    public static WorkflowInstanceDetailDto ToDetailDto(
        WorkflowInstance instance,
        IReadOnlyList<WorkflowNodeInstance> nodeInstances,
        IReadOnlyList<WorkflowBookmark> bookmarks,
        DateTimeKind clockKind)
    {
        return new WorkflowInstanceDetailDto
        {
            BasicId = WorkflowStoreMapper.ParseId(instance.Id),
            DefinitionCode = instance.DefinitionCode,
            DefinitionVersion = instance.DefinitionVersion,
            Name = instance.Name,
            Status = instance.Status,
            Outcome = instance.Outcome,
            CorrelationId = instance.CorrelationId,
            StarterId = instance.StarterId,
            ParentInstanceId = instance.ParentInstanceId is { } parentId ? WorkflowStoreMapper.ParseId(parentId) : null,
            Depth = instance.Depth,
            CreationTime = WorkflowTimes.ToOffset(instance.CreationTime, clockKind),
            StartTime = WorkflowTimes.ToOffset(instance.StartTime, clockKind),
            EndTime = WorkflowTimes.ToOffset(instance.EndTime, clockKind),
            FaultNodeId = instance.FaultNodeId,
            FaultMessage = instance.FaultMessage,
            CancellationReason = instance.CancellationReason,
            VariablesJson = JsonSerializer.Serialize(instance.Variables, JsonOptions),
            NodeInstances = [.. nodeInstances.Select(nodeInstance => ToNodeInstanceDto(nodeInstance, clockKind))],
            PendingBookmarks = [.. bookmarks.Select(bookmark => ToBookmarkDto(bookmark, clockKind))]
        };
    }

    /// <summary>
    /// 节点实例模型转 DTO
    /// </summary>
    public static WorkflowNodeInstanceDto ToNodeInstanceDto(WorkflowNodeInstance nodeInstance, DateTimeKind clockKind)
    {
        return new WorkflowNodeInstanceDto
        {
            Id = nodeInstance.Id,
            NodeId = nodeInstance.NodeId,
            Name = nodeInstance.Name,
            ActivityType = nodeInstance.ActivityType,
            Status = nodeInstance.Status,
            TryCount = nodeInstance.TryCount,
            StartTime = WorkflowTimes.ToOffset(nodeInstance.StartTime, clockKind),
            EndTime = WorkflowTimes.ToOffset(nodeInstance.EndTime, clockKind),
            FaultMessage = nodeInstance.FaultMessage,
            OutputsJson = nodeInstance.Outputs.Count == 0 ? null : JsonSerializer.Serialize(nodeInstance.Outputs, JsonOptions)
        };
    }

    /// <summary>
    /// 书签模型转 DTO
    /// </summary>
    public static WorkflowBookmarkDto ToBookmarkDto(WorkflowBookmark bookmark, DateTimeKind clockKind)
    {
        return new WorkflowBookmarkDto
        {
            Id = bookmark.Id,
            NodeId = bookmark.NodeId,
            Kind = bookmark.Kind,
            Key = bookmark.Key,
            DueTime = WorkflowTimes.ToOffset(bookmark.DueTime, clockKind),
            CreationTime = WorkflowTimes.ToOffset(bookmark.CreationTime, clockKind)
        };
    }

    /// <summary>
    /// 待办模型转列表项 DTO
    /// </summary>
    public static WorkflowTodoListItemDto ToTodoListItemDto(WorkflowUserTask task, DateTimeKind clockKind)
    {
        return new WorkflowTodoListItemDto
        {
            TaskId = task.TaskId,
            InstanceId = task.InstanceId,
            InstanceName = task.InstanceName,
            DefinitionCode = task.DefinitionCode,
            NodeId = task.NodeId,
            Title = task.Title,
            CorrelationId = task.CorrelationId,
            CreationTime = WorkflowTimes.ToOffset(task.CreationTime, clockKind)
        };
    }
    /// <summary>
    /// 审批业务描述转 DTO
    /// </summary>
    public static WorkflowBusinessTypeDto ToBusinessTypeDto(WorkflowBusinessDescriptor descriptor)
    {
        return new WorkflowBusinessTypeDto
        {
            Code = descriptor.Code,
            Name = descriptor.Name,
            Description = descriptor.Description,
            FormComponent = descriptor.FormComponent,
            Outcomes = [.. descriptor.Outcomes],
            Variables = [.. descriptor.Variables.Select(variable => new WorkflowBusinessVariableDto
            {
                Name = variable.Name,
                Label = variable.Label,
                ValueType = variable.ValueType,
                Description = variable.Description
            })]
        };
    }
}
