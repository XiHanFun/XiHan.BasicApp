#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasExceptionLogWriter
// Guid:cb6f1f6a-5d7e-4f97-8683-5f620847e5da
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/08 20:16:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using SqlSugar;
using System.Reflection;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Auditing;
using XiHan.Framework.Auditing.Writers;
using XiHan.Framework.Web.Core.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Logging;

/// <summary>
/// SaaS 异常日志写入器
/// </summary>
public class SaasExceptionLogWriter : IExceptionLogWriter
{
    private readonly ISqlSugarClientResolver _clientResolver;
    private readonly ICurrentTenant _currentTenant;
    private readonly IClientInfoProvider _clientInfoProvider;
    private readonly IWebHostEnvironment _hostingEnvironment;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SaasExceptionLogWriter(
        ISqlSugarClientResolver clientResolver,
        ICurrentTenant currentTenant,
        IClientInfoProvider clientInfoProvider,
        IWebHostEnvironment hostingEnvironment,
        IHttpContextAccessor httpContextAccessor)
    {
        _clientResolver = clientResolver;
        _currentTenant = currentTenant;
        _clientInfoProvider = clientInfoProvider;
        _hostingEnvironment = hostingEnvironment;
        _httpContextAccessor = httpContextAccessor;
    }

    private ISqlSugarClient DbClient => _clientResolver.GetCurrentClient();

    /// <summary>
    /// 写入异常日志
    /// </summary>
    public async Task WriteAsync(ExceptionLogRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        var now = DateTimeOffset.UtcNow;
        var clientInfo = _clientInfoProvider.GetCurrent();
        var sessionId = _httpContextAccessor.HttpContext?.Features.Get<ISessionFeature>()?.Session?.Id;
        var tenantId = _currentTenant.Id ?? 0;

        var entity = new SysExceptionLog
        {
            TenantId = tenantId,
            UserId = record.UserId,
            UserName = SaasLogMappingHelper.TrimOrNull(record.UserName, 50),
            RequestId = SaasLogMappingHelper.TrimOrNull(record.TraceId, 100),
            TraceId = SaasLogMappingHelper.TrimOrNull(record.TraceId, 64),
            SessionId = SaasLogMappingHelper.TrimOrNull(sessionId, 100),
            ExceptionType = SaasLogMappingHelper.TrimOrDefault(record.ExceptionType, 200, "Exception"),
            ExceptionMessage = SaasLogMappingHelper.TrimOrDefault(record.ExceptionMessage, 2000, "未捕获异常"),
            ExceptionStackTrace = SaasLogMappingHelper.TrimOrNull(record.ExceptionStackTrace, 32000),
            ExceptionSource = ClassifyExceptionSource(record.ExceptionType, record.ExceptionStackTrace),
            ExceptionLocation = SaasLogMappingHelper.TrimOrNull(BuildExceptionLocation(record), 300),
            SeverityLevel = record.StatusCode >= StatusCodes.Status500InternalServerError ? 5 : 3,
            RequestPath = SaasLogMappingHelper.TrimOrNull(record.Path, 500),
            RequestMethod = SaasLogMappingHelper.TrimOrNull(record.Method, 10),
            ControllerName = SaasLogMappingHelper.TrimOrNull(record.ControllerName, 100),
            ActionName = SaasLogMappingHelper.TrimOrNull(record.ActionName, 100),
            RequestParams = SaasLogMappingHelper.TrimOrNull(record.RequestParams, 32000),
            RequestBody = SaasLogMappingHelper.TrimOrNull(record.RequestBody, 32000),
            RequestHeaders = SaasLogMappingHelper.TrimOrNull(record.RequestHeaders, 32000),
            StatusCode = record.StatusCode,
            OperationIp = SaasLogMappingHelper.TrimOrNull(clientInfo.IpAddress ?? record.RemoteIp, 50),
            OperationLocation = SaasLogMappingHelper.TrimOrNull(clientInfo.Location, 200),
            UserAgent = SaasLogMappingHelper.TrimOrNull(clientInfo.UserAgent ?? record.UserAgent, 500),
            Browser = SaasLogMappingHelper.TrimOrNull(clientInfo.Browser, 100),
            Os = SaasLogMappingHelper.TrimOrNull(clientInfo.OperatingSystem, 100),
            DeviceType = SaasLogMappingHelper.ResolveDeviceType(clientInfo.DeviceName),
            DeviceInfo = SaasLogMappingHelper.TrimOrNull(clientInfo.DeviceName, 200),
            ApplicationName = SaasLogMappingHelper.TrimOrNull(_hostingEnvironment.ApplicationName, 100),
            ApplicationVersion = SaasLogMappingHelper.TrimOrNull(ResolveApplicationVersion(), 50),
            EnvironmentName = SaasLogMappingHelper.TrimOrNull(_hostingEnvironment.EnvironmentName, 50),
            ServerHostName = SaasLogMappingHelper.TrimOrNull(Environment.MachineName, 100),
            ThreadId = Environment.CurrentManagedThreadId,
            ProcessId = Environment.ProcessId,
            ExceptionTime = now,
            IsHandled = true,
            HandledTime = now,
            ErrorCode = SaasLogMappingHelper.TrimOrNull(record.StatusCode.ToString(), 50)
        };

        await DbClient.Insertable(entity).SplitTable().ExecuteCommandAsync();
    }

    /// <summary>
    /// 按异常类型/堆栈归类异常来源：业务异常、数据库异常、Redis 异常、第三方接口异常、消息队列异常、未捕获异常
    /// </summary>
    private static string ClassifyExceptionSource(string? exceptionType, string? stackTrace)
    {
        var type = exceptionType ?? string.Empty;
        var stack = stackTrace ?? string.Empty;

        if (ContainsAny(type, "BusinessException", "UserFriendlyException", "ValidationException", "ArgumentException", "InvalidOperationException"))
        {
            return "业务异常";
        }

        if (ContainsAny(type, "SqlSugarException", "DbException", "NpgsqlException", "SqlException", "MySqlException", "SqliteException", "PostgresException", "VersionExceptions")
            || ContainsAny(stack, "SqlSugar", "Npgsql."))
        {
            return "数据库异常";
        }

        if (ContainsAny(type, "RedisException", "RedisConnectionException", "RedisTimeoutException")
            || ContainsAny(stack, "StackExchange.Redis", "FreeRedis", "CSRedis"))
        {
            return "Redis异常";
        }

        if (ContainsAny(type, "HttpRequestException", "SocketException", "WebException")
            || ContainsAny(stack, "System.Net.Http.HttpClient"))
        {
            return "第三方接口异常";
        }

        if (ContainsAny(type, "RabbitMQ", "KafkaException", "MqttException", "BrokerUnreachableException")
            || ContainsAny(stack, "RabbitMQ.Client", "Confluent.Kafka", "MQTTnet"))
        {
            return "消息队列异常";
        }

        return "未捕获异常";
    }

    /// <summary>
    /// 文本包含任一关键字（忽略大小写）
    /// </summary>
    private static bool ContainsAny(string value, params string[] keywords)
    {
        return keywords.Any(keyword => value.Contains(keyword, StringComparison.OrdinalIgnoreCase));
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
