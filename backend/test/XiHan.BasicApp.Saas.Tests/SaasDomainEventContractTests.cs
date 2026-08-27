// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using Moq;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.BasicApp.Saas.Domain.ValueObjects;
using XiHan.Framework.Domain.Events;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 领域事件与值对象的契约测试。
/// 领域事件被缓存失效、审计落库等多个处理器消费，一旦某个事件属性可写就可能被处理器就地改写，
/// 因此「只读属性 + 密封类 + 统一基类」是必须锁死的结构约束；
/// 值对象则要保证工厂方法语义与相等性行为不漂移。
/// </summary>
public sealed class SaasDomainEventContractTests
{
    /// <summary>
    /// 领域事件命名空间下的所有具体事件类都必须密封，防止被继承后携带额外可变状态。
    /// </summary>
    [Fact]
    public void DomainEvents_ShouldAllBeSealed()
    {
        var violations = GetConcreteDomainEventTypes()
            .Where(type => !type.IsSealed)
            .Select(type => type.FullName ?? type.Name)
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"以下领域事件未密封：{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    /// <summary>
    /// 领域事件必须继承框架事件基类，才能拿到事件标识与发生时间并被事件总线识别。
    /// </summary>
    [Fact]
    public void DomainEvents_ShouldDeriveFromDomainEventBase()
    {
        var violations = GetConcreteDomainEventTypes()
            .Where(type => !typeof(DomainEventBase).IsAssignableFrom(type))
            .Select(type => type.FullName ?? type.Name)
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"以下领域事件未继承 DomainEventBase：{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    /// <summary>
    /// 领域事件的公开属性一律只读：事件是已发生事实的快照，不允许消费端改写后再传给下一个处理器。
    /// </summary>
    [Fact]
    public void DomainEvents_PublicProperties_ShouldBeReadOnly()
    {
        var violations = GetConcreteDomainEventTypes()
            .SelectMany(type => type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.SetMethod is not null)
                .Select(property => $"{type.Name}.{property.Name}"))
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"以下领域事件属性可写，事件应当是不可变快照：{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    /// <summary>
    /// 领域事件不得暴露无参公开构造函数，避免构造出缺少业务上下文的空事件。
    /// </summary>
    [Fact]
    public void DomainEvents_ShouldNotExposeParameterlessConstructor()
    {
        var violations = GetConcreteDomainEventTypes()
            .Where(type => type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, Type.EmptyTypes) is not null)
            .Select(type => type.Name)
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"以下领域事件存在无参公开构造函数：{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    /// <summary>
    /// 每个事件实例都要拿到互不相同的事件标识，事件标识是审计与幂等消费的唯一锚点。
    /// </summary>
    [Fact]
    public void DomainEvent_EachInstance_ShouldCarryDistinctEventId()
    {
        var first = new TenantStatusChangedDomainEvent(1, 2, TenantStatus.Normal, TenantStatus.Suspended);
        var second = new TenantStatusChangedDomainEvent(1, 2, TenantStatus.Normal, TenantStatus.Suspended);

        Assert.NotEqual(Guid.Empty, first.EventId);
        Assert.NotEqual(first.EventId, second.EventId);
        Assert.NotEqual(default, first.OccurredOn);
    }

    /// <summary>
    /// SaaS 事件基类统一携带租户、操作人与变更原因，缺省时操作人与原因为 null。
    /// </summary>
    [Fact]
    public void SaasDomainEventBase_ShouldCarryTenantOperatorAndReason()
    {
        var withOperator = new TenantStatusChangedDomainEvent(
            tenantId: 7,
            affectedTenantId: 9,
            oldStatus: TenantStatus.Normal,
            newStatus: TenantStatus.Disabled,
            operatorUserId: 3,
            reason: "欠费停用");

        Assert.Equal(7, withOperator.TenantId);
        Assert.Equal(9, withOperator.AffectedTenantId);
        Assert.Equal(TenantStatus.Normal, withOperator.OldStatus);
        Assert.Equal(TenantStatus.Disabled, withOperator.NewStatus);
        Assert.Equal(3, withOperator.OperatorUserId);
        Assert.Equal("欠费停用", withOperator.Reason, StringComparer.Ordinal);

        var minimal = new TenantStatusChangedDomainEvent(7, 9, TenantStatus.Normal, TenantStatus.Disabled);
        Assert.Null(minimal.OperatorUserId);
        Assert.Null(minimal.Reason);
    }

    /// <summary>
    /// 授权变更事件直接携带确定的变更类型，且用户级与角色级目标可分别为空——
    /// 分配/移除角色时权限主键为空，权限级变更时角色主键为空。
    /// </summary>
    [Fact]
    public void AuthorizationChangedDomainEvent_ShouldKeepExplicitChangeTypeAndNullableTargets()
    {
        var roleAssignment = new AuthorizationChangedDomainEvent(
            tenantId: 7,
            changeType: PermissionChangeType.UserAssignRole,
            targetUserId: 3,
            targetRoleId: 5,
            permissionId: null);

        Assert.Equal(PermissionChangeType.UserAssignRole, roleAssignment.ChangeType);
        Assert.Equal(3, roleAssignment.TargetUserId);
        Assert.Equal(5, roleAssignment.TargetRoleId);
        Assert.Null(roleAssignment.PermissionId);

        var permissionRevoke = new AuthorizationChangedDomainEvent(
            tenantId: 7,
            changeType: PermissionChangeType.RoleRevokePermission,
            targetUserId: null,
            targetRoleId: 5,
            permissionId: 11,
            operatorUserId: 1,
            reason: "撤销");

        Assert.Equal(PermissionChangeType.RoleRevokePermission, permissionRevoke.ChangeType);
        Assert.Null(permissionRevoke.TargetUserId);
        Assert.Equal(11, permissionRevoke.PermissionId);
    }

    /// <summary>
    /// 会话撤销事件区分「撤销单个会话」与「撤销该用户全部会话」，默认只撤销单个。
    /// </summary>
    [Fact]
    public void UserSessionRevokedDomainEvent_ShouldDefaultToSingleSessionRevocation()
    {
        var single = new UserSessionRevokedDomainEvent(7, 3, 100, "session-abc", "jti-abc");
        var all = new UserSessionRevokedDomainEvent(7, 3, null, null, null, revokeAllUserSessions: true);

        Assert.False(single.RevokeAllUserSessions);
        Assert.Equal("session-abc", single.UserSessionId, StringComparer.Ordinal);
        Assert.True(all.RevokeAllUserSessions);
        Assert.Null(all.SessionId);
    }

    /// <summary>
    /// 文件上传事件按原样携带文件与存储快照，供缓存与统计消费。
    /// </summary>
    [Fact]
    public void FileUploadedDomainEvent_ShouldCarryFileSnapshot()
    {
        var uploaded = new FileUploadedDomainEvent(7, 100, 200, "a.png", 1024, 3);

        Assert.Equal(7, uploaded.TenantId);
        Assert.Equal(100, uploaded.FileId);
        Assert.Equal(200, uploaded.StorageId);
        Assert.Equal("a.png", uploaded.FileName, StringComparer.Ordinal);
        Assert.Equal(1024, uploaded.FileSize);
        Assert.Equal(3, uploaded.OperatorUserId);
        Assert.Null(uploaded.Reason);
    }

    /// <summary>
    /// 租户成员变更事件携带用户与邀请状态，用于成员缓存失效与通知扇出。
    /// </summary>
    [Fact]
    public void TenantMembershipChangedDomainEvent_ShouldCarryUserAndInviteStatus()
    {
        var changed = new TenantMembershipChangedDomainEvent(7, 3, TenantMemberInviteStatus.Accepted);

        Assert.Equal(7, changed.TenantId);
        Assert.Equal(3, changed.UserId);
        Assert.Equal(TenantMemberInviteStatus.Accepted, changed.InviteStatus);
    }

    /// <summary>
    /// 平台运维态判定：无租户上下文或租户为 0 号平台租户即平台态，业务租户（大于 0）一律不是。
    /// </summary>
    /// <param name="currentTenantId">当前租户上下文标识。</param>
    /// <param name="expected">期望是否处于平台运维态。</param>
    [Theory]
    [InlineData(null, true)]
    [InlineData(0L, true)]
    [InlineData(1L, false)]
    [InlineData(999L, false)]
    public void IsPlatformOperation_ShouldTreatNullAndZeroAsPlatform(long? currentTenantId, bool expected)
    {
        var currentTenant = new Mock<ICurrentTenant>();
        _ = currentTenant.SetupGet(tenant => tenant.Id).Returns(currentTenantId);

        Assert.Equal(expected, currentTenant.Object.IsPlatformOperation());
    }

    /// <summary>
    /// 当前租户为空是调用方缺陷，必须抛空引用异常而不是默认当成平台态放行。
    /// </summary>
    [Fact]
    public void IsPlatformOperation_NullCurrentTenant_ShouldThrowArgumentNullException()
    {
        _ = Assert.Throws<ArgumentNullException>(() => CurrentTenantPlatformExtensions.IsPlatformOperation(null!));
    }

    /// <summary>
    /// 空业务引用是两个字段都为空的单例，且与等值构造的实例按值相等。
    /// </summary>
    [Fact]
    public void BusinessReference_Empty_ShouldBeNullPairAndValueEqual()
    {
        Assert.Null(BusinessReference.Empty.BusinessType);
        Assert.Null(BusinessReference.Empty.BusinessId);
        Assert.Equal(BusinessReference.Empty, new BusinessReference(null, null));
        Assert.NotEqual(BusinessReference.Empty, new BusinessReference("order", 1));
    }

    /// <summary>
    /// 授权裁决工厂：Grant 与 Deny 只在是否放行上不同，权限码、原因与权限主键原样透传。
    /// </summary>
    [Fact]
    public void AuthorizationDecision_Factories_ShouldOnlyDifferInGrantFlag()
    {
        var grant = AuthorizationDecision.Grant("saas:user:read", "角色授权命中", 11);
        var deny = AuthorizationDecision.Deny("saas:user:read", "未命中任何授权");

        Assert.True(grant.IsGranted);
        Assert.Equal("saas:user:read", grant.PermissionCode, StringComparer.Ordinal);
        Assert.Equal("角色授权命中", grant.Reason, StringComparer.Ordinal);
        Assert.Equal(11, grant.PermissionId);

        Assert.False(deny.IsGranted);
        Assert.Null(deny.PermissionId);
    }

    /// <summary>
    /// 数据范围裁决工厂：全部数据、仅本人、受限部门三种形态互斥，不得同时置位。
    /// </summary>
    [Fact]
    public void DataScopeDecision_Factories_ShouldProduceMutuallyExclusiveShapes()
    {
        var all = DataScopeDecision.All();
        var selfOnly = DataScopeDecision.SelfOnly();
        var restricted = DataScopeDecision.Restricted([1L, 2L], [1L, 2L, 3L]);

        Assert.True(all.AllowsAllData);
        Assert.False(all.AllowsSelfData);
        Assert.Empty(all.DepartmentIds);

        Assert.False(selfOnly.AllowsAllData);
        Assert.True(selfOnly.AllowsSelfData);
        Assert.Empty(selfOnly.DepartmentAndChildrenIds);

        Assert.False(restricted.AllowsAllData);
        Assert.False(restricted.AllowsSelfData);
        Assert.Equal([1L, 2L], restricted.DepartmentIds);
        Assert.Equal([1L, 2L, 3L], restricted.DepartmentAndChildrenIds);
    }

    /// <summary>
    /// 权限授予快照默认启用，生效周期与授权来源原样保留，供合并与裁决使用。
    /// </summary>
    [Fact]
    public void PermissionGrantSnapshot_ShouldDefaultToEnabled()
    {
        var snapshot = new PermissionGrantSnapshot(
            11,
            "saas:user:read",
            PermissionAction.Grant,
            AuthorizationGrantSource.Role,
            Priority: 10,
            Period: EffectivePeriod.Always);

        Assert.True(snapshot.IsEnabled);
        Assert.Equal(AuthorizationGrantSource.Role, snapshot.Source);
        Assert.Same(EffectivePeriod.Always, snapshot.Period);
    }

    /// <summary>
    /// 数据范围授权快照默认启用，部门集合与是否含子部门按入参保留。
    /// </summary>
    [Fact]
    public void DataScopeGrantSnapshot_ShouldDefaultToEnabled()
    {
        var snapshot = new DataScopeGrantSnapshot(
            5,
            AuthorizationGrantSource.User,
            DataPermissionScope.Custom,
            [1L],
            IncludeChildren: true,
            Period: EffectivePeriod.Always);

        Assert.True(snapshot.IsEnabled);
        Assert.True(snapshot.IncludeChildren);
        Assert.Equal([1L], snapshot.DepartmentIds);
    }

    /// <summary>
    /// 租户成员快照按值相等：同一份成员事实在不同调用点构造应当可直接比较。
    /// </summary>
    [Fact]
    public void TenantMemberSnapshot_ShouldUseValueEquality()
    {
        var left = new TenantMemberSnapshot(
            1, 2, TenantMemberType.Member, TenantMemberInviteStatus.Accepted, ValidityStatus.Valid, EffectivePeriod.Always);
        var right = new TenantMemberSnapshot(
            1, 2, TenantMemberType.Member, TenantMemberInviteStatus.Accepted, ValidityStatus.Valid, EffectivePeriod.Always);
        var other = left with { MemberType = TenantMemberType.Admin };

        Assert.Equal(left, right);
        Assert.NotEqual(left, other);
    }

    /// <summary>
    /// 客户端与设备信息是纯快照记录，按值相等且允许字段整体缺省。
    /// </summary>
    [Fact]
    public void ClientAndDeviceInfo_ShouldUseValueEquality()
    {
        Assert.Equal(
            new ClientInfo("127.0.0.1", null, "ua", null, null),
            new ClientInfo("127.0.0.1", null, "ua", null, null));
        Assert.Equal(
            new DeviceInfo(null, null, null, null, null),
            new DeviceInfo(null, null, null, null, null));
        Assert.NotEqual(
            new DeviceInfo("pc", null, null, null, null),
            new DeviceInfo("mobile", null, null, null, null));
    }

    private static IReadOnlyList<Type> GetConcreteDomainEventTypes()
    {
        return
        [
            .. typeof(SaasDomainEventBase).Assembly
                .GetTypes()
                .Where(type => type.IsClass
                    && !type.IsAbstract
                    && string.Equals(type.Namespace, typeof(SaasDomainEventBase).Namespace, StringComparison.Ordinal))
        ];
    }
}
