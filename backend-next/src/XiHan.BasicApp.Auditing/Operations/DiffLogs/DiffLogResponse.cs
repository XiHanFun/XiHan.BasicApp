// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Auditing.Domain.Entities;

namespace XiHan.BasicApp.Auditing.Operations.DiffLogs;

/// <summary>
/// 数据变更日志的列表项（不含前后值）。
/// </summary>
/// <param name="Id">主键。</param>
/// <param name="OccurredAt">发生时间。</param>
/// <param name="TraceId">trace-id。</param>
/// <param name="UserId">用户。</param>
/// <param name="UserName">登录名。</param>
/// <param name="SessionId">会话。</param>
/// <param name="ClientIp">客户端地址。</param>
/// <param name="UserAgent">User-Agent。</param>
/// <param name="OperationId">触发变更的操作 ID。</param>
/// <param name="Kind">变更种类。</param>
/// <param name="EntityType">实体类型名。</param>
/// <param name="TableName">表名。</param>
/// <param name="EntityId">主键值。</param>
/// <param name="ChangedFields">有变化的列，逗号分隔。</param>
public sealed record DiffLogResponse(
    Guid Id,
    DateTimeOffset OccurredAt,
    string TraceId,
    Guid? UserId,
    string? UserName,
    Guid? SessionId,
    string? ClientIp,
    string? UserAgent,
    string? OperationId,
    DiffKind Kind,
    string EntityType,
    string? TableName,
    string? EntityId,
    string? ChangedFields)
{
    /// <summary>
    /// 从实体投影。
    /// </summary>
    public static DiffLogResponse From(SysDiffLog log)
    {
        ArgumentNullException.ThrowIfNull(log);
        return new DiffLogResponse(
            log.Id,
            new DateTimeOffset(DateTime.SpecifyKind(log.OccurredAt, DateTimeKind.Utc)),
            log.TraceId,
            log.UserId,
            log.UserName,
            log.SessionId,
            log.ClientIp,
            log.UserAgent,
            log.OperationId,
            log.Kind,
            log.EntityType,
            log.TableName,
            log.EntityId,
            log.ChangedFields);
    }
}

/// <summary>
/// 数据变更日志详情：列表项的字段加前后值。
/// </summary>
/// <param name="Id">主键。</param>
/// <param name="OccurredAt">发生时间。</param>
/// <param name="TraceId">trace-id。</param>
/// <param name="UserId">用户。</param>
/// <param name="UserName">登录名。</param>
/// <param name="SessionId">会话。</param>
/// <param name="ClientIp">客户端地址。</param>
/// <param name="UserAgent">User-Agent。</param>
/// <param name="OperationId">触发变更的操作 ID。</param>
/// <param name="Kind">变更种类。</param>
/// <param name="EntityType">实体类型名。</param>
/// <param name="TableName">表名。</param>
/// <param name="EntityId">主键值。</param>
/// <param name="ChangedFields">有变化的列，逗号分隔。</param>
/// <param name="Before">变更前的值（JSON 对象）。</param>
/// <param name="After">变更后的值（JSON 对象）。</param>
public sealed record DiffLogDetailResponse(
    Guid Id,
    DateTimeOffset OccurredAt,
    string TraceId,
    Guid? UserId,
    string? UserName,
    Guid? SessionId,
    string? ClientIp,
    string? UserAgent,
    string? OperationId,
    DiffKind Kind,
    string EntityType,
    string? TableName,
    string? EntityId,
    string? ChangedFields,
    string? Before,
    string? After)
{
    /// <summary>
    /// 从实体投影。
    /// </summary>
    public static DiffLogDetailResponse From(SysDiffLog log)
    {
        ArgumentNullException.ThrowIfNull(log);
        return new DiffLogDetailResponse(
            log.Id,
            new DateTimeOffset(DateTime.SpecifyKind(log.OccurredAt, DateTimeKind.Utc)),
            log.TraceId,
            log.UserId,
            log.UserName,
            log.SessionId,
            log.ClientIp,
            log.UserAgent,
            log.OperationId,
            log.Kind,
            log.EntityType,
            log.TableName,
            log.EntityId,
            log.ChangedFields,
            log.Before,
            log.After);
    }
}

/// <summary>
/// <c>diff-logs.get</c> 的请求。
/// </summary>
/// <param name="Id">日志主键。</param>
public sealed record GetDiffLogRequest(Guid Id);
