// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Seeds;
using XiHan.BasicApp.Identity;
using XiHan.Framework.Security.Abstractions.Conditions;

namespace XiHan.BasicApp.Access.Domain.Services;

/// <summary>
/// 一个访问策略模板。
/// </summary>
/// <param name="Code">编码（种子写入的策略同名）。</param>
/// <param name="Name">名称。</param>
/// <param name="Description">说明。</param>
/// <param name="Action">种子写入时的动作（从模板新建一律只记录）。</param>
/// <param name="Status">种子写入时的状态。</param>
/// <param name="Priority">优先级。</param>
/// <param name="Conditions">条件。</param>
public sealed record AccessPolicyTemplate(string Code, string Name, string Description, AccessPolicyAction Action, AccessPolicyStatus Status, int Priority, IReadOnlyList<PolicyConditionDefinition> Conditions);

/// <summary>
/// 访问策略的预置模板。
/// </summary>
/// <remarks>
/// 共六个，4.x 同。
/// 平台种子按它写入预置策略，平台与租户都能在页面上从模板新建（新建一律只记录）。
/// 办公网段用文档保留网段 <c>203.0.113.0/24</c> 占位，启用拦截前要换成真实网段；国家取离线地址库的中文名，港澳台在库里归中国。
/// </remarks>
public static class AccessPolicyTemplates
{
    /// <summary>
    /// 占位的办公网段。
    /// </summary>
    public const string OfficeNetworkPlaceholder = "203.0.113.0/24";

    /// <summary>
    /// 开放平台调用的客户端类型（签名凭证调用不算脚本）。
    /// </summary>
    public const string OpenApiClientKind = "openapi";

    /// <summary>
    /// 全部模板。
    /// </summary>
    public static IReadOnlyList<AccessPolicyTemplate> All { get; } =
    [
        new(
            "preset.ip-blacklist",
            "IP blacklist",
            "Deny every request from the listed addresses or networks. Put the addresses to block into the condition before enabling it; the initial value only holds documentation ranges.",
            AccessPolicyAction.Deny,
            AccessPolicyStatus.Disabled,
            10,
            [Clause(0, AccessConditionAttributes.EnvironmentIp, ConditionOperator.In, "203.0.113.0/24,2001:db8::/32")]),
        new(
            "preset.admin-office-network",
            "Administrators only from the office network",
            "Administrators can only call from the office network. Replace the placeholder network with the real one and trial it from inside the office before enabling it.",
            AccessPolicyAction.Deny,
            AccessPolicyStatus.Disabled,
            20,
            [
                Clause(0, AccessConditionAttributes.SubjectRoleCodes, ConditionOperator.In, PlatformRoleSeeder.AdminRoleCode),
                Clause(0, AccessConditionAttributes.EnvironmentIp, ConditionOperator.NotIn, OfficeNetworkPlaceholder),
            ]),
        new(
            "preset.sensitive-ops-network",
            "Sensitive operations only from the office network",
            "Exports, deletions, impersonation and granting can only be started from the office network. Replace the placeholder network with the real one before enabling it.",
            AccessPolicyAction.Deny,
            AccessPolicyStatus.Disabled,
            30,
            [
                Clause(0, AccessConditionAttributes.ResourcePermissionCodes, ConditionOperator.EndsWith, ".export"),
                Clause(0, AccessConditionAttributes.EnvironmentIp, ConditionOperator.NotIn, OfficeNetworkPlaceholder),
                Clause(1, AccessConditionAttributes.ResourcePermissionCodes, ConditionOperator.EndsWith, ".delete"),
                Clause(1, AccessConditionAttributes.EnvironmentIp, ConditionOperator.NotIn, OfficeNetworkPlaceholder),
                Clause(2, AccessConditionAttributes.ResourcePermissionCodes, ConditionOperator.In, string.Join(',', IdentityPermissions.ImpersonationStart, AccessPermissions.RolesGrant, AccessPermissions.RolesAssign, AccessPermissions.UserGrantsGrant, AccessPermissions.DelegationsCreate)),
                Clause(2, AccessConditionAttributes.EnvironmentIp, ConditionOperator.NotIn, OfficeNetworkPlaceholder),
            ]),
        new(
            "preset.off-hours",
            "Off-hours access",
            "Weekends, and weekdays outside 08:00–20:00, in the time zone of BasicApp:Access:ConditionTimeZone.",
            AccessPolicyAction.LogOnly,
            AccessPolicyStatus.Active,
            100,
            [
                Clause(0, AccessConditionAttributes.EnvironmentWeekday, ConditionOperator.In, "6,7"),
                Clause(1, AccessConditionAttributes.EnvironmentTime, ConditionOperator.Between, "08:00,20:00", isNegated: true),
            ]),
        new(
            "preset.overseas",
            "Overseas access",
            "Requests whose country is known to the offline address database and is not China; unknown countries (intranet, loopback, missing from the database) do not count.",
            AccessPolicyAction.LogOnly,
            AccessPolicyStatus.Active,
            110,
            [
                Clause(0, AccessConditionAttributes.EnvironmentGeoCountry, ConditionOperator.IsNotNull, null),
                Clause(0, AccessConditionAttributes.EnvironmentGeoCountry, ConditionOperator.NotEquals, "中国"),
            ]),
        new(
            "preset.block-scripts",
            "Scripts and crawlers",
            "Requests without a User-Agent, or with the User-Agent of a common script or crawler tool; open platform calls signed with credentials are excluded.",
            AccessPolicyAction.LogOnly,
            AccessPolicyStatus.Active,
            120,
            [
                .. ScriptGroup(0, ConditionOperator.IsNull, null),
                .. ScriptGroup(1, ConditionOperator.Contains, "curl"),
                .. ScriptGroup(2, ConditionOperator.Contains, "python-requests"),
                .. ScriptGroup(3, ConditionOperator.Contains, "Go-http-client"),
                .. ScriptGroup(4, ConditionOperator.Contains, "Wget"),
                .. ScriptGroup(5, ConditionOperator.Contains, "Scrapy"),
                .. ScriptGroup(6, ConditionOperator.Contains, "HttpClient"),
            ]),
    ];

    /// <summary>
    /// 按编码找模板。
    /// </summary>
    public static AccessPolicyTemplate? Find(string code) => All.FirstOrDefault(t => string.Equals(t.Code, code, StringComparison.OrdinalIgnoreCase));

    private static PolicyConditionDefinition Clause(int group, string attribute, ConditionOperator @operator, string? value, bool isNegated = false)
        => new(group, attribute, @operator, isNegated, value, null);

    private static PolicyConditionDefinition[] ScriptGroup(int group, ConditionOperator userAgentOperator, string? userAgent)
        =>
        [
            Clause(group, AccessConditionAttributes.EnvironmentUserAgent, userAgentOperator, userAgent),
            Clause(group, AccessConditionAttributes.EnvironmentClientKind, ConditionOperator.NotEquals, OpenApiClientKind),
        ];
}
