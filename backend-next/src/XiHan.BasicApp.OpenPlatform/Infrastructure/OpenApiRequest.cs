// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.OpenPlatform.Infrastructure;

/// <summary>
/// 一次开放接口请求的原始材料（请求作用域）。
/// </summary>
/// <remarks>
/// 框架在进内核管道之前就读完请求体绑定载荷，验签要原文、加密的请求体还要先解密，
/// 所以由宿主在 <c>UseXiHan()</c> 之前装上 <see cref="OpenApiRequestBuffer"/>，把原文留在这里；验签、解密与重新绑定在 Authenticate 阶段的 <c>RequestSigningMiddleware</c> 里做。
/// </remarks>
public sealed class OpenApiRequest
{
    /// <summary>
    /// 本次请求。
    /// </summary>
    public HttpContext? HttpContext { get; internal set; }

    /// <summary>
    /// 请求体原文。
    /// </summary>
    public byte[] RawBody { get; internal set; } = [];

    /// <summary>
    /// 请求体超过了上限（原文没有留下）。
    /// </summary>
    public bool BodyTooLarge { get; internal set; }

    /// <summary>
    /// 验签通过后的调用方。
    /// </summary>
    /// <remarks>
    /// 验签前或失败为空。
    /// </remarks>
    public OpenApiCaller? Caller { get; internal set; }

    /// <summary>
    /// 验签通过的凭证密钥。
    /// </summary>
    /// <remarks>
    /// 加密响应用；不对操作公开。
    /// </remarks>
    internal string? Secret { get; set; }
}

/// <summary>
/// 验签通过的调用方（凭证与归属人）。
/// </summary>
/// <remarks>
/// 调用方不成为主体，不带权限与租户。
/// </remarks>
/// <param name="CredentialId">凭证 ID。</param>
/// <param name="AppKey">AppKey。</param>
/// <param name="CredentialName">凭证名称。</param>
/// <param name="UserId">归属人。</param>
/// <param name="UserName">归属人的登录名。</param>
public sealed record OpenApiCaller(Guid CredentialId, string AppKey, string CredentialName, Guid UserId, string UserName);

/// <summary>
/// 开放接口的识别。
/// </summary>
/// <remarks>
/// 操作 ID 的资源段为 <c>open</c>（<c>open.*</c>，HTTP 路由 <c>/api/open:{动词}</c>）。
/// </remarks>
public static class OpenApiOperations
{
    /// <summary>
    /// 资源段。
    /// </summary>
    public const string Resource = "open";

    /// <summary>
    /// 是否开放接口。
    /// </summary>
    public static bool IsOpen(string operationId) => OperationId.ResourceOf(operationId) == Resource;
}

/// <summary>
/// 开放接口请求体的缓冲（ASP.NET Core 中间件）。
/// </summary>
/// <remarks>
/// 宿主在 <c>UseXiHan()</c> 之前装；只对路由到开放接口的请求生效。
/// 把请求体读进内存（超过上限即只记一个标记）；明文请求体照原样交给框架绑定，加密的（带 <c>X-Encrypt-Algorithm</c>）与超限的先交给它一个空对象，
/// 等验签与解密后再重新绑定，绑定失败不会在验签之前就把请求拦下（那样开放接口日志里看不到它）。
/// </remarks>
public static class OpenApiRequestBuffer
{
    private static readonly byte[] _emptyObject = "{}"u8.ToArray();

    /// <summary>
    /// 装上缓冲。
    /// </summary>
    /// <param name="app">应用。</param>
    /// <returns>应用。</returns>
    public static IApplicationBuilder UseOpenApiRequestBuffer(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var options = app.ApplicationServices.GetRequiredService<OpenPlatformOptions>();
        return app.Use(async (httpContext, next) =>
        {
            if (httpContext.GetEndpoint()?.Metadata.GetMetadata<OperationDescriptor>() is { } operation && OpenApiOperations.IsOpen(operation.Id))
            {
                await BufferAsync(httpContext, options).ConfigureAwait(false);
            }

            await next(httpContext).ConfigureAwait(false);
        });
    }

    private static async Task BufferAsync(HttpContext httpContext, OpenPlatformOptions options)
    {
        var state = httpContext.RequestServices.GetRequiredService<OpenApiRequest>();
        state.HttpContext = httpContext;
        var request = httpContext.Request;
        using var memory = new MemoryStream();
        var buffer = new byte[16 * 1024];
        int read;
        while ((read = await request.Body.ReadAsync(buffer, httpContext.RequestAborted).ConfigureAwait(false)) > 0)
        {
            if (memory.Length + read > options.MaxRequestBodyBytes)
            {
                state.BodyTooLarge = true;
                break;
            }

            memory.Write(buffer, 0, read);
        }

        state.RawBody = state.BodyTooLarge ? [] : memory.ToArray();
        var encrypted = OpenApiSignature.NormalizeEncryption(request.Headers[OpenApiSignature.EncryptAlgorithmHeader].ToString()) != OpenApiSignature.None;
        var forBinding = state.BodyTooLarge || (encrypted && state.RawBody.Length > 0) ? _emptyObject : state.RawBody;
        request.Body = new MemoryStream(forBinding, writable: false);
        request.ContentLength = forBinding.Length;
    }
}
