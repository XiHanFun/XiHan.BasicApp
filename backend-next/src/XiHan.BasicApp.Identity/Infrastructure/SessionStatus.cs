// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;

namespace XiHan.BasicApp.Identity.Infrastructure;

/// <summary>
/// <see cref="ISessionStatus"/> 的实现：与 HTTP 调用的会话门禁同一份判定与缓存（<see cref="SessionGate"/>）。
/// </summary>
/// <param name="gate">会话门禁。</param>
public sealed class SessionStatus(SessionGate gate) : ISessionStatus
{
    /// <inheritdoc />
    public async ValueTask<bool> IsActiveAsync(Guid sessionId, CancellationToken cancellationToken)
        => (await gate.ValidateAsync(sessionId, cancellationToken).ConfigureAwait(false)).IsSuccess;
}
