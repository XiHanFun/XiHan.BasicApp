// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using XiHan.BasicApp.Saas.Application.AccessPolicies;
using XiHan.BasicApp.Saas.Application.Conditions;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Application.Contracts.Dtos;
using XiHan.Framework.Application.Contracts.Enums;

namespace XiHan.BasicApp.Saas.Infrastructure.Security;

/// <summary>
/// 访问策略认证前一段：管线最前，按平台级、只看请求本身属性的策略放行或拒绝，并拦下自动封禁中的 IP
/// </summary>
/// <remarks>
/// 由 <see cref="AccessPolicyStartupFilter"/> 挂在管线最前，接口、实时通道、静态文件与文档都在其后。
/// 此时框架的转发头中间件还没执行，客户端地址按同一份 <see cref="ForwardedHeadersOptions"/> 在请求副本上还原，不改动请求本身。
/// 命中平台的放行策略时记在 <see cref="AccessPolicyRequests.PlatformAllowedItemKey"/>，后面的平台拒绝与封禁都不再拦它。
/// 求值本身出错时记错误日志并放行。
/// </remarks>
public sealed class AccessPolicyMiddleware
{
    /// <summary>
    /// 策略拒绝的说明
    /// </summary>
    public const string ForbiddenMessage = "访问被安全策略拒绝。";

    /// <summary>
    /// 封禁中的说明
    /// </summary>
    public const string BannedMessage = "登录失败次数过多，当前 IP 已被暂时封禁。";

    private static readonly JsonSerializerOptions ResponseJsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan DisabledWarningInterval = TimeSpan.FromMinutes(10);

    private readonly RequestDelegate _next;
    private readonly ForwardedHeadersOptions _forwardedHeadersOptions;
    private readonly ForwardedHeadersMiddleware _forwarder;
    private readonly IOptionsMonitor<AccessPolicyOptions> _options;
    private readonly IAccessPolicySnapshotProvider _snapshots;
    private readonly AccessPolicyEvaluator _evaluator;
    private readonly IAccessPolicyHitRecorder _hits;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AccessPolicyMiddleware> _logger;
    private long _lastDisabledWarningTicks;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AccessPolicyMiddleware(
        RequestDelegate next,
        ILoggerFactory loggerFactory,
        IOptions<ForwardedHeadersOptions> forwardedHeadersOptions,
        IOptionsMonitor<AccessPolicyOptions> options,
        IAccessPolicySnapshotProvider snapshots,
        AccessPolicyEvaluator evaluator,
        IAccessPolicyHitRecorder hits,
        TimeProvider timeProvider)
    {
        _next = next;
        _forwardedHeadersOptions = forwardedHeadersOptions.Value;
        _forwarder = new ForwardedHeadersMiddleware(static _ => Task.CompletedTask, loggerFactory, forwardedHeadersOptions);
        _options = options;
        _snapshots = snapshots;
        _evaluator = evaluator;
        _hits = hits;
        _timeProvider = timeProvider;
        _logger = loggerFactory.CreateLogger<AccessPolicyMiddleware>();
    }

    /// <summary>
    /// 求值并拦下被拒绝或封禁中的请求
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.CurrentValue.Enabled)
        {
            WarnDisabled();
            await _next(context);
            return;
        }

        var rejection = await EvaluateAsync(context);
        if (rejection is not null)
        {
            await WriteAsync(context, rejection);
            return;
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

    private async Task<Rejection?> EvaluateAsync(HttpContext context)
    {
        try
        {
            var ip = ResolveClientIp(context);
            var now = _timeProvider.GetUtcNow();
            var snapshot = await _snapshots.GetAsync(context.RequestAborted);
            var platform = AccessPolicyLayerResult.None;
            if (snapshot.PlatformBeforeAuthentication.Count > 0)
            {
                var source = context.RequestServices.GetRequiredService<ISaasConditionAttributeSourceFactory>().Create(new SaasConditionContext
                {
                    TenantId = 0,
                    Now = now,
                    HttpContext = context,
                    RemoteIpAddress = ip
                });
                platform = await _evaluator.EvaluateAsync(snapshot.PlatformBeforeAuthentication, source, now, context.RequestAborted);
            }

            if (platform.Allow is not null)
            {
                context.Items[AccessPolicyRequests.PlatformAllowedItemKey] = true;
            }

            var request = AccessPolicyRequests.Describe(context, ip, null, null, withClient: false);
            var decision = AccessPolicyDecision.Combine(platform, platformAllowed: false, tenant: null);
            AccessPolicyRequests.Record(_hits, decision, AccessPolicyPhase.BeforeAuthentication, now, request);
            if (decision.IsBlocked)
            {
                return Rejection.Forbidden;
            }

            if (platform.Allow is not null || request.Ip is null)
            {
                return null;
            }

            var until = await context.RequestServices.GetRequiredService<ILoginBanService>().GetBanAsync(request.Ip, context.RequestAborted);
            if (until is not { } bannedUntil)
            {
                return null;
            }

            _hits.Record(new AccessPolicyHitEntry(
                0,
                AccessPolicyHitSource.AccessPolicy,
                null,
                AccessPolicyRequests.AutoBanPolicyCode,
                AccessPolicyRequests.AutoBanPolicyName,
                AccessPolicyAction.Deny,
                AccessPolicyPhase.BeforeAuthentication,
                true,
                now,
                request));
            return Rejection.Banned(bannedUntil - now);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "访问策略求值失败，本次请求放行");
            return null;
        }
    }

    private void WarnDisabled()
    {
        var now = _timeProvider.GetUtcNow().UtcTicks;
        var last = Interlocked.Read(ref _lastDisabledWarningTicks);
        if (now - last >= DisabledWarningInterval.Ticks && Interlocked.CompareExchange(ref _lastDisabledWarningTicks, now, last) == last)
        {
            _logger.LogWarning("访问策略总开关 {Section}:Enabled 已关闭，认证前、认证后与登录时的访问策略和自动封禁都不拦截", AccessPolicyOptions.SectionName);
        }
    }

    private static async Task WriteAsync(HttpContext context, Rejection rejection)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = (int)rejection.Code;
        context.Response.ContentType = "application/json; charset=utf-8";
        if (rejection.RetryAfter is { } retryAfter)
        {
            context.Response.Headers.RetryAfter = Math.Max(1, (long)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        }

        var payload = ApiResponse.Failure(rejection.Code, rejection.Message, AccessPolicyRequests.GetTraceId(context));
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, ResponseJsonOptions), context.RequestAborted);
    }

    /// <summary>
    /// 拦下请求时的响应
    /// </summary>
    private sealed record Rejection(ApiResponseCodes Code, string Message, TimeSpan? RetryAfter)
    {
        public static Rejection Forbidden { get; } = new(ApiResponseCodes.Forbidden, ForbiddenMessage, null);

        public static Rejection Banned(TimeSpan remaining)
        {
            return new Rejection(ApiResponseCodes.TooManyRequests, BannedMessage, remaining);
        }
    }
}

/// <summary>
/// 把 <see cref="AccessPolicyMiddleware"/> 挂到请求管线最前
/// </summary>
public sealed class AccessPolicyStartupFilter : IStartupFilter
{
    /// <summary>
    /// 先挂访问策略，再交给应用配置的其余管线
    /// </summary>
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.UseMiddleware<AccessPolicyMiddleware>();
            next(app);
        };
    }
}
