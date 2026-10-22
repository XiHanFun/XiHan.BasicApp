// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Http;
using XiHan.BasicApp.Saas.Application.Conditions;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Web.Api.Constants;

namespace XiHan.BasicApp.Saas.Application.AccessPolicies;

/// <summary>
/// 从请求里取命中记录要的信息，以及把裁决转成命中
/// </summary>
public static class AccessPolicyRequests
{
    /// <summary>
    /// <see cref="HttpContext.Items"/> 键：认证前已命中平台的放行策略（值为 <see langword="true"/>）
    /// </summary>
    public const string PlatformAllowedItemKey = "__XiHanAccessPolicyPlatformAllowed";

    /// <summary>
    /// 自动封禁拦下请求时命中记录里的策略编码
    /// </summary>
    public const string AutoBanPolicyCode = "login-auto-ban";

    /// <summary>
    /// 自动封禁拦下请求时命中记录里的策略名称
    /// </summary>
    public const string AutoBanPolicyName = "登录失败自动封禁";

    /// <summary>
    /// 认证前是否已命中平台的放行策略
    /// </summary>
    public static bool IsPlatformAllowed(HttpContext? httpContext)
    {
        return httpContext?.Items[PlatformAllowedItemKey] is true;
    }

    /// <summary>
    /// 规范化客户端 IP：IPv4 映射地址还原成 IPv4
    /// </summary>
    public static string? FormatIp(IPAddress? address)
    {
        return address is null ? null : (address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address).ToString();
    }

    /// <summary>
    /// 链路追踪 ID，与其他日志同口径：先取当前 <see cref="Activity"/> 的 TraceId，其次框架追踪中间件写下的值，最后 <see cref="HttpContext.TraceIdentifier"/>
    /// </summary>
    public static string? GetTraceId(HttpContext? httpContext)
    {
        if (httpContext is null)
        {
            return null;
        }

        var activity = Activity.Current;
        return activity is not null && activity.TraceId != default
            ? activity.TraceId.ToHexString()
            : httpContext.Items[XiHanWebApiConstants.TraceIdItemKey]?.ToString() ?? httpContext.TraceIdentifier;
    }

    /// <summary>
    /// 请求信息
    /// </summary>
    /// <param name="httpContext">请求</param>
    /// <param name="ip">客户端 IP（认证前由调用方按受信代理还原后传入）</param>
    /// <param name="userId">用户主键</param>
    /// <param name="userName">用户名</param>
    /// <param name="withClient">是否取端点权限码与客户端类型（认证、路由之后才有）</param>
    public static AccessPolicyRequestInfo Describe(HttpContext? httpContext, IPAddress? ip, long? userId, string? userName, bool withClient)
    {
        var permissionCodes = withClient
            ? httpContext?.GetEndpoint()?.Metadata.GetOrderedMetadata<PermissionAuthorizeAttribute>()
                .Select(attribute => attribute.PermissionCode)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : null;
        return new AccessPolicyRequestInfo(
            GetTraceId(httpContext),
            FormatIp(ip ?? httpContext?.Connection.RemoteIpAddress),
            userId,
            userName,
            httpContext?.Request.Method,
            httpContext?.Request.Path.Value,
            permissionCodes is { Length: > 0 } ? string.Join(',', permissionCodes) : null,
            httpContext?.Request.Headers.UserAgent.ToString() is { Length: > 0 } userAgent ? userAgent : null,
            withClient ? SaasConditionClient.Resolve(httpContext)?.Kind : null);
    }

    /// <summary>
    /// 把裁决里命中的策略记下
    /// </summary>
    public static void Record(IAccessPolicyHitRecorder recorder, AccessPolicyDecision decision, AccessPolicyPhase phase, DateTimeOffset now, AccessPolicyRequestInfo request)
    {
        ArgumentNullException.ThrowIfNull(recorder);
        ArgumentNullException.ThrowIfNull(decision);
        foreach (var policy in decision.Matched)
        {
            recorder.Record(new AccessPolicyHitEntry(
                policy.TenantId,
                AccessPolicyHitSource.AccessPolicy,
                policy.PolicyId,
                policy.PolicyCode,
                policy.PolicyName,
                policy.Action,
                phase,
                decision.IsBlocking(policy),
                now,
                request));
        }
    }
}
