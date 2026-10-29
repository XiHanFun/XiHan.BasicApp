// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Buffers;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using XiHan.BasicApp.OpenPlatform.Domain.Services;
using XiHan.Framework.Hosting.AspNetCore.Endpoints;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.OpenPlatform.Infrastructure.OAuth;

/// <summary>
/// OAuth 2.0 / OIDC 服务端的普通端点（宿主映射，前缀 <c>/api/oauth2</c>，开发时经前端代理、生产与前端同源）：
/// <c>GET authorize</c> 把查询串原样带去前端的同意页；<c>POST token</c>（表单，authorization_code + PKCE、refresh_token）、<c>POST revoke</c>（RFC 7009）、<c>GET|POST userinfo</c>、
/// <c>GET .well-known/openid-configuration</c> 与 <c>GET jwks</c> 回原始 OAuth JSON（不是 Problem Details），绕过内核管道（客户端的 Basic 认证与不透明令牌不是本系统的凭据），
/// 读写在一次平台调用里做（工作单元，失败回滚、出错的回应照样提交吊销）。签发者取配置 <c>BasicApp:OpenPlatform:Issuer</c>，不配即请求的来源加前缀。
/// </summary>
public static class OAuthEndpoints
{
    /// <summary>
    /// 路由前缀。
    /// </summary>
    public const string Prefix = "/api/oauth2";

    /// <summary>
    /// 映射端点。
    /// </summary>
    /// <param name="endpoints">端点。</param>
    /// <returns>端点。</returns>
    public static IEndpointRouteBuilder MapOAuthServer(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        Bypass(endpoints.MapGet(Prefix + "/authorize", Authorize));
        Bypass(endpoints.MapPost(Prefix + "/token", TokenAsync));
        Bypass(endpoints.MapPost(Prefix + "/revoke", RevokeAsync));
        Bypass(endpoints.MapMethods(Prefix + "/userinfo", ["GET", "POST"], UserInfoAsync));
        Bypass(endpoints.MapGet(Prefix + "/.well-known/openid-configuration", Discovery));
        Bypass(endpoints.MapGet(Prefix + "/jwks", JwksAsync));
        return endpoints;
    }

    private static void Bypass(IEndpointConventionBuilder builder) => PipelineBypassMetadata.BypassXiHanPipeline(builder.ExcludeFromDescription());

    /// <summary>
    /// 签发者。
    /// </summary>
    public static string IssuerOf(HttpContext http, OpenPlatformOptions options)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(options);
        return options.Issuer ?? $"{http.Request.Scheme}://{http.Request.Host}{http.Request.PathBase}{Prefix}";
    }

    private static IResult Authorize(HttpContext http, OpenPlatformOptions options)
    {
        var page = options.ConsentPageUrl;
        var query = http.Request.QueryString.HasValue ? http.Request.QueryString.Value![1..] : string.Empty;
        return Results.Redirect(query.Length == 0 ? page : page + (page.Contains('?', StringComparison.Ordinal) ? "&" : "?") + query);
    }

    private static async Task TokenAsync(HttpContext http, OpenPlatformOptions options)
    {
        if (!http.Request.HasFormContentType)
        {
            await WriteErrorAsync(http, StatusCodes.Status400BadRequest, OAuthErrors.InvalidRequest, "The request must be application/x-www-form-urlencoded.").ConfigureAwait(false);
            return;
        }

        var form = await http.Request.ReadFormAsync(http.RequestAborted).ConfigureAwait(false);
        var (clientId, clientSecret, basic) = ClientCredentials(http.Request, form);
        var issuer = IssuerOf(http, options);
        var grantType = form["grant_type"].ToString();
        if (grantType == OAuthAppService.AuthorizationCode)
        {
            // 签名密钥第一次用到时要在独立事务里生成：在调用的工作单元之外先备好（同一个库上嵌套写事务会互相等锁）
            await http.RequestServices.GetRequiredService<OAuthSigningKeys>().KeysAsync(http.RequestAborted).ConfigureAwait(false);
        }

        var outcome = await RunAsync(http, async (services, ct) =>
        {
            var service = services.GetRequiredService<OAuthTokenService>();
            var app = await service.AuthenticateClientAsync(clientId, clientSecret, ct).ConfigureAwait(false);
            if (app is null)
            {
                return (StatusCodes.Status401Unauthorized, OAuthTokenResult.Fail(OAuthErrors.InvalidClient, "Client authentication failed."));
            }

            return grantType switch
            {
                OAuthAppService.AuthorizationCode => (StatusCodes.Status400BadRequest, await service.ExchangeCodeAsync(app, form["code"].ToString(), form["redirect_uri"].ToString(), form["code_verifier"].ToString(), issuer, ct).ConfigureAwait(false)),
                OAuthAppService.RefreshToken => (StatusCodes.Status400BadRequest, await service.RefreshAsync(app, form["refresh_token"].ToString(), ct).ConfigureAwait(false)),
                _ => (StatusCodes.Status400BadRequest, OAuthTokenResult.Fail(OAuthErrors.UnsupportedGrantType, "Only authorization_code and refresh_token are supported.")),
            };
        }).ConfigureAwait(false);

        if (!outcome.Ok)
        {
            await WriteErrorAsync(http, StatusCodes.Status500InternalServerError, "server_error", "Internal server error.").ConfigureAwait(false);
            return;
        }

        var (status, result) = outcome.Value;
        if (result.Tokens is not { } issued)
        {
            if (result.Error == OAuthErrors.InvalidClient)
            {
                http.Response.Headers.WWWAuthenticate = basic ? "Basic realm=\"oauth2\"" : "Basic";
            }

            await WriteErrorAsync(http, status, result.Error!, result.Description).ConfigureAwait(false);
            return;
        }

        await WriteJsonAsync(http, StatusCodes.Status200OK, writer =>
        {
            writer.WriteString("access_token", issued.AccessToken);
            writer.WriteString("token_type", "Bearer");
            writer.WriteNumber("expires_in", issued.ExpiresIn);
            if (issued.RefreshToken is not null)
            {
                writer.WriteString("refresh_token", issued.RefreshToken);
            }

            writer.WriteString("scope", issued.Scope);
            if (issued.IdToken is not null)
            {
                writer.WriteString("id_token", issued.IdToken);
            }
        }).ConfigureAwait(false);
    }

    private static async Task RevokeAsync(HttpContext http)
    {
        if (!http.Request.HasFormContentType)
        {
            await WriteErrorAsync(http, StatusCodes.Status400BadRequest, OAuthErrors.InvalidRequest, "The request must be application/x-www-form-urlencoded.").ConfigureAwait(false);
            return;
        }

        var form = await http.Request.ReadFormAsync(http.RequestAborted).ConfigureAwait(false);
        var (clientId, clientSecret, basic) = ClientCredentials(http.Request, form);
        var outcome = await RunAsync(http, async (services, ct) =>
        {
            var service = services.GetRequiredService<OAuthTokenService>();
            var app = await service.AuthenticateClientAsync(clientId, clientSecret, ct).ConfigureAwait(false);
            if (app is null)
            {
                return false;
            }

            await service.RevokeAsync(app, form["token"].ToString(), ct).ConfigureAwait(false);
            return true;
        }).ConfigureAwait(false);

        if (outcome is { Ok: true, Value: false })
        {
            http.Response.Headers.WWWAuthenticate = basic ? "Basic realm=\"oauth2\"" : "Basic";
            await WriteErrorAsync(http, StatusCodes.Status401Unauthorized, OAuthErrors.InvalidClient, "Client authentication failed.").ConfigureAwait(false);
            return;
        }

        http.Response.StatusCode = outcome.Ok ? StatusCodes.Status200OK : StatusCodes.Status500InternalServerError;
        NoStore(http);
    }

    private static async Task UserInfoAsync(HttpContext http)
    {
        string? token = null;
        if (AuthenticationHeaderValue.TryParse(http.Request.Headers.Authorization.ToString(), out var header) && string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase))
        {
            token = header.Parameter;
        }
        else if (http.Request.HasFormContentType)
        {
            token = (await http.Request.ReadFormAsync(http.RequestAborted).ConfigureAwait(false))["access_token"].ToString();
        }

        var outcome = await RunAsync(http, async (services, ct) => await services.GetRequiredService<OAuthTokenService>().UserInfoAsync(token, ct).ConfigureAwait(false)).ConfigureAwait(false);
        if (outcome.Value is not { } claims)
        {
            http.Response.Headers.WWWAuthenticate = "Bearer error=\"invalid_token\"";
            await WriteErrorAsync(http, StatusCodes.Status401Unauthorized, OAuthErrors.InvalidToken, "The access token is invalid or has expired.").ConfigureAwait(false);
            return;
        }

        await WriteJsonAsync(http, StatusCodes.Status200OK, writer =>
        {
            foreach (var (key, value) in claims)
            {
                writer.WriteString(key, value);
            }
        }).ConfigureAwait(false);
    }

    private static Task Discovery(HttpContext http, OpenPlatformOptions options)
    {
        var issuer = IssuerOf(http, options);
        return WriteJsonAsync(http, StatusCodes.Status200OK, writer =>
        {
            writer.WriteString("issuer", issuer);
            writer.WriteString("authorization_endpoint", issuer + "/authorize");
            writer.WriteString("token_endpoint", issuer + "/token");
            writer.WriteString("userinfo_endpoint", issuer + "/userinfo");
            writer.WriteString("revocation_endpoint", issuer + "/revoke");
            writer.WriteString("jwks_uri", issuer + "/jwks");
            Array(writer, "response_types_supported", ["code"]);
            Array(writer, "grant_types_supported", OAuthAppService.SupportedGrantTypes);
            Array(writer, "subject_types_supported", ["public"]);
            Array(writer, "id_token_signing_alg_values_supported", [SecurityAlgorithms.RsaSha256]);
            Array(writer, "scopes_supported", OAuthAppService.SupportedScopes);
            Array(writer, "token_endpoint_auth_methods_supported", ["client_secret_basic", "client_secret_post", "none"]);
            Array(writer, "revocation_endpoint_auth_methods_supported", ["client_secret_basic", "client_secret_post", "none"]);
            Array(writer, "code_challenge_methods_supported", [OAuthProtocol.S256, OAuthProtocol.Plain]);
            Array(writer, "claims_supported", ["sub", "iss", "aud", "exp", "iat", "auth_time", "nonce", "name", "preferred_username", "email", "phone_number"]);
        }, cache: true);
    }

    private static async Task JwksAsync(HttpContext http)
    {
        var keys = await http.RequestServices.GetRequiredService<OAuthSigningKeys>().KeysAsync(http.RequestAborted).ConfigureAwait(false);
        await WriteJsonAsync(http, StatusCodes.Status200OK, writer =>
        {
            writer.WriteStartArray("keys");
            foreach (var key in keys)
            {
                var parameters = key.Rsa.ExportParameters(includePrivateParameters: false);
                writer.WriteStartObject();
                writer.WriteString("kty", "RSA");
                writer.WriteString("use", "sig");
                writer.WriteString("alg", SecurityAlgorithms.RsaSha256);
                writer.WriteString("kid", key.KeyId);
                writer.WriteString("n", OAuthProtocol.Base64UrlEncode(parameters.Modulus!));
                writer.WriteString("e", OAuthProtocol.Base64UrlEncode(parameters.Exponent!));
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }, cache: true).ConfigureAwait(false);
    }

    /// <summary>
    /// 在一次平台调用里做读写；调用失败（异常、提交失败）时 <c>Ok</c> 为否。
    /// </summary>
    private static async Task<(bool Ok, T? Value)> RunAsync<T>(HttpContext http, Func<IServiceProvider, CancellationToken, Task<T>> work)
    {
        var app = http.RequestServices.GetRequiredService<App>();
        var context = App.CreateContext(http.RequestServices, http.RequestAborted);
        T? result = default;
        var outcome = await app.InvokeAsync(context, async invocation =>
        {
            result = await work(invocation.Services, invocation.CancellationToken).ConfigureAwait(false);
            invocation.SetOutcome(Result.Success());
        }).ConfigureAwait(false);
        return outcome.IsSuccess ? (true, result) : (false, default);
    }

    /// <summary>
    /// 客户端凭据：<c>Authorization: Basic</c>（用户名与密码各自 form-url 解码，RFC 6749 §2.3.1）优先，其次表单。
    /// </summary>
    private static (string? ClientId, string? ClientSecret, bool Basic) ClientCredentials(HttpRequest request, IFormCollection form)
    {
        if (AuthenticationHeaderValue.TryParse(request.Headers.Authorization.ToString(), out var header)
            && string.Equals(header.Scheme, "Basic", StringComparison.OrdinalIgnoreCase) && header.Parameter is { } encoded)
        {
            try
            {
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
                var colon = decoded.IndexOf(':', StringComparison.Ordinal);
                if (colon > 0)
                {
                    return (Uri.UnescapeDataString(decoded[..colon].Replace('+', ' ')), Uri.UnescapeDataString(decoded[(colon + 1)..].Replace('+', ' ')), true);
                }
            }
            catch (FormatException)
            {
                return (null, null, true);
            }
        }

        return (Blank(form["client_id"].ToString()), Blank(form["client_secret"].ToString()), false);
    }

    private static string? Blank(string value) => value.Length == 0 ? null : value;

    private static void NoStore(HttpContext http)
    {
        http.Response.Headers.CacheControl = "no-store";
        http.Response.Headers.Pragma = "no-cache";
    }

    private static Task WriteErrorAsync(HttpContext http, int status, string error, string? description)
        => WriteJsonAsync(http, status, writer =>
        {
            writer.WriteString("error", error);
            if (description is not null)
            {
                writer.WriteString("error_description", description);
            }
        });

    private static void Array(Utf8JsonWriter writer, string name, IEnumerable<string> values)
    {
        writer.WriteStartArray(name);
        foreach (var value in values)
        {
            writer.WriteStringValue(value);
        }

        writer.WriteEndArray();
    }

    private static async Task WriteJsonAsync(HttpContext http, int status, Action<Utf8JsonWriter> body, bool cache = false)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            body(writer);
            writer.WriteEndObject();
        }

        http.Response.StatusCode = status;
        http.Response.ContentType = "application/json; charset=utf-8";
        if (cache)
        {
            http.Response.Headers.CacheControl = "public, max-age=300";
        }
        else
        {
            NoStore(http);
        }

        await http.Response.Body.WriteAsync(buffer.WrittenMemory, http.RequestAborted).ConfigureAwait(false);
    }
}
