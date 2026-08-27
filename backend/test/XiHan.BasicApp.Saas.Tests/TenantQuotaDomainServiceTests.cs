// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 租户配额领域服务测试：席位与存储上限的解析口径与拦截行为。
/// </summary>
public sealed class TenantQuotaDomainServiceTests
{
    /// <summary>
    /// 测试租户主键
    /// </summary>
    private const long TenantId = 7;

    /// <summary>
    /// 测试版本套餐主键
    /// </summary>
    private const long EditionId = 3;

    /// <summary>
    /// 每 MB 字节数
    /// </summary>
    private const long BytesPerMegabyte = 1024L * 1024L;

    /// <summary>
    /// 席位未达上限时放行。
    /// </summary>
    [Fact]
    public async Task EnsureSeat_WhenUnderLimit_ShouldPass()
    {
        var fixture = CreateFixture(tenantUserLimit: 10, usedSeats: 5);

        await fixture.Service.EnsureSeatQuotaAsync(1);
    }

    /// <summary>
    /// 席位已达上限时拒绝新增，并在提示中带出用量与上限。
    /// </summary>
    [Fact]
    public async Task EnsureSeat_WhenAtLimit_ShouldThrow()
    {
        var fixture = CreateFixture(tenantUserLimit: 10, usedSeats: 10);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.EnsureSeatQuotaAsync(1));

        Assert.Contains("席位已达上限", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 租户与套餐均未设上限时表示不限，任意新增都放行。
    /// </summary>
    [Fact]
    public async Task EnsureSeat_WithoutAnyLimit_ShouldPass()
    {
        var fixture = CreateFixture(usedSeats: 100_000);

        await fixture.Service.EnsureSeatQuotaAsync(1);
    }

    /// <summary>
    /// 租户级上限覆盖套餐级：套餐宽松也按租户的紧上限拦截。
    /// </summary>
    [Fact]
    public async Task EnsureSeat_TenantLimitShouldOverrideEdition()
    {
        var fixture = CreateFixture(tenantUserLimit: 3, editionUserLimit: 100, usedSeats: 3);

        _ = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.EnsureSeatQuotaAsync(1));
    }

    /// <summary>
    /// 租户级未设值时回落到套餐级上限。
    /// </summary>
    [Fact]
    public async Task EnsureSeat_ShouldFallBackToEditionLimit()
    {
        var fixture = CreateFixture(editionUserLimit: 2, usedSeats: 2);

        _ = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.EnsureSeatQuotaAsync(1));
    }

    /// <summary>
    /// 平台运维态无租户上下文，跳过校验且不触发任何用量统计查询。
    /// </summary>
    [Fact]
    public async Task EnsureSeat_InPlatformOperation_ShouldSkipWithoutQuery()
    {
        var fixture = CreateFixture(tenantUserLimit: 1, usedSeats: 100, currentTenantId: null);

        await fixture.Service.EnsureSeatQuotaAsync(1);

        fixture.TenantRepository.Verify(
            repo => repo.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 新增量为 0 时直接放行，不触发统计查询。
    /// </summary>
    [Fact]
    public async Task EnsureSeat_WithNonPositiveIncrement_ShouldSkipWithoutQuery()
    {
        var fixture = CreateFixture(tenantUserLimit: 1, usedSeats: 100);

        await fixture.Service.EnsureSeatQuotaAsync(0);

        fixture.TenantRepository.Verify(
            repo => repo.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 上限为空（不限）时不触发用量统计查询。
    /// </summary>
    [Fact]
    public async Task EnsureSeat_WithoutLimit_ShouldNotQueryUsage()
    {
        var fixture = CreateFixture(usedSeats: 100);

        await fixture.Service.EnsureSeatQuotaAsync(1);

        fixture.TenantUserRepository.Verify(
            repo => repo.CountActiveMembersByTenantIdsAsync(
                It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 存储校验不牵连席位统计：两个配额维度互不影响，避免高频上传路径上的无谓查询。
    /// </summary>
    [Fact]
    public async Task EnsureStorage_ShouldNotQuerySeatUsage()
    {
        var fixture = CreateFixture(editionStorageLimit: 10);

        await fixture.Service.EnsureStorageQuotaAsync(50);

        fixture.TenantUserRepository.Verify(
            repo => repo.CountActiveMembersByTenantIdsAsync(
                It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 存储上限以 MB 表达、用量以字节统计，换算后仍有余量时放行。
    /// </summary>
    [Fact]
    public async Task EnsureStorage_ShouldConvertMegabytesToBytes()
    {
        var fixture = CreateFixture(
            editionStorageLimit: 10,
            usedBytes: (10 * BytesPerMegabyte) - 100);

        await fixture.Service.EnsureStorageQuotaAsync(50);
    }

    /// <summary>
    /// 换算后超出存储上限时拒绝上传。
    /// </summary>
    [Fact]
    public async Task EnsureStorage_WhenExceeding_ShouldThrow()
    {
        var fixture = CreateFixture(
            editionStorageLimit: 10,
            usedBytes: (10 * BytesPerMegabyte) - 100);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.EnsureStorageQuotaAsync(200));

        Assert.Contains("存储空间不足", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 配额快照返回解析后的生效上限与已用量。
    /// </summary>
    [Fact]
    public async Task GetQuotaSnapshots_ShouldResolveEffectiveLimitsAndUsage()
    {
        var fixture = CreateFixture(
            tenantUserLimit: 20,
            editionUserLimit: 100,
            editionStorageLimit: 512,
            usedSeats: 8,
            usedBytes: 1024);

        var snapshots = await fixture.Service.GetQuotaSnapshotsAsync([TenantId]);

        Assert.True(snapshots.TryGetValue(TenantId, out var snapshot));
        Assert.Equal(20, snapshot!.UserLimit);
        Assert.Equal(8, snapshot.UsedUserCount);
        Assert.Equal(512, snapshot.StorageLimit);
        Assert.Equal(1024, snapshot.UsedStorageBytes);
    }

    /// <summary>
    /// 构造被测服务及其依赖替身。
    /// </summary>
    private static QuotaFixture CreateFixture(
        int? tenantUserLimit = null,
        long? tenantStorageLimit = null,
        int? editionUserLimit = null,
        long? editionStorageLimit = null,
        long usedSeats = 0,
        long usedBytes = 0,
        long? currentTenantId = TenantId)
    {
        var tenant = new TestTenant(TenantId)
        {
            TenantCode = "t1",
            TenantName = "测试租户",
            EditionId = EditionId,
            UserLimit = tenantUserLimit,
            StorageLimit = tenantStorageLimit
        };

        var edition = new TestEdition(EditionId)
        {
            EditionCode = "pro",
            EditionName = "专业版",
            UserLimit = editionUserLimit,
            StorageLimit = editionStorageLimit
        };

        var tenantRepository = new Mock<ITenantRepository>();
        _ = tenantRepository
            .Setup(repo => repo.GetByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SysTenant>)[tenant]);
        _ = tenantRepository
            .Setup(repo => repo.GetByIdAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysTenant?)tenant);

        var editionRepository = new Mock<ITenantEditionRepository>();
        _ = editionRepository
            .Setup(repo => repo.GetByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SysTenantEdition>)[edition]);
        _ = editionRepository
            .Setup(repo => repo.GetByIdAsync(EditionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysTenantEdition?)edition);

        var tenantUserRepository = new Mock<ITenantUserRepository>();
        _ = tenantUserRepository
            .Setup(repo => repo.CountActiveMembersByTenantIdsAsync(
                It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyDictionary<long, long>)new Dictionary<long, long> { [TenantId] = usedSeats });

        var fileRepository = new Mock<IFileRepository>();
        _ = fileRepository
            .Setup(repo => repo.SumUsedStorageByTenantIdsAsync(
                It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyDictionary<long, long>)new Dictionary<long, long> { [TenantId] = usedBytes });

        var currentTenant = new Mock<ICurrentTenant>();
        _ = currentTenant.SetupGet(context => context.Id).Returns(currentTenantId);

        var service = new TenantQuotaDomainService(
            tenantRepository.Object,
            editionRepository.Object,
            tenantUserRepository.Object,
            fileRepository.Object,
            currentTenant.Object);

        return new QuotaFixture(service, tenantRepository, tenantUserRepository, fileRepository);
    }

    /// <summary>
    /// 租户测试替身：BasicId 为 protected set，经派生类构造赋值，避免反射。
    /// </summary>
    private sealed class TestTenant : SysTenant
    {
        public TestTenant(long basicId)
        {
            BasicId = basicId;
        }
    }

    /// <summary>
    /// 版本套餐测试替身
    /// </summary>
    private sealed class TestEdition : SysTenantEdition
    {
        public TestEdition(long basicId)
        {
            BasicId = basicId;
        }
    }

    /// <summary>
    /// 租户配额测试依赖集合。
    /// </summary>
    private sealed record QuotaFixture(
        TenantQuotaDomainService Service,
        Mock<ITenantRepository> TenantRepository,
        Mock<ITenantUserRepository> TenantUserRepository,
        Mock<IFileRepository> FileRepository);
}
