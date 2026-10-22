// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.AccessPolicies;

/// <summary>
/// 一次命中
/// </summary>
/// <param name="TenantId">记在哪一层：规则所属的租户，0 为平台</param>
/// <param name="Source">来源</param>
/// <param name="PolicyId">访问策略主键；权限条件为空</param>
/// <param name="PolicyCode">策略编码或权限码</param>
/// <param name="PolicyName">策略名称</param>
/// <param name="Action">动作</param>
/// <param name="Phase">求值时机</param>
/// <param name="IsBlocked">请求是否因此被拦下</param>
/// <param name="Time">命中时间</param>
/// <param name="Request">请求信息</param>
public sealed record AccessPolicyHitEntry(
    long TenantId,
    AccessPolicyHitSource Source,
    long? PolicyId,
    string PolicyCode,
    string? PolicyName,
    AccessPolicyAction Action,
    AccessPolicyPhase Phase,
    bool IsBlocked,
    DateTimeOffset Time,
    AccessPolicyRequestInfo Request);
