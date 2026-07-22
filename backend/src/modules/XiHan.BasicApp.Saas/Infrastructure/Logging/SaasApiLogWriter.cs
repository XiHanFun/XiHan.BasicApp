// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Http;
using SqlSugar;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Security.Claims;
using XiHan.Framework.Auditing;
using XiHan.Framework.Auditing.Writers;
using XiHan.Framework.Web.Core.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Logging;

/// <summary>
/// SaaS 接口日志写入器
/// </summary>
public class SaasApiLogWriter : IApiLogWriter
{
    private readonly ISqlSugarClientResolver _clientResolver;
    private readonly ICurrentTenant _currentTenant;
    private readonly IClientInfoProvider _clientInfoProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SaasApiLogWriter(
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
    /// 写入接口日志
    /// </summary>
    public async Task WriteAsync(ApiLogRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        var now = DateTimeOffset.UtcNow;
        var clientInfo = _clientInfoProvider.GetCurrent();
        var httpContext = _httpContextAccessor.HttpContext;
        var elapsedMilliseconds = record.ElapsedMilliseconds < 0 ? 0 : record.ElapsedMilliseconds;
        var requestTime = now.AddMilliseconds(-elapsedMilliseconds);

        var sessionId = httpContext?.User?.FindFirst(XiHanClaimTypes.SessionId)?.Value;

        // 签名调用无 JWT 用户名：当已回退到凭证归属用户（record.UserId 有值）而用户名缺失时，按 UserId 补全（跨租户）
        var userName = record.UserName;
        if (string.IsNullOrWhiteSpace(userName) && record.UserId is > 0)
        {
            userName = await ResolveUserNameAsync(record.UserId.Value, cancellationToken);
        }

        var entity = new SysOpenApiLog
        {
            TenantId = _currentTenant.Id ?? 0,
            UserId = record.UserId,
            UserName = SaasLogMappingHelper.TrimOrNull(userName, 50),
            ApiName = SaasLogMappingHelper.TrimOrNull(record.ApiName, 200),
            RequestId = SaasLogMappingHelper.TrimOrNull(record.TraceId, 100),
            UserSessionId = SaasLogMappingHelper.TrimOrNull(sessionId, 100),
            TraceId = SaasLogMappingHelper.TrimOrNull(record.TraceId, 64),
            ClientId = SaasLogMappingHelper.TrimOrNull(record.ClientId, 100),
            AppId = SaasLogMappingHelper.TrimOrNull(record.AppId, 100),
            IsSignatureValid = record.IsSignatureValid,
            SignatureType = ResolveSignatureType(record.SignatureAlgorithm),
            ApiPath = SaasLogMappingHelper.TrimOrDefault(record.Path, 500, "/"),
            Method = SaasLogMappingHelper.TrimOrDefault(record.Method, 10, "GET"),
            ControllerName = SaasLogMappingHelper.TrimOrNull(record.ControllerName, 100),
            ActionName = SaasLogMappingHelper.TrimOrNull(record.ActionName, 100),
            RequestParams = SaasLogMappingHelper.TrimOrNull(record.RequestParams, 32000),
            RequestBody = SaasLogMappingHelper.TrimOrNull(record.RequestBody, 32000),
            ResponseBody = SaasLogMappingHelper.TrimOrNull(record.ResponseBody, 32000),
            StatusCode = record.StatusCode,
            RequestIp = SaasLogMappingHelper.TrimOrNull(clientInfo.IpAddress ?? record.RemoteIp, 50),
            RequestLocation = SaasLogMappingHelper.TrimOrNull(clientInfo.Location, 200),
            UserAgent = SaasLogMappingHelper.TrimOrNull(clientInfo.UserAgent ?? record.UserAgent, 500),
            Browser = SaasLogMappingHelper.TrimOrNull(clientInfo.Browser, 100),
            Referer = SaasLogMappingHelper.TrimOrNull(record.Referer, 500),
            RequestTime = requestTime,
            ResponseTime = now,
            ExecutionTime = elapsedMilliseconds,
            RequestSize = record.RequestSize < 0 ? 0 : record.RequestSize,
            ResponseSize = record.ResponseSize < 0 ? 0 : record.ResponseSize,
            IsSuccess = record.IsSuccess,
            ErrorMessage = SaasLogMappingHelper.TrimOrNull(record.ErrorMessage, 2000)
        };

        await DbClient.Insertable(entity).SplitTable().ExecuteCommandAsync();
    }

    /// <summary>
    /// 按用户主键解析用户名（跨租户，忽略租户过滤：凭证归属人可能是平台用户）；失败兜底空。
    /// </summary>
    private async Task<string?> ResolveUserNameAsync(long userId, CancellationToken cancellationToken)
    {
        try
        {
            return await DbClient.Queryable<SysUser>()
                .ClearFilter()
                .Where(user => user.BasicId == userId && !user.IsDeleted)
                .Select(user => user.UserName)
                .FirstAsync(cancellationToken);
        }
        catch
        {
            return null;
        }
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
