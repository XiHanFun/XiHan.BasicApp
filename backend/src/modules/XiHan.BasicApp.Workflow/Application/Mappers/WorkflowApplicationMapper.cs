// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
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
public static class WorkflowApplicationMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    /// <summary>
    /// 定义实体转列表项 DTO
    /// </summary>
    public static WorkflowDefinitionListItemDto ToListItemDto(SysWorkflowDefinition entity)
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
            PublishTime = entity.PublishTime,
            CreatedTime = entity.CreatedTime
        };
    }

    /// <summary>
    /// 定义实体转详情 DTO
    /// </summary>
    public static WorkflowDefinitionDetailDto ToDetailDto(SysWorkflowDefinition entity)
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
            PublishTime = entity.PublishTime,
            CreatedTime = entity.CreatedTime,
            DefinitionJson = entity.DefinitionJson
        };
    }

    /// <summary>
    /// 定义模型转详情 DTO（生命周期操作的返回）
    /// </summary>
    public static WorkflowDefinitionDetailDto ToDetailDto(WorkflowDefinition definition)
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
            PublishTime = definition.PublishTime,
            CreatedTime = definition.CreationTime,
            DefinitionJson = WorkflowDefinitionJsonSerializer.Serialize(definition)
        };
    }

    /// <summary>
    /// 实例实体转列表项 DTO
    /// </summary>
    public static WorkflowInstanceListItemDto ToListItemDto(SysWorkflowInstance entity)
    {
        return new WorkflowInstanceListItemDto
        {
            BasicId = entity.BasicId,
            DefinitionCode = entity.DefinitionCode,
            DefinitionVersion = entity.DefinitionVersion,
            Name = entity.Name,
            Status = entity.Status,
            CorrelationId = entity.CorrelationId,
            StarterId = entity.StarterId,
            ParentInstanceId = entity.ParentInstanceId,
            Depth = entity.Depth,
            CreationTime = entity.CreationTime,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            FaultNodeId = entity.FaultNodeId,
            FaultMessage = entity.FaultMessage
        };
    }

    /// <summary>
    /// 实例模型转列表项 DTO（实例操作的返回）
    /// </summary>
    public static WorkflowInstanceListItemDto ToListItemDto(WorkflowInstance instance)
    {
        return new WorkflowInstanceListItemDto
        {
            BasicId = WorkflowStoreMapper.ParseId(instance.Id),
            DefinitionCode = instance.DefinitionCode,
            DefinitionVersion = instance.DefinitionVersion,
            Name = instance.Name,
            Status = instance.Status,
            CorrelationId = instance.CorrelationId,
            StarterId = instance.StarterId,
            ParentInstanceId = instance.ParentInstanceId is { } parentId ? WorkflowStoreMapper.ParseId(parentId) : null,
            Depth = instance.Depth,
            CreationTime = instance.CreationTime,
            StartTime = instance.StartTime,
            EndTime = instance.EndTime,
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
        IReadOnlyList<WorkflowBookmark> bookmarks)
    {
        return new WorkflowInstanceDetailDto
        {
            BasicId = WorkflowStoreMapper.ParseId(instance.Id),
            DefinitionCode = instance.DefinitionCode,
            DefinitionVersion = instance.DefinitionVersion,
            Name = instance.Name,
            Status = instance.Status,
            CorrelationId = instance.CorrelationId,
            StarterId = instance.StarterId,
            ParentInstanceId = instance.ParentInstanceId is { } parentId ? WorkflowStoreMapper.ParseId(parentId) : null,
            Depth = instance.Depth,
            CreationTime = instance.CreationTime,
            StartTime = instance.StartTime,
            EndTime = instance.EndTime,
            FaultNodeId = instance.FaultNodeId,
            FaultMessage = instance.FaultMessage,
            CancellationReason = instance.CancellationReason,
            VariablesJson = JsonSerializer.Serialize(instance.Variables, JsonOptions),
            NodeInstances = [.. nodeInstances.Select(ToNodeInstanceDto)],
            PendingBookmarks = [.. bookmarks.Select(ToBookmarkDto)]
        };
    }

    /// <summary>
    /// 节点实例模型转 DTO
    /// </summary>
    public static WorkflowNodeInstanceDto ToNodeInstanceDto(WorkflowNodeInstance nodeInstance)
    {
        return new WorkflowNodeInstanceDto
        {
            Id = nodeInstance.Id,
            NodeId = nodeInstance.NodeId,
            Name = nodeInstance.Name,
            ActivityType = nodeInstance.ActivityType,
            Status = nodeInstance.Status,
            TryCount = nodeInstance.TryCount,
            StartTime = nodeInstance.StartTime,
            EndTime = nodeInstance.EndTime,
            FaultMessage = nodeInstance.FaultMessage,
            OutputsJson = nodeInstance.Outputs.Count == 0 ? null : JsonSerializer.Serialize(nodeInstance.Outputs, JsonOptions)
        };
    }

    /// <summary>
    /// 书签模型转 DTO
    /// </summary>
    public static WorkflowBookmarkDto ToBookmarkDto(WorkflowBookmark bookmark)
    {
        return new WorkflowBookmarkDto
        {
            Id = bookmark.Id,
            NodeId = bookmark.NodeId,
            Kind = bookmark.Kind,
            Key = bookmark.Key,
            DueTime = bookmark.DueTime,
            CreationTime = bookmark.CreationTime
        };
    }

    /// <summary>
    /// 待办模型转列表项 DTO
    /// </summary>
    public static WorkflowTodoListItemDto ToTodoListItemDto(WorkflowUserTask task)
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
            CreationTime = task.CreationTime
        };
    }
}
