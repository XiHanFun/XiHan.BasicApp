// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Middleware;

/// <summary>
/// Authenticate 阶段、排在框架 JWT 认证之后：令牌带 <c>sid</c> 声明时核对会话仍有效，已吊销 / 已过期 / 用户非正常即以 <c>IDENTITY_DENIED_001</c> 拒绝（前端据此登出）。
/// 没有 <c>sid</c> 的令牌（非本应用签发）不经门禁，由授权阶段按授予判定。
/// </summary>
public sealed class SessionGateMiddleware : IPipelineMiddleware
{
    private readonly SessionGate _gate;

    /// <summary>
    /// 构造中间件。
    /// </summary>
    public SessionGateMiddleware(SessionGate gate)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
    }

    /// <inheritdoc />
    public async ValueTask InvokeAsync(InvocationContext context, PipelineStep nextStep)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextStep);
        var principal = context.GetSecurity().Principal;
        if (principal.IsAuthenticated && principal.ClaimValues(IdentityClaims.Session).FirstOrDefault() is { } sid && Guid.TryParse(sid, out var sessionId))
        {
            var checkedSession = await _gate.ValidateAsync(sessionId, context.CancellationToken).ConfigureAwait(false);
            if (checkedSession.IsFailure)
            {
                context.SetOutcome(checkedSession);
                return;
            }
        }

        await nextStep(context).ConfigureAwait(false);
    }
}
