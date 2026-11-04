// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Middleware;

/// <summary>
/// 租户上下文必须与令牌一致
/// </summary>
/// <remarks>
/// Tenant 阶段、排在框架租户解析之后
/// 平台令牌没有 <c>tenant</c> 声明，框架的解析链接着看调用方自报的 Baggage，不拦的话平台账号带个请求头就能不经成员核对进任意租户
/// 进租户走 <c>auth.switch-tenant</c>，换一张带声明的令牌。只管本应用签发的令牌（带 <c>sid</c>）；内部调用没有令牌，租户经 Baggage 传递不受影响
/// </remarks>
public sealed class TokenTenantMiddleware : IPipelineMiddleware
{
    /// <inheritdoc />
    public async ValueTask InvokeAsync(InvocationContext context, PipelineStep nextStep)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextStep);
        var security = context.GetSecurity();
        var principal = security.Principal;
        if (principal.IsAuthenticated
            && principal.ClaimValues(IdentityClaims.Session).Any()
            && !string.Equals(principal.ClaimValues(IdentityClaims.Tenant).FirstOrDefault(), security.Tenant?.Id, StringComparison.Ordinal))
        {
            context.SetOutcome(Error.Denied(IdentityErrors.TenantNotAccessible, IdentityMessages.TenantNotAccessible, MessageArguments.Of(("tenantId", security.Tenant?.Id ?? TenantIds.Platform))));
            return;
        }

        await nextStep(context).ConfigureAwait(false);
    }
}
