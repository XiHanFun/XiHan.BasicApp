// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using System.Globalization;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Workflow.Application.Business;
using XiHan.BasicApp.Workflow.Domain.Entities;
using XiHan.BasicApp.Workflow.Domain.Repositories;
using XiHan.BasicApp.Workflow.Infrastructure.Stores;
using XiHan.Framework.EventBus.Abstractions.Local;
using XiHan.Framework.Workflow.Abstractions.Events;
using XiHan.Framework.Workflow.Abstractions.Stores;

namespace XiHan.BasicApp.Workflow.Application.EventHandlers;

/// <summary>
/// 工作流通知投递辅助（受理人标识 → 用户站内通知）
/// </summary>
internal static class WorkflowNotificationDispatcher
{
    /// <summary>
    /// 审批中心「待我审批」
    /// </summary>
    public const string TodoLink = "/approval/center?tab=todo";

    /// <summary>
    /// 审批中心「抄送我的」
    /// </summary>
    public const string CcLink = "/approval/center?tab=cc";

    /// <summary>
    /// 审批中心「我发起的」
    /// </summary>
    public const string StartedLink = "/approval/center?tab=started";

    /// <summary>
    /// 安全投递（标识非用户主键跳过、投递异常仅记日志，均不阻断流程推进）
    /// </summary>
    public static async Task DispatchSafelyAsync(
        IUserNotificationDispatchService dispatchService,
        ILogger logger,
        string userIdText,
        string title,
        string content,
        long? businessId,
        string link)
    {
        if (!long.TryParse(userIdText, NumberStyles.None, CultureInfo.InvariantCulture, out var userId))
        {
            logger.LogWarning("工作流通知跳过：接收人标识 {UserId} 不是用户主键", userIdText);
            return;
        }

        try
        {
            await dispatchService.DispatchToUserAsync(
                userId,
                title,
                content,
                NotificationType.Todo,
                businessType: "workflow",
                businessId: businessId,
                link: link);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "工作流通知投递失败：用户 {UserId}", userId);
        }
    }

    /// <summary>
    /// 尝试把工作流标识解析为业务主键
    /// </summary>
    public static long? TryParseId(string? id)
    {
        return long.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : null;
    }
}

/// <summary>
/// 人工任务创建通知处理器（受理人与抄送人站内通知 + 实时推送）
/// </summary>
/// <remarks>
/// 处理器须经 XiHanLocalEventBusOptions.Handlers 登记，裸 AddTransient 不会被订阅。
/// </remarks>
public class WorkflowUserTaskCreatedNotificationHandler : ILocalEventHandler<WorkflowUserTaskCreatedEventData>
{
    private readonly IUserNotificationDispatchService _dispatchService;
    private readonly ILogger<WorkflowUserTaskCreatedNotificationHandler> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public WorkflowUserTaskCreatedNotificationHandler(
        IUserNotificationDispatchService dispatchService,
        ILogger<WorkflowUserTaskCreatedNotificationHandler> logger)
    {
        _dispatchService = dispatchService;
        _logger = logger;
    }

    /// <summary>
    /// 事件处理器通过实现此方法来处理事件
    /// </summary>
    /// <param name="eventData">事件数据</param>
    public async Task HandleEventAsync(WorkflowUserTaskCreatedEventData eventData)
    {
        var task = eventData.Task;
        var businessId = WorkflowNotificationDispatcher.TryParseId(task.TaskId);

        await WorkflowNotificationDispatcher.DispatchSafelyAsync(
            _dispatchService, _logger, task.AssigneeId,
            $"待办审批：{task.Title}",
            $"流程「{task.InstanceName}」有一条待办任务待您处理。",
            businessId,
            WorkflowNotificationDispatcher.TodoLink);

        foreach (var ccUserId in eventData.CcUserIds)
        {
            await WorkflowNotificationDispatcher.DispatchSafelyAsync(
                _dispatchService, _logger, ccUserId,
                $"抄送：{task.Title}",
                $"流程「{task.InstanceName}」的审批任务已抄送给您。",
                businessId,
                WorkflowNotificationDispatcher.CcLink);
        }
    }
}

/// <summary>
/// 人工任务转办通知处理器（通知新受理人）
/// </summary>
public class WorkflowUserTaskTransferredNotificationHandler : ILocalEventHandler<WorkflowUserTaskTransferredEventData>
{
    private readonly IUserNotificationDispatchService _dispatchService;
    private readonly ILogger<WorkflowUserTaskTransferredNotificationHandler> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public WorkflowUserTaskTransferredNotificationHandler(
        IUserNotificationDispatchService dispatchService,
        ILogger<WorkflowUserTaskTransferredNotificationHandler> logger)
    {
        _dispatchService = dispatchService;
        _logger = logger;
    }

    /// <summary>
    /// 事件处理器通过实现此方法来处理事件
    /// </summary>
    /// <param name="eventData">事件数据</param>
    public async Task HandleEventAsync(WorkflowUserTaskTransferredEventData eventData)
    {
        await WorkflowNotificationDispatcher.DispatchSafelyAsync(
            _dispatchService, _logger, eventData.TargetAssigneeId,
            "转办待办",
            $"一条审批任务已由他人转办给您{(string.IsNullOrWhiteSpace(eventData.Comment) ? "。" : $"：{eventData.Comment}")}",
            WorkflowNotificationDispatcher.TryParseId(eventData.TaskId),
            WorkflowNotificationDispatcher.TodoLink);
    }
}

/// <summary>
/// 流程实例故障通知处理器（通知发起人）
/// </summary>
public class WorkflowInstanceFaultedNotificationHandler : ILocalEventHandler<WorkflowInstanceFaultedEventData>
{
    private readonly IUserNotificationDispatchService _dispatchService;
    private readonly ILogger<WorkflowInstanceFaultedNotificationHandler> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public WorkflowInstanceFaultedNotificationHandler(
        IUserNotificationDispatchService dispatchService,
        ILogger<WorkflowInstanceFaultedNotificationHandler> logger)
    {
        _dispatchService = dispatchService;
        _logger = logger;
    }

    /// <summary>
    /// 事件处理器通过实现此方法来处理事件
    /// </summary>
    /// <param name="eventData">事件数据</param>
    public async Task HandleEventAsync(WorkflowInstanceFaultedEventData eventData)
    {
        var instance = eventData.Instance;
        if (string.IsNullOrWhiteSpace(instance.StarterId))
        {
            return;
        }

        await WorkflowNotificationDispatcher.DispatchSafelyAsync(
            _dispatchService, _logger, instance.StarterId,
            $"流程故障：{instance.Name}",
            $"您发起的流程「{instance.Name}」执行故障：{instance.FaultMessage}",
            WorkflowNotificationDispatcher.TryParseId(instance.Id),
            WorkflowNotificationDispatcher.StartedLink);
    }
}

/// <summary>
/// 抄送记录处理器（被抄送人写入审批记录，供「抄送我的」与审批轨迹）
/// </summary>
/// <remarks>
/// 依次审批每轮生成待办都会带上抄送人，同一实例同一节点对同一人只记一次。
/// 记录失败只记日志，不影响待办与通知。
/// </remarks>
public class WorkflowCarbonCopyRecordHandler : ILocalEventHandler<WorkflowUserTaskCreatedEventData>
{
    private readonly WorkflowTaskRecordWriter _recordWriter;
    private readonly IWorkflowTaskRecordRepository _recordRepository;
    private readonly IWorkflowInstanceStore _instanceStore;
    private readonly ILogger<WorkflowCarbonCopyRecordHandler> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public WorkflowCarbonCopyRecordHandler(
        WorkflowTaskRecordWriter recordWriter,
        IWorkflowTaskRecordRepository recordRepository,
        IWorkflowInstanceStore instanceStore,
        ILogger<WorkflowCarbonCopyRecordHandler> logger)
    {
        _recordWriter = recordWriter;
        _recordRepository = recordRepository;
        _instanceStore = instanceStore;
        _logger = logger;
    }

    /// <summary>
    /// 事件处理器通过实现此方法来处理事件
    /// </summary>
    /// <param name="eventData">事件数据</param>
    public async Task HandleEventAsync(WorkflowUserTaskCreatedEventData eventData)
    {
        if (eventData.CcUserIds.Count == 0)
        {
            return;
        }

        var task = eventData.Task;
        try
        {
            var instance = await _instanceStore.FindAsync(task.InstanceId);
            if (instance is null)
            {
                return;
            }

            var instanceId = WorkflowStoreMapper.ParseId(task.InstanceId);
            var recorded = (await _recordRepository.GetListAsync(record =>
                    record.InstanceId == instanceId
                    && record.NodeId == task.NodeId
                    && record.Action == WorkflowTaskRecordAction.CarbonCopied))
                .Select(record => record.UserId.ToString(CultureInfo.InvariantCulture))
                .ToHashSet(StringComparer.Ordinal);
            var recipients = eventData.CcUserIds.Where(userId => !recorded.Contains(userId)).ToList();
            if (recipients.Count == 0)
            {
                return;
            }

            await _recordWriter.WriteAsync(
                new WorkflowTaskSnapshot(
                    WorkflowStoreMapper.ParseId(task.TaskId),
                    instanceId,
                    WorkflowStoreMapper.ParseId(instance.DefinitionId),
                    task.DefinitionCode,
                    task.InstanceName,
                    task.NodeId,
                    task.Title,
                    task.CorrelationId),
                recipients,
                WorkflowTaskRecordAction.CarbonCopied);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "工作流抄送记录写入失败：任务 {TaskId}", task.TaskId);
        }
    }
}
