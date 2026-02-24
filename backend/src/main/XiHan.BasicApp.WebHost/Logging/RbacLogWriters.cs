#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RbacLogWriters
// Guid:4cf9d518-30be-46d4-b0a3-0b1d32b8f7bc
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/12 16:36:30
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Repositories;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Web.Api.Logging;

namespace XiHan.BasicApp.WebHost.Logging;

/// <summary>
/// RBAC 访问日志写入器
/// </summary>
public class RbacAccessLogWriter(
    ISysAccessLogRepository repository,
    ICurrentUser currentUser) : IAccessLogWriter
{
    /// <inheritdoc />
    public async Task WriteAsync(AccessLogRecord record, CancellationToken cancellationToken = default)
    {
        var entity = new SysAccessLog
        {
            UserId = record.UserId ?? currentUser.UserId,
            UserName = record.UserName ?? currentUser.UserName,
            SessionId = record.SessionId,
            ResourcePath = record.Path,
            ResourceType = "Api",
            Method = record.Method,
            AccessResult = MapAccessResult(record.StatusCode),
            StatusCode = record.StatusCode,
            AccessIp = record.RemoteIp,
            UserAgent = record.UserAgent,
            Referer = record.Referer,
            ResponseTime = record.ElapsedMilliseconds,
            AccessTime = DateTimeOffset.Now,
            ErrorMessage = record.ErrorMessage,
            TenantId = currentUser.TenantId
        };

        await repository.SaveAsync(entity, cancellationToken);
    }

    private static AccessResult MapAccessResult(int statusCode)
    {
        return statusCode switch
        {
            >= 200 and < 400 => AccessResult.Success,
            401 => AccessResult.Unauthorized,
            403 => AccessResult.Forbidden,
            404 => AccessResult.NotFound,
            >= 500 => AccessResult.ServerError,
            _ => AccessResult.Failed
        };
    }
}

/// <summary>
/// RBAC 操作日志写入器
/// </summary>
public class RbacOperationLogWriter(
    ISysOperationLogRepository repository,
    ICurrentUser currentUser) : IOperationLogWriter
{
    /// <inheritdoc />
    public async Task WriteAsync(OperationLogRecord record, CancellationToken cancellationToken = default)
    {
        var entity = new SysOperationLog
        {
            UserId = record.UserId ?? currentUser.UserId,
            UserName = record.UserName ?? currentUser.UserName,
            OperationType = MapOperationType(record.Method),
            Module = record.ControllerName,
            Function = record.ActionName,
            Title = $"{record.ControllerName}.{record.ActionName}",
            Description = $"{record.Method} {record.Path}",
            Method = record.Method,
            RequestUrl = record.Path,
            RequestParams = record.RequestParams,
            ResponseResult = record.ResponseResult,
            ExecutionTime = record.ElapsedMilliseconds,
            OperationIp = record.RemoteIp,
            Status = string.IsNullOrWhiteSpace(record.ErrorMessage) && record.StatusCode < 500 ? YesOrNo.Yes : YesOrNo.No,
            ErrorMessage = record.ErrorMessage,
            OperationTime = DateTimeOffset.Now,
            TenantId = currentUser.TenantId
        };

        await repository.SaveAsync(entity, cancellationToken);
    }

    private static OperationType MapOperationType(string method)
    {
        return method.ToUpperInvariant() switch
        {
            "GET" => OperationType.Query,
            "POST" => OperationType.Create,
            "PUT" => OperationType.Update,
            "PATCH" => OperationType.Update,
            "DELETE" => OperationType.Delete,
            _ => OperationType.Other
        };
    }
}

/// <summary>
/// RBAC 异常日志写入器
/// </summary>
public class RbacExceptionLogWriter(
    ISysExceptionLogRepository repository,
    ICurrentUser currentUser) : IExceptionLogWriter
{
    /// <inheritdoc />
    public async Task WriteAsync(ExceptionLogRecord record, CancellationToken cancellationToken = default)
    {
        var entity = new SysExceptionLog
        {
            UserId = record.UserId ?? currentUser.UserId,
            UserName = record.UserName ?? currentUser.UserName,
            RequestId = record.TraceId,
            ExceptionType = record.ExceptionType,
            ExceptionMessage = record.ExceptionMessage,
            ExceptionStackTrace = record.ExceptionStackTrace,
            RequestPath = record.Path,
            RequestMethod = record.Method,
            ControllerName = record.ControllerName,
            ActionName = record.ActionName,
            RequestHeaders = record.RequestHeaders,
            RequestParams = record.RequestParams,
            StatusCode = record.StatusCode,
            OperationIp = record.RemoteIp,
            UserAgent = record.UserAgent,
            ExceptionTime = DateTimeOffset.Now,
            IsHandled = false,
            TenantId = currentUser.TenantId
        };

        await repository.SaveAsync(entity, cancellationToken);
    }
}
