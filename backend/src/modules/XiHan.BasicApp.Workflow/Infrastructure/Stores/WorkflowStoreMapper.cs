#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:WorkflowStoreMapper
// Guid:b91f47a3-0d65-4e28-92c7-58e30d16f4b9
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/17 10:13:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Globalization;
using System.Text.Json;
using XiHan.BasicApp.Workflow.Domain.Entities;
using XiHan.Framework.Workflow.Abstractions.Definitions;
using XiHan.Framework.Workflow.Abstractions.Exceptions;
using XiHan.Framework.Workflow.Abstractions.Runtime;
using XiHan.Framework.Workflow.Builders;

namespace XiHan.BasicApp.Workflow.Infrastructure.Stores;

/// <summary>
/// 工作流存储映射器（框架运行时模型 ↔ SqlSugar 实体）
/// </summary>
/// <remarks>
/// 实体的 JSON 列是真源（完整模型快照），投影列仅供检索；两者同写同变。
/// 框架标识为雪花数值字符串，与实体 BasicId（long）双向转换。
/// </remarks>
public static class WorkflowStoreMapper
{
    /// <summary>
    /// 运行时模型 JSON 序列化选项（camelCase，与框架变量归一化约定一致）
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// 解析框架标识为主键
    /// </summary>
    /// <param name="id">框架标识（雪花数值字符串）</param>
    /// <returns>主键</returns>
    /// <exception cref="WorkflowException">标识非数值时抛出</exception>
    public static long ParseId(string id)
    {
        return long.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out var value)
            ? value
            : throw new WorkflowException($"工作流标识 {id} 不是雪花数值字符串，无法映射到数据库主键");
    }

    #region 定义

    /// <summary>
    /// 定义模型转实体（投影列 + JSON 真源）
    /// </summary>
    /// <param name="definition">定义模型</param>
    /// <param name="entity">目标实体（为空则新建）</param>
    /// <returns>实体</returns>
    public static SysWorkflowDefinition ToEntity(WorkflowDefinition definition, SysWorkflowDefinition? entity = null)
    {
        entity ??= new SysWorkflowDefinition(ParseId(definition.Id));
        entity.Code = definition.Code;
        entity.Name = definition.Name;
        entity.Version = definition.Version;
        entity.Description = definition.Description;
        entity.Category = definition.Category;
        entity.Status = definition.Status;
        entity.EnableCompensation = definition.EnableCompensation;
        entity.PublishTime = definition.PublishTime;
        entity.TenantId = definition.TenantId ?? 0;
        entity.DefinitionJson = WorkflowDefinitionJsonSerializer.Serialize(definition);
        return entity;
    }

    /// <summary>
    /// 定义实体转模型（自 JSON 真源还原）
    /// </summary>
    /// <param name="entity">实体</param>
    /// <returns>定义模型</returns>
    public static WorkflowDefinition ToModel(SysWorkflowDefinition entity)
    {
        return WorkflowDefinitionJsonSerializer.Deserialize(entity.DefinitionJson);
    }

    #endregion 定义

    #region 实例

    /// <summary>
    /// 实例模型转实体
    /// </summary>
    /// <param name="instance">实例模型</param>
    /// <returns>实体</returns>
    public static SysWorkflowInstance ToEntity(WorkflowInstance instance)
    {
        return new SysWorkflowInstance(ParseId(instance.Id))
        {
            DefinitionId = ParseId(instance.DefinitionId),
            DefinitionCode = instance.DefinitionCode,
            DefinitionVersion = instance.DefinitionVersion,
            Name = instance.Name,
            Status = instance.Status,
            CorrelationId = instance.CorrelationId,
            StarterId = instance.StarterId,
            ParentInstanceId = instance.ParentInstanceId is { } parentId ? ParseId(parentId) : null,
            Depth = instance.Depth,
            TenantId = instance.TenantId ?? 0,
            CreationTime = instance.CreationTime,
            StartTime = instance.StartTime,
            EndTime = instance.EndTime,
            FaultNodeId = instance.FaultNodeId,
            FaultMessage = Truncate(instance.FaultMessage, 2000),
            InstanceJson = JsonSerializer.Serialize(instance, JsonOptions)
        };
    }

    /// <summary>
    /// 实例实体转模型（自 JSON 真源还原）
    /// </summary>
    /// <param name="entity">实体</param>
    /// <returns>实例模型</returns>
    /// <exception cref="WorkflowException">JSON 真源损坏时抛出</exception>
    public static WorkflowInstance ToModel(SysWorkflowInstance entity)
    {
        return JsonSerializer.Deserialize<WorkflowInstance>(entity.InstanceJson, JsonOptions)
            ?? throw new WorkflowException($"实例 {entity.BasicId} 的 JSON 真源为空，无法还原");
    }

    #endregion 实例

    #region 节点实例

    /// <summary>
    /// 节点实例模型转实体
    /// </summary>
    /// <param name="nodeInstance">节点实例模型</param>
    /// <returns>实体</returns>
    public static SysWorkflowNodeInstance ToEntity(WorkflowNodeInstance nodeInstance)
    {
        return new SysWorkflowNodeInstance(ParseId(nodeInstance.Id))
        {
            InstanceId = ParseId(nodeInstance.InstanceId),
            NodeId = nodeInstance.NodeId,
            ActivityType = nodeInstance.ActivityType,
            Status = nodeInstance.Status,
            StartTime = nodeInstance.StartTime,
            EndTime = nodeInstance.EndTime,
            TenantId = nodeInstance.TenantId ?? 0,
            NodeInstanceJson = JsonSerializer.Serialize(nodeInstance, JsonOptions)
        };
    }

    /// <summary>
    /// 节点实例实体转模型（自 JSON 真源还原）
    /// </summary>
    /// <param name="entity">实体</param>
    /// <returns>节点实例模型</returns>
    /// <exception cref="WorkflowException">JSON 真源损坏时抛出</exception>
    public static WorkflowNodeInstance ToModel(SysWorkflowNodeInstance entity)
    {
        return JsonSerializer.Deserialize<WorkflowNodeInstance>(entity.NodeInstanceJson, JsonOptions)
            ?? throw new WorkflowException($"节点实例 {entity.BasicId} 的 JSON 真源为空，无法还原");
    }

    #endregion 节点实例

    #region 书签

    /// <summary>
    /// 书签模型转实体
    /// </summary>
    /// <param name="bookmark">书签模型</param>
    /// <returns>实体</returns>
    public static SysWorkflowBookmark ToEntity(WorkflowBookmark bookmark)
    {
        return new SysWorkflowBookmark(ParseId(bookmark.Id))
        {
            InstanceId = ParseId(bookmark.InstanceId),
            NodeInstanceId = ParseId(bookmark.NodeInstanceId),
            Kind = bookmark.Kind,
            Key = bookmark.Key,
            CorrelationId = bookmark.CorrelationId,
            DueTime = bookmark.DueTime,
            CreationTime = bookmark.CreationTime,
            TenantId = bookmark.TenantId ?? 0,
            BookmarkJson = JsonSerializer.Serialize(bookmark, JsonOptions)
        };
    }

    /// <summary>
    /// 书签实体转模型（自 JSON 真源还原）
    /// </summary>
    /// <param name="entity">实体</param>
    /// <returns>书签模型</returns>
    /// <exception cref="WorkflowException">JSON 真源损坏时抛出</exception>
    public static WorkflowBookmark ToModel(SysWorkflowBookmark entity)
    {
        return JsonSerializer.Deserialize<WorkflowBookmark>(entity.BookmarkJson, JsonOptions)
            ?? throw new WorkflowException($"书签 {entity.BasicId} 的 JSON 真源为空，无法还原");
    }

    #endregion 书签

    private static string? Truncate(string? text, int maxLength)
    {
        return text is null || text.Length <= maxLength ? text : text[..maxLength];
    }
}
