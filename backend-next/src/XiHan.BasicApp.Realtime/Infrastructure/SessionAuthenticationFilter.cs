// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;
using XiHan.Framework.Hosting.AspNetCore.Authentication;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions;

namespace XiHan.BasicApp.Realtime.Infrastructure;

/// <summary>
/// Hub 认证的会话门禁。
/// </summary>
/// <remarks>
/// 认证方案 <c>XiHan</c> 认过令牌之后，按令牌的 <c>sid</c> 经 <see cref="ISessionStatus"/> 核对会话仍有效（Hub 绕过内核管道，HTTP 调用的会话门禁管不到这里）；
/// 没有会话或会话已吊销、过期即认证失败（401），连接建立不起来。
/// </remarks>
/// <param name="sessions">会话状态。</param>
internal sealed class SessionAuthenticationFilter(ISessionStatus sessions) : IInvocationAuthenticationFilter
{
    // Identity 签发的访问令牌声明（方案 5.8）
    private const string SessionClaim = "sid";

    /// <inheritdoc />
    public async ValueTask<Result> AuthenticatedAsync(Principal principal, InvocationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(principal);
        return Guid.TryParse(principal.ClaimValues(SessionClaim).FirstOrDefault(), out var sessionId) && await sessions.IsActiveAsync(sessionId, cancellationToken).ConfigureAwait(false)
            ? Result.Success()
            : Error.Denied(SecurityErrors.InvalidCredential, SecurityMessages.InvalidToken);
    }
}
