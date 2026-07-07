#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasAccessLogWriter
// Guid:1a2b8f30-c377-4ff9-9488-7d6ea5f41f53
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/08 20:08:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using System.Text.Json;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Auditing;
using XiHan.Framework.Auditing.Writers;
using XiHan.Framework.Web.Core.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Logging;

/// <summary>
/// SaaS 访问日志写入器
/// </summary>
public class SaasAccessLogWriter : IAccessLogWriter
{
    private const int ExtendDataLimit = 32000;

    private readonly ISqlSugarClientResolver _clientResolver;
    private readonly ICurrentTenant _currentTenant;
    private readonly IClientInfoProvider _clientInfoProvider;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SaasAccessLogWriter(
        ISqlSugarClientResolver clientResolver,
        ICurrentTenant currentTenant,
        IClientInfoProvider clientInfoProvider)
    {
        _clientResolver = clientResolver;
        _currentTenant = currentTenant;
        _clientInfoProvider = clientInfoProvider;
    }

    private ISqlSugarClient DbClient => _clientResolver.GetCurrentClient();

    /// <summary>
    /// 写入访问日志
    /// </summary>
    public async Task WriteAsync(AccessLogRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        var now = DateTimeOffset.UtcNow;
        var elapsedMilliseconds = SaasLogMappingHelper.NormalizeElapsed(record.ElapsedMilliseconds);
        var clientInfo = _clientInfoProvider.GetCurrent();
        var accessTime = now.AddMilliseconds(-elapsedMilliseconds);

        var entity = new SysAccessLog
        {
            TenantId = _currentTenant.Id ?? 0,
            UserId = record.UserId,
            UserName = SaasLogMappingHelper.TrimOrNull(record.UserName, 50),
            UserSessionId = SaasLogMappingHelper.TrimOrNull(record.SessionId, 100),
            TraceId = SaasLogMappingHelper.TrimOrNull(record.TraceId, 64),
            ResourcePath = SaasLogMappingHelper.TrimOrDefault(record.Path, 500, "/"),
            ResourceName = SaasLogMappingHelper.TrimOrNull(record.ResourceName, 200),
            ResourceType = "HttpApi",
            Method = SaasLogMappingHelper.TrimOrNull(record.Method, 10),
            AccessResult = SaasLogMappingHelper.ResolveAccessResult(record.StatusCode),
            StatusCode = record.StatusCode,
            AccessIp = SaasLogMappingHelper.TrimOrNull(clientInfo.IpAddress ?? record.RemoteIp, 50),
            AccessLocation = SaasLogMappingHelper.TrimOrNull(clientInfo.Location, 200),
            UserAgent = SaasLogMappingHelper.TrimOrNull(clientInfo.UserAgent ?? record.UserAgent, 500),
            Browser = SaasLogMappingHelper.TrimOrNull(clientInfo.Browser, 100),
            Os = SaasLogMappingHelper.TrimOrNull(clientInfo.OperatingSystem, 100),
            Device = SaasLogMappingHelper.TrimOrNull(clientInfo.DeviceName, 50),
            Referer = SaasLogMappingHelper.TrimOrNull(record.Referer, 500),
            ExecutionTime = elapsedMilliseconds,
            AccessTime = accessTime,
            ErrorMessage = SaasLogMappingHelper.TrimOrNull(record.ErrorMessage, 1000),
            ExtendData = BuildExtendData(record)
        };

        await DbClient.Insertable(entity).SplitTable().ExecuteCommandAsync();
    }

    private static string? BuildExtendData(AccessLogRecord record)
    {
        // 访问日志记录所有 HTTP 请求的访问行为，不记录完整请求体（敏感信息防泄露），
        // 仅保留查询串（已在请求日志中间件捕获点统一脱敏）
        if (string.IsNullOrWhiteSpace(record.QueryString))
        {
            return null;
        }

        var payload = new Dictionary<string, string?>
        {
            ["query"] = record.QueryString
        };

        try
        {
            var json = JsonSerializer.Serialize(payload);
            return SaasLogMappingHelper.TrimOrNull(json, ExtendDataLimit);
        }
        catch
        {
            return null;
        }
    }
}
