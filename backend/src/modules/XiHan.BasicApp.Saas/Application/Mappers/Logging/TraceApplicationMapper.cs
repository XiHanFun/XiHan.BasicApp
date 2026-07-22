// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 链路追踪时间线映射器：把各类日志实体归一化为统一的时间线条目
/// </summary>
public static class TraceApplicationMapper
{
    private const string StatusSuccess = "success";
    private const string StatusWarning = "warning";
    private const string StatusError = "error";
    private const string StatusInfo = "info";
    private const string StatusDefault = "default";

    /// <summary>
    /// 访问日志 → 时间线条目
    /// </summary>
    public static TraceTimelineItemDto FromAccess(SysAccessLog e)
    {
        ArgumentNullException.ThrowIfNull(e);
        return new TraceTimelineItemDto
        {
            LogType = TraceLogType.Access,
            BasicId = e.BasicId,
            Time = e.AccessTime,
            Title = ComposeRequest(e.Method, e.ResourcePath),
            Summary = e.ResourceName,
            Status = e.AccessResult switch
            {
                AccessResult.Success => StatusSuccess,
                AccessResult.Failed or AccessResult.ServerError => StatusError,
                AccessResult.Forbidden or AccessResult.Unauthorized or AccessResult.NotFound => StatusWarning,
                _ => StatusDefault
            },
            UserId = e.UserId,
            UserName = e.UserName,
            SessionId = e.UserSessionId,
            TraceId = e.TraceId,
            Ip = e.AccessIp,
            Location = e.AccessLocation,
            Method = e.Method,
            Path = e.ResourcePath,
            StatusCode = e.StatusCode,
            ExecutionTime = e.ExecutionTime
        };
    }

    /// <summary>
    /// 开放接口日志 → 时间线条目
    /// </summary>
    public static TraceTimelineItemDto FromApi(SysOpenApiLog e)
    {
        ArgumentNullException.ThrowIfNull(e);
        return new TraceTimelineItemDto
        {
            LogType = TraceLogType.Api,
            BasicId = e.BasicId,
            Time = e.RequestTime,
            Title = ComposeRequest(e.Method, e.ApiPath),
            Summary = e.ApiName,
            Status = e.IsSuccess && e.StatusCode < 400
                ? StatusSuccess
                : e.StatusCode >= 500 ? StatusError : StatusWarning,
            UserId = e.UserId,
            UserName = e.UserName,
            SessionId = e.UserSessionId,
            TraceId = e.TraceId,
            Ip = e.RequestIp,
            Location = e.RequestLocation,
            Method = e.Method,
            Path = e.ApiPath,
            StatusCode = e.StatusCode,
            ExecutionTime = e.ExecutionTime
        };
    }

    /// <summary>
    /// 操作日志 → 时间线条目
    /// </summary>
    public static TraceTimelineItemDto FromOperation(SysOperationLog e)
    {
        ArgumentNullException.ThrowIfNull(e);
        return new TraceTimelineItemDto
        {
            LogType = TraceLogType.Operation,
            BasicId = e.BasicId,
            Time = e.OperationTime,
            Title = FirstNonEmpty(e.Title, JoinNonEmpty("/", e.Module, e.Function)) ?? e.OperationType.ToString(),
            Summary = e.Description,
            Status = e.Result switch
            {
                OperationExecuteResult.Success => StatusSuccess,
                OperationExecuteResult.PartialSuccess => StatusWarning,
                _ => StatusError
            },
            UserId = e.UserId,
            UserName = e.UserName,
            SessionId = e.UserSessionId,
            TraceId = e.TraceId,
            Ip = e.OperationIp,
            Location = e.OperationLocation,
            Method = e.Method,
            Path = e.RequestUrl,
            ExecutionTime = e.ExecutionTime
        };
    }

    /// <summary>
    /// 登录日志 → 时间线条目
    /// </summary>
    public static TraceTimelineItemDto FromLogin(SysLoginLog e)
    {
        ArgumentNullException.ThrowIfNull(e);
        return new TraceTimelineItemDto
        {
            LogType = TraceLogType.Login,
            BasicId = e.BasicId,
            Time = e.LoginTime,
            Title = e.LoginResult.ToString(),
            Summary = e.Message,
            Status = e.LoginResult switch
            {
                LoginResult.Success => StatusSuccess,
                LoginResult.Logout or LoginResult.TokenRefreshed or LoginResult.PasswordChanged
                    or LoginResult.PasswordReset or LoginResult.MfaBound or LoginResult.MfaUnbound
                    or LoginResult.TenantSwitched => StatusInfo,
                LoginResult.SessionRevoked => StatusWarning,
                _ => StatusError
            },
            UserId = e.UserId,
            UserName = e.UserName,
            SessionId = e.SessionId,
            TraceId = e.TraceId,
            Ip = e.LoginIp,
            Location = e.LoginLocation
        };
    }

    /// <summary>
    /// 异常日志 → 时间线条目
    /// </summary>
    public static TraceTimelineItemDto FromException(SysExceptionLog e)
    {
        ArgumentNullException.ThrowIfNull(e);
        return new TraceTimelineItemDto
        {
            LogType = TraceLogType.Exception,
            BasicId = e.BasicId,
            Time = e.ExceptionTime,
            Title = e.ExceptionType,
            Summary = Truncate(e.ExceptionMessage, 200),
            Status = e.SeverityLevel >= 4 ? StatusError : StatusWarning,
            UserId = e.UserId,
            UserName = e.UserName,
            SessionId = e.SessionId,
            TraceId = e.TraceId,
            Ip = e.OperationIp,
            Location = e.OperationLocation,
            Method = e.RequestMethod,
            Path = e.RequestPath,
            StatusCode = e.StatusCode
        };
    }

    /// <summary>
    /// 数据变更日志 → 时间线条目
    /// </summary>
    public static TraceTimelineItemDto FromDiff(SysDiffLog e)
    {
        ArgumentNullException.ThrowIfNull(e);
        return new TraceTimelineItemDto
        {
            LogType = TraceLogType.Diff,
            BasicId = e.BasicId,
            Time = e.AuditTime,
            Title = JoinNonEmpty(" ", e.OperationType.ToString(), FirstNonEmpty(e.EntityName, e.TableName)),
            Summary = FirstNonEmpty(e.ChangeDescription, e.Description),
            Status = !e.IsSuccess
                ? StatusError
                : e.RiskLevel >= AuditRiskLevel.High ? StatusWarning : StatusSuccess,
            UserId = e.UserId,
            UserName = e.UserName,
            SessionId = e.SessionId,
            TraceId = e.TraceId,
            Ip = e.OperationIp,
            ExecutionTime = e.ExecutionTime
        };
    }

    /// <summary>
    /// 权限变更日志 → 时间线条目（无用户名/会话维度）
    /// </summary>
    public static TraceTimelineItemDto FromPermissionChange(SysPermissionChangeLog e)
    {
        ArgumentNullException.ThrowIfNull(e);
        return new TraceTimelineItemDto
        {
            LogType = TraceLogType.PermissionChange,
            BasicId = e.BasicId,
            Time = e.ChangeTime,
            Title = e.ChangeType.ToString(),
            Summary = FirstNonEmpty(e.Description, e.ChangeReason),
            Status = StatusInfo,
            UserId = e.OperatorUserId,
            TraceId = e.TraceId,
            Ip = e.OperationIp
        };
    }

    /// <summary>
    /// 组合 "方法 路径" 标题
    /// </summary>
    private static string ComposeRequest(string? method, string? path)
    {
        return JoinNonEmpty(" ", method, path) ?? "-";
    }

    /// <summary>
    /// 取首个非空字符串
    /// </summary>
    private static string? FirstNonEmpty(params string?[] values)
    {
        return Array.Find(values, v => !string.IsNullOrWhiteSpace(v));
    }

    /// <summary>
    /// 用分隔符连接非空片段
    /// </summary>
    private static string? JoinNonEmpty(string separator, params string?[] values)
    {
        var parts = values.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v!.Trim()).ToArray();
        return parts.Length == 0 ? null : string.Join(separator, parts);
    }

    /// <summary>
    /// 截断过长文本
    /// </summary>
    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return value.Length <= maxLength ? value : value[..maxLength] + "…";
    }
}
