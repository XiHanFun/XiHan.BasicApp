// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Auditing.Domain.Entities;
using XiHan.BasicApp.Contracts.Auditing;

namespace XiHan.BasicApp.Auditing.Operations.ConditionHits;

/// <summary>
/// 一条条件命中记录。
/// </summary>
/// <param name="Id">主键。</param>
/// <param name="OccurredAt">发生时间。</param>
/// <param name="TraceId">trace-id。</param>
/// <param name="UserId">用户。</param>
/// <param name="UserName">登录名。</param>
/// <param name="SessionId">会话。</param>
/// <param name="ClientIp">客户端地址。</param>
/// <param name="UserAgent">User-Agent。</param>
/// <param name="Source">来源。</param>
/// <param name="PolicyId">访问策略。</param>
/// <param name="PolicyCode">策略编码（权限条件为权限码）。</param>
/// <param name="PolicyName">策略名称。</param>
/// <param name="Action">规则要求的动作。</param>
/// <param name="Phase">求值时机。</param>
/// <param name="IsBlocked">请求是否因此被拦下。</param>
/// <param name="OperationId">操作 ID。</param>
/// <param name="Method">请求方法。</param>
/// <param name="Path">请求路径。</param>
/// <param name="PermissionCodes">端点要求的权限码，逗号分隔。</param>
/// <param name="HitCount">这一分钟里的命中次数。</param>
public sealed record ConditionHitResponse(
    Guid Id,
    DateTimeOffset OccurredAt,
    string TraceId,
    Guid? UserId,
    string? UserName,
    Guid? SessionId,
    string? ClientIp,
    string? UserAgent,
    ConditionHitSource Source,
    Guid? PolicyId,
    string PolicyCode,
    string? PolicyName,
    ConditionHitAction Action,
    ConditionHitPhase Phase,
    bool IsBlocked,
    string? OperationId,
    string? Method,
    string? Path,
    string? PermissionCodes,
    int HitCount)
{
    /// <summary>
    /// 从实体投影。
    /// </summary>
    public static ConditionHitResponse From(SysConditionHit log)
    {
        ArgumentNullException.ThrowIfNull(log);
        return new ConditionHitResponse(
            log.Id,
            new DateTimeOffset(DateTime.SpecifyKind(log.OccurredAt, DateTimeKind.Utc)),
            log.TraceId,
            log.UserId,
            log.UserName,
            log.SessionId,
            log.ClientIp,
            log.UserAgent,
            log.Source,
            log.PolicyId,
            log.PolicyCode,
            log.PolicyName,
            log.Action,
            log.Phase,
            log.IsBlocked,
            log.OperationId,
            log.Method,
            log.Path,
            log.PermissionCodes,
            log.HitCount);
    }
}

/// <summary>
/// <c>condition-hits.get</c> 的请求。
/// </summary>
/// <param name="Id">日志主键。</param>
public sealed record GetConditionHitRequest(Guid Id);
