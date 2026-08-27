// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Saas.Domain.ValueObjects;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 会话有效性、并发挤下线、租户准入与部门层级环路的领域规则测试。
/// 这几条规则决定「谁还能继续用这个登录态」和「部门树能不能被搬成环」，全部是拒绝路径优先。
/// </summary>
public sealed class SaasDomainSessionAndAccessTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// 会话有效性判定：仅活跃状态可用，离线、已撤销、已过期一律失效。
    /// </summary>
    /// <param name="status">会话状态。</param>
    /// <param name="expected">期望是否有效。</param>
    [Theory]
    [InlineData(SessionStatus.Active, true)]
    [InlineData(SessionStatus.Offline, false)]
    [InlineData(SessionStatus.Revoked, false)]
    [InlineData(SessionStatus.Expired, false)]
    public void IsSessionValid_ShouldOnlyAcceptActiveStatus(SessionStatus status, bool expected)
    {
        var service = new UserSessionDomainService();
        var session = new SysUserSession { Status = status };

        Assert.Equal(expected, service.IsSessionValid(session, Now));
    }

    /// <summary>
    /// 会话绝对过期判定为右开：过期时间等于当前时刻即已失效，未设置过期时间则永不因过期失效。
    /// </summary>
    /// <param name="expirationOffsetSeconds">过期时间相对当前时刻的秒偏移，null 表示未设置。</param>
    /// <param name="expected">期望是否有效。</param>
    [Theory]
    [InlineData(null, true)]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void IsSessionValid_ExpirationBoundary_ShouldTreatEqualAsExpired(int? expirationOffsetSeconds, bool expected)
    {
        var service = new UserSessionDomainService();
        var session = new SysUserSession
        {
            Status = SessionStatus.Active,
            ExpirationTime = expirationOffsetSeconds.HasValue ? Now.AddSeconds(expirationOffsetSeconds.Value) : null
        };

        Assert.Equal(expected, service.IsSessionValid(session, Now));
    }

    /// <summary>
    /// 会话实体为空是调用方缺陷，必须抛空引用异常而不是静默判定为无效。
    /// </summary>
    [Fact]
    public void IsSessionValid_NullSession_ShouldThrowArgumentNullException()
    {
        var service = new UserSessionDomainService();

        _ = Assert.Throws<ArgumentNullException>(() => service.IsSessionValid(null!, Now));
    }

    /// <summary>
    /// 并发上限小于等于 0 表示不限制并发，不得挤掉任何会话。
    /// </summary>
    /// <param name="maxConcurrent">最大并发会话数。</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GetSessionsToRevoke_NonPositiveLimit_ShouldRevokeNothing(int maxConcurrent)
    {
        var service = new UserSessionDomainService();
        var sessions = BuildSessions(5);

        Assert.Empty(service.GetSessionsToRevoke(sessions, maxConcurrent));
    }

    /// <summary>
    /// 会话数未超过上限（含刚好等于上限）时不挤下线，避免恰好达标就被踢。
    /// </summary>
    /// <param name="sessionCount">当前活跃会话数。</param>
    /// <param name="maxConcurrent">最大并发会话数。</param>
    [Theory]
    [InlineData(1, 3)]
    [InlineData(3, 3)]
    public void GetSessionsToRevoke_WithinLimit_ShouldRevokeNothing(int sessionCount, int maxConcurrent)
    {
        var service = new UserSessionDomainService();

        Assert.Empty(service.GetSessionsToRevoke(BuildSessions(sessionCount), maxConcurrent));
    }

    /// <summary>
    /// 超限时保留最后活跃时间最新的若干会话，挤掉最旧的那些（先进先出踢人）。
    /// </summary>
    [Fact]
    public void GetSessionsToRevoke_OverLimit_ShouldKeepMostRecentlyActive()
    {
        var service = new UserSessionDomainService();
        var sessions = BuildSessions(5);

        var toRevoke = service.GetSessionsToRevoke(sessions, maxConcurrent: 2);

        Assert.Equal(3, toRevoke.Count);
        // BuildSessions 让序号越大越新，保留 4、3，踢掉 2、1、0
        Assert.Equal(["session-2", "session-1", "session-0"], toRevoke.Select(item => item.UserSessionId));
    }

    /// <summary>
    /// 会话列表为空是调用方缺陷，必须抛空引用异常。
    /// </summary>
    [Fact]
    public void GetSessionsToRevoke_NullSessions_ShouldThrowArgumentNullException()
    {
        var service = new UserSessionDomainService();

        _ = Assert.Throws<ArgumentNullException>(() => service.GetSessionsToRevoke(null!, 1));
    }

    /// <summary>
    /// 租户准入：邀请已接受、状态有效、生效期覆盖当前时刻三者缺一不可。
    /// </summary>
    /// <param name="inviteStatus">邀请状态。</param>
    /// <param name="status">有效性状态。</param>
    /// <param name="expected">期望是否可进入租户。</param>
    [Theory]
    [InlineData(TenantMemberInviteStatus.Accepted, ValidityStatus.Valid, true)]
    [InlineData(TenantMemberInviteStatus.Pending, ValidityStatus.Valid, false)]
    [InlineData(TenantMemberInviteStatus.Revoked, ValidityStatus.Valid, false)]
    [InlineData(TenantMemberInviteStatus.Accepted, ValidityStatus.Invalid, false)]
    public void CanAccess_ShouldRequireAcceptedAndValidMember(
        TenantMemberInviteStatus inviteStatus,
        ValidityStatus status,
        bool expected)
    {
        var service = new TenantAccessDomainService();
        var member = BuildMember(TenantMemberType.Member, inviteStatus, status, EffectivePeriod.Always);

        Assert.Equal(expected, service.CanAccess(member, Now));
    }

    /// <summary>
    /// 租户准入沿用生效周期的左闭右开语义：过期时刻当天即失去准入。
    /// </summary>
    [Fact]
    public void CanAccess_ExpiredPeriod_ShouldDenyAccess()
    {
        var service = new TenantAccessDomainService();
        var member = BuildMember(
            TenantMemberType.Member,
            TenantMemberInviteStatus.Accepted,
            ValidityStatus.Valid,
            new EffectivePeriod(Now.AddDays(-10), Now));

        Assert.False(service.CanAccess(member, Now));
    }

    /// <summary>
    /// 平台管理员判定必须先过准入：即便成员类型是平台管理员，失效成员也不算平台管理员。
    /// </summary>
    [Fact]
    public void IsPlatformAdmin_InvalidMembership_ShouldReturnFalse()
    {
        var service = new TenantAccessDomainService();
        var invalidPlatformAdmin = BuildMember(
            TenantMemberType.PlatformAdmin,
            TenantMemberInviteStatus.Accepted,
            ValidityStatus.Invalid,
            EffectivePeriod.Always);

        Assert.False(service.IsPlatformAdmin(invalidPlatformAdmin, Now));
    }

    /// <summary>
    /// 平台管理员判定：只有成员类型为平台管理员且准入通过才成立。
    /// </summary>
    /// <param name="memberType">成员类型。</param>
    /// <param name="expected">期望是否平台管理员。</param>
    [Theory]
    [InlineData(TenantMemberType.PlatformAdmin, true)]
    [InlineData(TenantMemberType.Owner, false)]
    [InlineData(TenantMemberType.Admin, false)]
    [InlineData(TenantMemberType.Consultant, false)]
    public void IsPlatformAdmin_ShouldMatchOnlyPlatformAdminType(TenantMemberType memberType, bool expected)
    {
        var service = new TenantAccessDomainService();
        var member = BuildMember(memberType, TenantMemberInviteStatus.Accepted, ValidityStatus.Valid, EffectivePeriod.Always);

        Assert.Equal(expected, service.IsPlatformAdmin(member, Now));
    }

    /// <summary>
    /// 部门移动到根节点（无父级）永不成环，且不应触发层级查询。
    /// </summary>
    [Fact]
    public async Task DepartmentWouldCreateCycle_MoveToRoot_ShouldReturnFalseWithoutRepositoryCall()
    {
        var repository = new Mock<IDepartmentHierarchyRepository>();
        var service = new DepartmentHierarchyDomainService(repository.Object);

        var wouldCycle = await service.WouldCreateCycleAsync(10, null);

        Assert.False(wouldCycle);
        repository.Verify(
            repo => repo.GetDescendantIdsAsync(It.IsAny<long>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 部门以自身为父级必然成环，短路判定不查库。
    /// </summary>
    [Fact]
    public async Task DepartmentWouldCreateCycle_SelfAsParent_ShouldReturnTrueWithoutRepositoryCall()
    {
        var repository = new Mock<IDepartmentHierarchyRepository>();
        var service = new DepartmentHierarchyDomainService(repository.Object);

        var wouldCycle = await service.WouldCreateCycleAsync(10, 10);

        Assert.True(wouldCycle);
        repository.Verify(
            repo => repo.GetDescendantIdsAsync(It.IsAny<long>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 新父级落在自身后代中即成环，环路检测须查询「不含自身」的后代集合。
    /// </summary>
    [Fact]
    public async Task DepartmentWouldCreateCycle_NewParentIsDescendant_ShouldReturnTrue()
    {
        var repository = new Mock<IDepartmentHierarchyRepository>();
        _ = repository
            .Setup(repo => repo.GetDescendantIdsAsync(10, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long> { 11, 12 });
        var service = new DepartmentHierarchyDomainService(repository.Object);

        var wouldCycle = await service.WouldCreateCycleAsync(10, 12);

        Assert.True(wouldCycle);
        repository.Verify(repo => repo.GetDescendantIdsAsync(10, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 新父级不在自身后代中时允许移动。
    /// </summary>
    [Fact]
    public async Task DepartmentWouldCreateCycle_NewParentIsUnrelated_ShouldReturnFalse()
    {
        var repository = new Mock<IDepartmentHierarchyRepository>();
        _ = repository
            .Setup(repo => repo.GetDescendantIdsAsync(10, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long> { 11, 12 });
        var service = new DepartmentHierarchyDomainService(repository.Object);

        Assert.False(await service.WouldCreateCycleAsync(10, 99));
    }

    /// <summary>
    /// 对外的后代查询语义与环路检测相反：必须包含自身，供数据范围下发部门集合使用。
    /// </summary>
    [Fact]
    public async Task GetDescendantIds_ShouldQueryIncludingSelf()
    {
        var repository = new Mock<IDepartmentHierarchyRepository>();
        _ = repository
            .Setup(repo => repo.GetDescendantIdsAsync(10, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long> { 10, 11 });
        var service = new DepartmentHierarchyDomainService(repository.Object);

        var descendants = await service.GetDescendantIdsAsync(10);

        Assert.Equal([10L, 11L], descendants);
        repository.Verify(repo => repo.GetDescendantIdsAsync(10, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 已取消的令牌必须在访问仓储前就抛出取消异常。
    /// </summary>
    [Fact]
    public async Task DepartmentHierarchy_CancelledToken_ShouldThrowBeforeRepositoryCall()
    {
        var repository = new Mock<IDepartmentHierarchyRepository>();
        var service = new DepartmentHierarchyDomainService(repository.Object);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.WouldCreateCycleAsync(10, 11, cancellation.Token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.GetDescendantIdsAsync(10, cancellation.Token));
        repository.Verify(
            repo => repo.GetDescendantIdsAsync(It.IsAny<long>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static TenantMemberSnapshot BuildMember(
        TenantMemberType memberType,
        TenantMemberInviteStatus inviteStatus,
        ValidityStatus status,
        EffectivePeriod period)
    {
        return new TenantMemberSnapshot(1, 2, memberType, inviteStatus, status, period);
    }

    private static IReadOnlyList<SysUserSession> BuildSessions(int count)
    {
        return [.. Enumerable.Range(0, count).Select(index => new SysUserSession
        {
            UserSessionId = $"session-{index}",
            Status = SessionStatus.Active,
            LastActivityTime = Now.AddMinutes(index)
        })];
    }
}
