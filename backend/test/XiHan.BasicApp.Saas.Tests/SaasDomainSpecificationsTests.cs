// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Specifications;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 领域规约测试：锁定「启用/有效 + 生效期左闭右开」这一套查询口径。
/// 生效期边界必须是生效时间小于等于当前时刻（含等号即已生效）与失效时间严格大于当前时刻（等号即已过期），
/// 任何一处写反都会让刚好到点的授权多活或早死一个时刻。
/// </summary>
public sealed class SaasDomainSpecificationsTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// 启用权限规约：仅未软删且启用的权限满足。
    /// </summary>
    /// <param name="isDeleted">是否已软删。</param>
    /// <param name="status">启用状态。</param>
    /// <param name="expected">期望是否满足规约。</param>
    [Theory]
    [InlineData(false, EnableStatus.Enabled, true)]
    [InlineData(false, EnableStatus.Disabled, false)]
    [InlineData(true, EnableStatus.Enabled, false)]
    [InlineData(true, EnableStatus.Disabled, false)]
    public void EnabledPermissionSpecification_ShouldRequireNotDeletedAndEnabled(bool isDeleted, EnableStatus status, bool expected)
    {
        var permission = new SysPermission { IsDeleted = isDeleted, Status = status };

        Assert.Equal(expected, new EnabledPermissionSpecification().IsSatisfiedBy(permission));
    }

    /// <summary>
    /// 启用角色规约：仅未软删且启用的角色满足。
    /// </summary>
    /// <param name="isDeleted">是否已软删。</param>
    /// <param name="status">启用状态。</param>
    /// <param name="expected">期望是否满足规约。</param>
    [Theory]
    [InlineData(false, EnableStatus.Enabled, true)]
    [InlineData(false, EnableStatus.Disabled, false)]
    [InlineData(true, EnableStatus.Enabled, false)]
    public void EnabledRoleSpecification_ShouldRequireNotDeletedAndEnabled(bool isDeleted, EnableStatus status, bool expected)
    {
        var role = new SysRole { IsDeleted = isDeleted, Status = status };

        Assert.Equal(expected, new EnabledRoleSpecification().IsSatisfiedBy(role));
    }

    /// <summary>
    /// 启用用户规约：仅未软删且启用的用户满足。
    /// </summary>
    /// <param name="isDeleted">是否已软删。</param>
    /// <param name="status">启用状态。</param>
    /// <param name="expected">期望是否满足规约。</param>
    [Theory]
    [InlineData(false, EnableStatus.Enabled, true)]
    [InlineData(false, EnableStatus.Disabled, false)]
    [InlineData(true, EnableStatus.Enabled, false)]
    public void ActiveUserSpecification_ShouldRequireNotDeletedAndEnabled(bool isDeleted, EnableStatus status, bool expected)
    {
        var user = new SysUser { IsDeleted = isDeleted, Status = status };

        Assert.Equal(expected, new ActiveUserSpecification().IsSatisfiedBy(user));
    }

    /// <summary>
    /// 角色权限规约：无生效期即永久有效，无效状态直接淘汰。
    /// </summary>
    [Fact]
    public void ValidRolePermissionSpecification_WithoutPeriod_ShouldDependOnStatusOnly()
    {
        var specification = new ValidRolePermissionSpecification(Now);

        Assert.True(specification.IsSatisfiedBy(new SysRolePermission { Status = ValidityStatus.Valid }));
        Assert.False(specification.IsSatisfiedBy(new SysRolePermission { Status = ValidityStatus.Invalid }));
    }

    /// <summary>
    /// 角色权限规约生效期边界：生效时间等于当前时刻即已生效，失效时间等于当前时刻即已过期。
    /// </summary>
    /// <param name="effectiveOffsetSeconds">生效时间相对当前时刻的秒偏移，null 表示不限。</param>
    /// <param name="expirationOffsetSeconds">失效时间相对当前时刻的秒偏移，null 表示不限。</param>
    /// <param name="expected">期望是否满足规约。</param>
    [Theory]
    [InlineData(0, null, true)]
    [InlineData(1, null, false)]
    [InlineData(-1, null, true)]
    [InlineData(null, 0, false)]
    [InlineData(null, 1, true)]
    [InlineData(null, -1, false)]
    [InlineData(-1, 1, true)]
    public void ValidRolePermissionSpecification_ShouldUseLeftClosedRightOpenPeriod(
        int? effectiveOffsetSeconds,
        int? expirationOffsetSeconds,
        bool expected)
    {
        var rolePermission = new SysRolePermission
        {
            Status = ValidityStatus.Valid,
            EffectiveTime = effectiveOffsetSeconds.HasValue ? Now.AddSeconds(effectiveOffsetSeconds.Value) : null,
            ExpirationTime = expirationOffsetSeconds.HasValue ? Now.AddSeconds(expirationOffsetSeconds.Value) : null
        };

        Assert.Equal(expected, new ValidRolePermissionSpecification(Now).IsSatisfiedBy(rolePermission));
    }

    /// <summary>
    /// 用户直授权限规约与角色权限规约共用同一套生效期边界口径。
    /// </summary>
    /// <param name="effectiveOffsetSeconds">生效时间相对当前时刻的秒偏移，null 表示不限。</param>
    /// <param name="expirationOffsetSeconds">失效时间相对当前时刻的秒偏移，null 表示不限。</param>
    /// <param name="expected">期望是否满足规约。</param>
    [Theory]
    [InlineData(0, null, true)]
    [InlineData(1, null, false)]
    [InlineData(null, 0, false)]
    [InlineData(null, 1, true)]
    public void ValidUserPermissionSpecification_ShouldUseLeftClosedRightOpenPeriod(
        int? effectiveOffsetSeconds,
        int? expirationOffsetSeconds,
        bool expected)
    {
        var userPermission = new SysUserPermission
        {
            Status = ValidityStatus.Valid,
            EffectiveTime = effectiveOffsetSeconds.HasValue ? Now.AddSeconds(effectiveOffsetSeconds.Value) : null,
            ExpirationTime = expirationOffsetSeconds.HasValue ? Now.AddSeconds(expirationOffsetSeconds.Value) : null
        };

        Assert.Equal(expected, new ValidUserPermissionSpecification(Now).IsSatisfiedBy(userPermission));
    }

    /// <summary>
    /// 用户直授权限规约：状态无效时无论生效期如何都不满足。
    /// </summary>
    [Fact]
    public void ValidUserPermissionSpecification_InvalidStatus_ShouldNotBeSatisfied()
    {
        var userPermission = new SysUserPermission
        {
            Status = ValidityStatus.Invalid,
            EffectiveTime = Now.AddDays(-1),
            ExpirationTime = Now.AddDays(1)
        };

        Assert.False(new ValidUserPermissionSpecification(Now).IsSatisfiedBy(userPermission));
    }

    /// <summary>
    /// 用户角色规约与权限规约共用同一套生效期边界口径。
    /// </summary>
    /// <param name="effectiveOffsetSeconds">生效时间相对当前时刻的秒偏移，null 表示不限。</param>
    /// <param name="expirationOffsetSeconds">失效时间相对当前时刻的秒偏移，null 表示不限。</param>
    /// <param name="expected">期望是否满足规约。</param>
    [Theory]
    [InlineData(0, null, true)]
    [InlineData(1, null, false)]
    [InlineData(null, 0, false)]
    [InlineData(null, 1, true)]
    public void ValidUserRoleSpecification_ShouldUseLeftClosedRightOpenPeriod(
        int? effectiveOffsetSeconds,
        int? expirationOffsetSeconds,
        bool expected)
    {
        var userRole = new SysUserRole
        {
            Status = ValidityStatus.Valid,
            EffectiveTime = effectiveOffsetSeconds.HasValue ? Now.AddSeconds(effectiveOffsetSeconds.Value) : null,
            ExpirationTime = expirationOffsetSeconds.HasValue ? Now.AddSeconds(expirationOffsetSeconds.Value) : null
        };

        Assert.Equal(expected, new ValidUserRoleSpecification(Now).IsSatisfiedBy(userRole));
    }

    /// <summary>
    /// 有效租户成员规约：邀请必须已接受，待接受、已拒绝、已撤销、已过期一律不算成员。
    /// </summary>
    /// <param name="inviteStatus">邀请状态。</param>
    /// <param name="expected">期望是否满足规约。</param>
    [Theory]
    [InlineData(TenantMemberInviteStatus.Accepted, true)]
    [InlineData(TenantMemberInviteStatus.Pending, false)]
    [InlineData(TenantMemberInviteStatus.Rejected, false)]
    [InlineData(TenantMemberInviteStatus.Revoked, false)]
    [InlineData(TenantMemberInviteStatus.Expired, false)]
    public void ActiveTenantUserSpecification_ShouldRequireAcceptedInvite(TenantMemberInviteStatus inviteStatus, bool expected)
    {
        var member = new SysTenantUser
        {
            IsDeleted = false,
            InviteStatus = inviteStatus,
            Status = ValidityStatus.Valid
        };

        Assert.Equal(expected, new ActiveTenantUserSpecification(Now).IsSatisfiedBy(member));
    }

    /// <summary>
    /// 有效租户成员规约：软删成员必须被排除，避免删除后仍能进入租户。
    /// </summary>
    [Fact]
    public void ActiveTenantUserSpecification_DeletedMember_ShouldNotBeSatisfied()
    {
        var member = new SysTenantUser
        {
            IsDeleted = true,
            InviteStatus = TenantMemberInviteStatus.Accepted,
            Status = ValidityStatus.Valid
        };

        Assert.False(new ActiveTenantUserSpecification(Now).IsSatisfiedBy(member));
    }

    /// <summary>
    /// 有效租户成员规约生效期边界：与授权规约保持同一左闭右开口径。
    /// </summary>
    /// <param name="effectiveOffsetSeconds">生效时间相对当前时刻的秒偏移，null 表示不限。</param>
    /// <param name="expirationOffsetSeconds">失效时间相对当前时刻的秒偏移，null 表示不限。</param>
    /// <param name="expected">期望是否满足规约。</param>
    [Theory]
    [InlineData(0, null, true)]
    [InlineData(1, null, false)]
    [InlineData(null, 0, false)]
    [InlineData(null, 1, true)]
    public void ActiveTenantUserSpecification_ShouldUseLeftClosedRightOpenPeriod(
        int? effectiveOffsetSeconds,
        int? expirationOffsetSeconds,
        bool expected)
    {
        var member = new SysTenantUser
        {
            InviteStatus = TenantMemberInviteStatus.Accepted,
            Status = ValidityStatus.Valid,
            EffectiveTime = effectiveOffsetSeconds.HasValue ? Now.AddSeconds(effectiveOffsetSeconds.Value) : null,
            ExpirationTime = expirationOffsetSeconds.HasValue ? Now.AddSeconds(expirationOffsetSeconds.Value) : null
        };

        Assert.Equal(expected, new ActiveTenantUserSpecification(Now).IsSatisfiedBy(member));
    }

    /// <summary>
    /// 可用租户规约：仅正常状态可用，暂停、过期、停用租户一律排除。
    /// </summary>
    /// <param name="tenantStatus">租户状态。</param>
    /// <param name="expected">期望是否满足规约。</param>
    [Theory]
    [InlineData(TenantStatus.Normal, true)]
    [InlineData(TenantStatus.Suspended, false)]
    [InlineData(TenantStatus.Expired, false)]
    [InlineData(TenantStatus.Disabled, false)]
    public void AvailableTenantSpecification_ShouldRequireNormalStatus(TenantStatus tenantStatus, bool expected)
    {
        var tenant = new SysTenant { IsDeleted = false, TenantStatus = tenantStatus };

        Assert.Equal(expected, new AvailableTenantSpecification(Now).IsSatisfiedBy(tenant));
    }

    /// <summary>
    /// 可用租户规约：到期时间等于当前时刻即视为已过期（右开区间），软删租户同样排除。
    /// </summary>
    /// <param name="isDeleted">是否已软删。</param>
    /// <param name="expirationOffsetSeconds">到期时间相对当前时刻的秒偏移，null 表示不限。</param>
    /// <param name="expected">期望是否满足规约。</param>
    [Theory]
    [InlineData(false, null, true)]
    [InlineData(false, 1, true)]
    [InlineData(false, 0, false)]
    [InlineData(false, -1, false)]
    [InlineData(true, null, false)]
    public void AvailableTenantSpecification_ShouldTreatExpirationAsRightOpen(bool isDeleted, int? expirationOffsetSeconds, bool expected)
    {
        var tenant = new SysTenant
        {
            IsDeleted = isDeleted,
            TenantStatus = TenantStatus.Normal,
            ExpirationTime = expirationOffsetSeconds.HasValue ? Now.AddSeconds(expirationOffsetSeconds.Value) : null
        };

        Assert.Equal(expected, new AvailableTenantSpecification(Now).IsSatisfiedBy(tenant));
    }

    /// <summary>
    /// 规约组合运算（与、或、非）必须在内存判定中保持等价语义，供仓储表达式复用。
    /// </summary>
    [Fact]
    public void EnabledPermissionSpecification_Combinators_ShouldComposeConsistently()
    {
        var enabled = new EnabledPermissionSpecification();
        var negated = enabled.Not();
        var disabledPermission = new SysPermission { Status = EnableStatus.Disabled };

        Assert.True(negated.IsSatisfiedBy(disabledPermission));
        Assert.True(enabled.Or(negated).IsSatisfiedBy(disabledPermission));
        Assert.False(enabled.And(negated).IsSatisfiedBy(disabledPermission));
    }
}
