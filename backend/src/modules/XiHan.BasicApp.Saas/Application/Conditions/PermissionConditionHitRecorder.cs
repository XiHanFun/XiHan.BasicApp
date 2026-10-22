// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.


namespace XiHan.BasicApp.Saas.Application.Conditions;

/// <summary>
/// 一次因权限条件拒绝（或 LogOnly 下本该拒绝）的记录
/// </summary>
/// <param name="UserId">用户主键</param>
/// <param name="TenantId">租户主键，0 为平台</param>
/// <param name="PermissionCode">权限码</param>
/// <param name="Mode">当时的生效模式</param>
/// <param name="Path">请求路径</param>
/// <param name="TraceId">追踪标识</param>
/// <param name="Method">请求方法</param>
/// <param name="Ip">客户端 IP</param>
/// <param name="UserAgent">User-Agent</param>
public sealed record PermissionConditionHit(
    long UserId,
    long TenantId,
    string PermissionCode,
    PermissionConditionMode Mode,
    string? Path,
    string? TraceId,
    string? Method = null,
    string? Ip = null,
    string? UserAgent = null);

/// <summary>
/// 权限条件拒绝记录器
/// </summary>
public interface IPermissionConditionHitRecorder
{
    /// <summary>
    /// 记一次拒绝
    /// </summary>
    Task RecordAsync(PermissionConditionHit hit, CancellationToken cancellationToken = default);
}
