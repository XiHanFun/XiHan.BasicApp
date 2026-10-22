// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using XiHan.BasicApp.Saas.Application.Conditions;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Domain.Conditions;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Authorization.Conditions;
using ConditionOperator = XiHan.BasicApp.Saas.Domain.Entities.ConditionOperator;
using FrameworkConditionOperator = XiHan.Framework.Authorization.Conditions.ConditionOperator;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 权限条件的合并与判定：写在授予上是「满足才授予」，写在禁止上是「满足才禁止」
/// </summary>
public sealed class AuthorizationConditionResolverTests
{
    private const long Export = 501;
    private const long RoleA = 11;
    private const long RoleB = 12;
    private const long ParentOfA = 13;
    private const string Code = "saas:order:export";

    private readonly List<SysRolePermission> _rolePermissions = [];
    private readonly List<SysUserPermission> _userPermissions = [];
    private readonly List<SysPermissionCondition> _conditions = [];
    private readonly List<AuthorizationRoleChain> _chains = [];
    private readonly HashSet<long> _delegated = [];
    private long _nextId = 1000;

    /// <summary>
    /// 角色 A 无条件授予、角色 B 有条件授予同一权限：A 是无条件路径，直接授予，不进条件
    /// </summary>
    [Fact]
    public void UnconditionalPath_Wins()
    {
        Chain(RoleA);
        Chain(RoleB);
        Grant(RoleA);
        Condition(Grant(RoleB), SaasConditionAttributes.EnvironmentIp, ConditionOperator.In, "10.0.0.0/8");

        var conditions = Resolve(out var resolution);

        Assert.Contains(Export, resolution.GrantedPermissionIds);
        Assert.False(conditions.Requires(Code));
    }

    /// <summary>
    /// 只有角色 B 有条件授予：办公网内授予、外面拒绝；取不到 IP 也拒绝
    /// </summary>
    [Fact]
    public async Task OnlyConditionalGrant_DependsOnCondition()
    {
        Chain(RoleB);
        Condition(Grant(RoleB), SaasConditionAttributes.EnvironmentIp, ConditionOperator.In, "10.0.0.0/8");

        var conditions = Resolve(out var resolution);

        Assert.Contains(Export, resolution.GrantedPermissionIds);
        Assert.True(conditions.Requires(Code));
        Assert.Equal(PermissionConditionOutcome.Granted, await DecideAsync(conditions, (SaasConditionAttributes.EnvironmentIp, IPAddress.Parse("10.1.2.3"))));
        Assert.Equal(PermissionConditionOutcome.Denied, await DecideAsync(conditions, (SaasConditionAttributes.EnvironmentIp, IPAddress.Parse("8.8.8.8"))));
        Assert.Equal(PermissionConditionOutcome.Denied, await DecideAsync(conditions));
    }

    /// <summary>
    /// 角色 A 授予、A 的上级有条件禁止「不在 9–18 点」：9–18 点外这条链不授予；另有无条件路径时看那条
    /// </summary>
    [Fact]
    public async Task ConditionalDenyOnChain_OnlySubtractsThisChain()
    {
        Chain(RoleA, ParentOfA);
        Grant(RoleA);
        Condition(Deny(ParentOfA), SaasConditionAttributes.EnvironmentHour, ConditionOperator.Between, "9,18", isNegated: true);

        var conditions = Resolve(out _);

        Assert.Equal(PermissionConditionOutcome.Granted, await DecideAsync(conditions, (SaasConditionAttributes.EnvironmentHour, 10)));
        Assert.Equal(PermissionConditionOutcome.Denied, await DecideAsync(conditions, (SaasConditionAttributes.EnvironmentHour, 20)));

        // 另一条独立角色链无条件授予：上级的禁止不影响它
        Chain(RoleB);
        Grant(RoleB);
        Assert.False(Resolve(out _).Requires(Code));
    }

    /// <summary>
    /// 链上无条件的禁止：整条链不授予（与原先口径一致）
    /// </summary>
    [Fact]
    public void UnconditionalDenyOnChain_RemovesChain()
    {
        Chain(RoleA, ParentOfA);
        Condition(Grant(RoleA), SaasConditionAttributes.EnvironmentHour, ConditionOperator.GreaterThan, "8");
        Deny(ParentOfA);

        Resolve(out var resolution);

        Assert.DoesNotContain(Export, resolution.GrantedPermissionIds);
    }

    /// <summary>
    /// 用户直授的禁止，条件是「模仿登录」：模仿时拒绝，本人登录照常
    /// </summary>
    [Fact]
    public async Task UserConditionalDeny_WhenImpersonating()
    {
        Chain(RoleA);
        Grant(RoleA);
        Condition(UserDeny(), SaasConditionAttributes.SubjectIsImpersonating, ConditionOperator.Equals, "true");

        var conditions = Resolve(out var resolution);

        Assert.Contains(Export, resolution.GrantedPermissionIds);
        Assert.Equal(PermissionConditionOutcome.Denied, await DecideAsync(conditions, (SaasConditionAttributes.SubjectIsImpersonating, true)));
        Assert.Equal(PermissionConditionOutcome.Granted, await DecideAsync(conditions, (SaasConditionAttributes.SubjectIsImpersonating, false)));
    }

    /// <summary>
    /// 用户无条件的禁止压过有条件与无条件的授予
    /// </summary>
    [Fact]
    public void UserUnconditionalDeny_OverridesEverything()
    {
        Chain(RoleA);
        Grant(RoleA);
        Condition(UserGrant(), SaasConditionAttributes.EnvironmentHour, ConditionOperator.LessThan, "12");
        UserDeny();

        Resolve(out var resolution);

        Assert.DoesNotContain(Export, resolution.GrantedPermissionIds);
    }

    /// <summary>
    /// 委托出来的权限是无条件路径；委托角色按该角色的链结算，链上的条件照算
    /// </summary>
    [Fact]
    public async Task Delegation_DirectIsUnconditional_RoleChainKeepsConditions()
    {
        _chains.Add(new AuthorizationRoleChain($"delegation-role:{RoleB}", new HashSet<long> { RoleB }));
        Condition(Grant(RoleB), SaasConditionAttributes.EnvironmentWeekday, ConditionOperator.In, "1,2,3,4,5");

        var conditions = Resolve(out _);
        Assert.Equal(PermissionConditionOutcome.Denied, await DecideAsync(conditions, (SaasConditionAttributes.EnvironmentWeekday, 6)));

        _delegated.Add(Export);
        Assert.False(Resolve(out _).Requires(Code));
    }

    /// <summary>
    /// 没有启用条件的授权行就是无条件的
    /// </summary>
    [Fact]
    public void BindingWithoutConditions_IsUnconditional()
    {
        Chain(RoleA);
        Grant(RoleA);
        UserGrant();

        Assert.True(Resolve(out _).IsEmpty);
    }

    /// <summary>
    /// 组内且、组间或：任一组全部命中即命中
    /// </summary>
    [Fact]
    public async Task Groups_AndWithin_OrAcross()
    {
        Chain(RoleA);
        var grant = Grant(RoleA);
        Condition(grant, SaasConditionAttributes.EnvironmentHour, ConditionOperator.GreaterThanOrEquals, "9", group: 0);
        Condition(grant, SaasConditionAttributes.EnvironmentWeekday, ConditionOperator.LessThanOrEquals, "5", group: 0);
        Condition(grant, SaasConditionAttributes.SubjectRoleCodes, ConditionOperator.In, "oncall", group: 1);

        var conditions = Resolve(out _);

        Assert.Equal(PermissionConditionOutcome.Granted, await DecideAsync(conditions, (SaasConditionAttributes.EnvironmentHour, 10), (SaasConditionAttributes.EnvironmentWeekday, 3)));
        Assert.Equal(PermissionConditionOutcome.Denied, await DecideAsync(conditions, (SaasConditionAttributes.EnvironmentHour, 10), (SaasConditionAttributes.EnvironmentWeekday, 6)));
        Assert.Equal(PermissionConditionOutcome.Granted, await DecideAsync(conditions, (SaasConditionAttributes.EnvironmentWeekday, 6), (SaasConditionAttributes.SubjectRoleCodes, new[] { "oncall" })));
    }

    /// <summary>
    /// 不求值（应急）：授予行一律算授予、禁止行一律算禁止
    /// </summary>
    [Fact]
    public void DecideIgnoringConditions_TreatsRowsAsUnconditional()
    {
        Chain(RoleA, ParentOfA);
        Condition(Grant(RoleA), SaasConditionAttributes.EnvironmentHour, ConditionOperator.LessThan, "1");
        Assert.Equal(PermissionConditionOutcome.Granted, PermissionConditionDecider.DecideIgnoringConditions(Resolve(out _), Code));

        Condition(Deny(ParentOfA), SaasConditionAttributes.EnvironmentHour, ConditionOperator.LessThan, "1");
        Assert.Equal(PermissionConditionOutcome.Denied, PermissionConditionDecider.DecideIgnoringConditions(Resolve(out _), Code));
    }

    /// <summary>
    /// 快照里引用到却找不到的条件集：授予侧不命中，禁止侧当命中
    /// </summary>
    [Fact]
    public async Task MissingSet_FailsClosed()
    {
        var conditions = new AuthorizationConditions();
        conditions.Grants[Code] = [new ConditionalGrantPath { GrantSetKeys = ["rp:missing"] }];
        Assert.Equal(PermissionConditionOutcome.Denied, await DecideAsync(conditions));

        conditions = new AuthorizationConditions();
        conditions.Denies[Code] = ["up:missing"];
        Assert.Equal(PermissionConditionOutcome.Denied, await DecideAsync(conditions));
    }

    /// <summary>
    /// 随快照进分布式缓存：序列化往返后权限码仍不区分大小写
    /// </summary>
    [Fact]
    public async Task Conditions_SurviveJsonRoundTrip()
    {
        Chain(RoleB);
        Condition(Grant(RoleB), SaasConditionAttributes.EnvironmentIp, ConditionOperator.In, "10.0.0.0/8");
        var json = JsonSerializer.Serialize(Resolve(out _));

        var restored = JsonSerializer.Deserialize<AuthorizationConditions>(json)!.Normalize();

        Assert.True(restored.Requires(Code.ToUpperInvariant()));
        Assert.Equal(PermissionConditionOutcome.Granted, await DecideAsync(restored, (SaasConditionAttributes.EnvironmentIp, IPAddress.Parse("10.0.0.1"))));
    }

    /// <summary>
    /// 存进库的运算符整数值与框架的一一对应
    /// </summary>
    [Fact]
    public void ConditionOperator_MatchesFrameworkValues()
    {
        var local = Enum.GetValues<ConditionOperator>().ToDictionary(item => item.ToString(), item => (int)item);
        var framework = Enum.GetValues<FrameworkConditionOperator>().ToDictionary(item => item.ToString(), item => (int)item);

        Assert.Equal(framework.OrderBy(item => item.Key), local.OrderBy(item => item.Key));
    }

    private AuthorizationConditions Resolve(out AuthorizationGrantResolution resolution)
    {
        resolution = AuthorizationConditionResolver.Resolve(new AuthorizationGrantSources(_chains, _rolePermissions, _userPermissions, _delegated, _conditions));
        var conditions = new AuthorizationConditions();
        foreach (var (permissionId, paths) in resolution.ConditionalGrants)
        {
            conditions.Grants[permissionId == Export ? Code : permissionId.ToString()] = paths;
        }

        foreach (var (permissionId, keys) in resolution.ConditionalDenies)
        {
            conditions.Denies[permissionId == Export ? Code : permissionId.ToString()] = keys;
        }

        foreach (var (key, (_, set)) in resolution.Sets)
        {
            set.PermissionCode = Code;
            conditions.Sets[key] = set;
        }

        return conditions;
    }

    private static async Task<PermissionConditionOutcome> DecideAsync(AuthorizationConditions conditions, params (string Name, object Value)[] values)
    {
        var source = new DictionaryConditionAttributeSource(values.ToDictionary(item => item.Name, item => (object?)item.Value));
        return await PermissionConditionTestKit.Decider().DecideAsync(conditions, Code, source);
    }

    private void Chain(params long[] roleIds)
    {
        _chains.Add(new AuthorizationRoleChain($"role:{roleIds[0]}", roleIds.ToHashSet()));
    }

    private SysRolePermission Grant(long roleId)
    {
        return AddRolePermission(roleId, PermissionAction.Grant);
    }

    private SysRolePermission Deny(long roleId)
    {
        return AddRolePermission(roleId, PermissionAction.Deny);
    }

    private SysRolePermission AddRolePermission(long roleId, PermissionAction action)
    {
        var row = new SysRolePermission { RoleId = roleId, PermissionId = Export, PermissionAction = action };
        SaasTestHelper.SetBasicId(row, ++_nextId);
        _rolePermissions.Add(row);
        return row;
    }

    private SysUserPermission UserGrant()
    {
        return AddUserPermission(PermissionAction.Grant);
    }

    private SysUserPermission UserDeny()
    {
        return AddUserPermission(PermissionAction.Deny);
    }

    private SysUserPermission AddUserPermission(PermissionAction action)
    {
        var row = new SysUserPermission { UserId = 1, PermissionId = Export, PermissionAction = action };
        SaasTestHelper.SetBasicId(row, ++_nextId);
        _userPermissions.Add(row);
        return row;
    }

    private void Condition(SysRolePermission binding, string attribute, ConditionOperator @operator, string value, bool isNegated = false, int group = 0)
    {
        AddCondition(new SysPermissionCondition { RolePermissionId = binding.BasicId }, attribute, @operator, value, isNegated, group);
    }

    private void Condition(SysUserPermission binding, string attribute, ConditionOperator @operator, string value, bool isNegated = false, int group = 0)
    {
        AddCondition(new SysPermissionCondition { UserPermissionId = binding.BasicId }, attribute, @operator, value, isNegated, group);
    }

    private void AddCondition(SysPermissionCondition condition, string attribute, ConditionOperator @operator, string value, bool isNegated, int group)
    {
        condition.AttributeName = attribute;
        condition.Operator = @operator;
        condition.ConditionValue = value;
        condition.IsNegated = isNegated;
        condition.ConditionGroup = group;
        SaasTestHelper.SetBasicId(condition, ++_nextId);
        _conditions.Add(condition);
    }
}
