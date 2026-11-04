// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Host;

/// <summary>
/// 响应的安全头
/// </summary>
/// <remarks>
/// 一律不让浏览器猜内容类型。本地存储的签名端点 <c>/blobs</c> 与管理端同源，内容是用户上传的：放进沙箱，脚本拿不到应用的源
/// PDF 不进沙箱，内置阅读器在沙箱里打不开，它本身跑不了页面脚本
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
        if (context.Request.Path.StartsWithSegments("/blobs"))
        {
            context.Response.OnStarting(static state =>
            {
                var response = (HttpResponse)state;
                if (!string.Equals(response.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
                {
                    response.Headers.ContentSecurityPolicy = "sandbox; default-src 'none'; img-src 'self' data:; media-src 'self'; style-src 'unsafe-inline'";
                }

                return Task.CompletedTask;
            }, context.Response);
        }

        return next(context);
    }
}
