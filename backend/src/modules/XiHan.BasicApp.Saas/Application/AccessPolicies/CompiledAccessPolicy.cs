// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Authorization.Conditions;

namespace XiHan.BasicApp.Saas.Application.AccessPolicies;

/// <summary>
/// 编译好的访问策略（不可变，可跨请求共用）
/// </summary>
/// <param name="PolicyId">策略主键</param>
/// <param name="TenantId">所属层，0 为平台</param>
/// <param name="PolicyCode">策略编码</param>
/// <param name="PolicyName">策略名称</param>
/// <param name="Action">动作</param>
/// <param name="Priority">优先级</param>
/// <param name="EffectiveTime">生效时间</param>
/// <param name="ExpirationTime">失效时间</param>
/// <param name="Conditions">编译后的条件集</param>
public sealed record CompiledAccessPolicy(
    long PolicyId,
    long TenantId,
    string PolicyCode,
    string PolicyName,
    AccessPolicyAction Action,
    int Priority,
    DateTimeOffset? EffectiveTime,
    DateTimeOffset? ExpirationTime,
    CompiledConditionSet Conditions)
{
    /// <summary>
    /// 只引用认证前可取的属性
    /// </summary>
    public bool IsBeforeAuthentication { get; } = AccessPolicyPhases.IsBeforeAuthentication(Conditions.ReferencedAttributes);

    /// <summary>
    /// 在给定时刻是否在生效期内
    /// </summary>
    public bool IsEffectiveAt(DateTimeOffset now)
    {
        return (EffectiveTime is null || EffectiveTime.Value <= now)
            && (ExpirationTime is null || ExpirationTime.Value > now);
    }
}
