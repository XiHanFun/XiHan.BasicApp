// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using Microsoft.AspNetCore.Http;
using XiHan.BasicApp.Runtime.Schema;

namespace XiHan.BasicApp.Runtime.Middleware;

/// <summary>
/// 维护模式：别的节点升级共享的库期间返回 503。
/// </summary>
/// <remarks>
/// ASP.NET Core 中间件，宿主排在最前（普通端点与 Hub 也一样挡住）。放行健康检查与 <c>/.well-known/</c>，
/// 其余回 <c>application/problem+json</c>（错误码 <see cref="RuntimeErrors.MaintenanceMode"/>），带 <c>Retry-After</c>。
/// </remarks>
/// <param name="next">下一个中间件。</param>
/// <param name="state">维护模式状态。</param>
public sealed class MaintenanceModeMiddleware(RequestDelegate next, MaintenanceModeState state)
{
    private static readonly string[] _allowed = ["/health", "/.well-known/"];

    /// <summary>
    /// 处理请求。
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!state.IsActive || _allowed.Any(prefix => context.Request.Path.StartsWithSegments(prefix.TrimEnd('/'), StringComparison.OrdinalIgnoreCase)))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.Headers.RetryAfter = ((int)MaintenanceModeState.PollInterval.TotalSeconds * 3).ToString(CultureInfo.InvariantCulture);
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(
            $$"""{"type":"about:blank","title":"Service Unavailable","status":503,"code":"{{RuntimeErrors.MaintenanceMode}}","detail":"The database is being upgraded; please retry shortly."}""",
            context.RequestAborted).ConfigureAwait(false);
    }
}
