#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OpenApiDiagnosticsEndpoints
// Guid:1e5b8c74-3a90-4d2f-b6e1-8c47a0f5d239
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Text;
using XiHan.Framework.Web.Api.Security.OpenApi;

namespace XiHan.BasicApp.Saas.Infrastructure.OpenApi;

/// <summary>
/// OpenAPI 签名网关自测端点：/api/openapi/ping（GET）+ /api/openapi/echo（POST）。
/// </summary>
/// <remarks>
/// 用途：验证「签名验签 → 凭证解析 → 放行」整条链路是否通顺，供第三方对接联调与自测。
/// 端点本身 <c>AllowAnonymous</c>（不走 JWT）——身份由 <see cref="XiHanOpenApiSecurityMiddleware"/> 经签名确立，
/// 通过后把解析出的客户端放进 <c>HttpContext.Items</c>，这里回显其 AccessKey 以证明验签成功。
/// 仅当 <c>XiHan:Web:Api:OpenApiSecurity</c> 开启且 <c>ProtectedPathPrefixes</c> 覆盖 <c>/api/openapi</c> 时才真正强制签名。
/// 该端点为诊断用途，可按需移除。
/// </remarks>
public static class OpenApiDiagnosticsEndpoints
{
    /// <summary>
    /// 映射 OpenAPI 网关自测端点。
    /// </summary>
    public static IEndpointRouteBuilder MapOpenApiDiagnosticsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/openapi/ping", PingAsync).AllowAnonymous();
        endpoints.MapPost("/api/openapi/echo", EchoAsync).AllowAnonymous();
        return endpoints;
    }

    /// <summary>
    /// 连通性自测：签名校验通过即回显解析出的调用方 AccessKey。
    /// </summary>
    private static Task PingAsync(HttpContext context)
    {
        var client = context.Items[OpenApiSecurityConstants.SecurityClientContextKey] as OpenApiSecurityClient;
        return context.Response.WriteAsJsonAsync(new
        {
            ok = true,
            message = "OpenAPI 签名校验通过",
            accessKey = client?.AccessKey,
            serverTimeUtc = DateTimeOffset.UtcNow
        }, context.RequestAborted);
    }

    /// <summary>
    /// 回显自测：回显调用方 AccessKey 与收到的请求体（用于验证内容签名/请求体完整性）。
    /// </summary>
    private static async Task EchoAsync(HttpContext context)
    {
        var client = context.Items[OpenApiSecurityConstants.SecurityClientContextKey] as OpenApiSecurityClient;

        // 中间件已 EnableBuffering 并将流复位到 0（NONE 加密时为原始明文），此处再读一遍用于回显
        context.Request.Body.Position = 0;
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var body = await reader.ReadToEndAsync(context.RequestAborted);

        await context.Response.WriteAsJsonAsync(new
        {
            ok = true,
            message = "OpenAPI 签名校验通过",
            accessKey = client?.AccessKey,
            receivedBody = body,
            serverTimeUtc = DateTimeOffset.UtcNow
        }, context.RequestAborted);
    }
}
