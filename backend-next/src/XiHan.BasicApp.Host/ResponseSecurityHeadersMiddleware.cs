// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Host;

/// <summary>
/// 响应的安全头
/// </summary>
/// <remarks>
/// 一律不让浏览器猜内容类型。本地存储的签名端点 <c>/blobs</c> 的沙箱由框架给（ADR-0125）
/// </remarks>
/// <param name="next">下一个中间件</param>
public sealed class ResponseSecurityHeadersMiddleware(RequestDelegate next)
{
    /// <summary>
    /// 处理请求
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    /// <returns>异步任务</returns>
    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Response.Headers.XContentTypeOptions = "nosniff";
        return next(context);
    }
}
