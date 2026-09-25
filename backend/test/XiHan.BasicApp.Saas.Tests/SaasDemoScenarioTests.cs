// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Infrastructure.Seeders;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 演示场景的声明测试：引用处处对得上，每种要验证的情况都有一个演示
/// </summary>
/// <remarks>
/// 演示数据写入时引用对不上会直接报错、拖垮启动，所以在这里先把声明本身钉住；
/// 覆盖类断言保证「能验证所有情况」不会随着改动悄悄少掉某一种。
/// </remarks>
public sealed class SaasDemoScenarioTests
{
    private static readonly IReadOnlyList<DemoTenant> Tenants = SaasDemoScenario.Tenants;

    private static readonly IReadOnlyList<DemoAccount> AllAccounts =
        [.. SaasDemoScenario.PlatformAccounts, .. Tenants.SelectMany(static tenant => tenant.Accounts ?? [])];

    private static readonly IReadOnlyList<DemoMember> AllMembers = [.. Tenants.SelectMany(static tenant => tenant.Members ?? [])];

    private static readonly Dictionary<string, SaasPermissionDefinition> SaasPermissions =
        SaasPermissionDefinitions.All.ToDictionary(static definition => definition.PermissionCode, StringComparer.Ordinal);

    /// <summary>
    /// 租户编码唯一、以 demo- 开头，套餐都存在。
    /// </summary>
    [Fact]
    public void Tenants_ShouldHaveUniqueDemoCodesAndKnownEditions()
    {
        var editionCodes = SaasEditionSeeder.Editions.Select(static edition => edition.Code).ToHashSet(StringComparer.Ordinal);

        Assert.Equal(Tenants.Count, Tenants.Select(static tenant => tenant.Code).Distinct(StringComparer.Ordinal).Count());
        Assert.All(Tenants, tenant =>
        {
            Assert.StartsWith("demo-", tenant.Code, StringComparison.Ordinal);
            Assert.Contains(tenant.Edition, editionCodes);
            Assert.False(string.IsNullOrWhiteSpace(tenant.Scenario));
        });
    }

    /// <summary>
    /// 账号邮箱全平台唯一（它是租户账号的登录名），用户名在各自注册的地方唯一。
    /// </summary>
    [Fact]
    public void Accounts_ShouldHaveUniqueEmailsAndUserNames()
    {
        Assert.Equal(AllAccounts.Count, AllAccounts.Select(static account => account.Email).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(SaasDemoScenario.PlatformAccounts.Count, SaasDemoScenario.PlatformAccounts.Select(static account => account.UserName).Distinct(StringComparer.Ordinal).Count());
        Assert.DoesNotContain(SaasDemoScenario.PlatformAccounts, static account => account.UserName == SaasSuperAdminSeeder.UserName);
        Assert.All(Tenants, tenant =>
        {
            var accounts = tenant.Accounts ?? [];
            Assert.Equal(accounts.Count, accounts.Select(static account => account.UserName).Distinct(StringComparer.Ordinal).Count());
        });
    }

    /// <summary>
    /// 每个成员都引用一个已声明的账号；同一租户里一个账号只出现一次。
    /// </summary>
    [Fact]
    public void Members_ShouldReferenceDeclaredAccounts()
    {
        var emails = AllAccounts.Select(static account => account.Email).ToHashSet(StringComparer.Ordinal);

        Assert.All(AllMembers, member => Assert.Contains(member.Email, emails));
        Assert.All(Tenants, tenant =>
        {
            var members = tenant.Members ?? [];
            Assert.Equal(members.Count, members.Select(static member => member.Email).Distinct(StringComparer.Ordinal).Count());
        });
    }

    /// <summary>
    /// 有成员的租户恰好一名所有者，且所有者的账号注册在本租户；支持人员只能是平台账号。
    /// </summary>
    [Fact]
    public void Members_OwnerAndSupportShouldFollowTheTenancyRules()
    {
        var platformEmails = SaasDemoScenario.PlatformAccounts.Select(static account => account.Email).ToHashSet(StringComparer.Ordinal);
        foreach (var tenant in Tenants.Where(static tenant => tenant.Members is { Count: > 0 }))
        {
            var owner = Assert.Single(tenant.Members!, static member => member.Type == TenantMemberType.Owner);
            Assert.Contains(tenant.Accounts ?? [], account => account.Email == owner.Email);
            Assert.Equal(TenantMemberInviteStatus.Accepted, owner.InviteStatus);
            Assert.Null(owner.ExpiresInDays);

            Assert.All(tenant.Members!.Where(static member => member.Type == TenantMemberType.PlatformAdmin), member => Assert.Contains(member.Email, platformEmails));
            Assert.All(tenant.Members!.Where(static member => member.Type != TenantMemberType.PlatformAdmin), member => Assert.DoesNotContain(member.Email, platformEmails));
        }
    }

    /// <summary>
    /// 成员的角色、部门、岗位都在本租户（角色也可以是全局角色）；负责人必须有部门。
    /// </summary>
    [Fact]
    public void Members_ShouldReferenceRolesDepartmentsAndPositionsOfTheirTenant()
    {
        var globalRoles = SaasDemoScenario.GlobalRoles.Select(static role => role.Code).ToHashSet(StringComparer.Ordinal);
        foreach (var tenant in Tenants)
        {
            var roles = (tenant.Roles ?? []).Select(static role => role.Code).ToHashSet(StringComparer.Ordinal);
            var departments = (tenant.Departments ?? []).Select(static department => department.Code).ToHashSet(StringComparer.Ordinal);
            var positions = (tenant.Positions ?? []).Select(static position => position.Code).ToHashSet(StringComparer.Ordinal);
            foreach (var member in tenant.Members ?? [])
            {
                Assert.All(member.Roles ?? [], binding => Assert.True(roles.Contains(binding.RoleCode) || globalRoles.Contains(binding.RoleCode), $"{tenant.Code} 的成员 {member.Email} 引用了不存在的角色 {binding.RoleCode}"));
                Assert.True(member.Department is null || departments.Contains(member.Department), $"{member.Email} 的部门 {member.Department} 不存在");
                Assert.True(member.Position is null || positions.Contains(member.Position), $"{member.Email} 的岗位 {member.Position} 不存在");
                Assert.True(!member.IsLeader || member.Department is not null, $"{member.Email} 是负责人却没有部门");
                Assert.DoesNotContain(member.Roles ?? [], binding => binding.RoleCode == SaasRoleCodes.TenantOwner);
            }
        }
    }

    /// <summary>
    /// 部门的父级排在子级之前；角色的继承与自定义数据范围引用本租户的角色与部门，自定义档位与部门明细一一对应。
    /// </summary>
    [Fact]
    public void TenantStructure_ShouldBeConsistent()
    {
        foreach (var tenant in Tenants)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var department in tenant.Departments ?? [])
            {
                Assert.True(department.Parent is null || seen.Contains(department.Parent), $"{tenant.Code} 的部门 {department.Code} 的父部门须排在前面");
                Assert.True(seen.Add(department.Code), $"{tenant.Code} 的部门编码 {department.Code} 重复");
            }

            var roles = (tenant.Roles ?? []).Select(static role => role.Code).ToHashSet(StringComparer.Ordinal);
            Assert.Equal((tenant.Roles ?? []).Count, roles.Count);
            foreach (var role in tenant.Roles ?? [])
            {
                Assert.True(role.Inherits is null || (roles.Contains(role.Inherits) && role.Inherits != role.Code), $"{tenant.Code} 的角色 {role.Code} 继承了不存在的角色");
                Assert.Equal(role.DataScope == DataPermissionScope.Custom, role.CustomDepartments is { Count: > 0 });
                Assert.All(role.CustomDepartments ?? [], code => Assert.Contains(code, seen));
                Assert.NotEqual(SaasRoleCodes.TenantOwner, role.Code);
                Assert.NotEqual(SaasRoleCodes.SuperAdmin, role.Code);
            }

            var hasContent = tenant.Departments is { Count: > 0 } || tenant.Roles is { Count: > 0 } || tenant.Accounts is { Count: > 0 } || tenant.Members is { Count: > 0 };
            Assert.True(!hasContent || tenant.Isolation == TenantIsolationMode.Field, $"{tenant.Code} 不是字段隔离，库还没初始化，不能带租户数据");
        }
    }

    /// <summary>
    /// 授权里点名的 SaaS 权限码都存在；租户角色点名的码都能在租户生效；全局角色不能用自定义数据范围。
    /// </summary>
    [Fact]
    public void Grants_ShouldReferenceDeclaredPermissionsOfTheRightSide()
    {
        foreach (var role in SaasDemoScenario.GlobalRoles)
        {
            Assert.All(role.Grant.RequiredCodes, code => Assert.True(SaasPermissions.ContainsKey(code), $"全局角色 {role.Code} 的权限 {code} 不存在"));
            Assert.NotEqual(DataPermissionScope.Custom, role.DataScope);
        }

        foreach (var role in Tenants.SelectMany(static tenant => tenant.Roles ?? []))
        {
            Assert.All(role.Grant.RequiredCodes, code =>
            {
                Assert.True(SaasPermissions.TryGetValue(code, out var definition), $"角色 {role.Code} 的权限 {code} 不存在");
                Assert.True(definition!.Side.IsTenantEffective(), $"租户角色 {role.Code} 点名了平台侧权限 {code}");
            });
            Assert.All(role.Grant.OptionalCodes, code => Assert.False(code.StartsWith(SaasPermissionCodes.Module + ":", StringComparison.Ordinal), $"SaaS 的权限码 {code} 必须点名为必需"));
        }

        foreach (var grant in AllMembers.SelectMany(static member => member.Permissions ?? []))
        {
            if (grant.IsModuleCode)
            {
                Assert.False(grant.PermissionCode.StartsWith(SaasPermissionCodes.Module + ":", StringComparison.Ordinal));
                continue;
            }

            Assert.True(SaasPermissions.TryGetValue(grant.PermissionCode, out var definition), $"直授权限 {grant.PermissionCode} 不存在");
            Assert.True(definition!.Side.IsTenantEffective(), $"直授权限 {grant.PermissionCode} 在租户里不生效");
        }
    }

    /// <summary>
    /// 覆盖：每种成员类型、邀请状态都有演示。
    /// </summary>
    [Fact]
    public void Coverage_EveryMemberTypeAndInviteStatus()
    {
        Assert.All(Enum.GetValues<TenantMemberType>(), type => Assert.Contains(AllMembers, member => member.Type == type));
        Assert.All(Enum.GetValues<TenantMemberInviteStatus>(), status => Assert.Contains(AllMembers, member => member.InviteStatus == status));
    }

    /// <summary>
    /// 覆盖：每档数据范围都有角色或成员覆盖在用；角色继承、名额已满、停用角色、授权到期与预约、直授与禁止都有演示。
    /// </summary>
    [Fact]
    public void Coverage_EveryDataScopeAndRoleCase()
    {
        var roles = Tenants.SelectMany(static tenant => tenant.Roles ?? []).ToList();
        var bindings = AllMembers.SelectMany(static member => member.Roles ?? []).ToList();

        Assert.All(Enum.GetValues<DataPermissionScope>(), scope => Assert.True(
            roles.Any(role => role.DataScope == scope) || AllMembers.Any(member => member.DataScopeOverride == scope),
            $"没有演示数据范围 {scope}"));
        Assert.Contains(AllMembers, static member => member.DataScopeOverride is not null);
        Assert.Contains(roles, static role => role.Inherits is not null);
        Assert.Contains(roles, static role => role.MaxMembers > 0);
        Assert.Contains(roles, static role => role.Status == EnableStatus.Disabled);
        Assert.Contains(bindings, static binding => binding.ExpiresInDays < 0);
        Assert.Contains(bindings, static binding => binding.EffectiveInDays > 0);
        Assert.Contains(AllMembers.SelectMany(static member => member.Permissions ?? []), static grant => grant.Action == PermissionAction.Grant);
        Assert.Contains(AllMembers.SelectMany(static member => member.Permissions ?? []), static grant => grant.Action == PermissionAction.Deny);
        Assert.Contains(bindings, binding => SaasDemoScenario.GlobalRoles.Any(role => role.Code == binding.RoleCode));
    }

    /// <summary>
    /// 覆盖：租户的各种生命周期（正常、暂停、停用、到期、席位已满、待开通管理员、库隔离待初始化）与每档套餐都有演示。
    /// </summary>
    [Fact]
    public void Coverage_EveryTenantLifecycleAndEdition()
    {
        Assert.All(Enum.GetValues<TenantStatus>(), status => Assert.Contains(Tenants, tenant => tenant.Status == status));
        Assert.Contains(Tenants, static tenant => tenant.ExpiresInDays < 0);
        Assert.Contains(Tenants, static tenant => tenant.UserLimit is { } limit && (tenant.Members ?? []).Count(member => member.Type != TenantMemberType.PlatformAdmin && member.InviteStatus == TenantMemberInviteStatus.Accepted) >= limit);
        Assert.Contains(Tenants, static tenant => tenant.Isolation == TenantIsolationMode.Field && tenant.Members is null or { Count: 0 });
        Assert.Contains(Tenants, static tenant => tenant.Isolation == TenantIsolationMode.Database && tenant.ConfigStatus == TenantConfigStatus.Pending);
        Assert.All(SaasEditionSeeder.Editions, edition => Assert.Contains(Tenants, tenant => tenant.Edition == edition.Code && tenant.Status == TenantStatus.Normal && tenant.ExpiresInDays is null));
    }

    /// <summary>
    /// 覆盖：账号的停用、锁定、跨租户（一个账号多个租户）、支持人员在窗口内与窗口已结束、成员身份到期都有演示。
    /// </summary>
    [Fact]
    public void Coverage_EveryAccountAndMembershipCase()
    {
        Assert.Contains(AllAccounts, static account => account.Status == EnableStatus.Disabled);
        Assert.Contains(AllAccounts, static account => account.Locked);
        Assert.Contains(AllMembers.GroupBy(static member => member.Email), static group => group.Count(member => member.InviteStatus == TenantMemberInviteStatus.Accepted) > 1);
        Assert.Contains(AllMembers, static member => member.Type == TenantMemberType.PlatformAdmin && member.ExpiresInDays > 0);
        Assert.Contains(AllMembers, static member => member.Type == TenantMemberType.PlatformAdmin && member.ExpiresInDays < 0);
        Assert.Contains(AllMembers, static member => member.Type != TenantMemberType.PlatformAdmin && member.ExpiresInDays < 0);
    }
}
