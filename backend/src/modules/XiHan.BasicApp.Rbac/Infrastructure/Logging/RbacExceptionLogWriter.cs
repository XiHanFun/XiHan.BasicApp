#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RbacExceptionLogWriter
// Guid:cb6f1f6a-5d7e-4f97-8683-5f620847e5da
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/08 20:16:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using SqlSugar;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.SplitTables;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Web.Api.Logging;
using XiHan.Framework.Web.Core.Clients;

namespace XiHan.BasicApp.Rbac.Infrastructure.Logging;

/// <summary>
/// RBAC 异常日志写入器
/// </summary>
public class RbacExceptionLogWriter : IExceptionLogWriter
{
    private readonly ISqlSugarDbContext _dbContext;
    private readonly ISqlSugarSplitTableExecutor _splitTableExecutor;
    private readonly ICurrentTenant _currentTenant;
    private readonly IClientInfoProvider _clientInfoProvider;
    private readonly IWebHostEnvironment _hostingEnvironment;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private ISqlSugarClient DbClient => _dbContext.GetClient();

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="splitTableExecutor"></param>
    /// <param name="currentTenant"></param>
    /// <param name="clientInfoProvider"></param>
    /// <param name="hostingEnvironment"></param>
    /// <param name="httpContextAccessor"></param>
    public RbacExceptionLogWriter(
        ISqlSugarDbContext dbContext,
        ISqlSugarSplitTableExecutor splitTableExecutor,
        ICurrentTenant currentTenant,
        IClientInfoProvider clientInfoProvider,
        IWebHostEnvironment hostingEnvironment,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _splitTableExecutor = splitTableExecutor;
        _currentTenant = currentTenant;
        _clientInfoProvider = clientInfoProvider;
        _hostingEnvironment = hostingEnvironment;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// 写入异常日志
    /// </summary>
    /// <param name="record"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task WriteAsync(ExceptionLogRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        var now = DateTimeOffset.UtcNow;
        var clientInfo = _clientInfoProvider.GetCurrent();
        var sessionId = _httpContextAccessor.HttpContext?.Features.Get<ISessionFeature>()?.Session?.Id;
        var operationType = RbacLogMappingHelper.ResolveOperationTypeByHttpMethod(record.Method);

        var entity = new SysExceptionLog
        {
            TenantId = _currentTenant.Id,
            UserId = record.UserId,
            UserName = RbacLogMappingHelper.TrimOrNull(record.UserName, 50),
            RequestId = RbacLogMappingHelper.TrimOrNull(record.TraceId, 100),
            SessionId = RbacLogMappingHelper.TrimOrNull(sessionId, 100),
            ExceptionType = RbacLogMappingHelper.TrimOrDefault(record.ExceptionType, 200, "Exception"),
            ExceptionMessage = RbacLogMappingHelper.TrimOrDefault(record.ExceptionMessage, 2000, "未捕获异常"),
            ExceptionStackTrace = RbacLogMappingHelper.TrimOrNull(record.ExceptionStackTrace, 32000),
            ExceptionLocation = RbacLogMappingHelper.TrimOrNull(BuildExceptionLocation(record), 300),
            SeverityLevel = record.StatusCode >= StatusCodes.Status500InternalServerError ? 5 : 3,
            RequestPath = RbacLogMappingHelper.TrimOrNull(record.Path, 500),
            RequestMethod = RbacLogMappingHelper.TrimOrNull(record.Method, 10),
            ControllerName = RbacLogMappingHelper.TrimOrNull(record.ControllerName, 100),
            ActionName = RbacLogMappingHelper.TrimOrNull(record.ActionName, 100),
            RequestParams = RbacLogMappingHelper.TrimOrNull(record.RequestParams, 32000),
            RequestHeaders = RbacLogMappingHelper.TrimOrNull(record.RequestHeaders, 32000),
            StatusCode = record.StatusCode,
            OperationIp = RbacLogMappingHelper.TrimOrNull(clientInfo.IpAddress ?? record.RemoteIp, 50),
            OperationLocation = RbacLogMappingHelper.TrimOrNull(clientInfo.Location, 200),
            UserAgent = RbacLogMappingHelper.TrimOrNull(clientInfo.UserAgent ?? record.UserAgent, 500),
            Browser = RbacLogMappingHelper.TrimOrNull(clientInfo.Browser, 100),
            Os = RbacLogMappingHelper.TrimOrNull(clientInfo.OperatingSystem, 100),
            DeviceType = RbacLogMappingHelper.ResolveDeviceType(clientInfo.DeviceName),
            DeviceInfo = RbacLogMappingHelper.TrimOrNull(clientInfo.DeviceName, 200),
            ApplicationName = RbacLogMappingHelper.TrimOrNull(_hostingEnvironment.ApplicationName, 100),
            ApplicationVersion = RbacLogMappingHelper.TrimOrNull(ResolveApplicationVersion(), 50),
            EnvironmentName = RbacLogMappingHelper.TrimOrNull(_hostingEnvironment.EnvironmentName, 50),
            ServerHostName = RbacLogMappingHelper.TrimOrNull(Environment.MachineName, 100),
            ThreadId = Environment.CurrentManagedThreadId,
            ProcessId = Environment.ProcessId,
            ExceptionTime = now,
            IsHandled = true,
            HandledTime = now,
            BusinessModule = RbacLogMappingHelper.TrimOrNull(record.ControllerName, 100),
            BusinessId = RbacLogMappingHelper.TrimOrNull(record.TraceId, 100),
            BusinessType = RbacLogMappingHelper.TrimOrNull(operationType.ToString(), 50),
            ErrorCode = RbacLogMappingHelper.TrimOrNull(record.StatusCode.ToString(), 50),
            Remark = RbacLogMappingHelper.TrimOrNull(
                string.IsNullOrWhiteSpace(record.TraceId) ? null : $"TraceId:{record.TraceId}",
                500)
        };

        await _splitTableExecutor.InsertAsync(DbClient, [entity], cancellationToken);
    }

    private static string? BuildExceptionLocation(ExceptionLogRecord record)
    {
        if (!string.IsNullOrWhiteSpace(record.ControllerName) && !string.IsNullOrWhiteSpace(record.ActionName))
        {
            return $"{record.ControllerName}.{record.ActionName}";
        }

        return null;
    }

    private static string? ResolveApplicationVersion()
    {
        return Assembly.GetEntryAssembly()?.GetName().Version?.ToString();
    }
}
