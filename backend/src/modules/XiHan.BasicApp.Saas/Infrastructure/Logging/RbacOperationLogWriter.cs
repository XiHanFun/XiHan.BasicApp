#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RbacOperationLogWriter
// Guid:786da5ad-8cd6-4e66-bfd1-5c5206eb2fdd
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/08 20:12:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System.Security.Claims;
using SqlSugar;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Clients;

using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Security.Claims;
using XiHan.Framework.Web.Api.Constants;
using XiHan.Framework.Web.Api.Logging;
using XiHan.Framework.Web.Api.Logging.Writers;
using XiHan.Framework.Web.Core.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Logging;

/// <summary>
/// RBAC 操作日志写入器
/// </summary>
public class RbacOperationLogWriter : IOperationLogWriter
{
    private readonly ISqlSugarClientResolver _clientResolver;
    private readonly ICurrentTenant _currentTenant;
    private readonly IClientInfoProvider _clientInfoProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="clientResolver"></param>
    /// <param name="currentTenant"></param>
    /// <param name="clientInfoProvider"></param>
    /// <param name="httpContextAccessor"></param>
    public RbacOperationLogWriter(
        ISqlSugarClientResolver clientResolver,
        ICurrentTenant currentTenant,
        IClientInfoProvider clientInfoProvider,
        IHttpContextAccessor httpContextAccessor)
    {
        _clientResolver = clientResolver;
        _currentTenant = currentTenant;
        _clientInfoProvider = clientInfoProvider;
        _httpContextAccessor = httpContextAccessor;
    }

    private ISqlSugarClient DbClient => _clientResolver.GetCurrentClient();

    /// <summary>
    /// 写入操作日志
    /// </summary>
    /// <param name="record"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task WriteAsync(OperationLogRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        var clientInfo = _clientInfoProvider.GetCurrent();
        var operationType = RbacLogMappingHelper.ResolveOperationTypeByHttpMethod(record.Method);
        var elapsedMilliseconds = RbacLogMappingHelper.NormalizeElapsed(record.ElapsedMilliseconds);
        var title = BuildTitle(record);
        var description = BuildDescription(record);

        var httpContext = _httpContextAccessor.HttpContext;
        var traceId = record.TraceId;
        if (string.IsNullOrWhiteSpace(traceId))
        {
            traceId = httpContext?.Items[XiHanWebApiConstants.TraceIdItemKey]?.ToString()
                      ?? httpContext?.TraceIdentifier;
        }

        var sessionId = ResolveSessionId(httpContext, record);

        var entity = new SysOperationLog
        {
            TenantId = _currentTenant.Id.Value,
            UserId = record.UserId,
            UserName = RbacLogMappingHelper.TrimOrNull(record.UserName, 50),
            TraceId = RbacLogMappingHelper.TrimOrNull(traceId, 64),
            SessionId = RbacLogMappingHelper.TrimOrNull(sessionId, 100),
            OperationType = operationType,
            Module = RbacLogMappingHelper.TrimOrNull(record.ControllerName, 50),
            Function = RbacLogMappingHelper.TrimOrNull(record.ActionName, 50),
            Title = RbacLogMappingHelper.TrimOrNull(title, 200),
            Description = RbacLogMappingHelper.TrimOrNull(description, 500),
            Method = RbacLogMappingHelper.TrimOrNull(record.Method, 10),
            RequestUrl = RbacLogMappingHelper.TrimOrNull(record.Path, 500),
            RequestParams = RbacLogMappingHelper.TrimOrNull(record.RequestParams, 16000),
            ResponseResult = RbacLogMappingHelper.TrimOrNull(record.ResponseResult, 16000),
            ExecutionTime = elapsedMilliseconds,
            OperationIp = RbacLogMappingHelper.TrimOrNull(clientInfo.IpAddress ?? record.RemoteIp, 50),
            OperationLocation = RbacLogMappingHelper.TrimOrNull(clientInfo.Location, 200),
            Browser = RbacLogMappingHelper.TrimOrNull(clientInfo.Browser, 100),
            Os = RbacLogMappingHelper.TrimOrNull(clientInfo.OperatingSystem, 100),
            UserAgent = RbacLogMappingHelper.TrimOrNull(clientInfo.UserAgent ?? record.UserAgent, 500),
            Status = RbacLogMappingHelper.ResolveStatus(record.StatusCode, record.ErrorMessage),
            ErrorMessage = RbacLogMappingHelper.TrimOrNull(record.ErrorMessage, 1000),
            OperationTime = DateTimeOffset.UtcNow
        };

        await DbClient.Insertable(entity).SplitTable().ExecuteCommandAsync();
    }

    private static string BuildTitle(OperationLogRecord record)
    {
        if (!string.IsNullOrWhiteSpace(record.ControllerName) && !string.IsNullOrWhiteSpace(record.ActionName))
        {
            return $"{record.ControllerName}.{record.ActionName}";
        }

        return $"{record.Method} {record.Path}";
    }

    private static string BuildDescription(OperationLogRecord record)
    {
        return $"HTTP {record.Method} {record.Path} => {record.StatusCode}";
    }

    private static string? ResolveSessionId(HttpContext? httpContext, OperationLogRecord record)
    {
        var recordSessionId = TryGetRecordSessionId(record);
        if (!string.IsNullOrWhiteSpace(recordSessionId))
        {
            return recordSessionId;
        }

        var sessionId = httpContext?.Features.Get<ISessionFeature>()?.Session?.Id;
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            return sessionId;
        }

        var user = httpContext?.User;
        return user?.FindFirstValue(XiHanClaimTypes.SessionId)
            ?? user?.FindFirstValue(ClaimTypes.Sid)
            ?? user?.FindFirstValue("jti");
    }

    private static string? TryGetRecordSessionId(OperationLogRecord record)
    {
        var sessionIdProperty = record.GetType().GetProperty(nameof(SysOperationLog.SessionId));
        if (sessionIdProperty?.PropertyType != typeof(string))
        {
            return null;
        }

        return sessionIdProperty.GetValue(record) as string;
    }
}
