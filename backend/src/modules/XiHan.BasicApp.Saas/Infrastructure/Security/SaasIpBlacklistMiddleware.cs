// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Configurations;
using XiHan.BasicApp.Saas.Domain.Security;
using XiHan.Framework.Application.Contracts.Dtos;
using XiHan.Framework.Application.Contracts.Enums;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Infrastructure.Security;

/// <summary>
/// 全局 IP 黑名单：客户端地址命中平台参数 <see cref="SaasConfigKeys.Security.IpBlacklist"/> 的请求一律 403。
/// </summary>
/// <remarks>
/// 由 <see cref="SaasIpBlacklistStartupFilter"/> 挂在管线最前，接口、实时通道、静态文件与文档都在其后。
/// 此时框架的转发头中间件还没执行，客户端地址按同一份 <see cref="ForwardedHeadersOptions"/> 在请求副本上还原，
/// 只信任已登记的代理，不改动请求本身。参数只读平台那一条，经参数缓存读取，参数页保存后即失效；
/// 参数读取失败时记错误日志并放行，不因黑名单自身故障拒绝全部请求。
/// </remarks>
public sealed class SaasIpBlacklistMiddleware
{
    /// <summary>
    /// 被拒请求的说明。
    /// </summary>
    public const string ForbiddenMessage = "当前 IP 已被禁止访问。";

    private static readonly JsonSerializerOptions ResponseJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ForwardedHeadersOptions _forwardedHeadersOptions;
    private readonly ForwardedHeadersMiddleware _forwarder;
    private readonly ILogger<SaasIpBlacklistMiddleware> _logger;
    private Snapshot _snapshot = new(null, SaasIpBlacklist.Empty);

    /// <summary>
    /// 构造函数
    /// </summary>
    public SaasIpBlacklistMiddleware(
        RequestDelegate next,
        ILoggerFactory loggerFactory,
        IOptions<ForwardedHeadersOptions> forwardedHeadersOptions)
    {
        _next = next;
        _forwardedHeadersOptions = forwardedHeadersOptions.Value;
        _forwarder = new ForwardedHeadersMiddleware(static _ => Task.CompletedTask, loggerFactory, forwardedHeadersOptions);
        _logger = loggerFactory.CreateLogger<SaasIpBlacklistMiddleware>();
    }

    /// <summary>
    /// 拦截命中黑名单的请求
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var blacklist = await LoadAsync(context);
        if (blacklist.Count > 0)
        {
            var clientIp = ResolveClientIp(context);
            if (blacklist.Contains(clientIp))
            {
                _logger.LogInformation("IP 黑名单拒绝请求：{ClientIp} {Method} {Path}", clientIp, context.Request.Method, context.Request.Path);
                await WriteForbiddenAsync(context);
                return;
            }
        }

        await _next(context);
    }

    /// <summary>
    /// 按转发头配置还原客户端地址：在只带连接地址与转发头的请求副本上执行框架同一套还原逻辑
    /// </summary>
    private IPAddress? ResolveClientIp(HttpContext context)
    {
        var probe = new DefaultHttpContext();
        probe.Connection.RemoteIpAddress = context.Connection.RemoteIpAddress;
        probe.Connection.RemotePort = context.Connection.RemotePort;
        probe.Request.Scheme = context.Request.Scheme;
        probe.Request.Host = context.Request.Host;
        foreach (var header in new[]
                 {
                     _forwardedHeadersOptions.ForwardedForHeaderName,
                     _forwardedHeadersOptions.ForwardedProtoHeaderName,
                     _forwardedHeadersOptions.ForwardedHostHeaderName
                 })
        {
            if (context.Request.Headers.TryGetValue(header, out var values))
            {
                probe.Request.Headers[header] = values;
            }
        }

        _forwarder.ApplyForwarders(probe);
        return probe.Connection.RemoteIpAddress;
    }

    private async Task<SaasIpBlacklist> LoadAsync(HttpContext context)
    {
        string? raw;
        try
        {
            var services = context.RequestServices;
            var currentTenant = services.GetRequiredService<ICurrentTenant>();
            var configuration = services.GetRequiredService<ISaasConfigurationService>();
            using (currentTenant.Change(null))
            {
                raw = await configuration.GetStringAsync(SaasConfigKeys.Security.IpBlacklist, null, context.RequestAborted);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "读取 IP 黑名单失败，本次请求放行");
            return SaasIpBlacklist.Empty;
        }

        var snapshot = _snapshot;
        if (string.Equals(snapshot.Raw, raw, StringComparison.Ordinal))
        {
            return snapshot.Blacklist;
        }

        SaasIpBlacklist blacklist;
        try
        {
            blacklist = SaasIpBlacklist.FromJson(raw);
            if (blacklist.InvalidEntries.Count > 0)
            {
                _logger.LogWarning("IP 黑名单里有解析不了的条目，已忽略：{Entries}", string.Join(", ", blacklist.InvalidEntries));
            }
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogError(exception, "IP 黑名单参数值无效，按空黑名单处理");
            blacklist = SaasIpBlacklist.Empty;
        }

        _snapshot = new Snapshot(raw, blacklist);
        return blacklist;
    }

    private static async Task WriteForbiddenAsync(HttpContext context)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json; charset=utf-8";
        var payload = ApiResponse.Failure(ApiResponseCodes.Forbidden, ForbiddenMessage, context.TraceIdentifier);
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, ResponseJsonOptions), context.RequestAborted);
    }

    /// <summary>
    /// 参数原值与由它解析出的黑名单：原值不变就不重复解析
    /// </summary>
    private sealed record Snapshot(string? Raw, SaasIpBlacklist Blacklist);
}

/// <summary>
/// 把 <see cref="SaasIpBlacklistMiddleware"/> 挂到请求管线最前
/// </summary>
public sealed class SaasIpBlacklistStartupFilter : IStartupFilter
{
    /// <summary>
    /// 先挂黑名单，再交给应用配置的其余管线
    /// </summary>
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.UseMiddleware<SaasIpBlacklistMiddleware>();
            next(app);
        };
    }
}
