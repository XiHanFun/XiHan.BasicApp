// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using XiHan.BasicApp.Workflow.Application.AppServices;
using XiHan.BasicApp.Workflow.Application.Dtos;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Workflow.Abstractions.Definitions;
using XiHan.Framework.Workflow.Abstractions.Exceptions;
using XiHan.Framework.Workflow.Builders;

namespace XiHan.BasicApp.Workflow.Tests;

/// <summary>
/// 工作流定义命令应用服务的参数校验与异常翻译测试。
/// </summary>
/// <remarks>
/// 本服务只做三件事，每件都容易在重构中丢：把设计器 JSON 解析成定义模型、
/// 把 DTO 里的主键覆盖回定义标识（漏了就会拿 JSON 里的旧标识去更新别人的草稿）、
/// 把 <see cref="WorkflowException"/> 翻译成 <see cref="BusinessException"/>
/// （漏了就从"可纠正的 400"退化成"500 服务器错误"）。
/// </remarks>
public sealed class WorkflowDefinitionAppServiceTests
{
    /// <summary>
    /// 合法的设计器定义 JSON（与存储真源同一序列化口径）。
    /// </summary>
    private static string ValidDefinitionJson => WorkflowDefinitionJsonSerializer.Serialize(WorkflowTestHelper.CreateDefinition());

    /// <summary>
    /// 入参为 null 时必须抛空引用参数异常，不得进到解析环节。
    /// </summary>
    [Fact]
    public async Task CreateAsync_NullInput_ShouldThrowArgumentNullException()
    {
        var (service, manager) = CreateService();

        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => service.CreateAsync(null!));

        manager.Verify(value => value.CreateAsync(It.IsAny<WorkflowDefinition>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 定义 JSON 为空或全空白时必须被参数校验拦下（null 抛的是派生的 ArgumentNullException）。
    /// </summary>
    /// <param name="definitionJson">空白的定义 JSON。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_BlankDefinitionJson_ShouldThrowArgumentException(string? definitionJson)
    {
        var (service, _) = CreateService();

        _ = await Assert.ThrowsAnyAsync<ArgumentException>(
            () => service.CreateAsync(new WorkflowDefinitionCreateDto { DefinitionJson = definitionJson! }));
    }

    /// <summary>
    /// 非法 JSON 必须翻译成业务异常并保留框架给出的原因，前端才能提示"定义格式错误"。
    /// </summary>
    /// <param name="definitionJson">非法的定义 JSON。</param>
    [Theory]
    [InlineData("{")]
    [InlineData("not-json")]
    [InlineData("[]")]
    public async Task CreateAsync_InvalidDefinitionJson_ShouldThrowBusinessException(string definitionJson)
    {
        var (service, _) = CreateService();

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => service.CreateAsync(new WorkflowDefinitionCreateDto { DefinitionJson = definitionJson }));

        Assert.Contains("流程定义 JSON 非法", exception.Message!, StringComparison.Ordinal);
    }

    /// <summary>
    /// 创建成功后必须把解析出的定义交给管理器，并把返回模型映射成含 JSON 的详情 DTO。
    /// </summary>
    [Fact]
    public async Task CreateAsync_ValidInput_ShouldForwardParsedDefinitionAndMapDetail()
    {
        var (service, manager) = CreateService();
        WorkflowDefinition? forwarded = null;
        manager
            .Setup(value => value.CreateAsync(It.IsAny<WorkflowDefinition>(), It.IsAny<CancellationToken>()))
            .Callback<WorkflowDefinition, CancellationToken>((definition, _) => forwarded = definition)
            .ReturnsAsync(WorkflowTestHelper.CreateDefinition(status: WorkflowDefinitionStatus.Draft));

        var detail = await service.CreateAsync(new WorkflowDefinitionCreateDto { DefinitionJson = ValidDefinitionJson });

        Assert.NotNull(forwarded);
        Assert.Equal("leave", forwarded!.Code, StringComparer.Ordinal);
        Assert.Equal(2, forwarded.Nodes.Count);

        Assert.Equal(100200300400500L, detail.BasicId);
        Assert.Equal("leave", detail.Code, StringComparer.Ordinal);
        Assert.Equal("请假流程", detail.Name, StringComparer.Ordinal);
        Assert.Equal(3, detail.Version);
        Assert.Equal("员工请假审批", detail.Description, StringComparer.Ordinal);
        Assert.Equal("hr", detail.Category, StringComparer.Ordinal);
        Assert.Equal(WorkflowDefinitionStatus.Draft, detail.Status);
        Assert.True(detail.EnableCompensation);
        Assert.Equal(WorkflowTestHelper.PublishTime, detail.PublishTime);
        Assert.Equal(WorkflowTestHelper.CreationTime, detail.CreatedTime);
        Assert.Contains("\"code\": \"leave\"", detail.DefinitionJson, StringComparison.Ordinal);
    }

    /// <summary>
    /// 管理器抛出的工作流协议异常必须原样翻译成业务异常，消息不得被吞。
    /// </summary>
    [Fact]
    public async Task CreateAsync_ManagerThrowsWorkflowException_ShouldTranslateToBusinessException()
    {
        var (service, manager) = CreateService();
        manager
            .Setup(value => value.CreateAsync(It.IsAny<WorkflowDefinition>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new WorkflowException("流程编码 leave 已存在草稿版本"));

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => service.CreateAsync(new WorkflowDefinitionCreateDto { DefinitionJson = ValidDefinitionJson }));

        Assert.Equal("流程编码 leave 已存在草稿版本", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 更新草稿必须用 DTO 主键覆盖 JSON 里的定义标识，否则会拿旧标识去改别人的草稿。
    /// </summary>
    [Fact]
    public async Task UpdateDraftAsync_ShouldOverrideDefinitionIdWithInputKey()
    {
        var (service, manager) = CreateService();
        WorkflowDefinition? forwarded = null;
        manager
            .Setup(value => value.UpdateDraftAsync(It.IsAny<WorkflowDefinition>(), It.IsAny<CancellationToken>()))
            .Callback<WorkflowDefinition, CancellationToken>((definition, _) => forwarded = definition)
            .ReturnsAsync(WorkflowTestHelper.CreateDefinition());

        _ = await service.UpdateDraftAsync(new WorkflowDefinitionUpdateDraftDto
        {
            BasicId = 777L,
            DefinitionJson = ValidDefinitionJson
        });

        Assert.Equal("777", forwarded!.Id, StringComparer.Ordinal);
    }

    /// <summary>
    /// 更新草稿入参为 null 时必须抛空引用参数异常。
    /// </summary>
    [Fact]
    public async Task UpdateDraftAsync_NullInput_ShouldThrowArgumentNullException()
    {
        var (service, _) = CreateService();

        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => service.UpdateDraftAsync(null!));
    }

    /// <summary>
    /// 发布 / 停用 / 归档三个生命周期操作都必须把主键按不变文化十进制转成框架标识后下探。
    /// </summary>
    [Fact]
    public async Task LifecycleOperations_ShouldForwardInvariantStringifiedKey()
    {
        var (service, manager) = CreateService();
        manager
            .Setup(value => value.PublishAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(WorkflowTestHelper.CreateDefinition());
        manager
            .Setup(value => value.DisableAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(WorkflowTestHelper.CreateDefinition(status: WorkflowDefinitionStatus.Disabled));
        manager
            .Setup(value => value.ArchiveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(WorkflowTestHelper.CreateDefinition(status: WorkflowDefinitionStatus.Archived));

        _ = await service.PublishAsync(new WorkflowDefinitionIdDto { BasicId = 100200300400500L });
        var disabled = await service.DisableAsync(new WorkflowDefinitionIdDto { BasicId = 100200300400500L });
        var archived = await service.ArchiveAsync(new WorkflowDefinitionIdDto { BasicId = 100200300400500L });

        manager.Verify(value => value.PublishAsync("100200300400500", It.IsAny<CancellationToken>()), Times.Once);
        manager.Verify(value => value.DisableAsync("100200300400500", It.IsAny<CancellationToken>()), Times.Once);
        manager.Verify(value => value.ArchiveAsync("100200300400500", It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(WorkflowDefinitionStatus.Disabled, disabled.Status);
        Assert.Equal(WorkflowDefinitionStatus.Archived, archived.Status);
    }

    /// <summary>
    /// 生命周期操作入参为 null 时必须抛空引用参数异常。
    /// </summary>
    [Fact]
    public async Task LifecycleOperations_NullInput_ShouldThrowArgumentNullException()
    {
        var (service, _) = CreateService();

        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => service.PublishAsync(null!));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => service.DisableAsync(null!));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => service.ArchiveAsync(null!));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => service.NewVersionAsync(null!));
    }

    /// <summary>
    /// 新版本编码为空或全空白时必须被参数校验拦下。
    /// </summary>
    /// <param name="code">空白的流程编码。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task NewVersionAsync_BlankCode_ShouldThrowArgumentException(string? code)
    {
        var (service, _) = CreateService();

        _ = await Assert.ThrowsAnyAsync<ArgumentException>(
            () => service.NewVersionAsync(new WorkflowDefinitionNewVersionDto { Code = code! }));
    }

    /// <summary>
    /// 新版本编码必须去掉首尾空白后再下探，避免" leave "被当成另一个流程编码建出并行版本链。
    /// </summary>
    [Fact]
    public async Task NewVersionAsync_ShouldTrimCodeBeforeForwarding()
    {
        var (service, manager) = CreateService();
        manager
            .Setup(value => value.CreateNewVersionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(WorkflowTestHelper.CreateDefinition(status: WorkflowDefinitionStatus.Draft));

        _ = await service.NewVersionAsync(new WorkflowDefinitionNewVersionDto { Code = "  leave  " });

        manager.Verify(value => value.CreateNewVersionAsync("leave", It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 删除草稿的主键必须为正：0 与负数是明确的调用方错误，不得下探成一次全表误删的输入。
    /// </summary>
    /// <param name="id">非法主键。</param>
    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    [InlineData(long.MinValue)]
    public async Task DeleteAsync_NonPositiveId_ShouldThrowArgumentOutOfRange(long id)
    {
        var (service, manager) = CreateService();

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.DeleteAsync(id));

        Assert.Contains("定义主键必须大于 0", exception.Message, StringComparison.Ordinal);
        manager.Verify(value => value.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 删除草稿必须把主键转成框架标识后下探。
    /// </summary>
    [Fact]
    public async Task DeleteAsync_ValidId_ShouldForwardStringifiedKey()
    {
        var (service, manager) = CreateService();

        await service.DeleteAsync(100200300400500L);

        manager.Verify(value => value.DeleteAsync("100200300400500", It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 删除时管理器抛出的协议异常（如"已发布定义不可删除"）同样必须翻译成业务异常。
    /// </summary>
    [Fact]
    public async Task DeleteAsync_ManagerThrowsWorkflowException_ShouldTranslateToBusinessException()
    {
        var (service, manager) = CreateService();
        manager
            .Setup(value => value.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new WorkflowException("已发布的定义不可删除"));

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.DeleteAsync(1L));

        Assert.Equal("已发布的定义不可删除", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 取消令牌必须透传到定义管理器，长事务的取消才有意义。
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldForwardCancellationToken()
    {
        var (service, manager) = CreateService();
        using var cancellation = new CancellationTokenSource();
        var forwarded = CancellationToken.None;
        manager
            .Setup(value => value.CreateAsync(It.IsAny<WorkflowDefinition>(), It.IsAny<CancellationToken>()))
            .Callback<WorkflowDefinition, CancellationToken>((_, token) => forwarded = token)
            .ReturnsAsync(WorkflowTestHelper.CreateDefinition());

        _ = await service.CreateAsync(
            new WorkflowDefinitionCreateDto { DefinitionJson = ValidDefinitionJson },
            cancellation.Token);

        Assert.Equal(cancellation.Token, forwarded);
    }

    /// <summary>
    /// 构造被测服务与其 Moq 定义管理器。
    /// </summary>
    /// <returns>应用服务与管理器桩。</returns>
    private static (WorkflowDefinitionAppService Service, Mock<IWorkflowDefinitionManager> Manager) CreateService()
    {
        var manager = new Mock<IWorkflowDefinitionManager>();
        return (new WorkflowDefinitionAppService(manager.Object), manager);
    }
}
