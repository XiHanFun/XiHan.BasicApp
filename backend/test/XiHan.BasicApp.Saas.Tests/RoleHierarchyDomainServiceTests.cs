// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 角色继承领域服务测试：环路检测与继承链展开的安全语义。
/// </summary>
public sealed class RoleHierarchyDomainServiceTests
{
    /// <summary>
    /// 角色继承自身必然成环，且不应访问仓储。
    /// </summary>
    [Fact]
    public async Task WouldCreateCycle_SameRole_ShouldReturnTrueWithoutRepositoryCall()
    {
        var repository = new Mock<IRoleHierarchyRepository>();
        var service = new RoleHierarchyDomainService(repository.Object);

        var wouldCycle = await service.WouldCreateCycleAsync(5, 5);

        Assert.True(wouldCycle);
        repository.Verify(
            repo => repo.GetAncestorIdsAsync(
                It.IsAny<IEnumerable<long>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 子角色祖先链中包含父角色时判定成环。
    /// </summary>
    [Fact]
    public async Task WouldCreateCycle_WhenParentIsAncestor_ShouldReturnTrue()
    {
        var repository = new Mock<IRoleHierarchyRepository>();
        repository
            .Setup(repo => repo.GetAncestorIdsAsync(
                It.IsAny<IEnumerable<long>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long> { 100, 200 });
        var service = new RoleHierarchyDomainService(repository.Object);

        var wouldCycle = await service.WouldCreateCycleAsync(parentRoleId: 100, childRoleId: 3);

        Assert.True(wouldCycle);
    }

    /// <summary>
    /// 子角色祖先链中不含父角色时不成环。
    /// </summary>
    [Fact]
    public async Task WouldCreateCycle_WhenParentIsNotAncestor_ShouldReturnFalse()
    {
        var repository = new Mock<IRoleHierarchyRepository>();
        repository
            .Setup(repo => repo.GetAncestorIdsAsync(
                It.IsAny<IEnumerable<long>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long> { 100, 200 });
        var service = new RoleHierarchyDomainService(repository.Object);

        var wouldCycle = await service.WouldCreateCycleAsync(parentRoleId: 300, childRoleId: 3);

        Assert.False(wouldCycle);
    }

    /// <summary>
    /// 环路检测必须排除自身（若包含自身则任何已有祖先关系都误判成环）。
    /// </summary>
    [Fact]
    public async Task WouldCreateCycle_ShouldQueryAncestorsExcludingSelf()
    {
        var repository = new Mock<IRoleHierarchyRepository>();
        repository
            .Setup(repo => repo.GetAncestorIdsAsync(
                It.IsAny<IEnumerable<long>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long>());
        var service = new RoleHierarchyDomainService(repository.Object);

        _ = await service.WouldCreateCycleAsync(1, 2);

        repository.Verify(
            repo => repo.GetAncestorIdsAsync(It.IsAny<IEnumerable<long>>(), false, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 空角色集合展开应返回空列表且不访问仓储。
    /// </summary>
    [Fact]
    public async Task ExpandRoleHierarchy_EmptyInput_ShouldReturnEmptyWithoutRepositoryCall()
    {
        var repository = new Mock<IRoleHierarchyRepository>();
        var service = new RoleHierarchyDomainService(repository.Object);

        var result = await service.ExpandRoleHierarchyAsync([]);

        Assert.Empty(result);
        repository.Verify(
            repo => repo.GetAncestorIdsAsync(
                It.IsAny<IEnumerable<long>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 继承链展开必须包含自身（权限合并需要用户直接持有的角色）。
    /// </summary>
    [Fact]
    public async Task ExpandRoleHierarchy_ShouldIncludeSelf()
    {
        var repository = new Mock<IRoleHierarchyRepository>();
        repository
            .Setup(repo => repo.GetAncestorIdsAsync(
                It.IsAny<IEnumerable<long>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long> { 1, 100, 200 });
        var service = new RoleHierarchyDomainService(repository.Object);

        var result = await service.ExpandRoleHierarchyAsync([1]);

        Assert.Equal([1L, 100L, 200L], result);
        repository.Verify(
            repo => repo.GetAncestorIdsAsync(It.IsAny<IEnumerable<long>>(), true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 展开时应透传起始角色集合。
    /// </summary>
    [Fact]
    public async Task ExpandRoleHierarchy_ShouldPassRoleIdsToRepository()
    {
        var repository = new Mock<IRoleHierarchyRepository>();
        IEnumerable<long>? captured = null;
        repository
            .Setup(repo => repo.GetAncestorIdsAsync(
                It.IsAny<IEnumerable<long>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Callback((IEnumerable<long> ids, bool _, CancellationToken _) => captured = ids)
            .ReturnsAsync(new List<long>());
        var service = new RoleHierarchyDomainService(repository.Object);

        _ = await service.ExpandRoleHierarchyAsync([7, 8, 9]);

        Assert.Equal([7L, 8L, 9L], captured);
    }

    /// <summary>
    /// 已取消令牌必须立即抛出，不访问仓储。
    /// </summary>
    [Fact]
    public async Task WouldCreateCycle_Cancelled_ShouldThrow()
    {
        var repository = new Mock<IRoleHierarchyRepository>();
        var service = new RoleHierarchyDomainService(repository.Object);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.WouldCreateCycleAsync(1, 2, cts.Token));
    }
}
