// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using XiHan.BasicApp.Saas.Infrastructure.Repositories;
using XiHan.BasicApp.Workflow.Domain.Repositories;
using XiHan.BasicApp.Workflow.Infrastructure.Repositories;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Workflow.Abstractions;

namespace XiHan.BasicApp.Workflow.Tests;

/// <summary>
/// 四个工作流仓储的入参校验、取消透传与注册形态测试（守卫在建查询之前生效，因此无需连库）。
/// </summary>
/// <remarks>
/// 这些守卫都写在 <c>CreateQueryable()</c> 之前：空编码若被放行，
/// <c>Where(Code == "")</c> 会退化成一次全表扫描后返回空集，调用方看到的是"流程不存在"而非"参数错误"；
/// 取消检查若被删掉，取消的请求仍会把一次数据库往返跑完。
/// </remarks>
public sealed class WorkflowRepositoryGuardTests
{
    /// <summary>
    /// 定义仓储的三个按编码查询都必须拒绝空白编码。
    /// </summary>
    /// <param name="code">空白编码。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DefinitionRepository_CodeQueries_ShouldRejectBlankCode(string? code)
    {
        var repository = new WorkflowDefinitionRepository(Mock.Of<ISqlSugarClientResolver>());

        _ = await Assert.ThrowsAnyAsync<ArgumentException>(() => repository.GetByCodeAndVersionAsync(code!, 1));
        _ = await Assert.ThrowsAnyAsync<ArgumentException>(() => repository.GetLatestPublishedAsync(code!));
        _ = await Assert.ThrowsAnyAsync<ArgumentException>(() => repository.GetMaxVersionAsync(code!));
    }

    /// <summary>
    /// 定义仓储的每个查询在令牌已取消时都必须直接抛出，不得先跑一次数据库往返。
    /// </summary>
    [Fact]
    public async Task DefinitionRepository_CanceledToken_ShouldThrowBeforeQuerying()
    {
        var repository = new WorkflowDefinitionRepository(Mock.Of<ISqlSugarClientResolver>());
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetByCodeAndVersionAsync("leave", 1, cancellation.Token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetLatestPublishedAsync("leave", cancellation.Token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetMaxVersionAsync("leave", cancellation.Token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetDefinitionListAsync(null, null, cancellation.Token));
    }

    /// <summary>
    /// 书签仓储的种类 + 索引键查询必须同时拒绝空白种类与空白索引键。
    /// </summary>
    /// <param name="kind">书签种类。</param>
    /// <param name="key">索引键。</param>
    [Theory]
    [InlineData(null, "1001")]
    [InlineData("   ", "1001")]
    [InlineData("UserTask", null)]
    [InlineData("UserTask", "")]
    public async Task BookmarkRepository_GetByKindAndKeyAsync_ShouldRejectBlankArguments(string? kind, string? key)
    {
        var repository = new WorkflowBookmarkRepository(Mock.Of<ISqlSugarClientResolver>());

        _ = await Assert.ThrowsAnyAsync<ArgumentException>(() => repository.GetByKindAndKeyAsync(kind!, key!));
    }

    /// <summary>
    /// 信号查询必须拒绝空白信号名：空信号名会命中全部等待信号的书签并把它们一起唤醒。
    /// </summary>
    /// <param name="signalName">空白信号名。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task BookmarkRepository_GetBySignalAsync_ShouldRejectBlankSignalName(string? signalName)
    {
        var repository = new WorkflowBookmarkRepository(Mock.Of<ISqlSugarClientResolver>());

        _ = await Assert.ThrowsAnyAsync<ArgumentException>(() => repository.GetBySignalAsync(signalName!, null));
    }

    /// <summary>
    /// 书签仓储的每个查询与删除在令牌已取消时都必须直接抛出。
    /// </summary>
    [Fact]
    public async Task BookmarkRepository_CanceledToken_ShouldThrowBeforeQuerying()
    {
        var repository = new WorkflowBookmarkRepository(Mock.Of<ISqlSugarClientResolver>());
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetByInstanceIdAsync(1L, cancellation.Token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetByNodeInstanceIdAsync(1L, cancellation.Token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetDueAsync(WorkflowTestHelper.DueTime, 10, cancellation.Token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetByKindAndKeyAsync(WorkflowBookmarkKinds.UserTask, "1001", cancellation.Token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetBySignalAsync("paid", null, cancellation.Token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.DeleteByInstanceIdAsync(1L, cancellation.Token));
    }

    /// <summary>
    /// 实例仓储的两个查询在令牌已取消时都必须直接抛出。
    /// </summary>
    [Fact]
    public async Task InstanceRepository_CanceledToken_ShouldThrowBeforeQuerying()
    {
        var repository = new WorkflowInstanceRepository(Mock.Of<ISqlSugarClientResolver>());
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetChildrenAsync(1L, cancellation.Token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetInstanceListAsync(null, null, null, 100, cancellation.Token));
    }

    /// <summary>
    /// 节点实例仓储的查询与级联删除在令牌已取消时都必须直接抛出。
    /// </summary>
    [Fact]
    public async Task NodeInstanceRepository_CanceledToken_ShouldThrowBeforeQuerying()
    {
        var repository = new WorkflowNodeInstanceRepository(Mock.Of<ISqlSugarClientResolver>());
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetByInstanceIdAsync(1L, cancellation.Token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.DeleteByInstanceIdAsync(1L, cancellation.Token));
    }

    /// <summary>
    /// 四个仓储都必须继承 <c>SaasRepository</c>：租户行过滤与 Scoped 约定注册都由该基类链提供，
    /// 换成裸仓储基类会同时丢掉租户隔离与自动注册。
    /// </summary>
    /// <param name="repositoryType">仓储实现类型。</param>
    /// <param name="contractType">仓储契约类型。</param>
    [Theory]
    [InlineData(typeof(WorkflowDefinitionRepository), typeof(IWorkflowDefinitionRepository))]
    [InlineData(typeof(WorkflowInstanceRepository), typeof(IWorkflowInstanceRepository))]
    [InlineData(typeof(WorkflowNodeInstanceRepository), typeof(IWorkflowNodeInstanceRepository))]
    [InlineData(typeof(WorkflowBookmarkRepository), typeof(IWorkflowBookmarkRepository))]
    public void Repositories_ShouldDeriveFromSaasRepositoryAndImplementContract(Type repositoryType, Type contractType)
    {
        Assert.True(
            repositoryType.IsAssignableTo(contractType),
            $"{repositoryType.Name} 未实现 {contractType.Name}，约定扫描注册后存储层解析不到实现。");
        Assert.True(
            repositoryType.IsAssignableTo(typeof(IScopedDependency)),
            $"{repositoryType.Name} 未落在 Scoped 约定注册链上，存储层的 GetRequiredService 会直接抛异常。");
        Assert.True(repositoryType.IsSealed, $"{repositoryType.Name} 不是 sealed。");

        var baseType = repositoryType.BaseType;
        Assert.True(
            baseType is { IsGenericType: true } && baseType.GetGenericTypeDefinition() == typeof(SaasRepository<>),
            $"{repositoryType.Name} 的基类是 {baseType?.Name ?? "无"}，不是 SaasRepository<>，租户行过滤会失效。");
    }
}
