// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Users;
using XiHan.BasicApp.Workflow.Domain.Services;
using XiHan.Framework.Workflow.Abstractions.Instances;

namespace XiHan.BasicApp.Workflow.Operations.WorkflowInstances;

/// <summary>
/// 流程实例（列表的一行）。
/// </summary>
/// <param name="Id">实例 ID。</param>
/// <param name="DefinitionId">定义 ID。</param>
/// <param name="DefinitionName">定义名。</param>
/// <param name="DefinitionVersion">定义版本。</param>
/// <param name="Title">标题（发起时给的显示名，没给取定义的显示名）。</param>
/// <param name="Status">状态。</param>
/// <param name="CorrelationId">关联 ID（审批业务的单据 ID）。</param>
/// <param name="StarterId">发起人。</param>
/// <param name="StarterName">发起人显示名。</param>
/// <param name="ParentInstanceId">父实例（子流程）。</param>
/// <param name="Depth">子流程深度，顶层为 0。</param>
/// <param name="CreatedAt">创建时间。</param>
/// <param name="EndedAt">结束时间。</param>
/// <param name="Outcome">结论。</param>
/// <param name="FaultMessage">故障信息。</param>
public sealed record WorkflowInstanceResponse(
    Guid Id,
    Guid DefinitionId,
    string DefinitionName,
    int DefinitionVersion,
    string Title,
    WorkflowInstanceStatus Status,
    string? CorrelationId,
    Guid? StarterId,
    string? StarterName,
    Guid? ParentInstanceId,
    int Depth,
    DateTimeOffset CreatedAt,
    DateTimeOffset? EndedAt,
    string? Outcome,
    string? FaultMessage)
{
    /// <summary>
    /// 从框架的实例投影。
    /// </summary>
    /// <param name="instance">实例。</param>
    /// <param name="users">发起人摘要。</param>
    /// <returns>响应。</returns>
    public static WorkflowInstanceResponse From(WorkflowInstance instance, IReadOnlyDictionary<Guid, UserSummary> users)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(users);
        var starter = WorkflowIds.UserOf(instance.StarterId);
        return new(
            WorkflowIds.ToContract(instance.Id),
            WorkflowIds.ToContract(instance.DefinitionId),
            instance.DefinitionName.Value,
            instance.DefinitionVersion,
            instance.Title,
            instance.Status,
            instance.CorrelationId,
            starter,
            starter is { } id && users.TryGetValue(id, out var user) ? user.DisplayName : null,
            WorkflowIds.ToContractOrNull(instance.ParentInstanceId),
            instance.Depth,
            instance.CreatedAt,
            instance.EndedAt,
            instance.Outcome,
            instance.Fault?.Message);
    }
}

/// <summary>
/// 流程实例详情。
/// </summary>
/// <param name="Instance">实例。</param>
/// <param name="Variables">变量（JSON 对象文本）。</param>
/// <param name="CancellationReason">取消或终止的原因。</param>
/// <param name="FaultNodeId">故障节点。</param>
/// <param name="Nodes">节点执行历史（按开始时间）。</param>
/// <param name="Waits">待恢复的等待（用户任务、定时、信号、子流程等）。</param>
/// <param name="Document">实例所用的定义（WorkflowJson），画流程图与节点状态用。</param>
public sealed record WorkflowInstanceDetailResponse(
    WorkflowInstanceResponse Instance,
    string Variables,
    string? CancellationReason,
    string? FaultNodeId,
    IReadOnlyList<WorkflowNodeInstanceResponse> Nodes,
    IReadOnlyList<WorkflowWaitResponse> Waits,
    string? Document);

/// <summary>
/// 一次节点执行。
/// </summary>
/// <param name="Id">节点实例 ID。</param>
/// <param name="NodeId">节点 ID。</param>
/// <param name="ActivityType">活动类型。</param>
/// <param name="Status">状态。</param>
/// <param name="Attempt">第几次尝试。</param>
/// <param name="StartedAt">开始时间。</param>
/// <param name="EndedAt">结束时间。</param>
/// <param name="FaultMessage">故障信息。</param>
public sealed record WorkflowNodeInstanceResponse(Guid Id, string NodeId, string ActivityType, WorkflowNodeInstanceStatus Status, int Attempt, DateTimeOffset StartedAt, DateTimeOffset? EndedAt, string? FaultMessage)
{
    /// <summary>
    /// 从框架的节点实例投影。
    /// </summary>
    /// <param name="node">节点实例。</param>
    /// <returns>响应。</returns>
    public static WorkflowNodeInstanceResponse From(WorkflowNodeInstance node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return new(WorkflowIds.ToContract(node.Id), node.NodeId, node.ActivityType.Value, node.Status, node.Attempt, node.StartedAt, node.EndedAt, node.Fault?.Message);
    }
}

/// <summary>
/// 一个待恢复的等待。
/// </summary>
/// <param name="Id">等待 ID（用户任务即任务 ID）。</param>
/// <param name="NodeId">节点 ID。</param>
/// <param name="Kind">种类：UserTask、Timer、Signal、SubWorkflow、Retry、NodeTimeout 等。</param>
/// <param name="Key">键（受理人、信号名等）。</param>
/// <param name="DueAt">到期时间。</param>
/// <param name="CreatedAt">创建时间。</param>
public sealed record WorkflowWaitResponse(Guid Id, string NodeId, string Kind, string? Key, DateTimeOffset? DueAt, DateTimeOffset CreatedAt)
{
    /// <summary>
    /// 从框架的书签摘要投影。
    /// </summary>
    /// <param name="bookmark">书签摘要。</param>
    /// <returns>响应。</returns>
    public static WorkflowWaitResponse From(WorkflowBookmarkSummary bookmark)
    {
        ArgumentNullException.ThrowIfNull(bookmark);
        return new(WorkflowIds.ToContract(bookmark.Id), bookmark.NodeId, bookmark.Kind, bookmark.Key, bookmark.DueAt, bookmark.CreatedAt);
    }
}

/// <summary>
/// 取一个流程实例。
/// </summary>
/// <param name="Id">实例 ID。</param>
public sealed record GetWorkflowInstanceRequest(Guid Id);

/// <summary>
/// 发起流程。
/// </summary>
/// <param name="Name">定义名。</param>
/// <param name="Version">版本，空即最新已发布的。</param>
/// <param name="DisplayName">实例标题，空即取定义的显示名。</param>
/// <param name="CorrelationId">关联 ID。</param>
/// <param name="Variables">变量（JSON 对象文本）。</param>
public sealed record StartWorkflowInstanceRequest(
    [property: Required, StringLength(128, MinimumLength = 1), RegularExpression("^[a-z0-9][a-z0-9._-]{0,127}$")] string Name,
    [property: Range(1, int.MaxValue)] int? Version,
    [property: StringLength(256)] string? DisplayName,
    [property: StringLength(256)] string? CorrelationId,
    [property: StringLength(100_000)] string? Variables);

/// <summary>
/// 取消、终止或挂起一批实例。
/// </summary>
/// <param name="Ids">实例 ID。</param>
/// <param name="Reason">原因。</param>
public sealed record WorkflowInstanceControlRequest([property: Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids, [property: StringLength(500)] string? Reason);

/// <summary>
/// 一批实例。
/// </summary>
/// <param name="Ids">实例 ID。</param>
public sealed record WorkflowInstanceIdsRequest([property: Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids);

/// <summary>
/// 发信号。
/// </summary>
/// <param name="Name">信号名。</param>
/// <param name="Payload">载荷（JSON 对象文本）。</param>
/// <param name="CorrelationId">关联 ID，空即不限。</param>
public sealed record SignalWorkflowInstancesRequest(
    [property: Required, StringLength(256, MinimumLength = 1)] string Name,
    [property: StringLength(100_000)] string? Payload,
    [property: StringLength(256)] string? CorrelationId);

/// <summary>
/// 发信号的结果。
/// </summary>
/// <param name="Resumed">被它恢复的等待数。</param>
public sealed record SignalWorkflowInstancesResponse(int Resumed);
