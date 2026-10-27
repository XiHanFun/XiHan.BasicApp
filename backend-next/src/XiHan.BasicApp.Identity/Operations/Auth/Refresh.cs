// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.BasicApp.Identity.Operations.Users;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Auth;

/// <summary>
/// 刷新令牌（匿名操作）：按散列找到有效会话，轮换刷新令牌、顺延会话有效期，签发新的访问令牌。会话已吊销或过期即 <c>IDENTITY_DENIED_001</c>，客户端重新登录。
/// </summary>
/// <param name="accounts">账号域服务。</param>
/// <param name="users">用户仓储。</param>
/// <param name="sessions">会话仓储。</param>
/// <param name="issuer">令牌签发器。</param>
/// <param name="options">身份配置。</param>
/// <param name="clock">时间源。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http)]
public sealed class Refresh(
    UserAccountService accounts,
    IRepository<SysUser, Guid> users,
    IRepository<SysUserSession, Guid> sessions,
    AccessTokenIssuer issuer,
    IdentityOptions options,
    TimeProvider clock) : IOperation<RefreshRequest, TokenResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "auth.refresh";

    /// <inheritdoc />
    public async ValueTask<Result<TokenResponse>> HandleAsync(RefreshRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var now = clock.GetUtcNow();
        var hash = RefreshTokens.Hash(request.RefreshToken);
        var matches = await sessions.ListAsync(EntityQuery.All<SysUserSession>().Where(new Specification<SysUserSession>(s => s.RefreshTokenHash == hash)).Page(0, 1), context.CancellationToken).ConfigureAwait(false);
        if (matches.Count == 0 || !matches[0].IsActive(now))
        {
            return Error.Denied(IdentityErrors.SessionInvalid, IdentityMessages.SessionInvalid);
        }

        var session = matches[0];
        if (!(await users.FindAsync(session.UserId, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var user) || user.Status != UserStatus.Active)
        {
            return Error.Denied(IdentityErrors.AccountUnavailable, IdentityMessages.AccountUnavailable, MessageArguments.Of(("status", user?.Status.ToString() ?? "Deleted")));
        }

        var securityResult = await accounts.SecurityOfAsync(user.Id, context.CancellationToken).ConfigureAwait(false);
        if (!securityResult.TryGetValue(out var security))
        {
            return securityResult.Error!;
        }

        // 轮换：旧刷新令牌立即失效；会话有效期按滑动口径顺延
        var refreshToken = RefreshTokens.Generate();
        session.RefreshTokenHash = RefreshTokens.Hash(refreshToken);
        session.LastSeenAt = now;
        session.RefreshExpiresAt = now + options.RefreshTokenLifetime;
        var updated = await sessions.UpdateAsync(session, context.CancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        var access = issuer.Issue(user, session.Id, session.ActiveTenantId, []);
        return new TokenResponse(access.Token, access.ExpiresAt, refreshToken, session.RefreshExpiresAt, session.Id, session.ActiveTenantId, security.MustChangePassword, UserResponse.From(user));
    }
}
