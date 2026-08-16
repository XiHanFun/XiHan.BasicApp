// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.ValueObjects;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 权限裁决领域服务测试：权限码匹配、授权来源优先级与有效期过滤的安全语义。
/// </summary>
public sealed class PermissionDecisionDomainServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 28, 12, 0, 0, TimeSpan.Zero);

    private readonly PermissionDecisionDomainService _service = new();

    /// <summary>
    /// 空/空白权限码一律拒绝，不得空引用或误判。
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Decide_WithNullOrWhiteSpaceCode_ShouldDeny(string? permissionCode)
    {
        var decision = _service.Decide(permissionCode!, [Grant(1, "saas:user:read")], Now);

        Assert.False(decision.IsGranted);
        Assert.Contains("权限编码为空", decision.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// 无任何匹配授权时应拒绝并给出明确原因。
    /// </summary>
    [Fact]
    public void Decide_WithoutMatchingGrants_ShouldDeny()
    {
        var decision = _service.Decide("saas:user:read", [Grant(1, "saas:user:create")], Now);

        Assert.False(decision.IsGranted);
        Assert.Contains("未匹配有效授权", decision.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// 权限码匹配大小写不敏感（授权码存储与查询码大小写不同不应影响裁决）。
    /// </summary>
    [Fact]
    public void Decide_ShouldMatchPermissionCodeCaseInsensitively()
    {
        var decision = _service.Decide("SAAS:USER:READ", [Grant(1, "saas:user:read")], Now);

        Assert.True(decision.IsGranted);
    }

    /// <summary>
    /// 停用授权必须被过滤，不得参与裁决。
    /// </summary>
    [Fact]
    public void Decide_ShouldIgnoreDisabledGrant()
    {
        var decision = _service.Decide(
            "saas:user:read",
            [Grant(1, "saas:user:read", enabled: false)],
            Now);

        Assert.False(decision.IsGranted);
        Assert.Contains("未匹配有效授权", decision.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// 已过期授权必须被过滤。
    /// </summary>
    [Fact]
    public void Decide_ShouldIgnoreExpiredGrant()
    {
        var expired = new EffectivePeriod(Now.AddHours(-2), Now.AddHours(-1));

        var decision = _service.Decide("saas:user:read", [Grant(1, "saas:user:read", period: expired)], Now);

        Assert.False(decision.IsGranted);
    }

    /// <summary>
    /// 未到生效时间的授权必须被过滤。
    /// </summary>
    [Fact]
    public void Decide_ShouldIgnoreNotYetEffectiveGrant()
    {
        var future = new EffectivePeriod(Now.AddHours(1), null);

        var decision = _service.Decide("saas:user:read", [Grant(1, "saas:user:read", period: future)], Now);

        Assert.False(decision.IsGranted);
    }

    /// <summary>
    /// 用户直授拒绝优先于角色授予与委派授予。
    /// </summary>
    [Fact]
    public void Decide_UserDeny_ShouldOverrideRoleGrantAndDelegationGrant()
    {
        var grants = new[]
        {
            Grant(1, "saas:user:read", PermissionAction.Deny, AuthorizationGrantSource.User),
            Grant(2, "saas:user:read", PermissionAction.Grant, AuthorizationGrantSource.Role),
            Grant(3, "saas:user:read", PermissionAction.Grant, AuthorizationGrantSource.Delegation)
        };

        var decision = _service.Decide("saas:user:read", grants, Now);

        Assert.False(decision.IsGranted);
        Assert.Equal(1, decision.PermissionId);
        Assert.Contains("用户直授拒绝优先", decision.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// 用户直授授予覆盖角色拒绝（直授是最强授予来源）。
    /// </summary>
    [Fact]
    public void Decide_UserGrant_ShouldOverrideRoleDeny()
    {
        var grants = new[]
        {
            Grant(1, "saas:user:read", PermissionAction.Deny, AuthorizationGrantSource.Role),
            Grant(2, "saas:user:read", PermissionAction.Grant, AuthorizationGrantSource.User)
        };

        var decision = _service.Decide("saas:user:read", grants, Now);

        Assert.True(decision.IsGranted);
        Assert.Equal(2, decision.PermissionId);
    }

    /// <summary>
    /// 仅有角色授予时应授权通过。
    /// </summary>
    [Fact]
    public void Decide_RoleGrant_ShouldGrant()
    {
        var decision = _service.Decide(
            "saas:user:read",
            [Grant(1, "saas:user:read", PermissionAction.Grant, AuthorizationGrantSource.Role)],
            Now);

        Assert.True(decision.IsGranted);
        Assert.Contains("角色授权通过", decision.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// 仅有角色拒绝且无用户级授权时应拒绝。
    /// </summary>
    [Fact]
    public void Decide_RoleDeny_ShouldDeny()
    {
        var decision = _service.Decide(
            "saas:user:read",
            [Grant(1, "saas:user:read", PermissionAction.Deny, AuthorizationGrantSource.Role)],
            Now);

        Assert.False(decision.IsGranted);
        Assert.Contains("角色授权拒绝", decision.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// 无用户/角色级授权时，委派授予生效。
    /// </summary>
    [Fact]
    public void Decide_DelegationGrant_ShouldGrantWhenNoUserOrRoleGrant()
    {
        var decision = _service.Decide(
            "saas:user:read",
            [Grant(1, "saas:user:read", PermissionAction.Grant, AuthorizationGrantSource.Delegation)],
            Now);

        Assert.True(decision.IsGranted);
        Assert.Contains("委派授权通过", decision.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// 同一来源同一操作的多条授权按优先级取最高者。
    /// </summary>
    [Fact]
    public void Decide_ShouldPreferHigherPriorityGrantWithinSameSource()
    {
        var grants = new[]
        {
            Grant(1, "saas:user:read", PermissionAction.Grant, AuthorizationGrantSource.Role, priority: 5),
            Grant(2, "saas:user:read", PermissionAction.Grant, AuthorizationGrantSource.Role, priority: 10)
        };

        var decision = _service.Decide("saas:user:read", grants, Now);

        Assert.True(decision.IsGranted);
        Assert.Equal(2, decision.PermissionId);
    }

    /// <summary>
    /// 其他权限码的授权不应干扰当前权限码的裁决。
    /// </summary>
    [Fact]
    public void Decide_ShouldNotBeAffectedByOtherPermissionCodes()
    {
        var grants = new[]
        {
            Grant(1, "saas:user:create", PermissionAction.Deny, AuthorizationGrantSource.User),
            Grant(2, "saas:user:read", PermissionAction.Grant, AuthorizationGrantSource.Role)
        };

        var decision = _service.Decide("saas:user:read", grants, Now);

        Assert.True(decision.IsGranted);
    }

    /// <summary>
    /// 构造授权快照（默认永久有效且已启用）。
    /// </summary>
    private static PermissionGrantSnapshot Grant(
        long permissionId,
        string permissionCode,
        PermissionAction action = PermissionAction.Grant,
        AuthorizationGrantSource source = AuthorizationGrantSource.Role,
        int priority = 0,
        EffectivePeriod? period = null,
        bool enabled = true)
    {
        return new PermissionGrantSnapshot(
            permissionId,
            permissionCode,
            action,
            source,
            priority,
            period ?? EffectivePeriod.Always,
            enabled);
    }
}
