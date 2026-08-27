// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Moq;
using XiHan.BasicApp.Workflow.Domain.Entities;
using XiHan.BasicApp.Workflow.Domain.Repositories;
using XiHan.BasicApp.Workflow.Infrastructure.Stores;
using XiHan.Framework.Workflow.Abstractions;
using XiHan.Framework.Workflow.Abstractions.Definitions;
using XiHan.Framework.Workflow.Abstractions.Exceptions;
using XiHan.Framework.Workflow.Abstractions.Runtime;

namespace XiHan.BasicApp.Workflow.Tests;

/// <summary>
/// 三个 SqlSugar 存储实现的委派契约测试（Moq 驱动仓储接口，不连库）。
/// </summary>
/// <remarks>
/// 存储被框架注册为单例、仓储是 Scoped，因此每个操作都必须自建作用域解析仓储；
/// 直接持有仓储会跨请求泄漏数据库连接，且租户上下文停留在第一次解析时的租户上——
/// 后续所有读写都会落到错误的租户。本文件把"每操作一作用域"和"标识按雪花口径解析后再下探"
/// 两条契约变成可断言的行为。
/// </remarks>
public sealed class WorkflowSqlSugarStoreTests
{
    /// <summary>
    /// 查得到定义时必须从 JSON 真源还原完整模型，而不是只回投影列。
    /// </summary>
    [Fact]
    public async Task DefinitionStore_FindAsync_ShouldRestoreModelFromJsonSource()
    {
        var (store, repository, scopeFactory) = CreateDefinitionStore();
        repository
            .Setup(value => value.GetByIdAsync(100200300400500L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(WorkflowStoreMapper.ToEntity(WorkflowTestHelper.CreateDefinition()));

        var definition = await store.FindAsync("100200300400500");

        Assert.NotNull(definition);
        Assert.Equal("leave", definition!.Code, StringComparer.Ordinal);
        Assert.Equal(2, definition.Nodes.Count);
        Assert.Single(definition.Transitions);
        Assert.Equal(1, scopeFactory.ScopeCount);
    }

    /// <summary>
    /// 定义不存在时必须返回 null，不得把 null 实体喂给映射器炸空引用。
    /// </summary>
    [Fact]
    public async Task DefinitionStore_FindAsync_MissingEntity_ShouldReturnNull()
    {
        var (store, repository, _) = CreateDefinitionStore();
        repository
            .Setup(value => value.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysWorkflowDefinition?)null);

        Assert.Null(await store.FindAsync("1"));
    }

    /// <summary>
    /// 脏标识必须在进仓储之前就被拒绝，避免脏值被静默写成 0 号主键去查库。
    /// </summary>
    [Fact]
    public async Task DefinitionStore_FindAsync_DirtyId_ShouldThrowBeforeTouchingRepository()
    {
        var (store, repository, _) = CreateDefinitionStore();

        await Assert.ThrowsAsync<WorkflowException>(() => store.FindAsync("not-a-snowflake"));

        repository.Verify(value => value.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 按编码与版本查找必须原样把编码与版本下探到仓储，不得做大小写或裁剪处理。
    /// </summary>
    [Fact]
    public async Task DefinitionStore_FindByVersionAsync_ShouldForwardCodeAndVersion()
    {
        var (store, repository, _) = CreateDefinitionStore();
        repository
            .Setup(value => value.GetByCodeAndVersionAsync("leave", 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(WorkflowStoreMapper.ToEntity(WorkflowTestHelper.CreateDefinition()));

        var definition = await store.FindByVersionAsync("leave", 3);

        Assert.NotNull(definition);
        repository.Verify(value => value.GetByCodeAndVersionAsync("leave", 3, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 取最新已发布版本必须走仓储的专用查询（过滤已发布 + 版本降序取一条），不得在内存里挑。
    /// </summary>
    [Fact]
    public async Task DefinitionStore_FindLatestPublishedAsync_ShouldDelegateToRepository()
    {
        var (store, repository, _) = CreateDefinitionStore();
        repository
            .Setup(value => value.GetLatestPublishedAsync("leave", It.IsAny<CancellationToken>()))
            .ReturnsAsync(WorkflowStoreMapper.ToEntity(WorkflowTestHelper.CreateDefinition()));

        var definition = await store.FindLatestPublishedAsync("leave");

        Assert.Equal(WorkflowDefinitionStatus.Published, definition!.Status);
        repository.Verify(value => value.GetLatestPublishedAsync("leave", It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(value => value.GetDefinitionListAsync(It.IsAny<string?>(), It.IsAny<WorkflowDefinitionStatus?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 最大版本号必须原样回传，编码不存在时仓储返回 0，新建版本据此从 1 起跑。
    /// </summary>
    /// <param name="maxVersion">仓储返回的最大版本号。</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(37)]
    public async Task DefinitionStore_GetMaxVersionAsync_ShouldReturnRepositoryValue(int maxVersion)
    {
        var (store, repository, _) = CreateDefinitionStore();
        repository
            .Setup(value => value.GetMaxVersionAsync("leave", It.IsAny<CancellationToken>()))
            .ReturnsAsync(maxVersion);

        Assert.Equal(maxVersion, await store.GetMaxVersionAsync("leave"));
    }

    /// <summary>
    /// 列表查询必须把编码与状态两个过滤条件原样下探，并逐条从 JSON 真源还原。
    /// </summary>
    [Fact]
    public async Task DefinitionStore_GetListAsync_ShouldForwardFiltersAndMapEveryRow()
    {
        var (store, repository, _) = CreateDefinitionStore();
        repository
            .Setup(value => value.GetDefinitionListAsync("leave", WorkflowDefinitionStatus.Draft, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                WorkflowStoreMapper.ToEntity(WorkflowTestHelper.CreateDefinition(status: WorkflowDefinitionStatus.Draft)),
                WorkflowStoreMapper.ToEntity(WorkflowTestHelper.CreateDefinition("100200300400501", WorkflowDefinitionStatus.Draft))
            ]);

        var definitions = await store.GetListAsync("leave", WorkflowDefinitionStatus.Draft);

        Assert.Equal(2, definitions.Count);
        Assert.All(definitions, definition => Assert.Equal(WorkflowDefinitionStatus.Draft, definition.Status));
        Assert.Equal("100200300400501", definitions[1].Id, StringComparer.Ordinal);
    }

    /// <summary>
    /// 空过滤的列表查询必须把两个 null 原样传下去（仓储侧才是"不过滤"的语义所在）。
    /// </summary>
    [Fact]
    public async Task DefinitionStore_GetListAsync_WithoutFilters_ShouldForwardNulls()
    {
        var (store, repository, _) = CreateDefinitionStore();
        repository
            .Setup(value => value.GetDefinitionListAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        Assert.Empty(await store.GetListAsync());
        repository.Verify(value => value.GetDefinitionListAsync(null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 插入定义必须落成带全部投影列的实体，主键取引擎标识。
    /// </summary>
    [Fact]
    public async Task DefinitionStore_InsertAsync_ShouldPersistProjectedEntity()
    {
        var (store, repository, _) = CreateDefinitionStore();
        SysWorkflowDefinition? inserted = null;
        repository
            .Setup(value => value.AddAsync(It.IsAny<SysWorkflowDefinition>(), It.IsAny<CancellationToken>()))
            .Callback<SysWorkflowDefinition, CancellationToken>((entity, _) => inserted = entity)
            .ReturnsAsync((SysWorkflowDefinition entity, CancellationToken _) => entity);

        await store.InsertAsync(WorkflowTestHelper.CreateDefinition());

        Assert.NotNull(inserted);
        Assert.Equal(100200300400500L, inserted!.BasicId);
        Assert.Equal("leave", inserted.Code, StringComparer.Ordinal);
        Assert.Equal(7L, inserted.TenantId);
    }

    /// <summary>
    /// 更新必须先加载现有行再覆盖业务列，主键与审计列原样保全（乐观锁与创建痕迹不被顶掉）。
    /// </summary>
    [Fact]
    public async Task DefinitionStore_UpdateAsync_ShouldOverwriteBusinessColumnsOnLoadedEntity()
    {
        var (store, repository, _) = CreateDefinitionStore();
        var existing = new SysWorkflowDefinition();
        WorkflowTestHelper.SetBasicId(existing, 100200300400500L);
        existing.CreatedTime = DateTimeOffset.UnixEpoch;
        existing.CreatedId = 42L;
        existing.Code = "old";
        repository
            .Setup(value => value.GetByIdAsync(100200300400500L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        SysWorkflowDefinition? updated = null;
        repository
            .Setup(value => value.UpdateAsync(It.IsAny<SysWorkflowDefinition>(), It.IsAny<CancellationToken>()))
            .Callback<SysWorkflowDefinition, CancellationToken>((entity, _) => updated = entity)
            .ReturnsAsync((SysWorkflowDefinition entity, CancellationToken _) => entity);

        await store.UpdateAsync(WorkflowTestHelper.CreateDefinition());

        Assert.Same(existing, updated);
        Assert.Equal("leave", updated!.Code, StringComparer.Ordinal);
        Assert.Equal(DateTimeOffset.UnixEpoch, updated.CreatedTime);
        Assert.Equal(42L, updated.CreatedId);
    }

    /// <summary>
    /// 目标行不存在时更新必须静默跳过：不得凭模型新建一行，否则审计列全空的"幽灵定义"会被造出来。
    /// </summary>
    [Fact]
    public async Task DefinitionStore_UpdateAsync_MissingEntity_ShouldNotWrite()
    {
        var (store, repository, _) = CreateDefinitionStore();
        repository
            .Setup(value => value.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysWorkflowDefinition?)null);

        await store.UpdateAsync(WorkflowTestHelper.CreateDefinition());

        repository.Verify(value => value.UpdateAsync(It.IsAny<SysWorkflowDefinition>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(value => value.AddAsync(It.IsAny<SysWorkflowDefinition>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 删除定义必须按解析后的主键下探（定义表软删，由仓储基类落 IsDeleted）。
    /// </summary>
    [Fact]
    public async Task DefinitionStore_DeleteAsync_ShouldForwardParsedKey()
    {
        var (store, repository, _) = CreateDefinitionStore();

        await store.DeleteAsync("100200300400500");

        repository.Verify(value => value.DeleteByIdAsync(100200300400500L, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 每个存储操作都必须自建一个作用域解析仓储：连续 4 次操作应产生 4 个作用域。
    /// </summary>
    [Fact]
    public async Task DefinitionStore_EveryOperation_ShouldCreateItsOwnScope()
    {
        var (store, repository, scopeFactory) = CreateDefinitionStore();
        repository
            .Setup(value => value.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysWorkflowDefinition?)null);
        repository
            .Setup(value => value.GetDefinitionListAsync(It.IsAny<string?>(), It.IsAny<WorkflowDefinitionStatus?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _ = await store.FindAsync("1");
        _ = await store.GetListAsync();
        _ = await store.GetMaxVersionAsync("leave");
        await store.DeleteAsync("2");

        Assert.Equal(4, scopeFactory.ScopeCount);
    }

    /// <summary>
    /// 取消令牌必须透传到仓储，取消请求才能真正中断数据库往返。
    /// </summary>
    [Fact]
    public async Task DefinitionStore_FindAsync_ShouldForwardCancellationToken()
    {
        var (store, repository, _) = CreateDefinitionStore();
        using var cancellation = new CancellationTokenSource();
        var forwarded = CancellationToken.None;
        repository
            .Setup(value => value.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Callback<long, CancellationToken>((_, token) => forwarded = token)
            .ReturnsAsync((SysWorkflowDefinition?)null);

        _ = await store.FindAsync("1", cancellation.Token);

        Assert.Equal(cancellation.Token, forwarded);
    }

    /// <summary>
    /// 实例查找必须从 JSON 真源还原变量与汇聚状态，投影列不足以支撑引擎继续推进。
    /// </summary>
    [Fact]
    public async Task InstanceStore_FindAsync_ShouldRestoreRuntimeState()
    {
        var (store, instanceRepository, _, scopeFactory) = CreateInstanceStore();
        instanceRepository
            .Setup(value => value.GetByIdAsync(900800700600500L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(WorkflowStoreMapper.ToEntity(WorkflowTestHelper.CreateInstance()));

        var instance = await store.FindAsync("900800700600500");

        Assert.NotNull(instance);
        Assert.Equal(2, instance!.Variables.Count);
        Assert.Single(instance.JoinStates);
        Assert.Equal("111222333444555", instance.ParentInstanceId, StringComparer.Ordinal);
        Assert.Equal(1, scopeFactory.ScopeCount);
    }

    /// <summary>
    /// 实例不存在时必须返回 null。
    /// </summary>
    [Fact]
    public async Task InstanceStore_FindAsync_MissingEntity_ShouldReturnNull()
    {
        var (store, instanceRepository, _, _) = CreateInstanceStore();
        instanceRepository
            .Setup(value => value.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysWorkflowInstance?)null);

        Assert.Null(await store.FindAsync("1"));
    }

    /// <summary>
    /// 实例列表查询必须把状态/编码/相关性/条数四个条件全部下探到仓储。
    /// </summary>
    [Fact]
    public async Task InstanceStore_GetListAsync_ShouldForwardAllFilters()
    {
        var (store, instanceRepository, _, _) = CreateInstanceStore();
        instanceRepository
            .Setup(value => value.GetInstanceListAsync(
                WorkflowInstanceStatus.Running, "leave", "ORDER-2024-0001", 42, It.IsAny<CancellationToken>()))
            .ReturnsAsync([WorkflowStoreMapper.ToEntity(WorkflowTestHelper.CreateInstance(status: WorkflowInstanceStatus.Running))]);

        var instances = await store.GetListAsync(WorkflowInstanceStatus.Running, "leave", "ORDER-2024-0001", 42);

        _ = Assert.Single(instances);
        instanceRepository.Verify(
            value => value.GetInstanceListAsync(
                WorkflowInstanceStatus.Running, "leave", "ORDER-2024-0001", 42, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 子实例查询必须把父实例标识解析成主键，字符串条件不会命中 long 列。
    /// </summary>
    [Fact]
    public async Task InstanceStore_GetChildrenAsync_ShouldForwardParsedParentKey()
    {
        var (store, instanceRepository, _, _) = CreateInstanceStore();
        instanceRepository
            .Setup(value => value.GetChildrenAsync(111222333444555L, It.IsAny<CancellationToken>()))
            .ReturnsAsync([WorkflowStoreMapper.ToEntity(WorkflowTestHelper.CreateInstance())]);

        _ = Assert.Single(await store.GetChildrenAsync("111222333444555"));
    }

    /// <summary>
    /// 删除实例必须先级联删除节点实例、再删实例本身：反过来会留下永远查不到父行的孤儿历史。
    /// </summary>
    [Fact]
    public async Task InstanceStore_DeleteAsync_ShouldDeleteNodeInstancesBeforeInstance()
    {
        var (store, instanceRepository, nodeInstanceRepository, _) = CreateInstanceStore();
        var order = new List<string>();
        nodeInstanceRepository
            .Setup(value => value.DeleteByInstanceIdAsync(900800700600500L, It.IsAny<CancellationToken>()))
            .Callback(() => order.Add("node"))
            .Returns(Task.CompletedTask);
        instanceRepository
            .Setup(value => value.DeleteByIdAsync(900800700600500L, It.IsAny<CancellationToken>()))
            .Callback(() => order.Add("instance"))
            .ReturnsAsync(true);

        await store.DeleteAsync("900800700600500");

        Assert.Equal(new[] { "node", "instance" }, order);
    }

    /// <summary>
    /// 删除实例只用一个作用域：两个仓储必须在同一作用域内解析，才共享同一条连接与租户上下文。
    /// </summary>
    [Fact]
    public async Task InstanceStore_DeleteAsync_ShouldResolveBothRepositoriesInOneScope()
    {
        var (store, _, _, scopeFactory) = CreateInstanceStore();

        await store.DeleteAsync("900800700600500");

        Assert.Equal(1, scopeFactory.ScopeCount);
    }

    /// <summary>
    /// 节点实例历史必须保持仓储返回的顺序：补偿按执行逆序回滚，顺序被打乱即补偿错节点。
    /// </summary>
    [Fact]
    public async Task InstanceStore_GetNodeInstancesAsync_ShouldPreserveRepositoryOrder()
    {
        var (store, _, nodeInstanceRepository, _) = CreateInstanceStore();
        nodeInstanceRepository
            .Setup(value => value.GetByInstanceIdAsync(900800700600500L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                WorkflowStoreMapper.ToEntity(WorkflowTestHelper.CreateNodeInstance("300300300300301")),
                WorkflowStoreMapper.ToEntity(WorkflowTestHelper.CreateNodeInstance("300300300300302")),
                WorkflowStoreMapper.ToEntity(WorkflowTestHelper.CreateNodeInstance("300300300300303"))
            ]);

        var nodeInstances = await store.GetNodeInstancesAsync("900800700600500");

        Assert.Equal(
            new[] { "300300300300301", "300300300300302", "300300300300303" },
            nodeInstances.Select(nodeInstance => nodeInstance.Id).ToArray());
    }

    /// <summary>
    /// 单条节点实例查找必须还原输入/输出/活动私有状态，续跑活动依赖私有游标。
    /// </summary>
    [Fact]
    public async Task InstanceStore_FindNodeInstanceAsync_ShouldRestorePrivateState()
    {
        var (store, _, nodeInstanceRepository, _) = CreateInstanceStore();
        nodeInstanceRepository
            .Setup(value => value.GetByIdAsync(300300300300300L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(WorkflowStoreMapper.ToEntity(WorkflowTestHelper.CreateNodeInstance()));

        var nodeInstance = await store.FindNodeInstanceAsync("300300300300300");

        Assert.NotNull(nodeInstance);
        Assert.Single(nodeInstance!.State);
        Assert.Single(nodeInstance.Inputs);
        Assert.Single(nodeInstance.Outputs);
    }

    /// <summary>
    /// 实例与节点实例的写入都直接按模型重建实体（运行时行无审计列需要保全）。
    /// </summary>
    [Fact]
    public async Task InstanceStore_Writes_ShouldPersistMappedEntities()
    {
        var (store, instanceRepository, nodeInstanceRepository, _) = CreateInstanceStore();
        SysWorkflowInstance? insertedInstance = null;
        SysWorkflowNodeInstance? updatedNodeInstance = null;
        instanceRepository
            .Setup(value => value.AddAsync(It.IsAny<SysWorkflowInstance>(), It.IsAny<CancellationToken>()))
            .Callback<SysWorkflowInstance, CancellationToken>((entity, _) => insertedInstance = entity)
            .ReturnsAsync((SysWorkflowInstance entity, CancellationToken _) => entity);
        nodeInstanceRepository
            .Setup(value => value.UpdateAsync(It.IsAny<SysWorkflowNodeInstance>(), It.IsAny<CancellationToken>()))
            .Callback<SysWorkflowNodeInstance, CancellationToken>((entity, _) => updatedNodeInstance = entity)
            .ReturnsAsync((SysWorkflowNodeInstance entity, CancellationToken _) => entity);

        await store.InsertAsync(WorkflowTestHelper.CreateInstance());
        await store.UpdateNodeInstanceAsync(WorkflowTestHelper.CreateNodeInstance());

        Assert.Equal(900800700600500L, insertedInstance!.BasicId);
        Assert.Equal(100200300400500L, insertedInstance.DefinitionId);
        Assert.Equal(300300300300300L, updatedNodeInstance!.BasicId);
        Assert.Equal(900800700600500L, updatedNodeInstance.InstanceId);
    }

    /// <summary>
    /// 书签查找必须还原节点标识与载荷（两者都没有投影列，只活在 JSON 真源里）。
    /// </summary>
    [Fact]
    public async Task BookmarkStore_FindAsync_ShouldRestoreNodeIdAndPayload()
    {
        var (store, repository, scopeFactory) = CreateBookmarkStore();
        repository
            .Setup(value => value.GetByIdAsync(400400400400400L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(WorkflowStoreMapper.ToEntity(WorkflowTestHelper.CreateBookmark()));

        var bookmark = await store.FindAsync("400400400400400");

        Assert.NotNull(bookmark);
        Assert.Equal("approve", bookmark!.NodeId, StringComparer.Ordinal);
        Assert.Equal(2, bookmark.Payload.Count);
        Assert.Equal(1, scopeFactory.ScopeCount);
    }

    /// <summary>
    /// 书签不存在时必须返回 null。
    /// </summary>
    [Fact]
    public async Task BookmarkStore_FindAsync_MissingEntity_ShouldReturnNull()
    {
        var (store, repository, _) = CreateBookmarkStore();
        repository
            .Setup(value => value.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysWorkflowBookmark?)null);

        Assert.Null(await store.FindAsync("1"));
    }

    /// <summary>
    /// 按实例与按节点实例检索书签都必须把标识解析成主键后下探。
    /// </summary>
    [Fact]
    public async Task BookmarkStore_ScopedQueries_ShouldForwardParsedKeys()
    {
        var (store, repository, _) = CreateBookmarkStore();
        repository
            .Setup(value => value.GetByInstanceIdAsync(900800700600500L, It.IsAny<CancellationToken>()))
            .ReturnsAsync([WorkflowStoreMapper.ToEntity(WorkflowTestHelper.CreateBookmark())]);
        repository
            .Setup(value => value.GetByNodeInstanceIdAsync(300300300300300L, It.IsAny<CancellationToken>()))
            .ReturnsAsync([WorkflowStoreMapper.ToEntity(WorkflowTestHelper.CreateBookmark())]);

        _ = Assert.Single(await store.GetByInstanceAsync("900800700600500"));
        _ = Assert.Single(await store.GetByNodeInstanceAsync("300300300300300"));
    }

    /// <summary>
    /// 到期书签查询必须把当前时间与条数上限原样下探，定时器 Worker 的批量领取依赖这两个参数。
    /// </summary>
    [Fact]
    public async Task BookmarkStore_GetDueAsync_ShouldForwardNowAndMaxResultCount()
    {
        var (store, repository, _) = CreateBookmarkStore();
        repository
            .Setup(value => value.GetDueAsync(WorkflowTestHelper.DueTime, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                WorkflowStoreMapper.ToEntity(
                    WorkflowTestHelper.CreateBookmark(kind: WorkflowBookmarkKinds.Timer, key: null, dueTime: WorkflowTestHelper.DueTime))
            ]);

        var due = Assert.Single(await store.GetDueAsync(WorkflowTestHelper.DueTime, 50));

        Assert.Equal(WorkflowBookmarkKinds.Timer, due.Kind, StringComparer.Ordinal);
        Assert.Equal(WorkflowTestHelper.DueTime, due.DueTime);
    }

    /// <summary>
    /// 种类 + 索引键检索必须原样下探（待办按受理人标识、信号按信号名共用这条路径）。
    /// </summary>
    [Fact]
    public async Task BookmarkStore_GetByKindAndKeyAsync_ShouldForwardKindAndKey()
    {
        var (store, repository, _) = CreateBookmarkStore();
        repository
            .Setup(value => value.GetByKindAndKeyAsync(WorkflowBookmarkKinds.UserTask, "1001", It.IsAny<CancellationToken>()))
            .ReturnsAsync([WorkflowStoreMapper.ToEntity(WorkflowTestHelper.CreateBookmark())]);

        _ = Assert.Single(await store.GetByKindAndKeyAsync(WorkflowBookmarkKinds.UserTask, "1001"));
        repository.Verify(
            value => value.GetByKindAndKeyAsync(WorkflowBookmarkKinds.UserTask, "1001", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 信号检索必须把相关性标识原样下探，包含"广播（相关性为 null）"这条分支。
    /// </summary>
    /// <param name="correlationId">业务相关性标识（null 表示广播）。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("ORDER-2024-0001")]
    public async Task BookmarkStore_GetBySignalAsync_ShouldForwardCorrelationId(string? correlationId)
    {
        var (store, repository, _) = CreateBookmarkStore();
        repository
            .Setup(value => value.GetBySignalAsync("paid", correlationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                WorkflowStoreMapper.ToEntity(WorkflowTestHelper.CreateBookmark(kind: WorkflowBookmarkKinds.Signal, key: "paid"))
            ]);

        _ = Assert.Single(await store.GetBySignalAsync("paid", correlationId));
        repository.Verify(value => value.GetBySignalAsync("paid", correlationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 书签的插入/更新都按模型重建实体，删除按解析后的主键与实例主键下探。
    /// </summary>
    [Fact]
    public async Task BookmarkStore_Writes_ShouldMapEntityAndForwardKeys()
    {
        var (store, repository, _) = CreateBookmarkStore();
        SysWorkflowBookmark? inserted = null;
        repository
            .Setup(value => value.AddAsync(It.IsAny<SysWorkflowBookmark>(), It.IsAny<CancellationToken>()))
            .Callback<SysWorkflowBookmark, CancellationToken>((entity, _) => inserted = entity)
            .ReturnsAsync((SysWorkflowBookmark entity, CancellationToken _) => entity);

        await store.InsertAsync(WorkflowTestHelper.CreateBookmark());
        await store.DeleteAsync("400400400400400");
        await store.DeleteByInstanceAsync("900800700600500");

        Assert.Equal(400400400400400L, inserted!.BasicId);
        Assert.Equal(WorkflowBookmarkKinds.UserTask, inserted.Kind, StringComparer.Ordinal);
        repository.Verify(value => value.DeleteByIdAsync(400400400400400L, It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(value => value.DeleteByInstanceIdAsync(900800700600500L, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 书签存储的每个操作同样必须自建作用域：连续 3 次操作应产生 3 个作用域。
    /// </summary>
    [Fact]
    public async Task BookmarkStore_EveryOperation_ShouldCreateItsOwnScope()
    {
        var (store, repository, scopeFactory) = CreateBookmarkStore();
        repository
            .Setup(value => value.GetByInstanceIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _ = await store.GetByInstanceAsync("1");
        await store.DeleteAsync("2");
        await store.DeleteByInstanceAsync("3");

        Assert.Equal(3, scopeFactory.ScopeCount);
    }

    /// <summary>
    /// 构造定义存储及其 Moq 仓储与计数作用域工厂。
    /// </summary>
    /// <returns>存储、仓储桩、作用域工厂。</returns>
    private static (SqlSugarWorkflowDefinitionStore Store, Mock<IWorkflowDefinitionRepository> Repository, CountingScopeFactory ScopeFactory) CreateDefinitionStore()
    {
        var repository = new Mock<IWorkflowDefinitionRepository>();
        var scopeFactory = WorkflowTestHelper.CreateScopeFactory(
            services => services.AddScoped(_ => repository.Object));
        return (new SqlSugarWorkflowDefinitionStore(scopeFactory), repository, scopeFactory);
    }

    /// <summary>
    /// 构造实例存储及其两个 Moq 仓储与计数作用域工厂。
    /// </summary>
    /// <returns>存储、实例仓储桩、节点实例仓储桩、作用域工厂。</returns>
    private static (SqlSugarWorkflowInstanceStore Store, Mock<IWorkflowInstanceRepository> InstanceRepository, Mock<IWorkflowNodeInstanceRepository> NodeInstanceRepository, CountingScopeFactory ScopeFactory) CreateInstanceStore()
    {
        var instanceRepository = new Mock<IWorkflowInstanceRepository>();
        var nodeInstanceRepository = new Mock<IWorkflowNodeInstanceRepository>();
        var scopeFactory = WorkflowTestHelper.CreateScopeFactory(services =>
        {
            _ = services.AddScoped(_ => instanceRepository.Object);
            _ = services.AddScoped(_ => nodeInstanceRepository.Object);
        });
        return (new SqlSugarWorkflowInstanceStore(scopeFactory), instanceRepository, nodeInstanceRepository, scopeFactory);
    }

    /// <summary>
    /// 构造书签存储及其 Moq 仓储与计数作用域工厂。
    /// </summary>
    /// <returns>存储、仓储桩、作用域工厂。</returns>
    private static (SqlSugarWorkflowBookmarkStore Store, Mock<IWorkflowBookmarkRepository> Repository, CountingScopeFactory ScopeFactory) CreateBookmarkStore()
    {
        var repository = new Mock<IWorkflowBookmarkRepository>();
        var scopeFactory = WorkflowTestHelper.CreateScopeFactory(
            services => services.AddScoped(_ => repository.Object));
        return (new SqlSugarWorkflowBookmarkStore(scopeFactory), repository, scopeFactory);
    }
}
