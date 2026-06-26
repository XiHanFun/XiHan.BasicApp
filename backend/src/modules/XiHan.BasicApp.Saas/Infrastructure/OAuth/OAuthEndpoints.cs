#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OAuthEndpoints
// Guid:2f5e9c41-6b8a-4d72-9c3e-1a7f0b2d4e63
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/16 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Authentication.OAuth;
using XiHan.Framework.Core.DynamicProxy;

namespace XiHan.BasicApp.Saas.Infrastructure.OAuth;

/// <summary>
/// 第三方登录（OAuth）HTTP 端点。
/// </summary>
/// <remarks>
/// 仅承载 HTTP 胶水：发起授权（Challenge）+ 回调（读 ExternalCookie → 调编排 → 302 跳回前端）。
/// 业务编排（登录/绑定/建号/签发令牌）在 <see cref="IAuthAppService.ExternalLoginAsync"/>，属应用层；
/// 此处不返回数据、只发 302 跳转与操作认证方案，故置于 Web 表现层而非 AppService。
/// </remarks>
public static class OAuthEndpoints
{
    /// <summary>
    /// OAuth 临时 Cookie scheme（与框架 AddXiHanWebApiAuth 注册的名称一致）
    /// </summary>
    private const string ExternalCookieScheme = "ExternalCookie";

    /// <summary>
    /// 日志分类
    /// </summary>
    private const string LogCategory = "XiHan.BasicApp.WebHost.OAuthEndpoints";

    /// <summary>
    /// 映射第三方登录端点：/api/OAuth/ExternalLogin（发起授权）与 /api/OAuth/Callback（授权回调）。
    /// </summary>
    /// <param name="endpoints">端点路由构建器</param>
    public static IEndpointRouteBuilder MapOAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // 第三方登录：发起授权（Challenge）
        endpoints.MapGet("/api/OAuth/ExternalLogin", ExternalLoginChallengeAsync).AllowAnonymous();

        // 第三方登录：授权回调编排（换取身份 → 登录/绑定 → 跳回前端）
        endpoints.MapGet("/api/OAuth/Callback", ExternalLoginCallbackAsync).AllowAnonymous();

        return endpoints;
    }

    /// <summary>
    /// 发起第三方授权：可选携带绑定票据（bindTicket）以表达"绑定到当前用户"意图。
    /// </summary>
    private static async Task ExternalLoginChallengeAsync(
        HttpContext httpContext,
        IDistributedCache distributedCache,
        IConfiguration configuration,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger(LogCategory);
        var frontendCallback = ResolveFrontendCallbackUrl(configuration);
        var provider = httpContext.Request.Query["provider"].ToString();
        if (string.IsNullOrWhiteSpace(provider))
        {
            httpContext.Response.Redirect(AppendQuery(frontendCallback, "error", "invalid_provider"));
            return;
        }

        long? bindUserId = null;
        var bindTicket = httpContext.Request.Query["bindTicket"].ToString();
        if (!string.IsNullOrWhiteSpace(bindTicket))
        {
            // 直接读分布式缓存消费一次性票据（与签发方 CreateOAuthBindTicketAsync 共用 OAuthBindTicket.CacheKey）：
            // 本端点仅需缓存读+删，故不经过被代理的 AppService —— 匿名 Minimal-API 端点首次跨越代理接口边界
            // 会令 Castle 异步拦截器适配器无法把结果交还调用方而挂起（与回调端点取未代理目标实例同因）。
            var key = OAuthBindTicket.CacheKey(bindTicket);
            var value = await distributedCache.GetStringAsync(key, httpContext.RequestAborted);
            if (!string.IsNullOrWhiteSpace(value))
            {
                // 一次性：读取后立即删除，防重放
                await distributedCache.RemoveAsync(key, httpContext.RequestAborted);
            }

            if (string.IsNullOrWhiteSpace(value) || !long.TryParse(value, out var parsedUserId))
            {
                logger.LogWarning("OAuth: 绑定票据无效或已过期");
                httpContext.Response.Redirect(AppendQuery(frontendCallback, "bind", "ticket_invalid"));
                return;
            }
            bindUserId = parsedUserId;
        }

        // 意图与目标用户写入 OAuth state（经数据保护签名加密，随回调安全回传，天然防 CSRF）
        var properties = new AuthenticationProperties { RedirectUri = "/api/OAuth/Callback" };
        properties.Items["provider"] = provider;
        properties.Items["mode"] = bindUserId is null ? "login" : "bind";
        if (bindUserId is { } uid)
        {
            properties.Items["bindUserId"] = uid.ToString();
        }

        try
        {
            // 触发 OAuth 处理器 302 跳转到三方授权页（state 经数据保护加密，回调时校验，天然防 CSRF）
            await httpContext.ChallengeAsync(provider, properties);
        }
        catch (Exception ex)
        {
            // 发起跳转若抛异常，不把用户卡在转圈页，统一跳回前端并带错误码，异常落日志便于排查
            logger.LogError(ex, "OAuth: 发起 Challenge 失败 provider={Provider}", provider);
            httpContext.Response.Redirect(AppendQuery(frontendCallback, bindUserId is null ? "error" : "bind", "challenge_failed"));
        }
    }

    /// <summary>
    /// 第三方授权回调：读取 ExternalCookie 中的身份 → 调用编排服务 → 302 跳回前端回调页。
    /// </summary>
    private static async Task ExternalLoginCallbackAsync(
        HttpContext httpContext,
        IAuthAppService authAppService,
        IConfiguration configuration,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger(LogCategory);
        var frontendCallback = ResolveFrontendCallbackUrl(configuration);

        var authResult = await httpContext.AuthenticateAsync(ExternalCookieScheme);
        // 临时 Cookie 用后即清
        await httpContext.SignOutAsync(ExternalCookieScheme);

        if (!authResult.Succeeded || authResult.Principal is null)
        {
            httpContext.Response.Redirect(AppendQuery(frontendCallback, "error", "external_auth_failed"));
            return;
        }

        var items = authResult.Properties?.Items ?? new Dictionary<string, string?>();
        items.TryGetValue("provider", out var provider);
        items.TryGetValue("mode", out var mode);
        var isBind = string.Equals(mode, "bind", StringComparison.OrdinalIgnoreCase);

        var principal = authResult.Principal;
        var providerKey = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = principal.FindFirst(ClaimTypes.Email)?.Value;
        var displayName = principal.FindFirst(ClaimTypes.Name)?.Value;
        // 各 provider 注册时已把头像 JSON 字段统一映射到 OAuthOptions.AvatarClaimType（见框架 RegisterProvider），此处只读这一个 Claim。
        var avatar = principal.FindFirst(OAuthOptions.AvatarClaimType)?.Value;

        if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(providerKey))
        {
            httpContext.Response.Redirect(AppendQuery(frontendCallback, isBind ? "bind" : "error", "external_profile_invalid"));
            return;
        }

        long? bindUserId = null;
        if (isBind && items.TryGetValue("bindUserId", out var bindUserIdText) && long.TryParse(bindUserIdText, out var parsed))
        {
            bindUserId = parsed;
        }

        try
        {
            // 关键：取代理的真实目标实例后再调用，绕开 Castle 异步拦截器。
            // 实测从匿名 Minimal-API 端点首次跨越被代理的 AppService 接口边界时，方法体能跑完
            // （绑定已写库），但拦截器适配器无法把结果交还给 await 的调用方，请求被永久挂起。
            // ExternalLoginAsync 本就标了 [UnitOfWork(IsDisabled=true)]（无论是否走代理都不开事务），
            // 故直接调用目标实例行为完全一致，只是不再经过会死锁的代理机器。
            var orchestrator = ProxyHelper.UnProxy(authAppService) as IAuthAppService ?? authAppService;
            var result = await orchestrator.ExternalLoginAsync(
                new ExternalLoginCommand(provider!, providerKey!, displayName, email, avatar, isBind, bindUserId),
                httpContext.RequestAborted);

            if (isBind)
            {
                var bindRedirect = result.Success
                    ? AppendQuery(AppendQuery(frontendCallback, "bind", "success"), "provider", provider!)
                    : AppendQuery(frontendCallback, "bind", result.ErrorCode ?? "failed");
                httpContext.Response.Redirect(bindRedirect);
                return;
            }

            if (result is { Success: true, Token: { } token })
            {
                var loginRedirect = AppendQuery(frontendCallback, "accessToken", token.AccessToken);
                loginRedirect = AppendQuery(loginRedirect, "refreshToken", token.RefreshToken);
                loginRedirect = AppendQuery(loginRedirect, "expiresIn", token.ExpiresIn.ToString());
                httpContext.Response.Redirect(loginRedirect);
                return;
            }

            httpContext.Response.Redirect(AppendQuery(frontendCallback, "error", result.ErrorCode ?? "login_failed"));
        }
        catch (Exception ex)
        {
            // 兜底：编排异常不再把用户卡在跳转中，统一跳回前端并带错误码；异常落日志便于排查
            logger.LogError(ex, "第三方登录回调编排失败，provider={Provider}, isBind={IsBind}", provider, isBind);
            httpContext.Response.Redirect(AppendQuery(frontendCallback, isBind ? "bind" : "error", "server_error"));
        }
    }

    private static string ResolveFrontendCallbackUrl(IConfiguration configuration)
    {
        return configuration.GetValue<string>($"{OAuthOptions.SectionName}:FrontendCallbackUrl")
            ?? "http://localhost:7777/#/auth/oauth-callback";
    }

    private static string AppendQuery(string url, string key, string value)
    {
        var separator = url.Contains('?') ? '&' : '?';
        return $"{url}{separator}{key}={Uri.EscapeDataString(value)}";
    }
}
