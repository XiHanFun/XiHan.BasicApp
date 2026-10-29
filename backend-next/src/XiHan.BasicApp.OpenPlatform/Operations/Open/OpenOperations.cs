// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.OpenPlatform.Infrastructure;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Localization.Abstractions.Messages;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.OpenPlatform.Operations.Open;

/// <summary>
/// <c>open.ping</c> 的请求：没有输入。
/// </summary>
public sealed record OpenPingRequest;

/// <summary>
/// 签名自检的结果。
/// </summary>
/// <param name="Ok">验签通过。</param>
/// <param name="Message">说明（按当前区域）。</param>
/// <param name="AppKey">调用用的 AppKey。</param>
/// <param name="OwnerUserId">凭证归属人。</param>
/// <param name="ServerTime">服务端时间（UTC，供接入方对时）。</param>
public sealed record OpenPingResponse(bool Ok, string Message, string AppKey, Guid OwnerUserId, DateTimeOffset ServerTime);

/// <summary>
/// <c>open.echo</c> 的请求：原样回显，用于核对请求体、内容签名与加密。
/// </summary>
/// <param name="Message">任意文本。</param>
/// <param name="Number">任意整数。</param>
public sealed record OpenEchoRequest([StringLength(4096)] string? Message, int? Number);

/// <summary>
/// 回显的结果。
/// </summary>
/// <param name="Ok">验签通过。</param>
/// <param name="Message">说明（按当前区域）。</param>
/// <param name="AppKey">调用用的 AppKey。</param>
/// <param name="Echo">收到的请求（解密后）。</param>
/// <param name="ServerTime">服务端时间（UTC）。</param>
public sealed record OpenEchoResponse(bool Ok, string Message, string AppKey, OpenEchoRequest Echo, DateTimeOffset ServerTime);

/// <summary>
/// 签名自检。
/// </summary>
/// <remarks>
/// <c>GET /api/open:ping</c>（4.x <c>OpenApi/Ping</c>）。只能签名调用，验签由 Authenticate 阶段的 <c>RequestSigningMiddleware</c> 做。
/// </remarks>
/// <param name="request">本次开放接口请求。</param>
/// <param name="clock">时间源。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http)]
public sealed class OpenPing(OpenApiRequest request, TimeProvider clock) : IOperation<OpenPingRequest, OpenPingResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "open.ping";

    /// <inheritdoc />
    public ValueTask<Result<OpenPingResponse>> HandleAsync(OpenPingRequest input, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);
        if (request.Caller is not { } caller)
        {
            return ValueTask.FromResult<Result<OpenPingResponse>>(Error.Denied(OpenPlatformErrors.SignatureHeadersMissing, OpenPlatformMessages.SignatureHeadersMissing, MessageArguments.Of(("headers", OpenApiSignature.SignatureHeader))));
        }

        return ValueTask.FromResult<Result<OpenPingResponse>>(new OpenPingResponse(true, context.Localize(OpenPlatformMessages.SignatureVerified), caller.AppKey, caller.UserId, clock.GetUtcNow()));
    }
}

/// <summary>
/// 回显请求。
/// </summary>
/// <remarks>
/// <c>POST /api/open:echo</c>（4.x <c>OpenApi/Echo</c>）。只能签名调用；请求体可以加密，响应随之加密。
/// </remarks>
/// <param name="request">本次开放接口请求。</param>
/// <param name="clock">时间源。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http)]
public sealed class OpenEcho(OpenApiRequest request, TimeProvider clock) : IOperation<OpenEchoRequest, OpenEchoResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "open.echo";

    /// <inheritdoc />
    public ValueTask<Result<OpenEchoResponse>> HandleAsync(OpenEchoRequest input, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);
        if (request.Caller is not { } caller)
        {
            return ValueTask.FromResult<Result<OpenEchoResponse>>(Error.Denied(OpenPlatformErrors.SignatureHeadersMissing, OpenPlatformMessages.SignatureHeadersMissing, MessageArguments.Of(("headers", OpenApiSignature.SignatureHeader))));
        }

        return ValueTask.FromResult<Result<OpenEchoResponse>>(new OpenEchoResponse(true, context.Localize(OpenPlatformMessages.SignatureVerified), caller.AppKey, input, clock.GetUtcNow()));
    }
}
