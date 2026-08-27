// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Workflow.Application.Dtos;
using XiHan.BasicApp.Workflow.Application.QueryServices;
using XiHan.BasicApp.Workflow.Domain.Entities;
using XiHan.BasicApp.Workflow.Domain.Repositories;
using XiHan.BasicApp.Workflow.Infrastructure.Stores;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.Domain.Shared.Paging.Models;
using XiHan.Framework.Workflow.Abstractions;
using XiHan.Framework.Workflow.Abstractions.Definitions;
using XiHan.Framework.Workflow.Abstractions.Runtime;
using XiHan.Framework.Workflow.Abstractions.Stores;

namespace XiHan.BasicApp.Workflow.Tests;

/// <summary>
/// 定义 / 实例查询应用服务的查询条件构建、字段级安全门控与详情装配测试。
/// </summary>
/// <remarks>
/// 两个查询服务共享同一套约定：过滤条件在服务端按实体表达式构建（前端只给业务参数），
/// 排序与过滤在落到仓储之前必须先过字段级安全门控——门控被绕过就能按脱敏字段排序反推真实值；
/// 默认排序缺失则列表在不同数据库上顺序漂移，分页出现重复行与漏行。
/// </remarks>
public sealed class WorkflowQueryServiceTests
{
    /// <summary>
    /// 定义分页入参为 null 时必须抛空引用参数异常。
    /// </summary>
    [Fact]
    public async Task DefinitionQuery_GetPageAsync_NullInput_ShouldThrowArgumentNullException()
    {
        var (service, _, _) = CreateDefinitionQueryService();

        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => service.GetPageAsync(null!));
    }

    /// <summary>
    /// 已取消的令牌必须在触库前抛出，避免白跑一次分页查询。
    /// </summary>
    [Fact]
    public async Task DefinitionQuery_GetPageAsync_CanceledToken_ShouldThrowBeforeRepository()
    {
        var (service, repository, _) = CreateDefinitionQueryService();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.GetPageAsync(new WorkflowDefinitionPageQueryDto(), cancellation.Token));

        repository.Verify(
            value => value.GetPagedAsync(It.IsAny<PageRequestDtoBase>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 调用方未指定排序时必须补上「编码升序 + 版本降序」，同编码多版本才会稳定地新版在前。
    /// </summary>
    [Fact]
    public async Task DefinitionQuery_GetPageAsync_WithoutSorts_ShouldApplyCodeAscVersionDesc()
    {
        var (service, repository, _) = CreateDefinitionQueryService();
        var request = SetupDefinitionPage(repository);

        _ = await service.GetPageAsync(new WorkflowDefinitionPageQueryDto());

        var sorts = request.Value!.Conditions.Sorts;
        Assert.Equal(2, sorts.Count);
        Assert.Equal(nameof(SysWorkflowDefinition.Code), sorts[0].Field, StringComparer.Ordinal);
        Assert.Equal(SortDirection.Ascending, sorts[0].Direction);
        Assert.Equal(0, sorts[0].Priority);
        Assert.Equal(nameof(SysWorkflowDefinition.Version), sorts[1].Field, StringComparer.Ordinal);
        Assert.Equal(SortDirection.Descending, sorts[1].Direction);
        Assert.Equal(1, sorts[1].Priority);
    }

    /// <summary>
    /// 调用方给了排序时不得再追加默认排序，否则默认列会覆盖用户选择的排序优先级。
    /// </summary>
    [Fact]
    public async Task DefinitionQuery_GetPageAsync_WithCallerSorts_ShouldNotAppendDefaults()
    {
        var (service, repository, _) = CreateDefinitionQueryService();
        var request = SetupDefinitionPage(repository);
        var input = new WorkflowDefinitionPageQueryDto();
        _ = input.Conditions.AddSort(nameof(SysWorkflowDefinition.Name), SortDirection.Descending);

        _ = await service.GetPageAsync(input);

        var sort = Assert.Single(request.Value!.Conditions.Sorts);
        Assert.Equal(nameof(SysWorkflowDefinition.Name), sort.Field, StringComparer.Ordinal);
        Assert.Equal(SortDirection.Descending, sort.Direction);
    }

    /// <summary>
    /// 关键字必须落在编码与名称两列上并去掉首尾空白；状态与分类各自转成等值过滤。
    /// </summary>
    [Fact]
    public async Task DefinitionQuery_GetPageAsync_ShouldBuildKeywordAndFilters()
    {
        var (service, repository, _) = CreateDefinitionQueryService();
        var request = SetupDefinitionPage(repository);

        _ = await service.GetPageAsync(new WorkflowDefinitionPageQueryDto
        {
            Keyword = "  leave  ",
            Status = WorkflowDefinitionStatus.Published,
            Category = "  hr  "
        });

        var conditions = request.Value!.Conditions;
        Assert.NotNull(conditions.Keyword);
        Assert.Equal("leave", conditions.Keyword!.Value, StringComparer.Ordinal);
        Assert.Equal(
            new[] { nameof(SysWorkflowDefinition.Code), nameof(SysWorkflowDefinition.Name) },
            conditions.Keyword.Fields.ToArray());

        var statusFilter = Assert.Single(conditions.Filters, filter => string.Equals(filter.Field, nameof(SysWorkflowDefinition.Status), StringComparison.Ordinal));
        Assert.Equal(WorkflowDefinitionStatus.Published, statusFilter.Value);
        var categoryFilter = Assert.Single(conditions.Filters, filter => string.Equals(filter.Field, nameof(SysWorkflowDefinition.Category), StringComparison.Ordinal));
        Assert.Equal("hr", categoryFilter.Value);
    }

    /// <summary>
    /// 空白关键字与空白分类不得产生过滤条件，否则会退化成"匹配空串"的空结果页。
    /// </summary>
    [Fact]
    public async Task DefinitionQuery_GetPageAsync_BlankTextFilters_ShouldBeIgnored()
    {
        var (service, repository, _) = CreateDefinitionQueryService();
        var request = SetupDefinitionPage(repository);

        _ = await service.GetPageAsync(new WorkflowDefinitionPageQueryDto { Keyword = "   ", Category = "" });

        Assert.Null(request.Value!.Conditions.Keyword);
        Assert.Empty(request.Value.Conditions.Filters);
    }

    /// <summary>
    /// 排序与过滤都必须按定义实体名过一遍字段级安全门控，且发生在落库之前。
    /// </summary>
    [Fact]
    public async Task DefinitionQuery_GetPageAsync_ShouldGuardSortsAndFiltersByEntityName()
    {
        var (service, repository, fieldSecurity) = CreateDefinitionQueryService();
        _ = SetupDefinitionPage(repository);

        _ = await service.GetPageAsync(new WorkflowDefinitionPageQueryDto());

        fieldSecurity.Verify(
            value => value.GuardSortsAsync(It.IsAny<QueryConditions>(), "SysWorkflowDefinition", It.IsAny<CancellationToken>()),
            Times.Once);
        fieldSecurity.Verify(
            value => value.GuardFiltersAsync(It.IsAny<QueryConditions>(), "SysWorkflowDefinition", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 分页结果必须逐条映射成列表项 DTO 并原样保留分页元数据与扩展数据。
    /// </summary>
    [Fact]
    public async Task DefinitionQuery_GetPageAsync_ShouldMapItemsAndKeepPageMetadata()
    {
        var (service, repository, _) = CreateDefinitionQueryService();
        var entity = WorkflowStoreMapper.ToEntity(WorkflowTestHelper.CreateDefinition());
        entity.CreatedTime = DateTimeOffset.UnixEpoch;
        repository
            .Setup(value => value.GetPagedAsync(It.IsAny<PageRequestDtoBase>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageResultDtoBase<SysWorkflowDefinition>([entity], 2, 10, 31)
            {
                ExtendDatas = new Dictionary<string, object> { ["hint"] = "x" }
            });

        var page = await service.GetPageAsync(new WorkflowDefinitionPageQueryDto());

        var item = Assert.Single(page.Items);
        Assert.Equal(100200300400500L, item.BasicId);
        Assert.Equal("leave", item.Code, StringComparer.Ordinal);
        Assert.Equal(3, item.Version);
        Assert.Equal(WorkflowDefinitionStatus.Published, item.Status);
        Assert.Equal(DateTimeOffset.UnixEpoch, item.CreatedTime);
        Assert.Equal(2, page.Page.PageIndex);
        Assert.Equal(10, page.Page.PageSize);
        Assert.Equal(31, page.Page.TotalCount);
        Assert.Equal("x", page.ExtendDatas!["hint"]);
    }

    /// <summary>
    /// 定义详情主键必须为正，0 与负数是明确的调用方错误。
    /// </summary>
    /// <param name="id">非法主键。</param>
    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public async Task DefinitionQuery_GetDetailAsync_NonPositiveId_ShouldThrowArgumentOutOfRange(long id)
    {
        var (service, _, _) = CreateDefinitionQueryService();

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.GetDetailAsync(id));

        Assert.Contains("定义主键必须大于 0", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 定义不存在时必须返回 null，由上层决定 404 还是空态。
    /// </summary>
    [Fact]
    public async Task DefinitionQuery_GetDetailAsync_MissingEntity_ShouldReturnNull()
    {
        var (service, repository, _) = CreateDefinitionQueryService();
        repository
            .Setup(value => value.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysWorkflowDefinition?)null);

        Assert.Null(await service.GetDetailAsync(1L));
    }

    /// <summary>
    /// 定义详情必须带出 JSON 真源，前端设计器打开草稿完全依赖这一字段。
    /// </summary>
    [Fact]
    public async Task DefinitionQuery_GetDetailAsync_ShouldReturnDefinitionJson()
    {
        var (service, repository, _) = CreateDefinitionQueryService();
        repository
            .Setup(value => value.GetByIdAsync(100200300400500L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(WorkflowStoreMapper.ToEntity(WorkflowTestHelper.CreateDefinition()));

        var detail = await service.GetDetailAsync(100200300400500L);

        Assert.NotNull(detail);
        Assert.Equal(100200300400500L, detail!.BasicId);
        Assert.Contains("\"code\": \"leave\"", detail.DefinitionJson, StringComparison.Ordinal);
    }

    /// <summary>
    /// 实例分页入参为 null 时必须抛空引用参数异常。
    /// </summary>
    [Fact]
    public async Task InstanceQuery_GetPageAsync_NullInput_ShouldThrowArgumentNullException()
    {
        var (service, _, _, _, _) = CreateInstanceQueryService();

        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => service.GetPageAsync(null!));
    }

    /// <summary>
    /// 实例列表未指定排序时必须补上「创建时间降序」，最新发起的实例排在最前。
    /// </summary>
    [Fact]
    public async Task InstanceQuery_GetPageAsync_WithoutSorts_ShouldApplyCreationTimeDescending()
    {
        var (service, repository, _, _, _) = CreateInstanceQueryService();
        var request = SetupInstancePage(repository);

        _ = await service.GetPageAsync(new WorkflowInstancePageQueryDto());

        var sort = Assert.Single(request.Value!.Conditions.Sorts);
        Assert.Equal(nameof(SysWorkflowInstance.CreationTime), sort.Field, StringComparer.Ordinal);
        Assert.Equal(SortDirection.Descending, sort.Direction);
    }

    /// <summary>
    /// 实例关键字必须落在实例名与业务相关性两列，其余三个业务参数各自转成等值过滤并去空白。
    /// </summary>
    [Fact]
    public async Task InstanceQuery_GetPageAsync_ShouldBuildKeywordAndFilters()
    {
        var (service, repository, _, _, _) = CreateInstanceQueryService();
        var request = SetupInstancePage(repository);

        _ = await service.GetPageAsync(new WorkflowInstancePageQueryDto
        {
            Keyword = " 张三 ",
            Status = WorkflowInstanceStatus.Running,
            DefinitionCode = " leave ",
            CorrelationId = " ORDER-2024-0001 "
        });

        var conditions = request.Value!.Conditions;
        Assert.Equal("张三", conditions.Keyword!.Value, StringComparer.Ordinal);
        Assert.Equal(
            new[] { nameof(SysWorkflowInstance.Name), nameof(SysWorkflowInstance.CorrelationId) },
            conditions.Keyword.Fields.ToArray());
        Assert.Equal(3, conditions.Filters.Count);
        Assert.Equal(
            WorkflowInstanceStatus.Running,
            Assert.Single(conditions.Filters, filter => string.Equals(filter.Field, nameof(SysWorkflowInstance.Status), StringComparison.Ordinal)).Value);
        Assert.Equal(
            "leave",
            Assert.Single(conditions.Filters, filter => string.Equals(filter.Field, nameof(SysWorkflowInstance.DefinitionCode), StringComparison.Ordinal)).Value);
        Assert.Equal(
            "ORDER-2024-0001",
            Assert.Single(conditions.Filters, filter => string.Equals(filter.Field, nameof(SysWorkflowInstance.CorrelationId), StringComparison.Ordinal)).Value);
    }

    /// <summary>
    /// 实例排序与过滤必须按实例实体名过字段级安全门控。
    /// </summary>
    [Fact]
    public async Task InstanceQuery_GetPageAsync_ShouldGuardSortsAndFiltersByEntityName()
    {
        var (service, repository, _, _, fieldSecurity) = CreateInstanceQueryService();
        _ = SetupInstancePage(repository);

        _ = await service.GetPageAsync(new WorkflowInstancePageQueryDto());

        fieldSecurity.Verify(
            value => value.GuardSortsAsync(It.IsAny<QueryConditions>(), "SysWorkflowInstance", It.IsAny<CancellationToken>()),
            Times.Once);
        fieldSecurity.Verify(
            value => value.GuardFiltersAsync(It.IsAny<QueryConditions>(), "SysWorkflowInstance", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 实例详情主键必须为正。
    /// </summary>
    /// <param name="id">非法主键。</param>
    [Theory]
    [InlineData(0L)]
    [InlineData(-9L)]
    public async Task InstanceQuery_GetDetailAsync_NonPositiveId_ShouldThrowArgumentOutOfRange(long id)
    {
        var (service, _, _, _, _) = CreateInstanceQueryService();

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.GetDetailAsync(id));

        Assert.Contains("实例主键必须大于 0", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 实例不存在时必须直接返回 null，且不得再去查执行历史与等待点（省两次无谓往返）。
    /// </summary>
    [Fact]
    public async Task InstanceQuery_GetDetailAsync_MissingInstance_ShouldSkipHistoryQueries()
    {
        var (service, _, instanceStore, bookmarkStore, _) = CreateInstanceQueryService();
        instanceStore
            .Setup(value => value.FindAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowInstance?)null);

        Assert.Null(await service.GetDetailAsync(900800700600500L));

        instanceStore.Verify(value => value.GetNodeInstancesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        bookmarkStore.Verify(value => value.GetByInstanceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 实例详情必须按主键的不变文化十进制文本去存储取模型，并装配变量 JSON、执行历史与待恢复等待点。
    /// </summary>
    [Fact]
    public async Task InstanceQuery_GetDetailAsync_ShouldAssembleVariablesHistoryAndBookmarks()
    {
        var (service, _, instanceStore, bookmarkStore, _) = CreateInstanceQueryService();
        instanceStore
            .Setup(value => value.FindAsync("900800700600500", It.IsAny<CancellationToken>()))
            .ReturnsAsync(WorkflowTestHelper.CreateInstance());
        instanceStore
            .Setup(value => value.GetNodeInstancesAsync("900800700600500", It.IsAny<CancellationToken>()))
            .ReturnsAsync([WorkflowTestHelper.CreateNodeInstance()]);
        bookmarkStore
            .Setup(value => value.GetByInstanceAsync("900800700600500", It.IsAny<CancellationToken>()))
            .ReturnsAsync([WorkflowTestHelper.CreateBookmark(dueTime: WorkflowTestHelper.DueTime)]);

        var detail = await service.GetDetailAsync(900800700600500L);

        Assert.NotNull(detail);
        Assert.Equal(900800700600500L, detail!.BasicId);
        Assert.Equal("申请人撤回", detail.CancellationReason, StringComparer.Ordinal);
        Assert.Contains("\"days\": \"3\"", detail.VariablesJson, StringComparison.Ordinal);

        var nodeInstance = Assert.Single(detail.NodeInstances);
        Assert.Equal("300300300300300", nodeInstance.Id, StringComparer.Ordinal);
        Assert.Equal("部门审批", nodeInstance.Name, StringComparer.Ordinal);
        Assert.Equal(WorkflowNodeInstanceStatus.Compensated, nodeInstance.Status);
        Assert.Equal(2, nodeInstance.TryCount);
        Assert.Equal("审批人不存在", nodeInstance.FaultMessage, StringComparer.Ordinal);
        Assert.Contains("\"outcome\": \"approved\"", nodeInstance.OutputsJson!, StringComparison.Ordinal);

        var bookmark = Assert.Single(detail.PendingBookmarks);
        Assert.Equal("400400400400400", bookmark.Id, StringComparer.Ordinal);
        Assert.Equal("approve", bookmark.NodeId, StringComparer.Ordinal);
        Assert.Equal(WorkflowBookmarkKinds.UserTask, bookmark.Kind, StringComparer.Ordinal);
        Assert.Equal("1001", bookmark.Key, StringComparer.Ordinal);
        Assert.Equal(WorkflowTestHelper.DueTime, bookmark.DueTime);
        Assert.Equal(WorkflowTestHelper.CreationTime, bookmark.CreationTime);
    }

    /// <summary>
    /// 无输出的节点实例其输出 JSON 必须为 null（空字典不序列化成 "{}"，前端据此判断是否展示输出区）。
    /// </summary>
    [Fact]
    public async Task InstanceQuery_GetDetailAsync_NodeInstanceWithoutOutputs_ShouldLeaveOutputsJsonNull()
    {
        var (service, _, instanceStore, bookmarkStore, _) = CreateInstanceQueryService();
        var nodeInstance = WorkflowTestHelper.CreateNodeInstance();
        nodeInstance.Outputs.Clear();
        instanceStore
            .Setup(value => value.FindAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(WorkflowTestHelper.CreateInstance());
        instanceStore
            .Setup(value => value.GetNodeInstancesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([nodeInstance]);
        bookmarkStore
            .Setup(value => value.GetByInstanceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var detail = await service.GetDetailAsync(900800700600500L);

        Assert.Null(Assert.Single(detail!.NodeInstances).OutputsJson);
    }

    /// <summary>
    /// 实例详情的已取消令牌必须在触存储前抛出。
    /// </summary>
    [Fact]
    public async Task InstanceQuery_GetDetailAsync_CanceledToken_ShouldThrowBeforeStore()
    {
        var (service, _, instanceStore, _, _) = CreateInstanceQueryService();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.GetDetailAsync(1L, cancellation.Token));

        instanceStore.Verify(value => value.FindAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 登记定义分页查询的返回值，并捕获传给仓储的分页请求。
    /// </summary>
    /// <param name="repository">定义仓储桩。</param>
    /// <returns>捕获到的分页请求容器。</returns>
    private static StrongBox<PageRequestDtoBase> SetupDefinitionPage(Mock<IWorkflowDefinitionRepository> repository)
    {
        var captured = new StrongBox<PageRequestDtoBase>();
        repository
            .Setup(value => value.GetPagedAsync(It.IsAny<PageRequestDtoBase>(), It.IsAny<CancellationToken>()))
            .Callback<PageRequestDtoBase, CancellationToken>((request, _) => captured.Value = request)
            .ReturnsAsync(new PageResultDtoBase<SysWorkflowDefinition>([], 1, 20, 0));
        return captured;
    }

    /// <summary>
    /// 登记实例分页查询的返回值，并捕获传给仓储的分页请求。
    /// </summary>
    /// <param name="repository">实例仓储桩。</param>
    /// <returns>捕获到的分页请求容器。</returns>
    private static StrongBox<PageRequestDtoBase> SetupInstancePage(Mock<IWorkflowInstanceRepository> repository)
    {
        var captured = new StrongBox<PageRequestDtoBase>();
        repository
            .Setup(value => value.GetPagedAsync(It.IsAny<PageRequestDtoBase>(), It.IsAny<CancellationToken>()))
            .Callback<PageRequestDtoBase, CancellationToken>((request, _) => captured.Value = request)
            .ReturnsAsync(new PageResultDtoBase<SysWorkflowInstance>([], 1, 20, 0));
        return captured;
    }

    /// <summary>
    /// 构造定义查询服务及其仓储与字段级安全桩。
    /// </summary>
    /// <returns>查询服务、仓储桩、字段级安全桩。</returns>
    private static (WorkflowDefinitionQueryService Service, Mock<IWorkflowDefinitionRepository> Repository, Mock<IFieldSecurityService> FieldSecurity) CreateDefinitionQueryService()
    {
        var repository = new Mock<IWorkflowDefinitionRepository>();
        var fieldSecurity = new Mock<IFieldSecurityService>();
        return (new WorkflowDefinitionQueryService(repository.Object, fieldSecurity.Object), repository, fieldSecurity);
    }

    /// <summary>
    /// 构造实例查询服务及其仓储、两个存储与字段级安全桩。
    /// </summary>
    /// <returns>查询服务、实例仓储桩、实例存储桩、书签存储桩、字段级安全桩。</returns>
    private static (WorkflowInstanceQueryService Service, Mock<IWorkflowInstanceRepository> Repository, Mock<IWorkflowInstanceStore> InstanceStore, Mock<IWorkflowBookmarkStore> BookmarkStore, Mock<IFieldSecurityService> FieldSecurity) CreateInstanceQueryService()
    {
        var repository = new Mock<IWorkflowInstanceRepository>();
        var instanceStore = new Mock<IWorkflowInstanceStore>();
        var bookmarkStore = new Mock<IWorkflowBookmarkStore>();
        var fieldSecurity = new Mock<IFieldSecurityService>();
        var service = new WorkflowInstanceQueryService(
            repository.Object, instanceStore.Object, bookmarkStore.Object, fieldSecurity.Object);
        return (service, repository, instanceStore, bookmarkStore, fieldSecurity);
    }

    /// <summary>
    /// 单值捕获容器（把回调里拿到的对象带出闭包）。
    /// </summary>
    /// <typeparam name="T">被捕获的类型。</typeparam>
    private sealed class StrongBox<T>
        where T : class
    {
        /// <summary>
        /// 捕获到的值。
        /// </summary>
        public T? Value { get; set; }
    }
}
