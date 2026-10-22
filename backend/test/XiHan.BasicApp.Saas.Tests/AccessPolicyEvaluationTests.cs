// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.AccessPolicies;
using XiHan.BasicApp.Saas.Domain.Conditions;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Authorization.Conditions;
using static XiHan.BasicApp.Saas.Tests.AccessPolicyTestKit;
using ConditionOperator = XiHan.BasicApp.Saas.Domain.Entities.ConditionOperator;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 访问策略的求值与裁决：同层放行压过拒绝、平台的拒绝租户放行不了、生效期、求值时机与条件校验
/// </summary>
public sealed class AccessPolicyEvaluationTests
{
    private static readonly AccessPolicyConditionCommand OfficeIp = Clause(SaasConditionAttributes.EnvironmentIp, ConditionOperator.In, "10.0.0.0/8");
    private static readonly AccessPolicyConditionCommand AdminRole = Clause(SaasConditionAttributes.SubjectRoleCodes, ConditionOperator.In, "admin");

    /// <summary>
    /// 命中的策略按优先级排列；同层里有放行就没有生效的拒绝
    /// </summary>
    [Fact]
    public async Task Layer_AllowBeatsDenyAndKeepsMatches()
    {
        var deny = Policy(1, 0, AccessPolicyAction.Deny, OfficeIp);
        var logOnly = Policy(2, 0, AccessPolicyAction.LogOnly, OfficeIp);
        var allow = Policy(3, 0, AccessPolicyAction.Allow, OfficeIp);
        var source = new DictionaryConditionAttributeSource(new Dictionary<string, object?> { [SaasConditionAttributes.EnvironmentIp] = System.Net.IPAddress.Parse("10.1.2.3") });

        var withAllow = await Evaluator().EvaluateAsync([deny, logOnly, allow], source, Now);
        var withoutAllow = await Evaluator().EvaluateAsync([deny, logOnly], source, Now);

        Assert.Equal([1L, 2L, 3L], withAllow.Matched.Select(policy => policy.PolicyId));
        Assert.Same(allow, withAllow.Allow);
        Assert.Null(withAllow.Deny);
        Assert.Same(deny, withoutAllow.Deny);
    }

    /// <summary>
    /// 生效期外的策略不参与；到期时间当刻起失效
    /// </summary>
    [Fact]
    public async Task Layer_SkipsPoliciesOutsideEffectivePeriod()
    {
        var future = Policy(1, 0, AccessPolicyAction.Deny, Now.AddMinutes(1), null, OfficeIp);
        var expired = Policy(2, 0, AccessPolicyAction.Deny, null, Now, OfficeIp);
        var current = Policy(3, 0, AccessPolicyAction.Deny, Now, Now.AddMinutes(1), OfficeIp);
        var source = new DictionaryConditionAttributeSource(new Dictionary<string, object?> { [SaasConditionAttributes.EnvironmentIp] = System.Net.IPAddress.Parse("10.1.2.3") });

        var result = await Evaluator().EvaluateAsync([future, expired, current], source, Now);

        Assert.Same(current, Assert.Single(result.Matched));
    }

    /// <summary>
    /// 平台拒绝时租户层不再求，租户的放行也救不回来
    /// </summary>
    [Fact]
    public void Decision_PlatformDenyCannotBeOverriddenByTenant()
    {
        var platformDeny = Policy(1, 0, AccessPolicyAction.Deny, OfficeIp);
        var tenantAllow = Policy(2, 7, AccessPolicyAction.Allow, OfficeIp);

        var decision = AccessPolicyDecision.Combine(new AccessPolicyLayerResult([platformDeny]), platformAllowed: false, new AccessPolicyLayerResult([tenantAllow]));

        Assert.Same(platformDeny, decision.BlockedBy);
        Assert.Equal([platformDeny], decision.Matched);
    }

    /// <summary>
    /// 认证前已命中平台放行时，认证后的平台拒绝不再拦；租户的拒绝照样生效
    /// </summary>
    [Fact]
    public void Decision_EarlierPlatformAllowSuppressesPlatformDenyOnly()
    {
        var platformDeny = Policy(1, 0, AccessPolicyAction.Deny, AdminRole);
        var tenantDeny = Policy(2, 7, AccessPolicyAction.Deny, AdminRole);
        var otherTenantDeny = Policy(3, 7, AccessPolicyAction.Deny, AdminRole);

        var onlyPlatform = AccessPolicyDecision.Combine(new AccessPolicyLayerResult([platformDeny]), platformAllowed: true, AccessPolicyLayerResult.None);
        var withTenant = AccessPolicyDecision.Combine(new AccessPolicyLayerResult([platformDeny]), platformAllowed: true, new AccessPolicyLayerResult([tenantDeny, otherTenantDeny]));

        Assert.False(onlyPlatform.IsBlocked);
        Assert.Same(tenantDeny, withTenant.BlockedBy);
        Assert.False(withTenant.IsBlocking(platformDeny));
        Assert.True(withTenant.IsBlocking(tenantDeny));
        Assert.True(withTenant.IsBlocking(otherTenantDeny));
    }

    /// <summary>
    /// 租户的放行压过租户自己的拒绝
    /// </summary>
    [Fact]
    public void Decision_TenantAllowBeatsTenantDeny()
    {
        var tenantDeny = Policy(1, 7, AccessPolicyAction.Deny, AdminRole);
        var tenantAllow = Policy(2, 7, AccessPolicyAction.Allow, AdminRole);

        var decision = AccessPolicyDecision.Combine(AccessPolicyLayerResult.None, platformAllowed: false, new AccessPolicyLayerResult([tenantDeny, tenantAllow]));

        Assert.False(decision.IsBlocked);
        Assert.False(decision.IsBlocking(tenantDeny));
    }

    /// <summary>
    /// 只引用请求本身属性的策略在认证前求值，其余在认证后；快照按层与时机分好
    /// </summary>
    [Fact]
    public void Snapshot_SplitsByLayerAndPhase()
    {
        var beforeAuthentication = Policy(2, 0, AccessPolicyAction.Deny,
            OfficeIp,
            Clause(SaasConditionAttributes.EnvironmentTime, ConditionOperator.Between, "22:00,06:00", group: 1),
            Clause(SaasConditionAttributes.ResourcePath, ConditionOperator.StartsWith, "/api/Auth", group: 2));
        var afterAuthentication = Policy(1, 0, AccessPolicyAction.Deny, OfficeIp, AdminRole);
        var tenant = Policy(3, 7, AccessPolicyAction.Deny, OfficeIp);

        var snapshot = new AccessPolicySnapshot([tenant, beforeAuthentication, afterAuthentication], "v1");

        Assert.True(beforeAuthentication.IsBeforeAuthentication);
        Assert.False(afterAuthentication.IsBeforeAuthentication);
        Assert.Equal([1L, 2L], snapshot.Platform.Select(policy => policy.PolicyId));
        Assert.Same(beforeAuthentication, Assert.Single(snapshot.PlatformBeforeAuthentication));
        Assert.Same(afterAuthentication, Assert.Single(snapshot.PlatformAfterAuthentication));
        Assert.Same(tenant, Assert.Single(snapshot.GetTenant(7)));
        Assert.Empty(snapshot.GetTenant(8));
    }

    /// <summary>
    /// 写路径校验：属性未登记（含数据行属性）、运算符不适用、值写错、值类型与属性不符都拒绝，并指出第几条
    /// </summary>
    [Theory]
    [InlineData("resource.status", ConditionOperator.Equals, "draft", ConfigDataType.String, "第 1 条")]
    [InlineData(SaasConditionAttributes.EnvironmentIp, ConditionOperator.GreaterThan, "10.0.0.1", ConfigDataType.String, "第 1 条")]
    [InlineData(SaasConditionAttributes.EnvironmentIp, ConditionOperator.In, "10.0.0.256/8", ConfigDataType.String, "第 1 条")]
    [InlineData(SaasConditionAttributes.EnvironmentIp, ConditionOperator.In, "10.0.0.0/8", ConfigDataType.Number, "值类型")]
    public void Compiler_RejectsConditionsThatCannotBeEvaluated(string attribute, ConditionOperator @operator, string value, ConfigDataType valueType, string expected)
    {
        var error = Assert.Throws<InvalidOperationException>(() => Compiler().Validate(
            [new AccessPolicyConditionCommand(0, attribute, @operator, false, valueType, value, null)]));

        Assert.Contains(expected, error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 合规的条件（含旧名别名、跨零点时段）通过校验
    /// </summary>
    [Fact]
    public void Compiler_AcceptsValidConditions()
    {
        Compiler().Validate(
        [
            OfficeIp,
            Clause("subject.role", ConditionOperator.In, "admin", group: 1),
            Clause(SaasConditionAttributes.EnvironmentTime, ConditionOperator.Between, "22:00,06:00", group: 2),
            new AccessPolicyConditionCommand(3, SaasConditionAttributes.EnvironmentWeekday, ConditionOperator.In, true, ConfigDataType.Array, "[6,7]", null)
        ]);
    }
}
