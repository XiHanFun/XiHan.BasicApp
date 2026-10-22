// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Conditions;

namespace XiHan.BasicApp.Saas.Application.AccessPolicies;

/// <summary>
/// 属性的求值时机：认证前只有请求本身的属性，其余属性要等认证、租户解析与路由之后
/// </summary>
public static class AccessPolicyPhases
{
    private static readonly HashSet<string> BeforeAuthenticationAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        SaasConditionAttributes.EnvironmentIp,
        SaasConditionAttributes.EnvironmentGeoCountry,
        SaasConditionAttributes.EnvironmentGeoProvince,
        SaasConditionAttributes.EnvironmentGeoCity,
        SaasConditionAttributes.EnvironmentGeoIsp,
        SaasConditionAttributes.EnvironmentLocation,
        SaasConditionAttributes.EnvironmentTime,
        SaasConditionAttributes.EnvironmentHour,
        SaasConditionAttributes.EnvironmentWeekday,
        SaasConditionAttributes.EnvironmentDate,
        SaasConditionAttributes.EnvironmentClientUserAgent,
        SaasConditionAttributes.ResourcePath,
        SaasConditionAttributes.ResourceMethod
    };

    /// <summary>
    /// 属性（规范名）在认证前是否可取
    /// </summary>
    public static bool IsAvailableBeforeAuthentication(string canonicalName)
    {
        ArgumentNullException.ThrowIfNull(canonicalName);
        return BeforeAuthenticationAttributes.Contains(canonicalName);
    }

    /// <summary>
    /// 引用的属性是否全部在认证前可取
    /// </summary>
    public static bool IsBeforeAuthentication(IEnumerable<string> referencedAttributes)
    {
        ArgumentNullException.ThrowIfNull(referencedAttributes);
        return referencedAttributes.All(IsAvailableBeforeAuthentication);
    }
}
