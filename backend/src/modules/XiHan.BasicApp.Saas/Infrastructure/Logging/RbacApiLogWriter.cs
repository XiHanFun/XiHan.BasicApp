#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RbacApiLogWriter
// Guid:3b4c5d6e-7f80-91a2-b3c4-d5e6f7081920
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/08 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Http;
using SqlSugar;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.SplitTables;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Security.Claims;
using XiHan.Framework.Web.Api.Constants;
using XiHan.Framework.Web.Api.Logging;
using XiHan.Framework.Web.Api.Logging.Writers;
using XiHan.Framework.Web.Core.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Logging;

/// <summary>
/// RBAC 接口日志写入器
/// </summary>
public class RbacApiLogWriter : IApiLogWriter
{
    private readonly ISqlSugarDbContext _dbContext;
    private readonly ISqlSugarSplitTableExecutor _splitTableExecutor;
    private readonly ICurrentTenant _currentTenant;
    private readonly IClientInfoProvider _clientInfoProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="splitTableExecutor"></param>
    /// <param name="currentTenant"></param>
    /// <param name="clientInfoProvider"></param>
    /// <param name="httpContextAccessor"></param>
    public RbacApiLogWriter(
        ISqlSugarDbContext dbContext,
        ISqlSugarSplitTableExecutor splitTableExecutor,
        ICurrentTenant currentTenant,
        IClientInfoProvider clientInfoProvider,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _splitTableExecutor = splitTableExecutor;
        _currentTenant = currentTenant;
        _clientInfoProvider = clientInfoProvider;
        _httpContextAccessor = httpContextAccessor;
    }

    private ISqlSugarClient DbClient => _dbContext.GetClient();

    /// <summary>
    /// 写入接口日志
    /// </summary>
    /// <param name="record"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task WriteAsync(ApiLogRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        var now = DateTimeOffset.UtcNow;
        var clientInfo = _clientInfoProvider.GetCurrent();
        var httpContext = _httpContextAccessor.HttpContext;
        var elapsedMilliseconds = record.ElapsedMilliseconds < 0 ? 0 : record.ElapsedMilliseconds;
        var requestTime = now.AddMilliseconds(-elapsedMilliseconds);

        var sessionId = httpContext?.User?.FindFirst(XiHanClaimTypes.SessionId)?.Value;

        var entity = new SysApiLog
        {
            TenantId = _currentTenant.Id,
            UserId = record.UserId,
            UserName = RbacLogMappingHelper.TrimOrNull(record.UserName, 50),
            RequestId = RbacLogMappingHelper.TrimOrNull(record.TraceId, 100),
            SessionId = RbacLogMappingHelper.TrimOrNull(sessionId, 100),
            TraceId = RbacLogMappingHelper.TrimOrNull(record.TraceId, 64),
            ClientId = RbacLogMappingHelper.TrimOrNull(record.ClientId, 100),
            AppId = RbacLogMappingHelper.TrimOrNull(record.AppId, 100),
            IsSignatureValid = record.IsSignatureValid,
            SignatureType = ResolveSignatureType(record.SignatureAlgorithm),
            ApiPath = RbacLogMappingHelper.TrimOrDefault(record.Path, 500, "/"),
            Method = RbacLogMappingHelper.TrimOrDefault(record.Method, 10, "GET"),
            ControllerName = RbacLogMappingHelper.TrimOrNull(record.ControllerName, 100),
            ActionName = RbacLogMappingHelper.TrimOrNull(record.ActionName, 100),
            RequestParams = RbacLogMappingHelper.TrimOrNull(record.RequestParams, 32000),
            RequestBody = RbacLogMappingHelper.TrimOrNull(record.RequestBody, 32000),
            ResponseBody = RbacLogMappingHelper.TrimOrNull(record.ResponseBody, 32000),
            StatusCode = record.StatusCode,
            RequestIp = RbacLogMappingHelper.TrimOrNull(clientInfo.IpAddress ?? record.RemoteIp, 50),
            RequestLocation = RbacLogMappingHelper.TrimOrNull(clientInfo.Location, 200),
            UserAgent = RbacLogMappingHelper.TrimOrNull(clientInfo.UserAgent ?? record.UserAgent, 500),
            Browser = RbacLogMappingHelper.TrimOrNull(clientInfo.Browser, 100),
            Os = RbacLogMappingHelper.TrimOrNull(clientInfo.OperatingSystem, 100),
            Referer = RbacLogMappingHelper.TrimOrNull(record.Referer, 500),
            RequestTime = requestTime,
            ResponseTime = now,
            ExecutionTime = elapsedMilliseconds,
            RequestSize = record.RequestSize < 0 ? 0 : record.RequestSize,
            ResponseSize = record.ResponseSize < 0 ? 0 : record.ResponseSize,
            IsSuccess = record.IsSuccess,
            ErrorMessage = RbacLogMappingHelper.TrimOrNull(record.ErrorMessage, 2000)
        };

        await _splitTableExecutor.InsertAsync(DbClient, [entity], cancellationToken);
    }

    private static SignatureType ResolveSignatureType(string? algorithm)
    {
        if (string.IsNullOrWhiteSpace(algorithm))
        {
            return SignatureType.None;
        }

        return algorithm.ToUpperInvariant() switch
        {
            "HMACSHA256" => SignatureType.HmacSha256,
            "HMACSHA512" => SignatureType.HmacSha512,
            "RSASHA256" => SignatureType.RsaSha256,
            "RSASHA512" => SignatureType.RsaSha512,
            "SM2" => SignatureType.Sm2,
            "MD5" => SignatureType.Md5,
            _ => SignatureType.None
        };
    }
}
