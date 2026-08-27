// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using Moq;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Workflow.Application.EventHandlers;
using XiHan.Framework.Workflow.Abstractions.Events;
using XiHan.Framework.Workflow.Abstractions.Runtime;

namespace XiHan.BasicApp.Workflow.Tests;

/// <summary>
/// 工作流站内通知事件处理器的分发与旁路测试。
/// </summary>
/// <remarks>
/// 通知是流程推进的旁路：受理人标识可能是"角色/群组/表达式"而非用户主键，投递也可能因下游故障失败。
/// 两种情况都必须只记日志、不抛异常——一旦让异常冒出去，本地事件总线会把整条流程推进一起带崩，
/// 表现为"审批点了没反应，但下一节点其实已经执行过一半"。
/// </remarks>
public sealed class WorkflowNotificationEventHandlerTests
{
    private const string TaskId = "400400400400400";

    /// <summary>
    /// 待办创建必须给受理人投一条待办类站内通知，标题/正文/业务类型/业务主键/跳转链接全部锁定。
    /// </summary>
    [Fact]
    public async Task UserTaskCreated_ShouldDispatchTodoNotificationToAssignee()
    {
        var dispatch = new Mock<IUserNotificationDispatchService>();
        var handler = new WorkflowUserTaskCreatedNotificationHandler(
            dispatch.Object, new RecordingLogger<WorkflowUserTaskCreatedNotificationHandler>());

        await handler.HandleEventAsync(new WorkflowUserTaskCreatedEventData(WorkflowTestHelper.CreateUserTask(), []));

        dispatch.Verify(
            value => value.DispatchToUserAsync(
                1001L,
                "待办审批：部门审批",
                "流程「张三的请假」有一条待办任务待您处理。",
                NotificationType.Todo,
                "workflow",
                400400400400400L,
                false,
                "/workflow/todo",
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 抄送人必须逐个收到"抄送"文案的通知，与受理人通知区分开。
    /// </summary>
    [Fact]
    public async Task UserTaskCreated_ShouldDispatchCarbonCopyNotificationToEveryCcUser()
    {
        var dispatch = new Mock<IUserNotificationDispatchService>();
        var handler = new WorkflowUserTaskCreatedNotificationHandler(
            dispatch.Object, new RecordingLogger<WorkflowUserTaskCreatedNotificationHandler>());

        await handler.HandleEventAsync(
            new WorkflowUserTaskCreatedEventData(WorkflowTestHelper.CreateUserTask(), ["2001", "2002"]));

        dispatch.Verify(
            value => value.DispatchToUserAsync(
                2001L, "抄送：部门审批", "流程「张三的请假」的审批任务已抄送给您。",
                NotificationType.Todo, "workflow", 400400400400400L, false, "/workflow/todo", null, It.IsAny<CancellationToken>()),
            Times.Once);
        dispatch.Verify(
            value => value.DispatchToUserAsync(
                2002L, "抄送：部门审批", It.IsAny<string?>(),
                NotificationType.Todo, "workflow", 400400400400400L, false, "/workflow/todo", null, It.IsAny<CancellationToken>()),
            Times.Once);
        dispatch.Verify(
            value => value.DispatchToUserAsync(
                It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<NotificationType>(),
                It.IsAny<string?>(), It.IsAny<long?>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    /// <summary>
    /// 受理人标识不是用户主键（角色码、表达式）时必须跳过投递并记一条 Warning，不得抛异常打断流程。
    /// </summary>
    /// <param name="assigneeId">非用户主键的受理人标识。</param>
    [Theory]
    [InlineData("role:manager")]
    [InlineData("")]
    [InlineData("-1")]
    [InlineData("1001.0")]
    public async Task UserTaskCreated_NonNumericAssignee_ShouldSkipAndWarn(string assigneeId)
    {
        var dispatch = new Mock<IUserNotificationDispatchService>();
        var logger = new RecordingLogger<WorkflowUserTaskCreatedNotificationHandler>();
        var handler = new WorkflowUserTaskCreatedNotificationHandler(dispatch.Object, logger);
        var task = WorkflowTestHelper.CreateUserTask();
        task.AssigneeId = assigneeId;

        await handler.HandleEventAsync(new WorkflowUserTaskCreatedEventData(task, []));

        dispatch.Verify(
            value => value.DispatchToUserAsync(
                It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<NotificationType>(),
                It.IsAny<string?>(), It.IsAny<long?>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Contains("工作流通知跳过", entry.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 投递失败必须被吞掉并记 Error：后续抄送人的通知不受影响，流程推进也不被打断。
    /// </summary>
    [Fact]
    public async Task UserTaskCreated_DispatchThrows_ShouldLogErrorAndKeepGoing()
    {
        var dispatch = new Mock<IUserNotificationDispatchService>();
        dispatch
            .Setup(value => value.DispatchToUserAsync(
                1001L, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<NotificationType>(),
                It.IsAny<string?>(), It.IsAny<long?>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("站内信服务不可用"));
        var logger = new RecordingLogger<WorkflowUserTaskCreatedNotificationHandler>();
        var handler = new WorkflowUserTaskCreatedNotificationHandler(dispatch.Object, logger);

        await handler.HandleEventAsync(
            new WorkflowUserTaskCreatedEventData(WorkflowTestHelper.CreateUserTask(), ["2001"]));

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains("工作流通知投递失败", entry.Message, StringComparison.Ordinal);
        dispatch.Verify(
            value => value.DispatchToUserAsync(
                2001L, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<NotificationType>(),
                It.IsAny<string?>(), It.IsAny<long?>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 任务标识不是雪花数值时业务主键必须留空，而不是把解析失败当成 0 号业务对象。
    /// </summary>
    [Fact]
    public async Task UserTaskCreated_NonNumericTaskId_ShouldLeaveBusinessIdNull()
    {
        var dispatch = new Mock<IUserNotificationDispatchService>();
        var handler = new WorkflowUserTaskCreatedNotificationHandler(
            dispatch.Object, new RecordingLogger<WorkflowUserTaskCreatedNotificationHandler>());
        var task = WorkflowTestHelper.CreateUserTask(taskId: "task-abc");

        await handler.HandleEventAsync(new WorkflowUserTaskCreatedEventData(task, []));

        dispatch.Verify(
            value => value.DispatchToUserAsync(
                1001L, It.IsAny<string>(), It.IsAny<string?>(), NotificationType.Todo,
                "workflow", null, false, "/workflow/todo", null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 转办必须通知新受理人，并把转办意见拼进正文（有意见用冒号，无意见用句号收尾）。
    /// </summary>
    /// <param name="comment">转办意见（null 或空白表示未填）。</param>
    /// <param name="expectedContent">期望正文。</param>
    [Theory]
    [InlineData("出差代办", "一条审批任务已由他人转办给您：出差代办")]
    [InlineData(null, "一条审批任务已由他人转办给您。")]
    [InlineData("   ", "一条审批任务已由他人转办给您。")]
    public async Task UserTaskTransferred_ShouldNotifyTargetAssigneeWithComment(string? comment, string expectedContent)
    {
        var dispatch = new Mock<IUserNotificationDispatchService>();
        var handler = new WorkflowUserTaskTransferredNotificationHandler(
            dispatch.Object, new RecordingLogger<WorkflowUserTaskTransferredNotificationHandler>());

        await handler.HandleEventAsync(
            new WorkflowUserTaskTransferredEventData(TaskId, "900800700600500", "1001", "1002", comment));

        dispatch.Verify(
            value => value.DispatchToUserAsync(
                1002L, "转办待办", expectedContent, NotificationType.Todo,
                "workflow", 400400400400400L, false, "/workflow/todo", null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 转办目标不是用户主键时同样跳过并记 Warning。
    /// </summary>
    [Fact]
    public async Task UserTaskTransferred_NonNumericTarget_ShouldSkipAndWarn()
    {
        var dispatch = new Mock<IUserNotificationDispatchService>();
        var logger = new RecordingLogger<WorkflowUserTaskTransferredNotificationHandler>();
        var handler = new WorkflowUserTaskTransferredNotificationHandler(dispatch.Object, logger);

        await handler.HandleEventAsync(
            new WorkflowUserTaskTransferredEventData(TaskId, "900800700600500", "1001", "group:hr", null));

        Assert.Equal(LogLevel.Warning, Assert.Single(logger.Entries).Level);
        dispatch.Verify(
            value => value.DispatchToUserAsync(
                It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<NotificationType>(),
                It.IsAny<string?>(), It.IsAny<long?>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 实例故障必须通知发起人，正文带上故障信息，业务主键取实例标识。
    /// </summary>
    [Fact]
    public async Task InstanceFaulted_ShouldNotifyStarterWithFaultMessage()
    {
        var dispatch = new Mock<IUserNotificationDispatchService>();
        var handler = new WorkflowInstanceFaultedNotificationHandler(
            dispatch.Object, new RecordingLogger<WorkflowInstanceFaultedNotificationHandler>());

        await handler.HandleEventAsync(new WorkflowInstanceFaultedEventData(WorkflowTestHelper.CreateInstance()));

        dispatch.Verify(
            value => value.DispatchToUserAsync(
                1001L,
                "流程故障：张三的请假",
                "您发起的流程「张三的请假」执行故障：节点执行超时",
                NotificationType.Todo,
                "workflow",
                900800700600500L,
                false,
                "/workflow/todo",
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 无发起人（系统自动发起 / 子流程）时必须整体跳过，连 Warning 都不记——这不是异常情况。
    /// </summary>
    /// <param name="starterId">空白的发起人标识。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task InstanceFaulted_WithoutStarter_ShouldSkipSilently(string? starterId)
    {
        var dispatch = new Mock<IUserNotificationDispatchService>();
        var logger = new RecordingLogger<WorkflowInstanceFaultedNotificationHandler>();
        var handler = new WorkflowInstanceFaultedNotificationHandler(dispatch.Object, logger);
        var instance = WorkflowTestHelper.CreateInstance();
        instance.StarterId = starterId;

        await handler.HandleEventAsync(new WorkflowInstanceFaultedEventData(instance));

        Assert.Empty(logger.Entries);
        dispatch.Verify(
            value => value.DispatchToUserAsync(
                It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<NotificationType>(),
                It.IsAny<string?>(), It.IsAny<long?>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 发起人标识不是用户主键时跳过投递并记 Warning。
    /// </summary>
    [Fact]
    public async Task InstanceFaulted_NonNumericStarter_ShouldSkipAndWarn()
    {
        var dispatch = new Mock<IUserNotificationDispatchService>();
        var logger = new RecordingLogger<WorkflowInstanceFaultedNotificationHandler>();
        var handler = new WorkflowInstanceFaultedNotificationHandler(dispatch.Object, logger);
        var instance = WorkflowTestHelper.CreateInstance();
        instance.StarterId = "system";

        await handler.HandleEventAsync(new WorkflowInstanceFaultedEventData(instance));

        Assert.Equal(LogLevel.Warning, Assert.Single(logger.Entries).Level);
    }

    /// <summary>
    /// 故障实例的投递异常同样必须被吞掉，避免通知故障反过来打断故障处理。
    /// </summary>
    [Fact]
    public async Task InstanceFaulted_DispatchThrows_ShouldLogErrorWithoutRethrowing()
    {
        var dispatch = new Mock<IUserNotificationDispatchService>();
        dispatch
            .Setup(value => value.DispatchToUserAsync(
                It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<NotificationType>(),
                It.IsAny<string?>(), It.IsAny<long?>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("推送超时"));
        var logger = new RecordingLogger<WorkflowInstanceFaultedNotificationHandler>();
        var handler = new WorkflowInstanceFaultedNotificationHandler(dispatch.Object, logger);

        await handler.HandleEventAsync(new WorkflowInstanceFaultedEventData(WorkflowTestHelper.CreateInstance()));

        Assert.Equal(LogLevel.Error, Assert.Single(logger.Entries).Level);
    }

    /// <summary>
    /// 故障通知内容必须随实例状态无关地取用实例名与故障信息，无故障信息时正文以空结尾而不是抛异常。
    /// </summary>
    [Fact]
    public async Task InstanceFaulted_WithoutFaultMessage_ShouldStillDispatch()
    {
        var dispatch = new Mock<IUserNotificationDispatchService>();
        var handler = new WorkflowInstanceFaultedNotificationHandler(
            dispatch.Object, new RecordingLogger<WorkflowInstanceFaultedNotificationHandler>());
        var instance = WorkflowTestHelper.CreateInstance(status: WorkflowInstanceStatus.Faulted, faultMessage: null);

        await handler.HandleEventAsync(new WorkflowInstanceFaultedEventData(instance));

        dispatch.Verify(
            value => value.DispatchToUserAsync(
                1001L, "流程故障：张三的请假", "您发起的流程「张三的请假」执行故障：", NotificationType.Todo,
                "workflow", 900800700600500L, false, "/workflow/todo", null, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
