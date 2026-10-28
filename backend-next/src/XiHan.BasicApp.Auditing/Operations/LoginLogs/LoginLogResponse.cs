// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Auditing.Domain.Entities;
using XiHan.BasicApp.Contracts.Auditing;

namespace XiHan.BasicApp.Auditing.Operations.LoginLogs;

/// <summary>
/// 一条登录日志。
/// </summary>
/// <param name="Id">主键。</param>
/// <param name="OccurredAt">发生时间。</param>
/// <param name="TraceId">trace-id。</param>
/// <param name="UserId">用户。</param>
/// <param name="UserName">登录名。</param>
/// <param name="SessionId">会话。</param>
/// <param name="ClientIp">客户端地址。</param>
/// <param name="UserAgent">User-Agent。</param>
/// <param name="Event">事件。</param>
/// <param name="Succeeded">是否成功。</param>
/// <param name="ErrorCode">失败的错误码。</param>
/// <param name="ErrorMessage">失败的错误消息。</param>
/// <param name="AccountTenantId">账号所在的租户。</param>
/// <param name="ImpersonatorUserId">模仿事件的发起人。</param>
/// <param name="Detail">补充说明。</param>
/// <param name="Browser">浏览器。</param>
/// <param name="Os">操作系统。</param>
/// <param name="Device">设备类型。</param>
public sealed record LoginLogResponse(
    Guid Id,
    DateTimeOffset OccurredAt,
    string TraceId,
    Guid? UserId,
    string? UserName,
    Guid? SessionId,
    string? ClientIp,
    string? UserAgent,
    LoginEvent Event,
    bool Succeeded,
    string? ErrorCode,
    string? ErrorMessage,
    string? AccountTenantId,
    Guid? ImpersonatorUserId,
    string? Detail,
    string? Browser,
    string? Os,
    string? Device)
{
    /// <summary>
    /// 从实体投影。
    /// </summary>
    public static LoginLogResponse From(SysLoginLog log)
    {
        ArgumentNullException.ThrowIfNull(log);
        return new LoginLogResponse(
            log.Id,
            new DateTimeOffset(DateTime.SpecifyKind(log.OccurredAt, DateTimeKind.Utc)),
            log.TraceId,
            log.UserId,
            log.UserName,
            log.SessionId,
            log.ClientIp,
            log.UserAgent,
            log.Event,
            log.Succeeded,
            log.ErrorCode,
            log.ErrorMessage,
            log.AccountTenantId,
            log.ImpersonatorUserId,
            log.Detail,
            log.Browser,
            log.Os,
            log.Device);
    }
}

/// <summary>
/// <c>login-logs.get</c> 的请求。
/// </summary>
/// <param name="Id">日志主键。</param>
public sealed record GetLoginLogRequest(Guid Id);
