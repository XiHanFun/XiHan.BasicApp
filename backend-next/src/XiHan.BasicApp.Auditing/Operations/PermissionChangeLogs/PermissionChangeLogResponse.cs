// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Auditing.Domain.Entities;
using XiHan.BasicApp.Contracts.Auditing;

namespace XiHan.BasicApp.Auditing.Operations.PermissionChangeLogs;

/// <summary>
/// 一条权限变更日志；操作人是公共列里的用户。
/// </summary>
/// <param name="Id">主键。</param>
/// <param name="OccurredAt">发生时间。</param>
/// <param name="TraceId">trace-id。</param>
/// <param name="UserId">用户。</param>
/// <param name="UserName">登录名。</param>
/// <param name="SessionId">会话。</param>
/// <param name="ClientIp">客户端地址。</param>
/// <param name="UserAgent">User-Agent。</param>
/// <param name="ChangeType">变更类型。</param>
/// <param name="RoleId">角色。</param>
/// <param name="RoleName">角色名称。</param>
/// <param name="TargetUserId">目标用户。</param>
/// <param name="TargetUserName">目标用户的登录名。</param>
/// <param name="EditionId">套餐。</param>
/// <param name="EditionCode">套餐编码。</param>
/// <param name="TargetTenantId">换套餐的租户。</param>
/// <param name="PermissionCodes">涉及的权限码，逗号分隔。</param>
/// <param name="Detail">补充说明。</param>
public sealed record PermissionChangeLogResponse(
    Guid Id,
    DateTimeOffset OccurredAt,
    string TraceId,
    Guid? UserId,
    string? UserName,
    Guid? SessionId,
    string? ClientIp,
    string? UserAgent,
    PermissionChangeType ChangeType,
    Guid? RoleId,
    string? RoleName,
    Guid? TargetUserId,
    string? TargetUserName,
    Guid? EditionId,
    string? EditionCode,
    string? TargetTenantId,
    string? PermissionCodes,
    string? Detail)
{
    /// <summary>
    /// 从实体投影。
    /// </summary>
    public static PermissionChangeLogResponse From(SysPermissionChangeLog log)
    {
        ArgumentNullException.ThrowIfNull(log);
        return new PermissionChangeLogResponse(
            log.Id,
            new DateTimeOffset(DateTime.SpecifyKind(log.OccurredAt, DateTimeKind.Utc)),
            log.TraceId,
            log.UserId,
            log.UserName,
            log.SessionId,
            log.ClientIp,
            log.UserAgent,
            log.ChangeType,
            log.RoleId,
            log.RoleName,
            log.TargetUserId,
            log.TargetUserName,
            log.EditionId,
            log.EditionCode,
            log.TargetTenantId,
            log.PermissionCodes,
            log.Detail);
    }
}

/// <summary>
/// <c>permission-change-logs.get</c> 的请求。
/// </summary>
/// <param name="Id">日志主键。</param>
public sealed record GetPermissionChangeLogRequest(Guid Id);
