// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using XiHan.BasicApp.Workflow.Domain.Entities;
using XiHan.BasicApp.Workflow.Domain.Repositories;
using XiHan.Framework.Workflow.Abstractions.Stores;

namespace XiHan.BasicApp.Workflow.Application.Business;

/// <summary>
/// 审批记录写入器（办理、转办、加签、抄送各写一行，供已办、抄送与审批轨迹查询）
/// </summary>
public sealed class WorkflowTaskRecordWriter
{
    private readonly IWorkflowTaskRecordRepository _recordRepository;
    private readonly IWorkflowDefinitionStore _definitionStore;

    /// <summary>
    /// 构造函数
    /// </summary>
    public WorkflowTaskRecordWriter(IWorkflowTaskRecordRepository recordRepository, IWorkflowDefinitionStore definitionStore)
    {
        _recordRepository = recordRepository;
        _definitionStore = definitionStore;
    }

    /// <summary>
    /// 写入审批记录
    /// </summary>
    /// <param name="task">任务快照</param>
    /// <param name="userIds">记录归属用户（办理人或被抄送人，非数值标识跳过）</param>
    /// <param name="action">动作</param>
    /// <param name="outcome">办理结果</param>
    /// <param name="comment">意见</param>
    /// <param name="targetUserIds">目标用户（转办/加签）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    public async Task WriteAsync(
        WorkflowTaskSnapshot task,
        IEnumerable<string> userIds,
        WorkflowTaskRecordAction action,
        string? outcome = null,
        string? comment = null,
        IEnumerable<string>? targetUserIds = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);

        var owners = userIds
            .Select(id => long.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : 0)
            .Where(id => id > 0)
            .Distinct()
            .ToList();
        if (owners.Count == 0)
        {
            return;
        }

        var nodeName = await new WorkflowNodeNameCache(_definitionStore).GetAsync(task.DefinitionId, task.NodeId, cancellationToken);
        var targets = targetUserIds is null ? null : string.Join(",", targetUserIds.Where(id => !string.IsNullOrWhiteSpace(id)));

        var records = owners.Select(userId => new SysWorkflowTaskRecord
        {
            InstanceId = task.InstanceId,
            TaskId = task.TaskId,
            NodeId = task.NodeId,
            NodeName = Truncate(nodeName, 200),
            DefinitionCode = task.DefinitionCode,
            InstanceName = Truncate(task.InstanceName, 200),
            Title = Truncate(task.Title, 500),
            CorrelationId = task.CorrelationId,
            UserId = userId,
            Action = action,
            Outcome = outcome,
            Comment = string.IsNullOrWhiteSpace(comment) ? null : Truncate(comment.Trim(), 2000),
            TargetUserIds = string.IsNullOrEmpty(targets) ? null : Truncate(targets, 1000)
        });

        _ = await _recordRepository.AddRangeAsync(records, cancellationToken);
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}

/// <summary>
/// 任务快照（记录审批动作时的任务与实例信息）
/// </summary>
/// <param name="TaskId">任务标识</param>
/// <param name="InstanceId">实例标识</param>
/// <param name="DefinitionId">定义标识</param>
/// <param name="DefinitionCode">定义编码</param>
/// <param name="InstanceName">实例名称</param>
/// <param name="NodeId">节点标识</param>
/// <param name="Title">任务标题</param>
/// <param name="CorrelationId">业务相关性标识</param>
public sealed record WorkflowTaskSnapshot(
    long TaskId,
    long InstanceId,
    long DefinitionId,
    string DefinitionCode,
    string InstanceName,
    string NodeId,
    string Title,
    string? CorrelationId);
