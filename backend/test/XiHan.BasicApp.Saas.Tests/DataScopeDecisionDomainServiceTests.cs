// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.ValueObjects;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 数据范围裁决领域服务测试：多角色数据范围合并的语义（全部 &gt; 部门并集 &gt; 本人）。
/// </summary>
public sealed class DataScopeDecisionDomainServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 28, 12, 0, 0, TimeSpan.Zero);

    private readonly DataScopeDecisionDomainService _service = new();

    /// <summary>
    /// 无任何数据范围授权时只能看本人数据。
    /// </summary>
    [Fact]
    public void Decide_WithoutGrants_ShouldBeSelfOnly()
    {
        var decision = _service.Decide([], [1, 2], Now);

        Assert.True(decision.AllowsSelfData);
        Assert.False(decision.AllowsAllData);
        Assert.Empty(decision.DepartmentIds);
        Assert.Empty(decision.DepartmentAndChildrenIds);
    }

    /// <summary>
    /// 停用或过期授权必须被过滤，回落为本人数据。
    /// </summary>
    [Fact]
    public void Decide_ShouldIgnoreDisabledAndExpiredGrants()
    {
        var grants = new[]
        {
            Snapshot(DataPermissionScope.All, enabled: false),
            Snapshot(DataPermissionScope.All, period: new EffectivePeriod(Now.AddHours(-2), Now.AddHours(-1)))
        };

        var decision = _service.Decide(grants, [1], Now);

        Assert.True(decision.AllowsSelfData);
    }

    /// <summary>
    /// 任一有效授权为全部数据范围时放行全部数据。
    /// </summary>
    [Fact]
    public void Decide_AllScope_ShouldAllowAllData()
    {
        var decision = _service.Decide([Snapshot(DataPermissionScope.All)], [1], Now);

        Assert.True(decision.AllowsAllData);
        Assert.False(decision.AllowsSelfData);
    }

    /// <summary>
    /// 全部数据范围与其它范围混合时仍应放行全部数据。
    /// </summary>
    [Fact]
    public void Decide_AllScopeMixedWithOthers_ShouldAllowAllData()
    {
        var grants = new[]
        {
            Snapshot(DataPermissionScope.DepartmentOnly),
            Snapshot(DataPermissionScope.All)
        };

        var decision = _service.Decide(grants, [1], Now);

        Assert.True(decision.AllowsAllData);
    }

    /// <summary>
    /// 本部门范围以用户当前有效部门为基准。
    /// </summary>
    [Fact]
    public void Decide_DepartmentOnly_ShouldUseUserDepartments()
    {
        var decision = _service.Decide([Snapshot(DataPermissionScope.DepartmentOnly)], [1, 2, 3], Now);

        Assert.Equal([1L, 2L, 3L], decision.DepartmentIds);
        Assert.Empty(decision.DepartmentAndChildrenIds);
        Assert.False(decision.AllowsSelfData);
    }

    /// <summary>
    /// 本部门及下级范围归入含下级集合。
    /// </summary>
    [Fact]
    public void Decide_DepartmentAndChildren_ShouldUseUserDepartments()
    {
        var decision = _service.Decide([Snapshot(DataPermissionScope.DepartmentAndChildren)], [4, 5], Now);

        Assert.Empty(decision.DepartmentIds);
        Assert.Equal([4L, 5L], decision.DepartmentAndChildrenIds);
    }

    /// <summary>
    /// 自定义范围（不含下级）使用授权指定的部门列表。
    /// </summary>
    [Fact]
    public void Decide_CustomWithoutChildren_ShouldUseGrantDepartmentIds()
    {
        var grant = Snapshot(DataPermissionScope.Custom, [5, 6], includeChildren: false);

        var decision = _service.Decide([grant], [99], Now);

        Assert.Equal([5L, 6L], decision.DepartmentIds);
        Assert.Empty(decision.DepartmentAndChildrenIds);
    }

    /// <summary>
    /// 自定义范围（含下级）归入含下级集合。
    /// </summary>
    [Fact]
    public void Decide_CustomWithChildren_ShouldUseGrantDepartmentIds()
    {
        var grant = Snapshot(DataPermissionScope.Custom, [7, 8], includeChildren: true);

        var decision = _service.Decide([grant], [99], Now);

        Assert.Empty(decision.DepartmentIds);
        Assert.Equal([7L, 8L], decision.DepartmentAndChildrenIds);
    }

    /// <summary>
    /// 多种范围按并集叠加、去重并升序输出。
    /// </summary>
    [Fact]
    public void Decide_MixedScopes_ShouldUnionDistinctAndSort()
    {
        var grants = new[]
        {
            Snapshot(DataPermissionScope.DepartmentOnly),
            Snapshot(DataPermissionScope.Custom, [2, 1], includeChildren: false),
            Snapshot(DataPermissionScope.Custom, [8, 7], includeChildren: true)
        };

        var decision = _service.Decide(grants, [3, 1], Now);

        Assert.Equal([1L, 2L, 3L], decision.DepartmentIds);
        Assert.Equal([7L, 8L], decision.DepartmentAndChildrenIds);
    }

    /// <summary>
    /// 授权部门列表中的非正主键（0/负数）必须被过滤。
    /// </summary>
    [Fact]
    public void Decide_ShouldFilterNonPositiveGrantDepartmentIds()
    {
        var grant = Snapshot(DataPermissionScope.Custom, [0, -1, 2], includeChildren: false);

        var decision = _service.Decide([grant], [], Now);

        Assert.Equal([2L], decision.DepartmentIds);
        Assert.Empty(decision.DepartmentAndChildrenIds);
    }

    /// <summary>
    /// 用户部门集合中的非正主键（0/负数）必须被过滤。
    /// </summary>
    [Fact]
    public void Decide_ShouldFilterNonPositiveUserDepartmentIds()
    {
        var decision = _service.Decide([Snapshot(DataPermissionScope.DepartmentOnly)], [0, -5, 4], Now);

        Assert.Equal([4L], decision.DepartmentIds);
        Assert.Empty(decision.DepartmentAndChildrenIds);
    }

    /// <summary>
    /// 授权要求部门范围但用户无有效部门时应回落为本人数据。
    /// </summary>
    [Fact]
    public void Decide_UserWithoutDepartments_ShouldFallBackToSelfOnly()
    {
        var decision = _service.Decide([Snapshot(DataPermissionScope.DepartmentOnly)], [], Now);

        Assert.True(decision.AllowsSelfData);
        Assert.Empty(decision.DepartmentIds);
        Assert.Empty(decision.DepartmentAndChildrenIds);
    }

    /// <summary>
    /// 用户部门集合重复项应去重。
    /// </summary>
    [Fact]
    public void Decide_ShouldDeduplicateUserDepartmentIds()
    {
        var decision = _service.Decide([Snapshot(DataPermissionScope.DepartmentOnly)], [1, 1, 2], Now);

        Assert.Equal([1L, 2L], decision.DepartmentIds);
    }

    /// <summary>
    /// 构造数据范围授权快照（默认永久有效且已启用）。
    /// </summary>
    private static DataScopeGrantSnapshot Snapshot(
        DataPermissionScope scope,
        long[]? departmentIds = null,
        bool includeChildren = false,
        EffectivePeriod? period = null,
        bool enabled = true)
    {
        return new DataScopeGrantSnapshot(
            1,
            AuthorizationGrantSource.Role,
            scope,
            departmentIds ?? [],
            includeChildren,
            period ?? EffectivePeriod.Always,
            enabled);
    }
}
