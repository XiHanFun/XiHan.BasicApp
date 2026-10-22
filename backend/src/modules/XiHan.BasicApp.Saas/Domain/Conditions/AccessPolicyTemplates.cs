// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;

namespace XiHan.BasicApp.Saas.Domain.Conditions;

/// <summary>
/// 访问策略的预置模板：平台按它写入预置策略；平台与租户都能在页面上「从模板新建」，新建出来一律只记录
/// </summary>
/// <remarks>
/// 办公网段用文档保留网段 203.0.113.0/24 占位，启用前要换成真实网段。
/// 国家取离线地址库的中文名，港澳台的国家都是「中国」，要算作境外须另加省份条件。
/// </remarks>
public static class AccessPolicyTemplates
{
    /// <summary>
    /// 占位的办公网段
    /// </summary>
    public const string OfficeNetworkPlaceholder = "203.0.113.0/24";

    /// <summary>
    /// 全部模板
    /// </summary>
    public static IReadOnlyList<AccessPolicyTemplate> All { get; } =
    [
        new(
            "preset.ip-blacklist",
            "IP 黑名单",
            "命中的客户端地址拒绝一切请求。把要封的地址或网段填进条件再启用拦截；初始值只放文档保留网段。",
            AccessPolicyAction.Deny,
            EnableStatus.Disabled,
            10,
            [Clause(0, SaasConditionAttributes.EnvironmentIp, ConditionOperator.In, "203.0.113.0/24,2001:db8::/32")]),
        new(
            "preset.admin-office-network",
            "管理员只能在办公网访问",
            "超级管理员与租户所有者只能从办公网访问。启用前把办公网段换成真实网段，并在办公网内试运行。",
            AccessPolicyAction.Deny,
            EnableStatus.Disabled,
            20,
            [
                Clause(0, SaasConditionAttributes.SubjectRoleCodes, ConditionOperator.In, $"{SaasRoleCodes.SuperAdmin},{SaasRoleCodes.TenantOwner}"),
                Clause(0, SaasConditionAttributes.EnvironmentIp, ConditionOperator.NotIn, OfficeNetworkPlaceholder)
            ]),
        new(
            "preset.sensitive-ops-network",
            "敏感操作限定网络",
            "导出、删除、模仿登录与授权类操作只能从办公网发起。启用前把办公网段换成真实网段。",
            AccessPolicyAction.Deny,
            EnableStatus.Disabled,
            30,
            [
                Clause(0, SaasConditionAttributes.ResourcePermissionCodes, ConditionOperator.EndsWith, ":export"),
                Clause(0, SaasConditionAttributes.EnvironmentIp, ConditionOperator.NotIn, OfficeNetworkPlaceholder),
                Clause(1, SaasConditionAttributes.ResourcePermissionCodes, ConditionOperator.EndsWith, ":delete"),
                Clause(1, SaasConditionAttributes.EnvironmentIp, ConditionOperator.NotIn, OfficeNetworkPlaceholder),
                Clause(2, SaasConditionAttributes.ResourcePermissionCodes, ConditionOperator.In,
                    $"{SaasPermissionCodes.Impersonation.Start},{SaasPermissionCodes.Impersonation.CrossTenant},{SaasPermissionCodes.RolePermission.Grant},{SaasPermissionCodes.UserPermission.Grant},{SaasPermissionCodes.UserRole.Grant}"),
                Clause(2, SaasConditionAttributes.EnvironmentIp, ConditionOperator.NotIn, OfficeNetworkPlaceholder)
            ]),
        new(
            "preset.off-hours",
            "非工作时间访问",
            "周末全天，以及工作日 08:00–20:00 之外的访问。时段按 Saas:Conditions:TimeZone 计算。",
            AccessPolicyAction.LogOnly,
            EnableStatus.Enabled,
            100,
            [
                Clause(0, SaasConditionAttributes.EnvironmentWeekday, ConditionOperator.In, "6,7"),
                Clause(1, SaasConditionAttributes.EnvironmentTime, ConditionOperator.Between, "08:00,20:00", isNegated: true)
            ]),
        new(
            "preset.overseas",
            "境外访问",
            "离线地址库查得到国家、且国家不是中国的访问；查不到国家的（内网、本机、库里没有）不算。港澳台在库里归中国，要算作境外须另加省份条件。",
            AccessPolicyAction.LogOnly,
            EnableStatus.Enabled,
            110,
            [
                Clause(0, SaasConditionAttributes.EnvironmentGeoCountry, ConditionOperator.IsNotNull, string.Empty),
                Clause(0, SaasConditionAttributes.EnvironmentGeoCountry, ConditionOperator.NotEquals, "中国")
            ]),
        new(
            "preset.block-scripts",
            "爬虫与脚本访问",
            "没有 User-Agent，或 User-Agent 是常见脚本与爬虫工具的请求；开放接口凭证签名的调用除外。",
            AccessPolicyAction.LogOnly,
            EnableStatus.Enabled,
            120,
            [
                .. ScriptGroup(0, ConditionOperator.IsNull, string.Empty),
                .. ScriptGroup(1, ConditionOperator.Contains, "curl"),
                .. ScriptGroup(2, ConditionOperator.Contains, "python-requests"),
                .. ScriptGroup(3, ConditionOperator.Contains, "Go-http-client"),
                .. ScriptGroup(4, ConditionOperator.Contains, "Wget"),
                .. ScriptGroup(5, ConditionOperator.Contains, "Scrapy"),
                .. ScriptGroup(6, ConditionOperator.Contains, "HttpClient")
            ])
    ];

    /// <summary>
    /// 按编码找模板
    /// </summary>
    public static AccessPolicyTemplate? Find(string code)
    {
        return All.FirstOrDefault(template => string.Equals(template.Code, code, StringComparison.OrdinalIgnoreCase));
    }

    private static AccessPolicyConditionCommand Clause(int group, string attribute, ConditionOperator @operator, string value, bool isNegated = false)
    {
        return new AccessPolicyConditionCommand(group, attribute, @operator, isNegated, ConfigDataType.String, value, null);
    }

    private static AccessPolicyConditionCommand[] ScriptGroup(int group, ConditionOperator userAgentOperator, string userAgent)
    {
        return
        [
            Clause(group, SaasConditionAttributes.EnvironmentClientUserAgent, userAgentOperator, userAgent),
            Clause(group, SaasConditionAttributes.EnvironmentClientKind, ConditionOperator.NotEquals, SaasConditionClientKinds.OpenApi)
        ];
    }
}
