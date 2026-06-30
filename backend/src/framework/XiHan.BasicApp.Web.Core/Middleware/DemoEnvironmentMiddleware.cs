#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DemoEnvironmentMiddleware
// Guid:e8a1b3c5-7d4f-4a2e-9b6c-1f3e5d7a9b2c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/30 19:37:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Options;
using XiHan.BasicApp.Web.Core.Options;
using XiHan.Framework.Application.Contracts.Dtos;
using XiHan.Framework.Utils.Logging;
using XiHan.Framework.Utils.Serialization.Json;

namespace XiHan.BasicApp.Web.Core.Middleware;

/// <summary>
/// 演示环境中间件
/// </summary>
public class DemoEnvironmentMiddleware
{
    private static readonly HashSet<string> ModifyingMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "PUT", "PATCH", "DELETE"
    };

    private readonly RequestDelegate _next;

    /// <summary>
    /// 演示环境中间件
    /// </summary>
    /// <param name="next"></param>
    public DemoEnvironmentMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// 调用
    /// </summary>
    /// <param name="context"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public async Task InvokeAsync(HttpContext context, IOptions<DemoEnvironmentOptions> options)
    {
        var demoOptions = options.Value;

        if (!demoOptions.Enabled)
        {
            await _next(context);
            return;
        }

        if (!ModifyingMethods.Contains(context.Request.Method))
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        if (demoOptions.AllowedPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        LogHelper.Warn($"演示环境禁止修改数据：{context.Request.Method} {path}");

        var detailMessage = $"{demoOptions.Message}（{context.Request.Method} {path}）";

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json; charset=utf-8";

        var response = ApiResponse.Forbidden();
        response.Message = detailMessage;

        await context.Response.WriteAsync(response.ToJson());
    }
}
