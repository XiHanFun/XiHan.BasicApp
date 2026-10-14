// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Linq.Expressions;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Workflow.Application.Business;
using XiHan.BasicApp.Workflow.Domain.Entities;
using XiHan.BasicApp.Workflow.Domain.Repositories;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Timing;
using XiHan.Framework.Uow;
using XiHan.Framework.Uow.Options;
using XiHan.Framework.Workflow.Abstractions;
using XiHan.Framework.Workflow.Abstractions.Definitions;
using XiHan.Framework.Workflow.Abstractions.Engine;
using XiHan.Framework.Workflow.Abstractions.Exceptions;
using XiHan.Framework.Workflow.Abstractions.Outcomes;
using XiHan.Framework.Workflow.Abstractions.Runtime;
using XiHan.Framework.Workflow.Abstractions.Stores;
using XiHan.Framework.Workflow.Abstractions.UserTasks;
using XiHan.Framework.Workflow.Builders;

namespace XiHan.BasicApp.Workflow.Tests;

/// <summary>
/// 审批业务接入测试：发起/撤回/进度、结论分发、发布校验。
/// </summary>
/// <remarks>
/// 业务编码即流程定义编码，单据标识即实例相关性标识——这条约定贯穿发起、进度查询与结论回写，
/// 任何一处换了口径（如用定义标识、或把相关性标识拼上前缀）都会让业务对不上自己的实例。
/// </remarks>
public sealed class WorkflowBusinessIntegrationTests
{
    private const string BusinessCode = "expense";

    /// <summary>
    /// 同一单据已有进行中的审批时必须拒绝重复发起，且不触碰引擎。
    /// </summary>
    [Fact]
    public async Task StartAsync_ActiveInstanceExists_ShouldReject()
    {
        var fixture = new ServiceFixture();
        fixture.Instances
            .Setup(value => value.GetActiveByCorrelationIdAsync(BusinessCode, "B-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new SysWorkflowInstance(1L)]);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => fixture.Service.StartAsync(CreateStartRequest()));

        Assert.Contains("已有进行中的审批", exception.Message, StringComparison.Ordinal);
        fixture.Engine.Verify(value => value.StartAsync(It.IsAny<WorkflowStartRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 发起时按业务编码取流程、单据标识写入相关性标识、发起人锁定为当前用户，并原样带上流程变量。
    /// </summary>
    [Fact]
    public async Task StartAsync_ShouldStartWithBusinessCodeCorrelationAndStarter()
    {
        var fixture = new ServiceFixture(userId: 42L);
        fixture.Definitions
            .Setup(value => value.AnyAsync(It.IsAny<Expression<Func<SysWorkflowDefinition, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        WorkflowStartRequest? captured = null;
        fixture.Engine
            .Setup(value => value.StartAsync(It.IsAny<WorkflowStartRequest>(), It.IsAny<CancellationToken>()))
            .Callback<WorkflowStartRequest, CancellationToken>((request, _) => captured = request)
            .ReturnsAsync(WorkflowTestHelper.CreateInstance(status: WorkflowInstanceStatus.Running, parentInstanceId: null));

        var result = await fixture.Service.StartAsync(CreateStartRequest());

        Assert.NotNull(captured);
        Assert.Equal(BusinessCode, captured.DefinitionCode, StringComparer.Ordinal);
        Assert.Equal("B-1", captured.CorrelationId, StringComparer.Ordinal);
        Assert.Equal("42", captured.StarterId, StringComparer.Ordinal);
        Assert.Equal("报销：差旅费", captured.Name, StringComparer.Ordinal);
        Assert.Equal(1280m, captured.Variables["amount"]);
        Assert.Equal(900800700600500L, result.InstanceId);
        Assert.Equal(WorkflowInstanceStatus.Running, result.Status);
        fixture.Manager.Verify(value => value.CreateAsync(It.IsAny<WorkflowDefinition>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 本租户还没有任何同编码流程时，按业务默认模板建一个并发布，再发起。
    /// </summary>
    [Fact]
    public async Task StartAsync_NoDefinitionYet_ShouldProvisionDefaultTemplate()
    {
        var fixture = new ServiceFixture(defaultDefinition: () => WorkflowDefinitionBuilder.Create(BusinessCode, "报销")
            .AddStart()
            .AddOutcomeEnd("end", WorkflowUserTaskOutcomes.Approved)
            .AddTransition("start", "end")
            .Build());
        fixture.Definitions
            .Setup(value => value.AnyAsync(It.IsAny<Expression<Func<SysWorkflowDefinition, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        fixture.Manager
            .Setup(value => value.CreateAsync(It.IsAny<WorkflowDefinition>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(WorkflowTestHelper.CreateDefinition(id: "555", status: WorkflowDefinitionStatus.Draft));
        fixture.Engine
            .Setup(value => value.StartAsync(It.IsAny<WorkflowStartRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(WorkflowTestHelper.CreateInstance(status: WorkflowInstanceStatus.Running, parentInstanceId: null));

        _ = await fixture.Service.StartAsync(CreateStartRequest());

        fixture.Manager.Verify(value => value.CreateAsync(It.Is<WorkflowDefinition>(definition => definition.Code == BusinessCode), It.IsAny<CancellationToken>()), Times.Once);
        fixture.Manager.Verify(value => value.PublishAsync("555", It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 发起即故障（如首个节点解析不到审批人）：不在事务里时终止掉落库的实例，并以业务异常整单失败。
    /// </summary>
    [Fact]
    public async Task StartAsync_FaultedOutsideTransaction_ShouldTerminateAndThrow()
    {
        var fixture = new ServiceFixture();
        fixture.Definitions
            .Setup(value => value.AnyAsync(It.IsAny<Expression<Func<SysWorkflowDefinition, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        fixture.Engine
            .Setup(value => value.StartAsync(It.IsAny<WorkflowStartRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(WorkflowTestHelper.CreateInstance(status: WorkflowInstanceStatus.Faulted, faultMessage: "人工任务节点 approve 未解析到受理人"));

        var exception = await Assert.ThrowsAsync<BusinessException>(() => fixture.Service.StartAsync(CreateStartRequest()));

        Assert.Contains("未解析到受理人", exception.Message, StringComparison.Ordinal);
        fixture.Engine.Verify(value => value.TerminateAsync("900800700600500", It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 流程未发布等引擎拒绝发起时，业务异常要点明去发布哪个编码的流程。
    /// </summary>
    [Fact]
    public async Task StartAsync_EngineRejects_ShouldPointToDefinitionCode()
    {
        var fixture = new ServiceFixture();
        fixture.Definitions
            .Setup(value => value.AnyAsync(It.IsAny<Expression<Func<SysWorkflowDefinition, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        fixture.Engine
            .Setup(value => value.StartAsync(It.IsAny<WorkflowStartRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new WorkflowException("编码 expense 没有已发布的定义"));

        var exception = await Assert.ThrowsAsync<BusinessException>(() => fixture.Service.StartAsync(CreateStartRequest()));

        Assert.Contains("expense", exception.Message, StringComparison.Ordinal);
        Assert.Contains("已发布", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 未登记的业务编码必须拒绝，不能当成普通流程悄悄发起。
    /// </summary>
    [Fact]
    public async Task StartAsync_UnknownBusiness_ShouldThrow()
    {
        var fixture = new ServiceFixture();

        var exception = await Assert.ThrowsAsync<BusinessException>(() => fixture.Service.StartAsync(new WorkflowBusinessStartRequest
        {
            BusinessCode = "unknown",
            BusinessId = "1",
            Title = "未知"
        }));

        Assert.Contains("未登记", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 撤回取消该单据的全部进行中实例；没有进行中的实例返回 false。
    /// </summary>
    [Fact]
    public async Task WithdrawAsync_ShouldCancelActiveInstances()
    {
        var fixture = new ServiceFixture();
        fixture.Instances
            .Setup(value => value.GetActiveByCorrelationIdAsync(BusinessCode, "B-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new SysWorkflowInstance(11L), new SysWorkflowInstance(12L)]);
        fixture.Instances
            .Setup(value => value.GetActiveByCorrelationIdAsync(BusinessCode, "B-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        Assert.True(await fixture.Service.WithdrawAsync(BusinessCode, "B-1", "申请人撤回"));
        Assert.False(await fixture.Service.WithdrawAsync(BusinessCode, "B-2", "申请人撤回"));

        fixture.Engine.Verify(value => value.CancelAsync("11", "申请人撤回", It.IsAny<CancellationToken>()), Times.Once);
        fixture.Engine.Verify(value => value.CancelAsync("12", "申请人撤回", It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 进度：运行中实例带出当前待办节点名与审批人显示名；已结束实例带出结果、不查待办。
    /// </summary>
    [Fact]
    public async Task GetProgressAsync_ShouldMapPendingAssigneesAndOutcome()
    {
        var fixture = new ServiceFixture();
        var running = new SysWorkflowInstance(21L) { DefinitionId = 100200300400500L, Status = WorkflowInstanceStatus.Running, CorrelationId = "B-1" };
        var completed = new SysWorkflowInstance(22L) { DefinitionId = 100200300400500L, Status = WorkflowInstanceStatus.Completed, Outcome = "rejected", CorrelationId = "B-2" };
        fixture.Instances
            .Setup(value => value.GetLatestByCorrelationIdsAsync(BusinessCode, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, SysWorkflowInstance>(StringComparer.Ordinal) { ["B-1"] = running, ["B-2"] = completed });
        fixture.Tasks
            .Setup(value => value.GetPendingByInstanceAsync("21", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new WorkflowUserTask { TaskId = "1", InstanceId = "21", NodeId = "end", AssigneeId = "7001" }]);
        fixture.DefinitionStore
            .Setup(value => value.FindAsync("100200300400500", It.IsAny<CancellationToken>()))
            .ReturnsAsync(WorkflowTestHelper.CreateDefinition());
        var user = new SysUser { UserName = "zhangsan", RealName = "张三" };
        WorkflowTestHelper.SetBasicId(user, 7001L);
        fixture.Users
            .Setup(value => value.GetListByIdsIgnoreTenantAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([user]);

        var progress = await fixture.Service.GetProgressAsync(BusinessCode, ["B-1", "B-2"]);

        Assert.Equal(["结束"], progress["B-1"].PendingNodeNames);
        var assignee = Assert.Single(progress["B-1"].PendingAssignees);
        Assert.Equal((7001L, "张三"), (assignee.UserId, assignee.DisplayName));
        Assert.Equal("rejected", progress["B-2"].Outcome, StringComparer.Ordinal);
        Assert.Empty(progress["B-2"].PendingAssignees);
        fixture.Tasks.Verify(value => value.GetPendingByInstanceAsync("22", It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 结论分发：没有环境事务时为业务处理器单开事务型工作单元并提交，上下文里业务编码/单据/结果对得上。
    /// </summary>
    [Fact]
    public async Task OutcomeHandler_WithoutAmbientTransaction_ShouldRunInNewUnitOfWork()
    {
        var handler = CreateHandler();
        WorkflowBusinessOutcomeContext? captured = null;
        handler
            .Setup(value => value.OnOutcomeAsync(It.IsAny<WorkflowBusinessOutcomeContext>(), It.IsAny<CancellationToken>()))
            .Callback<WorkflowBusinessOutcomeContext, CancellationToken>((context, _) => captured = context)
            .Returns(Task.CompletedTask);
        var unitOfWork = new Mock<IUnitOfWork>();
        var unitOfWorkManager = new Mock<IUnitOfWorkManager>();
        unitOfWorkManager
            .Setup(value => value.Begin(It.Is<XiHanUnitOfWorkOptions>(options => options.IsTransactional), true))
            .Returns(unitOfWork.Object);
        var dispatcher = new WorkflowBusinessOutcomeHandler(CreateRegistry(handler.Object), unitOfWorkManager.Object);

        await dispatcher.HandleAsync(CreateOutcomeContext(WorkflowUserTaskOutcomes.Approved));

        Assert.NotNull(captured);
        Assert.Equal(BusinessCode, captured.BusinessCode, StringComparer.Ordinal);
        Assert.Equal("ORDER-2024-0001", captured.BusinessId, StringComparer.Ordinal);
        Assert.Equal(WorkflowUserTaskOutcomes.Approved, captured.Outcome, StringComparer.Ordinal);
        Assert.Equal(900800700600500L, captured.InstanceId);
        unitOfWork.Verify(value => value.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 结论不在业务允许的结果里视为定义错误：抛出异常（结束节点随之故障），不回调业务。
    /// </summary>
    [Fact]
    public async Task OutcomeHandler_UnsupportedOutcome_ShouldThrow()
    {
        var handler = CreateHandler();
        var dispatcher = new WorkflowBusinessOutcomeHandler(CreateRegistry(handler.Object), Mock.Of<IUnitOfWorkManager>());

        var exception = await Assert.ThrowsAsync<BusinessException>(() => dispatcher.HandleAsync(CreateOutcomeContext("returned")));

        Assert.Contains("returned", exception.Message, StringComparison.Ordinal);
        handler.Verify(value => value.OnOutcomeAsync(It.IsAny<WorkflowBusinessOutcomeContext>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 非审批业务流程与子流程实例都不分发。
    /// </summary>
    /// <param name="definitionCode">定义编码。</param>
    /// <param name="parentInstanceId">父实例标识。</param>
    [Theory]
    [InlineData("leave", null)]
    [InlineData(BusinessCode, "111222333444555")]
    public async Task OutcomeHandler_NotBusinessTopLevel_ShouldSkip(string definitionCode, string? parentInstanceId)
    {
        var handler = CreateHandler();
        var dispatcher = new WorkflowBusinessOutcomeHandler(CreateRegistry(handler.Object), Mock.Of<IUnitOfWorkManager>());

        await dispatcher.HandleAsync(CreateOutcomeContext(WorkflowUserTaskOutcomes.Approved, definitionCode, parentInstanceId));

        handler.Verify(value => value.OnOutcomeAsync(It.IsAny<WorkflowBusinessOutcomeContext>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 发布校验：审批业务流程的结束节点必须声明允许的结果，且不得使用终止节点；非业务流程不受约束。
    /// </summary>
    [Fact]
    public void DefinitionGuard_ShouldRequireOutcomeEndsAndForbidTerminate()
    {
        var guard = new WorkflowBusinessDefinitionGuard(CreateRegistry(CreateHandler().Object));
        var definition = WorkflowDefinitionBuilder.Create(BusinessCode, "报销")
            .AddStart()
            .AddEnd("plain-end")
            .AddOutcomeEnd("odd-end", "returned")
            .AddOutcomeEnd("ok-end", WorkflowUserTaskOutcomes.Approved)
            .AddNode("stop", WorkflowActivityTypes.Terminate, "终止")
            .Build();

        var errors = guard.Validate(definition);

        Assert.Contains(errors, error => error.Contains("plain-end", StringComparison.Ordinal) && error.Contains("必须声明结果", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("odd-end", StringComparison.Ordinal) && error.Contains("returned", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("stop", StringComparison.Ordinal) && error.Contains("终止节点", StringComparison.Ordinal));
        Assert.DoesNotContain(errors, error => error.Contains("ok-end", StringComparison.Ordinal));

        definition.Code = "leave";
        Assert.Empty(guard.Validate(definition));
    }

    private static WorkflowBusinessStartRequest CreateStartRequest()
    {
        return new WorkflowBusinessStartRequest
        {
            BusinessCode = BusinessCode,
            BusinessId = " B-1 ",
            Title = "报销：差旅费",
            Variables = new Dictionary<string, object?> { ["amount"] = 1280m }
        };
    }

    private static WorkflowOutcomeContext CreateOutcomeContext(string outcome, string definitionCode = BusinessCode, string? parentInstanceId = null)
    {
        var definition = WorkflowTestHelper.CreateDefinition();
        definition.Code = definitionCode;
        var instance = WorkflowTestHelper.CreateInstance(status: WorkflowInstanceStatus.Running, parentInstanceId: parentInstanceId);
        instance.CorrelationId = "ORDER-2024-0001";
        return new WorkflowOutcomeContext
        {
            Definition = definition,
            Instance = instance,
            Node = definition.Nodes[^1],
            Outcome = outcome,
            Variables = new Dictionary<string, object?>(),
            ServiceProvider = new ServiceCollection().BuildServiceProvider()
        };
    }

    private static Mock<IWorkflowBusinessHandler> CreateHandler(Func<WorkflowDefinition>? defaultDefinition = null)
    {
        var handler = new Mock<IWorkflowBusinessHandler>();
        handler.SetupGet(value => value.Descriptor).Returns(new WorkflowBusinessDescriptor
        {
            Code = BusinessCode,
            Name = "费用报销",
            FormComponent = "expense/approval",
            DefaultDefinition = defaultDefinition
        });
        return handler;
    }

    private static WorkflowBusinessRegistry CreateRegistry(IWorkflowBusinessHandler handler)
    {
        var services = new ServiceCollection();
        services.AddSingleton(handler);
        return new WorkflowBusinessRegistry(services.BuildServiceProvider());
    }

    /// <summary>
    /// 审批业务服务夹具（全部依赖为 Moq 桩，默认不在事务里、没有进行中的实例）。
    /// </summary>
    private sealed class ServiceFixture
    {
        public ServiceFixture(long? userId = 99L, Func<WorkflowDefinition>? defaultDefinition = null)
        {
            Instances
                .Setup(value => value.GetActiveByCorrelationIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            Users
                .Setup(value => value.GetListByIdsIgnoreTenantAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            var currentUser = new Mock<ICurrentUser>();
            currentUser.SetupGet(value => value.UserId).Returns(userId);

            Service = new WorkflowBusinessService(
                CreateRegistry(CreateHandler(defaultDefinition).Object),
                new WorkflowUserNameResolver(Users.Object),
                Engine.Object,
                Manager.Object,
                Definitions.Object,
                DefinitionStore.Object,
                Instances.Object,
                Tasks.Object,
                currentUser.Object,
                Mock.Of<IUnitOfWorkManager>(),
                new RecordingLogger<WorkflowBusinessService>(),
                Mock.Of<IClock>());
        }

        public WorkflowBusinessService Service { get; }

        public Mock<IWorkflowEngine> Engine { get; } = new();

        public Mock<IWorkflowDefinitionManager> Manager { get; } = new();

        public Mock<IWorkflowDefinitionRepository> Definitions { get; } = new();

        public Mock<IWorkflowDefinitionStore> DefinitionStore { get; } = new();

        public Mock<IWorkflowInstanceRepository> Instances { get; } = new();

        public Mock<IWorkflowUserTaskService> Tasks { get; } = new();

        public Mock<IUserRepository> Users { get; } = new();
    }
}
