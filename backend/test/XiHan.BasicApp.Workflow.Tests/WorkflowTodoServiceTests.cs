// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using System.Text.Json;
using XiHan.BasicApp.Workflow.Application.AppServices;
using XiHan.BasicApp.Workflow.Application.Dtos;
using XiHan.BasicApp.Workflow.Application.QueryServices;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Workflow.Abstractions.Exceptions;
using XiHan.Framework.Workflow.Abstractions.Runtime;
using XiHan.Framework.Workflow.Abstractions.UserTasks;

namespace XiHan.BasicApp.Workflow.Tests;

/// <summary>
/// 我的待办命令 / 查询应用服务的办理人锁定与内存分页测试。
/// </summary>
/// <remarks>
/// 待办接口刻意不挂权限码（登录即可办理），因此"办理人必须是当前登录用户"是这里唯一的越权防线：
/// actorId 一旦改成从 DTO 取，任何登录用户都能替别人签批。
/// 查询侧同理——受理人是服务端从当前用户取的，不接受前端指定。
/// </remarks>
public sealed class WorkflowTodoServiceTests
{
    /// <summary>
    /// 办理待办入参为 null 时必须抛空引用参数异常。
    /// </summary>
    [Fact]
    public async Task CompleteAsync_NullInput_ShouldThrowArgumentNullException()
    {
        var (service, taskService, _) = CreateAppService();

        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => service.CompleteAsync(null!));

        taskService.Verify(
            value => value.CompleteAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<Dictionary<string, object?>?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 任务标识与办理结果任一为空白都必须被参数校验拦下。
    /// </summary>
    /// <param name="taskId">任务标识。</param>
    /// <param name="outcome">办理结果。</param>
    [Theory]
    [InlineData(null, "approved")]
    [InlineData("", "approved")]
    [InlineData("   ", "approved")]
    [InlineData("400400400400400", null)]
    [InlineData("400400400400400", "")]
    [InlineData("400400400400400", "  ")]
    public async Task CompleteAsync_BlankRequiredField_ShouldThrowArgumentException(string? taskId, string? outcome)
    {
        var (service, _, _) = CreateAppService();

        _ = await Assert.ThrowsAnyAsync<ArgumentException>(
            () => service.CompleteAsync(new WorkflowTodoCompleteDto { TaskId = taskId!, Outcome = outcome! }));
    }

    /// <summary>
    /// 未登录时必须以业务异常拒绝办理，绝不能把 null 办理人交给任务服务。
    /// </summary>
    [Fact]
    public async Task CompleteAsync_WithoutCurrentUser_ShouldThrowBusinessException()
    {
        var (service, taskService, _) = CreateAppService(userId: null);

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => service.CompleteAsync(new WorkflowTodoCompleteDto { TaskId = "400400400400400", Outcome = "approved" }));

        Assert.Equal("当前用户未登录，无法办理待办", exception.Message, StringComparer.Ordinal);
        taskService.Verify(
            value => value.CompleteAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<Dictionary<string, object?>?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 办理待办必须裁剪任务标识与结果、把办理人锁定为当前用户，并解析附加变量。
    /// </summary>
    [Fact]
    public async Task CompleteAsync_ShouldLockActorToCurrentUserAndTrimInputs()
    {
        var (service, taskService, _) = CreateAppService(userId: 99L);
        string? forwardedTaskId = null;
        string? forwardedActorId = null;
        string? forwardedOutcome = null;
        string? forwardedComment = null;
        Dictionary<string, object?>? forwardedVariables = null;
        taskService
            .Setup(value => value.CompleteAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<Dictionary<string, object?>?>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, string?, Dictionary<string, object?>?, CancellationToken>(
                (taskId, actorId, outcome, comment, variables, _) =>
                {
                    forwardedTaskId = taskId;
                    forwardedActorId = actorId;
                    forwardedOutcome = outcome;
                    forwardedComment = comment;
                    forwardedVariables = variables;
                })
            .ReturnsAsync(WorkflowTestHelper.CreateInstance(status: WorkflowInstanceStatus.Completed));

        var result = await service.CompleteAsync(new WorkflowTodoCompleteDto
        {
            TaskId = "  400400400400400  ",
            Outcome = "  approved  ",
            Comment = "同意",
            VariablesJson = "{\"days\":3}"
        });

        Assert.Equal("400400400400400", forwardedTaskId, StringComparer.Ordinal);
        Assert.Equal("99", forwardedActorId, StringComparer.Ordinal);
        Assert.Equal("approved", forwardedOutcome, StringComparer.Ordinal);
        Assert.Equal("同意", forwardedComment, StringComparer.Ordinal);
        Assert.Equal(3, Assert.IsType<JsonElement>(forwardedVariables!["days"]).GetInt32());
        Assert.Equal("900800700600500", result.InstanceId, StringComparer.Ordinal);
        Assert.Equal(WorkflowInstanceStatus.Completed, result.InstanceStatus);
    }

    /// <summary>
    /// 附加变量为空白文本时必须传 null（表示"不带附加变量"），不得传空字典改写实例变量语义。
    /// </summary>
    /// <param name="variablesJson">空白的变量 JSON。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CompleteAsync_BlankVariablesJson_ShouldForwardNull(string? variablesJson)
    {
        var (service, taskService, _) = CreateAppService();
        Dictionary<string, object?>? forwarded = new() { ["sentinel"] = 1 };
        taskService
            .Setup(value => value.CompleteAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<Dictionary<string, object?>?>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, string?, Dictionary<string, object?>?, CancellationToken>(
                (_, _, _, _, variables, _) => forwarded = variables)
            .ReturnsAsync(WorkflowTestHelper.CreateInstance());

        _ = await service.CompleteAsync(new WorkflowTodoCompleteDto
        {
            TaskId = "400400400400400",
            Outcome = "approved",
            VariablesJson = variablesJson
        });

        Assert.Null(forwarded);
    }

    /// <summary>
    /// 附加变量 JSON 非法时必须翻译成带"附加变量"字样的业务异常。
    /// </summary>
    [Fact]
    public async Task CompleteAsync_InvalidVariablesJson_ShouldThrowBusinessException()
    {
        var (service, _, _) = CreateAppService();

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => service.CompleteAsync(new WorkflowTodoCompleteDto
            {
                TaskId = "400400400400400",
                Outcome = "approved",
                VariablesJson = "{"
            }));

        Assert.Contains("附加变量 JSON 非法", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 任务服务抛出的协议异常（如"任务不属于当前受理人"）必须翻译成业务异常。
    /// </summary>
    [Fact]
    public async Task CompleteAsync_TaskServiceThrowsWorkflowException_ShouldTranslateToBusinessException()
    {
        var (service, taskService, _) = CreateAppService();
        taskService
            .Setup(value => value.CompleteAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<Dictionary<string, object?>?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new WorkflowException("任务不属于当前受理人"));

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => service.CompleteAsync(new WorkflowTodoCompleteDto { TaskId = "1", Outcome = "approved" }));

        Assert.Equal("任务不属于当前受理人", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 转办的任务标识与目标受理人任一为空白都必须被参数校验拦下。
    /// </summary>
    /// <param name="taskId">任务标识。</param>
    /// <param name="targetAssigneeId">目标受理人标识。</param>
    [Theory]
    [InlineData(null, "1002")]
    [InlineData("  ", "1002")]
    [InlineData("400400400400400", null)]
    [InlineData("400400400400400", "   ")]
    public async Task TransferAsync_BlankRequiredField_ShouldThrowArgumentException(string? taskId, string? targetAssigneeId)
    {
        var (service, _, _) = CreateAppService();

        _ = await Assert.ThrowsAnyAsync<ArgumentException>(
            () => service.TransferAsync(new WorkflowTodoTransferDto
            {
                TaskId = taskId!,
                TargetAssigneeId = targetAssigneeId!
            }));
    }

    /// <summary>
    /// 转办必须裁剪任务标识与目标受理人，并把操作人锁定为当前用户。
    /// </summary>
    [Fact]
    public async Task TransferAsync_ShouldTrimInputsAndLockActor()
    {
        var (service, taskService, _) = CreateAppService(userId: 99L);

        await service.TransferAsync(new WorkflowTodoTransferDto
        {
            TaskId = " 400400400400400 ",
            TargetAssigneeId = " 1002 ",
            Comment = "出差代办"
        });

        taskService.Verify(
            value => value.TransferAsync("400400400400400", "99", "1002", "出差代办", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 未登录时转办必须以业务异常拒绝。
    /// </summary>
    [Fact]
    public async Task TransferAsync_WithoutCurrentUser_ShouldThrowBusinessException()
    {
        var (service, _, _) = CreateAppService(userId: null);

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => service.TransferAsync(new WorkflowTodoTransferDto { TaskId = "1", TargetAssigneeId = "1002" }));

        Assert.Equal("当前用户未登录，无法办理待办", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 加签受理人列表为空必须以业务异常拒绝：空列表加签等价于一次无效写入且没有任何提示。
    /// </summary>
    [Fact]
    public async Task AddAssigneesAsync_EmptyAssignees_ShouldThrowBusinessException()
    {
        var (service, taskService, _) = CreateAppService();

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => service.AddAssigneesAsync(new WorkflowTodoAddAssigneesDto { TaskId = "400400400400400" }));

        Assert.Equal("加签受理人不能为空", exception.Message, StringComparer.Ordinal);
        taskService.Verify(
            value => value.AddAssigneesAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 加签的任务标识为空白必须被参数校验拦下（先于受理人数量检查）。
    /// </summary>
    [Fact]
    public async Task AddAssigneesAsync_BlankTaskId_ShouldThrowArgumentException()
    {
        var (service, _, _) = CreateAppService();

        _ = await Assert.ThrowsAnyAsync<ArgumentException>(
            () => service.AddAssigneesAsync(new WorkflowTodoAddAssigneesDto { TaskId = "   ", AssigneeIds = ["1002"] }));
    }

    /// <summary>
    /// 加签必须原样透传受理人集合与意见，并把操作人锁定为当前用户。
    /// </summary>
    [Fact]
    public async Task AddAssigneesAsync_ShouldForwardAssigneesAndLockActor()
    {
        var (service, taskService, _) = CreateAppService(userId: 99L);
        List<string>? forwarded = null;
        taskService
            .Setup(value => value.AddAssigneesAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, IEnumerable<string>, string?, CancellationToken>(
                (_, _, assignees, _, _) => forwarded = [.. assignees])
            .Returns(Task.CompletedTask);

        await service.AddAssigneesAsync(new WorkflowTodoAddAssigneesDto
        {
            TaskId = " 400400400400400 ",
            AssigneeIds = ["1002", "1003"],
            Comment = "会签"
        });

        Assert.Equal(new[] { "1002", "1003" }, forwarded!.ToArray());
        taskService.Verify(
            value => value.AddAssigneesAsync("400400400400400", "99", It.IsAny<IEnumerable<string>>(), "会签", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 待办查询入参为 null 时必须抛空引用参数异常。
    /// </summary>
    [Fact]
    public async Task QueryService_GetPageAsync_NullInput_ShouldThrowArgumentNullException()
    {
        var (service, _, _) = CreateQueryService();

        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => service.GetPageAsync(null!));
    }

    /// <summary>
    /// 已取消的令牌必须在查询开始前就抛出取消异常，不得先去拉一遍待办列表。
    /// </summary>
    [Fact]
    public async Task QueryService_GetPageAsync_CanceledToken_ShouldThrowBeforeQuery()
    {
        var (service, taskService, _) = CreateQueryService();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.GetPageAsync(new WorkflowTodoPageQueryDto(), cancellation.Token));

        taskService.Verify(value => value.GetPendingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 未登录查询待办必须以业务异常拒绝，而不是返回空列表掩盖会话失效。
    /// </summary>
    [Fact]
    public async Task QueryService_GetPageAsync_WithoutCurrentUser_ShouldThrowBusinessException()
    {
        var (service, _, _) = CreateQueryService(userId: null);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.GetPageAsync(new WorkflowTodoPageQueryDto()));

        Assert.Equal("当前用户未登录，无法查询待办", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 受理人必须服务端锁定为当前登录用户主键，前端无从指定他人。
    /// </summary>
    [Fact]
    public async Task QueryService_GetPageAsync_ShouldQueryByCurrentUserId()
    {
        var (service, taskService, _) = CreateQueryService(userId: 99L);
        taskService
            .Setup(value => value.GetPendingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _ = await service.GetPageAsync(new WorkflowTodoPageQueryDto());

        taskService.Verify(value => value.GetPendingAsync("99", It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 关键字必须在标题、实例名、定义编码、业务相关性四个字段上大小写不敏感命中。
    /// </summary>
    /// <param name="keyword">查询关键字。</param>
    /// <param name="expectedTaskId">期望命中的任务标识。</param>
    [Theory]
    [InlineData("部门", "1")]
    [InlineData("采购单", "2")]
    [InlineData("EXPENSE", "3")]
    [InlineData("order-2024", "4")]
    public async Task QueryService_GetPageAsync_Keyword_ShouldMatchFourFieldsIgnoringCase(string keyword, string expectedTaskId)
    {
        var (service, taskService, _) = CreateQueryService();
        taskService
            .Setup(value => value.GetPendingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                WorkflowTestHelper.CreateUserTask("1", title: "部门审批", instanceName: "甲", definitionCode: "leave", correlationId: null),
                WorkflowTestHelper.CreateUserTask("2", title: "复核", instanceName: "采购单-A", definitionCode: "leave", correlationId: null),
                WorkflowTestHelper.CreateUserTask("3", title: "复核", instanceName: "乙", definitionCode: "expense", correlationId: null),
                WorkflowTestHelper.CreateUserTask("4", title: "复核", instanceName: "丙", definitionCode: "leave", correlationId: "ORDER-2024-0001")
            ]);

        var page = await service.GetPageAsync(new WorkflowTodoPageQueryDto { Keyword = keyword });

        var item = Assert.Single(page.Items);
        Assert.Equal(expectedTaskId, item.TaskId, StringComparer.Ordinal);
        Assert.Equal(1, page.Page.TotalCount);
    }

    /// <summary>
    /// 关键字前后空白必须裁剪；命中不到任何待办时返回空页且总数为 0。
    /// </summary>
    [Fact]
    public async Task QueryService_GetPageAsync_KeywordWithoutMatch_ShouldReturnEmptyPage()
    {
        var (service, taskService, _) = CreateQueryService();
        taskService
            .Setup(value => value.GetPendingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([WorkflowTestHelper.CreateUserTask()]);

        var page = await service.GetPageAsync(new WorkflowTodoPageQueryDto { Keyword = "  不存在的关键字  " });

        Assert.Empty(page.Items);
        Assert.Equal(0, page.Page.TotalCount);
    }

    /// <summary>
    /// 待办必须按创建时间倒序排列（最新的在最前），并按页切片。
    /// </summary>
    [Fact]
    public async Task QueryService_GetPageAsync_ShouldOrderByCreationTimeDescendingAndSlice()
    {
        var (service, taskService, _) = CreateQueryService();
        taskService
            .Setup(value => value.GetPendingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                WorkflowTestHelper.CreateUserTask("1", creationTime: WorkflowTestHelper.CreationTime),
                WorkflowTestHelper.CreateUserTask("2", creationTime: WorkflowTestHelper.CreationTime.AddHours(2)),
                WorkflowTestHelper.CreateUserTask("3", creationTime: WorkflowTestHelper.CreationTime.AddHours(1)),
                WorkflowTestHelper.CreateUserTask("4", creationTime: WorkflowTestHelper.CreationTime.AddHours(3))
            ]);

        var input = new WorkflowTodoPageQueryDto();
        input.Page.PageIndex = 2;
        input.Page.PageSize = 2;

        var page = await service.GetPageAsync(input);

        Assert.Equal(new[] { "3", "1" }, page.Items.Select(item => item.TaskId).ToArray());
        Assert.Equal(4, page.Page.TotalCount);
        Assert.Equal(2, page.Page.PageIndex);
        Assert.Equal(2, page.Page.PageSize);
    }

    /// <summary>
    /// 超过上限的页大小必须被收敛到 500，避免一次拉爆内存。
    /// </summary>
    [Fact]
    public async Task QueryService_GetPageAsync_OversizedPageSize_ShouldClampToUpperBound()
    {
        var (service, taskService, _) = CreateQueryService();
        taskService
            .Setup(value => value.GetPendingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([WorkflowTestHelper.CreateUserTask()]);

        var input = new WorkflowTodoPageQueryDto();
        input.Page.PageSize = 10000;

        var page = await service.GetPageAsync(input);

        Assert.Equal(500, page.Page.PageSize);
        _ = Assert.Single(page.Items);
    }

    /// <summary>
    /// 待办映射必须带出办理页所需的全部字段；表单数据与受理人不进列表 DTO（列表不暴露他人受理信息）。
    /// </summary>
    [Fact]
    public async Task QueryService_GetPageAsync_ShouldMapTaskToListItemDto()
    {
        var (service, taskService, _) = CreateQueryService();
        taskService
            .Setup(value => value.GetPendingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([WorkflowTestHelper.CreateUserTask()]);

        var item = Assert.Single((await service.GetPageAsync(new WorkflowTodoPageQueryDto())).Items);

        Assert.Equal("400400400400400", item.TaskId, StringComparer.Ordinal);
        Assert.Equal("900800700600500", item.InstanceId, StringComparer.Ordinal);
        Assert.Equal("张三的请假", item.InstanceName, StringComparer.Ordinal);
        Assert.Equal("leave", item.DefinitionCode, StringComparer.Ordinal);
        Assert.Equal("approve", item.NodeId, StringComparer.Ordinal);
        Assert.Equal("部门审批", item.Title, StringComparer.Ordinal);
        Assert.Equal("ORDER-2024-0001", item.CorrelationId, StringComparer.Ordinal);
        Assert.Equal(WorkflowTestHelper.CreationTime, item.CreationTime);
    }

    /// <summary>
    /// 构造待办命令服务及其 Moq 任务服务与当前用户。
    /// </summary>
    /// <param name="userId">当前登录用户主键（null 表示未登录）。</param>
    /// <returns>命令服务、任务服务桩、当前用户桩。</returns>
    private static (WorkflowTodoAppService Service, Mock<IWorkflowUserTaskService> TaskService, Mock<ICurrentUser> CurrentUser) CreateAppService(long? userId = 99L)
    {
        var taskService = new Mock<IWorkflowUserTaskService>();
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(value => value.UserId).Returns(userId);
        return (new WorkflowTodoAppService(taskService.Object, currentUser.Object), taskService, currentUser);
    }

    /// <summary>
    /// 构造待办查询服务及其 Moq 任务服务与当前用户。
    /// </summary>
    /// <param name="userId">当前登录用户主键（null 表示未登录）。</param>
    /// <returns>查询服务、任务服务桩、当前用户桩。</returns>
    private static (WorkflowTodoQueryService Service, Mock<IWorkflowUserTaskService> TaskService, Mock<ICurrentUser> CurrentUser) CreateQueryService(long? userId = 99L)
    {
        var taskService = new Mock<IWorkflowUserTaskService>();
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(value => value.UserId).Returns(userId);
        return (new WorkflowTodoQueryService(taskService.Object, currentUser.Object), taskService, currentUser);
    }
}
