// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using System.Text.Json;
using XiHan.BasicApp.Workflow.Application.AppServices;
using XiHan.BasicApp.Workflow.Application.Dtos;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Workflow.Abstractions.Engine;
using XiHan.Framework.Workflow.Abstractions.Exceptions;
using XiHan.Framework.Workflow.Abstractions.Runtime;

namespace XiHan.BasicApp.Workflow.Tests;

/// <summary>
/// 工作流实例命令应用服务的发起人锁定、JSON 入参解析与异常翻译测试。
/// </summary>
/// <remarks>
/// 关键约定是"发起人由服务端从当前登录用户取，不接受前端传入"——一旦改成从 DTO 取，
/// 任何人都能以他人身份发起流程；另一条是 JSON 文本入参必须是对象，
/// 数组/标量/null 都要在应用层变成可纠正的业务异常，而不是让引擎收到半个字典。
/// </remarks>
public sealed class WorkflowInstanceAppServiceTests
{
    /// <summary>
    /// 发起实例入参为 null 时必须抛空引用参数异常。
    /// </summary>
    [Fact]
    public async Task StartAsync_NullInput_ShouldThrowArgumentNullException()
    {
        var (service, engine, _) = CreateService();

        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => service.StartAsync(null!));

        engine.Verify(value => value.StartAsync(It.IsAny<WorkflowStartRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 流程编码为空或全空白时必须被参数校验拦下。
    /// </summary>
    /// <param name="definitionCode">空白的流程编码。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task StartAsync_BlankDefinitionCode_ShouldThrowArgumentException(string? definitionCode)
    {
        var (service, _, _) = CreateService();

        _ = await Assert.ThrowsAnyAsync<ArgumentException>(
            () => service.StartAsync(new WorkflowInstanceStartDto { DefinitionCode = definitionCode! }));
    }

    /// <summary>
    /// 发起实例必须裁剪编码、透传版本与相关性，并把发起人锁定为当前登录用户主键。
    /// </summary>
    [Fact]
    public async Task StartAsync_ShouldLockStarterToCurrentUserAndForwardRequest()
    {
        var (service, engine, _) = CreateService(userId: 99L);
        WorkflowStartRequest? request = null;
        engine
            .Setup(value => value.StartAsync(It.IsAny<WorkflowStartRequest>(), It.IsAny<CancellationToken>()))
            .Callback<WorkflowStartRequest, CancellationToken>((value, _) => request = value)
            .ReturnsAsync(WorkflowTestHelper.CreateInstance());

        _ = await service.StartAsync(new WorkflowInstanceStartDto
        {
            DefinitionCode = "  leave  ",
            DefinitionVersion = 3,
            Name = "张三的请假",
            CorrelationId = "ORDER-2024-0001",
            VariablesJson = "{\"days\":3}"
        });

        Assert.NotNull(request);
        Assert.Equal("leave", request!.DefinitionCode, StringComparer.Ordinal);
        Assert.Equal(3, request.DefinitionVersion);
        Assert.Equal("张三的请假", request.Name, StringComparer.Ordinal);
        Assert.Equal("ORDER-2024-0001", request.CorrelationId, StringComparer.Ordinal);
        Assert.Equal("99", request.StarterId, StringComparer.Ordinal);
        Assert.Equal(3, Assert.IsType<JsonElement>(request.Variables["days"]).GetInt32());
    }

    /// <summary>
    /// 未登录时发起人为空：发起动作本身的鉴权由权限特性负责，本服务不得伪造一个发起人。
    /// </summary>
    [Fact]
    public async Task StartAsync_WithoutCurrentUser_ShouldLeaveStarterNull()
    {
        var (service, engine, _) = CreateService(userId: null);
        WorkflowStartRequest? request = null;
        engine
            .Setup(value => value.StartAsync(It.IsAny<WorkflowStartRequest>(), It.IsAny<CancellationToken>()))
            .Callback<WorkflowStartRequest, CancellationToken>((value, _) => request = value)
            .ReturnsAsync(WorkflowTestHelper.CreateInstance());

        _ = await service.StartAsync(new WorkflowInstanceStartDto { DefinitionCode = "leave" });

        Assert.Null(request!.StarterId);
    }

    /// <summary>
    /// 启动变量为空文本时必须落成空字典，而不是 null——引擎按字典遍历初始变量。
    /// </summary>
    /// <param name="variablesJson">空白的变量 JSON。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task StartAsync_BlankVariablesJson_ShouldProduceEmptyDictionary(string? variablesJson)
    {
        var (service, engine, _) = CreateService();
        WorkflowStartRequest? request = null;
        engine
            .Setup(value => value.StartAsync(It.IsAny<WorkflowStartRequest>(), It.IsAny<CancellationToken>()))
            .Callback<WorkflowStartRequest, CancellationToken>((value, _) => request = value)
            .ReturnsAsync(WorkflowTestHelper.CreateInstance());

        _ = await service.StartAsync(new WorkflowInstanceStartDto
        {
            DefinitionCode = "leave",
            VariablesJson = variablesJson
        });

        Assert.NotNull(request!.Variables);
        Assert.Empty(request.Variables);
    }

    /// <summary>
    /// 启动变量不是 JSON 对象时必须翻译成业务异常，并带上"启动变量"这个可定位的名词。
    /// </summary>
    /// <param name="variablesJson">非 JSON 对象的变量文本。</param>
    [Theory]
    [InlineData("[1,2]")]
    [InlineData("{")]
    [InlineData("\"text\"")]
    public async Task StartAsync_InvalidVariablesJson_ShouldThrowBusinessException(string variablesJson)
    {
        var (service, _, _) = CreateService();

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => service.StartAsync(new WorkflowInstanceStartDto
            {
                DefinitionCode = "leave",
                VariablesJson = variablesJson
            }));

        Assert.Contains("启动变量", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 变量文本为 null 字面量时必须给出"必须是 JSON 对象"的明确提示，而不是静默当成空变量。
    /// </summary>
    [Fact]
    public async Task StartAsync_NullLiteralVariablesJson_ShouldRequireJsonObject()
    {
        var (service, _, _) = CreateService();

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => service.StartAsync(new WorkflowInstanceStartDto
            {
                DefinitionCode = "leave",
                VariablesJson = "null"
            }));

        Assert.Equal("启动变量必须是 JSON 对象", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 发起结果必须映射成列表项 DTO：父实例标识由雪花字符串还原成 long，故障字段一并带出。
    /// </summary>
    [Fact]
    public async Task StartAsync_ShouldMapInstanceModelToListItemDto()
    {
        var (service, engine, _) = CreateService();
        engine
            .Setup(value => value.StartAsync(It.IsAny<WorkflowStartRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(WorkflowTestHelper.CreateInstance());

        var dto = await service.StartAsync(new WorkflowInstanceStartDto { DefinitionCode = "leave" });

        Assert.Equal(900800700600500L, dto.BasicId);
        Assert.Equal("leave", dto.DefinitionCode, StringComparer.Ordinal);
        Assert.Equal(3, dto.DefinitionVersion);
        Assert.Equal("张三的请假", dto.Name, StringComparer.Ordinal);
        Assert.Equal(WorkflowInstanceStatus.Faulted, dto.Status);
        Assert.Equal("ORDER-2024-0001", dto.CorrelationId, StringComparer.Ordinal);
        Assert.Equal("1001", dto.StarterId, StringComparer.Ordinal);
        Assert.Equal(111222333444555L, dto.ParentInstanceId);
        Assert.Equal(1, dto.Depth);
        Assert.Equal(WorkflowTestHelper.CreationTime, dto.CreationTime);
        Assert.Equal(WorkflowTestHelper.StartTime, dto.StartTime);
        Assert.Equal(WorkflowTestHelper.EndTime, dto.EndTime);
        Assert.Equal("approve", dto.FaultNodeId, StringComparer.Ordinal);
        Assert.Equal("节点执行超时", dto.FaultMessage, StringComparer.Ordinal);
    }

    /// <summary>
    /// 顶层实例映射后父实例主键必须为空，不得被解析成 0。
    /// </summary>
    [Fact]
    public async Task StartAsync_TopLevelInstance_ShouldMapNullParentKey()
    {
        var (service, engine, _) = CreateService();
        engine
            .Setup(value => value.StartAsync(It.IsAny<WorkflowStartRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(WorkflowTestHelper.CreateInstance(parentInstanceId: null));

        var dto = await service.StartAsync(new WorkflowInstanceStartDto { DefinitionCode = "leave" });

        Assert.Null(dto.ParentInstanceId);
    }

    /// <summary>
    /// 引擎抛出的协议异常必须翻译成业务异常，消息原样保留。
    /// </summary>
    [Fact]
    public async Task StartAsync_EngineThrowsWorkflowException_ShouldTranslateToBusinessException()
    {
        var (service, engine, _) = CreateService();
        engine
            .Setup(value => value.StartAsync(It.IsAny<WorkflowStartRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new WorkflowException("流程 leave 没有已发布版本"));

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => service.StartAsync(new WorkflowInstanceStartDto { DefinitionCode = "leave" }));

        Assert.Equal("流程 leave 没有已发布版本", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 取消 / 终止 / 挂起必须把主键与原因一起下探，原因是审计与前端展示的唯一来源。
    /// </summary>
    [Fact]
    public async Task ReasonedOperations_ShouldForwardKeyAndReason()
    {
        var (service, engine, _) = CreateService();
        engine
            .Setup(value => value.CancelAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(WorkflowTestHelper.CreateInstance(status: WorkflowInstanceStatus.Canceled));
        engine
            .Setup(value => value.TerminateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(WorkflowTestHelper.CreateInstance(status: WorkflowInstanceStatus.Terminated));
        engine
            .Setup(value => value.SuspendAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(WorkflowTestHelper.CreateInstance(status: WorkflowInstanceStatus.Suspended));

        var canceled = await service.CancelAsync(new WorkflowInstanceOperationDto { BasicId = 900800700600500L, Reason = "申请人撤回" });
        var terminated = await service.TerminateAsync(new WorkflowInstanceOperationDto { BasicId = 900800700600500L, Reason = "运维强制终止" });
        var suspended = await service.SuspendAsync(new WorkflowInstanceOperationDto { BasicId = 900800700600500L, Reason = null });

        engine.Verify(value => value.CancelAsync("900800700600500", "申请人撤回", It.IsAny<CancellationToken>()), Times.Once);
        engine.Verify(value => value.TerminateAsync("900800700600500", "运维强制终止", It.IsAny<CancellationToken>()), Times.Once);
        engine.Verify(value => value.SuspendAsync("900800700600500", null, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(WorkflowInstanceStatus.Canceled, canceled.Status);
        Assert.Equal(WorkflowInstanceStatus.Terminated, terminated.Status);
        Assert.Equal(WorkflowInstanceStatus.Suspended, suspended.Status);
    }

    /// <summary>
    /// 重试与恢复只带主键，必须按不变文化十进制转成框架标识。
    /// </summary>
    [Fact]
    public async Task KeyOnlyOperations_ShouldForwardStringifiedKey()
    {
        var (service, engine, _) = CreateService();
        engine
            .Setup(value => value.RetryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(WorkflowTestHelper.CreateInstance(status: WorkflowInstanceStatus.Running));
        engine
            .Setup(value => value.ResumeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(WorkflowTestHelper.CreateInstance(status: WorkflowInstanceStatus.Running));

        _ = await service.RetryAsync(new WorkflowInstanceIdDto { BasicId = 900800700600500L });
        _ = await service.ResumeAsync(new WorkflowInstanceIdDto { BasicId = 900800700600500L });

        engine.Verify(value => value.RetryAsync("900800700600500", It.IsAny<CancellationToken>()), Times.Once);
        engine.Verify(value => value.ResumeAsync("900800700600500", It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 全部实例操作入参为 null 时必须抛空引用参数异常，不得下探到引擎。
    /// </summary>
    [Fact]
    public async Task InstanceOperations_NullInput_ShouldThrowArgumentNullException()
    {
        var (service, _, _) = CreateService();

        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => service.CancelAsync(null!));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => service.TerminateAsync(null!));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => service.SuspendAsync(null!));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => service.RetryAsync(null!));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => service.ResumeAsync(null!));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => service.PublishSignalAsync(null!));
    }

    /// <summary>
    /// 信号名为空或全空白时必须被参数校验拦下，空信号名会命中全部等待信号的书签。
    /// </summary>
    /// <param name="signalName">空白的信号名。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task PublishSignalAsync_BlankSignalName_ShouldThrowArgumentException(string? signalName)
    {
        var (service, _, _) = CreateService();

        _ = await Assert.ThrowsAnyAsync<ArgumentException>(
            () => service.PublishSignalAsync(new WorkflowSignalPublishDto { SignalName = signalName! }));
    }

    /// <summary>
    /// 发布信号必须裁剪信号名、解析载荷、透传相关性，并把恢复条数原样回传。
    /// </summary>
    [Fact]
    public async Task PublishSignalAsync_ShouldForwardTrimmedNamePayloadAndCorrelation()
    {
        var (service, engine, _) = CreateService();
        Dictionary<string, object?>? payload = null;
        engine
            .Setup(value => value.PublishSignalAsync(
                It.IsAny<string>(), It.IsAny<Dictionary<string, object?>?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<string, Dictionary<string, object?>?, string?, CancellationToken>((_, value, _, _) => payload = value)
            .ReturnsAsync(4);

        var result = await service.PublishSignalAsync(new WorkflowSignalPublishDto
        {
            SignalName = "  paid  ",
            CorrelationId = "ORDER-2024-0001",
            PayloadJson = "{\"amount\":100}"
        });

        Assert.Equal(4, result.ResumedCount);
        Assert.Equal(100, Assert.IsType<JsonElement>(payload!["amount"]).GetInt32());
        engine.Verify(
            value => value.PublishSignalAsync("paid", It.IsAny<Dictionary<string, object?>?>(), "ORDER-2024-0001", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 信号载荷非法时必须翻译成带"信号载荷"字样的业务异常。
    /// </summary>
    [Fact]
    public async Task PublishSignalAsync_InvalidPayloadJson_ShouldThrowBusinessException()
    {
        var (service, _, _) = CreateService();

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => service.PublishSignalAsync(new WorkflowSignalPublishDto
            {
                SignalName = "paid",
                PayloadJson = "[1]"
            }));

        Assert.Contains("信号载荷", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 取消令牌必须透传到引擎。
    /// </summary>
    [Fact]
    public async Task CancelAsync_ShouldForwardCancellationToken()
    {
        var (service, engine, _) = CreateService();
        using var cancellation = new CancellationTokenSource();
        var forwarded = CancellationToken.None;
        engine
            .Setup(value => value.CancelAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<string, string?, CancellationToken>((_, _, token) => forwarded = token)
            .ReturnsAsync(WorkflowTestHelper.CreateInstance(status: WorkflowInstanceStatus.Canceled));

        _ = await service.CancelAsync(new WorkflowInstanceOperationDto { BasicId = 1L }, cancellation.Token);

        Assert.Equal(cancellation.Token, forwarded);
    }

    /// <summary>
    /// 构造被测服务与其 Moq 引擎、当前用户。
    /// </summary>
    /// <param name="userId">当前登录用户主键（null 表示未登录）。</param>
    /// <returns>应用服务、引擎桩、当前用户桩。</returns>
    private static (WorkflowInstanceAppService Service, Mock<IWorkflowEngine> Engine, Mock<ICurrentUser> CurrentUser) CreateService(long? userId = 99L)
    {
        var engine = new Mock<IWorkflowEngine>();
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(value => value.UserId).Returns(userId);
        return (new WorkflowInstanceAppService(engine.Object, currentUser.Object), engine, currentUser);
    }
}
