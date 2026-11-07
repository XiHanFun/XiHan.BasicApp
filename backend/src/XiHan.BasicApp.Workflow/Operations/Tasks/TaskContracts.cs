// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.Framework.Workflow.Abstractions.Instances;

namespace XiHan.BasicApp.Workflow.Operations.Tasks;

/// <summary>
/// 一条待办
/// </summary>
/// <param name="Id">任务 ID</param>
/// <param name="InstanceId">流程实例</param>
/// <param name="InstanceTitle">实例标题</param>
/// <param name="DefinitionName">流程定义名</param>
/// <param name="NodeId">节点 ID</param>
/// <param name="NodeName">节点名</param>
/// <param name="Title">任务标题</param>
/// <param name="CorrelationId">关联 ID（审批业务的单据 ID）</param>
/// <param name="CreatedAt">到达时间</param>
/// <param name="StarterId">发起人</param>
/// <param name="StarterName">发起人显示名</param>
/// <param name="BusinessNameKey">审批业务显示名的文案键（不是审批业务的顶层实例为空）</param>
public sealed record WorkflowTaskResponse(
    Guid Id,
    Guid InstanceId,
    string InstanceTitle,
    string DefinitionName,
    string NodeId,
    string NodeName,
    string Title,
    string? CorrelationId,
    DateTimeOffset CreatedAt,
    Guid? StarterId,
    string? StarterName,
    string? BusinessNameKey);

/// <summary>
/// 办理待办
/// </summary>
/// <param name="Id">任务 ID</param>
/// <param name="Outcome">结论（<c>approved</c> / <c>rejected</c> 或节点允许的其他结论）</param>
/// <param name="Comment">意见</param>
/// <param name="Variables">附加的流程变量（JSON 对象文本）</param>
public sealed record CompleteTaskRequest(
    Guid Id,
    [property: Required, StringLength(64, MinimumLength = 1)] string Outcome,
    [property: StringLength(2000)] string? Comment = null,
    [property: StringLength(65536)] string? Variables = null);

/// <summary>
/// 转办
/// </summary>
/// <param name="Id">任务 ID</param>
/// <param name="TargetUserId">转给谁（当前一侧可见的在用账号）</param>
/// <param name="Comment">意见</param>
public sealed record TransferTaskRequest(Guid Id, Guid TargetUserId, [property: StringLength(2000)] string? Comment = null);

/// <summary>
/// 加签
/// </summary>
/// <param name="Id">任务 ID</param>
/// <param name="UserIds">加给谁（当前一侧可见的在用账号）</param>
/// <param name="Comment">意见</param>
public sealed record AddTaskAssigneesRequest(Guid Id, [property: Required, MinLength(1), MaxLength(20)] IReadOnlyList<Guid> UserIds, [property: StringLength(2000)] string? Comment = null);

/// <summary>
/// 办理、转办或加签之后的实例
/// </summary>
/// <param name="InstanceId">流程实例</param>
/// <param name="InstanceStatus">实例状态</param>
/// <param name="FaultMessage">办理后实例故障时的原因（审批已记下，是后续节点或业务回写出错，要管理员处理后重试）</param>
public sealed record WorkflowTaskActionResponse(Guid InstanceId, WorkflowInstanceStatus InstanceStatus, string? FaultMessage);
