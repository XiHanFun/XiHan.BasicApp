#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasOperationLogWriter
// Guid:786da5ad-8cd6-4e66-bfd1-5c5206eb2fdd
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/08 20:12:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using SqlSugar;
using System.Security.Claims;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Security.Claims;
using XiHan.Framework.Web.Api.Constants;
using XiHan.Framework.Web.Api.Logging;
using XiHan.Framework.Web.Api.Logging.Writers;
using XiHan.Framework.Web.Core.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Logging;

/// <summary>
/// SaaS 操作日志写入器
/// </summary>
public class SaasOperationLogWriter : IOperationLogWriter
{
    private readonly ISqlSugarClientResolver _clientResolver;
    private readonly ICurrentTenant _currentTenant;
    private readonly IClientInfoProvider _clientInfoProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SaasOperationLogWriter(
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
    /// 认证类动作集合：登录/登出/令牌等认证行为由登录日志承担审计，不重复落操作日志
    /// </summary>
    private static readonly HashSet<string> AuthAuditActions = new(StringComparer.OrdinalIgnoreCase)
    {
        "Login", "Logout", "EmailLogin", "EmailLoginCode", "PhoneLogin", "PhoneLoginCode",
        "RefreshToken", "Register", "PasswordResetRequest", "LoginConfig", "SwitchTenant"
    };

    /// <summary>
    /// 写入操作日志
    /// </summary>
    public async Task WriteAsync(OperationLogRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        // 操作日志只记录业务行为：查询类动作不记录；认证类动作由登录日志负责审计
        var operationType = SaasLogMappingHelper.ResolveOperationTypeByAction(record.ActionName, record.Method);
        if (operationType == OperationType.Query)
        {
            return;
        }

        if (string.Equals(record.ControllerName, "Auth", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(record.ActionName)
            && AuthAuditActions.Contains(record.ActionName))
        {
            return;
        }

        var clientInfo = _clientInfoProvider.GetCurrent();
        var elapsedMilliseconds = SaasLogMappingHelper.NormalizeElapsed(record.ElapsedMilliseconds);
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
        var tenantId = _currentTenant.Id ?? 0;

        var entity = new SysOperationLog
        {
            TenantId = tenantId,
            UserId = record.UserId,
            UserName = SaasLogMappingHelper.TrimOrNull(record.UserName, 50),
            TraceId = SaasLogMappingHelper.TrimOrNull(traceId, 64),
            UserSessionId = SaasLogMappingHelper.TrimOrNull(sessionId, 100),
            OperationType = operationType,
            Module = SaasLogMappingHelper.TrimOrNull(record.ControllerName, 50),
            Function = SaasLogMappingHelper.TrimOrNull(record.ActionName, 50),
            Title = SaasLogMappingHelper.TrimOrNull(title, 200),
            Description = SaasLogMappingHelper.TrimOrNull(description, 500),
            Method = SaasLogMappingHelper.TrimOrNull(record.Method, 10),
            RequestUrl = SaasLogMappingHelper.TrimOrNull(record.Path, 500),
            ExecutionTime = elapsedMilliseconds,
            OperationIp = SaasLogMappingHelper.TrimOrNull(clientInfo.IpAddress ?? record.RemoteIp, 50),
            OperationLocation = SaasLogMappingHelper.TrimOrNull(clientInfo.Location, 200),
            Browser = SaasLogMappingHelper.TrimOrNull(clientInfo.Browser, 100),
            Os = SaasLogMappingHelper.TrimOrNull(clientInfo.OperatingSystem, 100),
            UserAgent = SaasLogMappingHelper.TrimOrNull(clientInfo.UserAgent ?? record.UserAgent, 500),
            Result = SaasLogMappingHelper.ResolveResult(record.StatusCode, record.ErrorMessage),
            ErrorMessage = SaasLogMappingHelper.TrimOrNull(record.ErrorMessage, 1000),
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
}
