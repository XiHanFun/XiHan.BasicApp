// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Auth;

/// <summary>
/// 发一个图形验证码。
/// </summary>
/// <remarks>
/// 匿名操作，登记在架构测试的匿名白名单。<c>POST /api/auth:captcha</c>。每次都是新码（不缓存，所以不走 GET）；码只在服务端以散列保存，五分钟内用一次即作废。
/// </remarks>
/// <param name="captchas">图形验证码。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http)]
public sealed class IssueCaptcha(Captchas captchas) : IOperation<CaptchaRequest, CaptchaResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "auth.captcha";

    /// <inheritdoc />
    public async ValueTask<Result<CaptchaResponse>> HandleAsync(CaptchaRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var (id, image, expiresInSeconds) = await captchas.IssueAsync(context.CancellationToken).ConfigureAwait(false);
        return new CaptchaResponse(id, image, expiresInSeconds);
    }
}
