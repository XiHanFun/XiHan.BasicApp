// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Conditions;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.AccessPolicies;

/// <summary>
/// 权限条件的拒绝记进命中记录：<see cref="PermissionConditionMode.Enforce"/> 记作拒绝并拦下，<see cref="PermissionConditionMode.LogOnly"/> 记作只记录
/// </summary>
public sealed class AccessPolicyPermissionConditionHitRecorder : IPermissionConditionHitRecorder
{
    private readonly IAccessPolicyHitRecorder _recorder;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AccessPolicyPermissionConditionHitRecorder(IAccessPolicyHitRecorder recorder, TimeProvider timeProvider)
    {
        _recorder = recorder;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public Task RecordAsync(PermissionConditionHit hit, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(hit);
        var enforced = hit.Mode == PermissionConditionMode.Enforce;
        _recorder.Record(new AccessPolicyHitEntry(
            hit.TenantId,
            AccessPolicyHitSource.PermissionCondition,
            null,
            hit.PermissionCode,
            null,
            enforced ? AccessPolicyAction.Deny : AccessPolicyAction.LogOnly,
            AccessPolicyPhase.Authorization,
            enforced,
            _timeProvider.GetUtcNow(),
            new AccessPolicyRequestInfo(hit.TraceId, hit.Ip, hit.UserId, null, hit.Method, hit.Path, hit.PermissionCode, hit.UserAgent, null)));
        return Task.CompletedTask;
    }
}
