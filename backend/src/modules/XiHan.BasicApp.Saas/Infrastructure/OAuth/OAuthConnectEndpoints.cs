// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.Framework.Authentication.OAuth;

namespace XiHan.BasicApp.Saas.Infrastructure.OAuth;

/// <summary>
/// OAuth2 授权服务端标准端点：/connect/authorize、/connect/token、/connect/revoke。
/// </summary>
/// <remarks>
/// 定位：本平台<b>作为授权服务端</b>对外提供标准 OAuth2 端点（区别于 <see cref="OAuthEndpoints"/> 的"本平台作为第三方登录客户端"）。
/// <list type="bullet">
/// <item><c>/connect/authorize</c>：匿名浏览器入口，仅 302 跳转到已登录 SPA 同意页（第一方为 JWT bearer，无会话 Cookie，无法在后端判定登录态）。</item>
/// <item><c>/connect/token</c>：匿名、表单编码、标准路径；直接注入普通 Scoped <see cref="IOAuthServerService"/>（不跨代理接口边界，避免挂起）。</item>
/// <item><c>/connect/revoke</c>：匿名、表单编码（RFC 7009）。</item>
/// </list>
/// 令牌/撤销端点返回<b>原始 OAuth2 JSON</b>（不套用应用统一响应信封）；Minimal-API 天然不经 MVC 结果过滤器。
/// </remarks>
public static class OAuthConnectEndpoints
{
    private const string LogCategory = "XiHan.BasicApp.WebHost.OAuthConnectEndpoints";

    /// <summary>
    /// 映射 OAuth2 授权服务端标准端点。
    /// </summary>
    public static IEndpointRouteBuilder MapOAuthConnectEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/connect/authorize", AuthorizeRedirectAsync).AllowAnonymous();
        endpoints.MapPost("/connect/token", TokenAsync).AllowAnonymous();
        endpoints.MapPost("/connect/revoke", RevokeAsync).AllowAnonymous();
        return endpoints;
    }

    /// <summary>
    /// 授权入口：把标准授权请求 302 跳转到已登录 SPA 同意页（原样保留查询串）。
    /// </summary>
    private static Task AuthorizeRedirectAsync(HttpContext httpContext, IConfiguration configuration)
    {
        var consentBase = ResolveConsentPageUrl(configuration);
        var query = httpContext.Request.QueryString.Value ?? string.Empty;
        httpContext.Response.Redirect($"{consentBase}{query}");
        return Task.CompletedTask;
    }

    /// <summary>
    /// 令牌端点：表单编码请求，支持 authorization_code / refresh_token / client_credentials。
    /// </summary>
    private static async Task TokenAsync(HttpContext httpContext, IOAuthServerService oauthServerService, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger(LogCategory);

        if (!httpContext.Request.HasFormContentType)
        {
            await WriteTokenErrorAsync(httpContext, 400, "invalid_request", "请求必须为 application/x-www-form-urlencoded。");
            return;
        }

        var form = await httpContext.Request.ReadFormAsync(httpContext.RequestAborted);
        var (basicClientId, basicClientSecret) = ReadBasicAuth(httpContext);

        var request = new OAuthTokenRequest(
            GrantType: Field(form, "grant_type"),
            Code: Field(form, "code"),
            RedirectUri: Field(form, "redirect_uri"),
            CodeVerifier: Field(form, "code_verifier"),
            RefreshToken: Field(form, "refresh_token"),
            Scope: Field(form, "scope"),
            ClientId: basicClientId ?? Field(form, "client_id"),
            ClientSecret: basicClientSecret ?? Field(form, "client_secret"));

        OAuthTokenOutcome outcome;
        try
        {
            outcome = await oauthServerService.ExchangeTokenAsync(request, httpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OAuth: /connect/token 处理异常 grantType={GrantType}", request.GrantType);
            await WriteTokenErrorAsync(httpContext, 500, "server_error", "服务器内部错误。");
            return;
        }

        // OAuth2 要求令牌响应禁用缓存
        httpContext.Response.Headers.CacheControl = "no-store";
        httpContext.Response.Headers.Pragma = "no-cache";

        if (outcome is { Success: true, Response: { } response })
        {
            var payload = new Dictionary<string, object?>
            {
                ["access_token"] = response.AccessToken,
                ["token_type"] = response.TokenType,
                ["expires_in"] = response.ExpiresIn
            };
            if (!string.IsNullOrWhiteSpace(response.RefreshToken))
            {
                payload["refresh_token"] = response.RefreshToken;
            }

            if (!string.IsNullOrWhiteSpace(response.Scope))
            {
                payload["scope"] = response.Scope;
            }

            await httpContext.Response.WriteAsJsonAsync(payload, httpContext.RequestAborted);
            return;
        }

        await WriteTokenErrorAsync(httpContext, outcome.StatusCode, outcome.Error ?? "invalid_request", outcome.ErrorDescription);
    }

    /// <summary>
    /// 撤销端点（RFC 7009）：撤销刷新令牌；无论令牌是否存在均返回 200。
    /// </summary>
    private static async Task RevokeAsync(HttpContext httpContext, IOAuthServerService oauthServerService, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger(LogCategory);

        if (!httpContext.Request.HasFormContentType)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var form = await httpContext.Request.ReadFormAsync(httpContext.RequestAborted);
        var (basicClientId, basicClientSecret) = ReadBasicAuth(httpContext);

        var request = new OAuthRevokeRequest(
            Token: Field(form, "token"),
            TokenTypeHint: Field(form, "token_type_hint"),
            ClientId: basicClientId ?? Field(form, "client_id"),
            ClientSecret: basicClientSecret ?? Field(form, "client_secret"));

        try
        {
            await oauthServerService.RevokeTokenAsync(request, httpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            // RFC 7009：撤销失败不向客户端暴露细节，异常落日志后仍返回 200
            logger.LogError(ex, "OAuth: /connect/revoke 处理异常");
        }

        httpContext.Response.StatusCode = StatusCodes.Status200OK;
    }

    /// <summary>
    /// 从 Authorization: Basic 头解析客户端凭证（RFC 6749：值先 form-url 编码再 base64）。
    /// </summary>
    private static (string? ClientId, string? ClientSecret) ReadBasicAuth(HttpContext httpContext)
    {
        var header = httpContext.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            return (null, null);
        }

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header["Basic ".Length..].Trim()));
            var separator = decoded.IndexOf(':');
            if (separator < 0)
            {
                return (null, null);
            }

            var clientId = Uri.UnescapeDataString(decoded[..separator]);
            var clientSecret = Uri.UnescapeDataString(decoded[(separator + 1)..]);
            return (string.IsNullOrWhiteSpace(clientId) ? null : clientId, clientSecret);
        }
        catch
        {
            return (null, null);
        }
    }

    private static string? Field(IFormCollection form, string key)
    {
        var value = form[key].ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static async Task WriteTokenErrorAsync(HttpContext httpContext, int statusCode, string error, string? description)
    {
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.Headers.CacheControl = "no-store";
        httpContext.Response.Headers.Pragma = "no-cache";
        if (statusCode == StatusCodes.Status401Unauthorized)
        {
            httpContext.Response.Headers.WWWAuthenticate = "Basic";
        }

        var payload = new Dictionary<string, object?> { ["error"] = error };
        if (!string.IsNullOrWhiteSpace(description))
        {
            payload["error_description"] = description;
        }

        await httpContext.Response.WriteAsJsonAsync(payload, httpContext.RequestAborted);
    }

    /// <summary>
    /// 推导 SPA 同意页地址：复用 OAuth 前端回调基址，替换为 /#/oauth/authorize（哈希路由）。
    /// </summary>
    private static string ResolveConsentPageUrl(IConfiguration configuration)
    {
        var callback = configuration.GetValue<string>($"{OAuthOptions.SectionName}:FrontendCallbackUrl")
            ?? "http://localhost:7777/#/auth/oauth-callback";

        var hashIndex = callback.IndexOf("/#/", StringComparison.Ordinal);
        var baseUrl = hashIndex > 0 ? callback[..hashIndex] : callback.TrimEnd('/');
        return $"{baseUrl}/#/oauth/authorize";
    }
}
