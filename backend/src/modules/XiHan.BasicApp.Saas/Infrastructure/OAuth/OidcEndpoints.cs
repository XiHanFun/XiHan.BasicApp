// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Json;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Authentication.Oidc;

namespace XiHan.BasicApp.Saas.Infrastructure.OAuth;

/// <summary>
/// OpenID Connect 端点
/// </summary>
/// <remarks>
/// 发现文档与 JWKS 匿名开放：客户端在拿到任何凭据之前就要读它们完成自动配置。
/// 用户信息端点要求携带访问令牌，返回内容按令牌已授予的 scope 收窄。
/// </remarks>
public static class OidcEndpoints
{
    /// <summary>
    /// 映射 OIDC 端点。未启用（<c>XiHan:Authentication:Oidc:IsEnabled</c> 为假）时不注册任何路由。
    /// </summary>
    /// <param name="endpoints">端点路由构建器</param>
    /// <returns>端点路由构建器</returns>
    public static IEndpointRouteBuilder MapOidcEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<OidcOptions>>().Value;
        if (!options.IsEnabled)
        {
            return endpoints;
        }

        _ = endpoints.MapGet(OidcConstants.Endpoints.Discovery, GetDiscoveryAsync).AllowAnonymous();
        _ = endpoints.MapGet(OidcConstants.Endpoints.Jwks, GetJwksAsync).AllowAnonymous();
        _ = endpoints.MapGet(OidcConstants.Endpoints.UserInfo, GetUserInfoAsync).RequireAuthorization();
        _ = endpoints.MapPost(OidcConstants.Endpoints.UserInfo, GetUserInfoAsync).RequireAuthorization();

        return endpoints;
    }

    /// <summary>
    /// 发现文档
    /// </summary>
    private static async Task GetDiscoveryAsync(HttpContext httpContext, IOptions<OidcOptions> options)
    {
        var document = OidcDiscoveryDocument.Create(options.Value);
        httpContext.Response.ContentType = "application/json; charset=utf-8";
        await httpContext.Response.WriteAsync(JsonSerializer.Serialize(document));
    }

    /// <summary>
    /// 签名公钥集
    /// </summary>
    private static async Task GetJwksAsync(HttpContext httpContext, IOidcSigningKeyProvider signingKeyProvider)
    {
        httpContext.Response.ContentType = "application/json; charset=utf-8";
        await httpContext.Response.WriteAsync(signingKeyProvider.GetJsonWebKeySetJson());
    }

    /// <summary>
    /// 用户信息：以访问令牌的 sub 定位用户，按令牌记录的 scope 决定下发字段。
    /// </summary>
    private static async Task GetUserInfoAsync(
        HttpContext httpContext,
        IUserRepository userRepository,
        IOAuthTokenRepository tokenRepository)
    {
        var subject = httpContext.User.FindFirstValue(OidcConstants.Claims.Subject)
                      ?? httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!long.TryParse(subject, out var userId) || userId <= 0)
        {
            await WriteErrorAsync(httpContext, StatusCodes.Status401Unauthorized, "invalid_token", "访问令牌缺少可用的主体标识。");
            return;
        }

        var jti = httpContext.User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti);
        if (string.IsNullOrWhiteSpace(jti))
        {
            await WriteErrorAsync(httpContext, StatusCodes.Status401Unauthorized, "invalid_token", "访问令牌缺少 jti。");
            return;
        }

        var token = await tokenRepository.GetByAccessTokenIgnoreTenantAsync(jti, httpContext.RequestAborted);
        if (token is null || token.IsRevoked || token.Status != EnableStatus.Enabled)
        {
            await WriteErrorAsync(httpContext, StatusCodes.Status401Unauthorized, "invalid_token", "访问令牌已失效。");
            return;
        }

        var user = await userRepository.GetByIdIgnoreTenantAsync(userId, httpContext.RequestAborted);
        if (user is null || user.Status != EnableStatus.Enabled)
        {
            await WriteErrorAsync(httpContext, StatusCodes.Status401Unauthorized, "invalid_token", "用户不存在或已被禁用。");
            return;
        }

        var granted = (token.Scopes ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var payload = new Dictionary<string, object?>
        {
            [OidcConstants.Claims.Subject] = user.BasicId.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };

        foreach (var claim in OAuthServerService.BuildProfileClaims(user, granted))
        {
            payload[claim.Type] = claim.Value;
        }

        httpContext.Response.ContentType = "application/json; charset=utf-8";
        await httpContext.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }

    /// <summary>
    /// 按 OAuth2 约定回写错误
    /// </summary>
    private static async Task WriteErrorAsync(HttpContext httpContext, int statusCode, string error, string description)
    {
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json; charset=utf-8";
        await httpContext.Response.WriteAsync(JsonSerializer.Serialize(new { error, error_description = description }));
    }
}
