// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;

namespace XiHan.BasicApp.Identity.Infrastructure;

/// <summary>
/// <see cref="ISessionStatus"/> 的实现。
/// </summary>
/// <remarks>
/// 与 HTTP 调用的会话门禁同一份判定与缓存（<see cref="SessionGate"/>）。
/// </remarks>
/// <param name="gate">会话门禁。</param>
public sealed class SessionStatus(SessionGate gate) : ISessionStatus
{
    /// <inheritdoc />
    public async ValueTask<bool> IsActiveAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        // 锁屏的会话仍算有效（实时连接照旧，能收到强制下线），只是请求被门禁挡住
        var checkedSession = await gate.ValidateAsync(sessionId, cancellationToken).ConfigureAwait(false);
        return checkedSession.IsSuccess || string.Equals(checkedSession.Error!.Code, IdentityErrors.SessionLocked, StringComparison.Ordinal);
    }
}
