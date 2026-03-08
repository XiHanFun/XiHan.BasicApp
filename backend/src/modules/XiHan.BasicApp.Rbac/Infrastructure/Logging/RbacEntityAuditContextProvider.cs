#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RbacEntityAuditContextProvider
// Guid:4f1914a2-0b03-4f00-884e-7d8f75ef7338
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/08 20:20:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Http;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Data.Auditing;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Web.Api.Constants;
using XiHan.Framework.Web.Api.Contexts;

namespace XiHan.BasicApp.Rbac.Infrastructure.Logging;

/// <summary>
/// RBAC 实体审计上下文提供器
/// </summary>
public class RbacEntityAuditContextProvider : IEntityAuditContextProvider
{
    private static readonly HashSet<string> ExcludedEntityNames = new(StringComparer.Ordinal)
    {
        nameof(SysAccessLog),
        nameof(SysApiLog),
        nameof(SysAuditLog),
        nameof(SysExceptionLog),
        nameof(SysLoginLog),
        nameof(SysOperationLog),
        nameof(SysReviewLog),
        nameof(SysTaskLog)
    };

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IRequestContextAccessor _requestContextAccessor;
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentTenant _currentTenant;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="httpContextAccessor"></param>
    /// <param name="requestContextAccessor"></param>
    /// <param name="currentUser"></param>
    /// <param name="currentTenant"></param>
    public RbacEntityAuditContextProvider(
        IHttpContextAccessor httpContextAccessor,
        IRequestContextAccessor requestContextAccessor,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant)
    {
        _httpContextAccessor = httpContextAccessor;
        _requestContextAccessor = requestContextAccessor;
        _currentUser = currentUser;
        _currentTenant = currentTenant;
    }

    /// <summary>
    /// 创建基础审计记录
    /// </summary>
    /// <returns></returns>
    public EntityAuditLogRecord CreateBaseRecord()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var requestContext = _requestContextAccessor.Current;
        var requestId = ResolveRequestId(requestContext, httpContext);

        return new EntityAuditLogRecord
        {
            AuditType = "EntityChange",
            RequestPath = RbacLogMappingHelper.TrimOrNull(requestContext?.Path ?? httpContext?.Request.Path.ToString(), 500),
            RequestMethod = RbacLogMappingHelper.TrimOrNull(requestContext?.Method ?? httpContext?.Request.Method, 10),
            OperationIp = RbacLogMappingHelper.TrimOrNull(requestContext?.RemoteIp ?? httpContext?.Connection.RemoteIpAddress?.ToString(), 50),
            RequestId = requestId,
            UserId = requestContext?.UserId ?? _currentUser.UserId,
            UserName = RbacLogMappingHelper.TrimOrNull(requestContext?.UserName ?? _currentUser.UserName, 50),
            TenantId = requestContext?.TenantId ?? _currentTenant.Id ?? _currentUser.TenantId
        };
    }

    /// <summary>
    /// 是否应审计指定实体
    /// </summary>
    /// <param name="entityType"></param>
    /// <returns></returns>
    public bool ShouldAudit(Type entityType)
    {
        ArgumentNullException.ThrowIfNull(entityType);

        if (entityType.Namespace is null ||
            !entityType.Namespace.StartsWith("XiHan.BasicApp.Rbac.Domain.Entities", StringComparison.Ordinal))
        {
            return false;
        }

        return !ExcludedEntityNames.Contains(entityType.Name);
    }

    private static string? ResolveRequestId(RequestContext? requestContext, HttpContext? httpContext)
    {
        var traceId = requestContext?.TraceId
            ?? httpContext?.Items[XiHanWebApiConstants.TraceIdItemKey]?.ToString()
            ?? httpContext?.TraceIdentifier;
        return RbacLogMappingHelper.TrimOrNull(traceId, 100);
    }
}
