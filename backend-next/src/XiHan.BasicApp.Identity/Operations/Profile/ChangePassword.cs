// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.BasicApp.Identity.Operations.Users;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Profile;

/// <summary>
/// 本人改密（自助操作，只要求已认证）：<c>POST /api/profile:change-password</c>。校验当前密码后设新密码，吊销除当前会话外的其他会话。
/// </summary>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="clock">时间源。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class ChangePassword(IRepository<SysUser, Guid> users, UserAccountService accounts, TimeProvider clock) : IOperation<ChangePasswordRequest, PasswordChangedResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "profile.change-password";

    /// <inheritdoc />
    public async ValueTask<Result<PasswordChangedResponse>> HandleAsync(ChangePasswordRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var principal = context.GetSecurity().Principal;
        if (!Guid.TryParse(principal.Id, out var userId) || !(await users.FindAsync(userId, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var user))
        {
            return Error.NotFound(IdentityErrors.UserNotFound, IdentityMessages.UserNotFound, MessageArguments.Of(("id", principal.Id)));
        }

        var securityResult = await accounts.SecurityOfAsync(user.Id, context.CancellationToken).ConfigureAwait(false);
        if (!securityResult.TryGetValue(out var security))
        {
            return securityResult.Error!;
        }

        if (!PasswordHasher.Verify(request.CurrentPassword, security.PasswordHash))
        {
            return Error.Denied(IdentityErrors.InvalidCredentials, IdentityMessages.InvalidCredentials);
        }

        var set = await accounts.SetPasswordAsync(security, request.NewPassword, mustChange: false, context.CancellationToken).ConfigureAwait(false);
        if (set.IsFailure)
        {
            return set.Error!;
        }

        var sid = principal.ClaimValues(IdentityClaims.Session).FirstOrDefault();
        var current = sid is not null && Guid.TryParse(sid, out var sessionId) ? sessionId : (Guid?)null;
        var revoked = await accounts.RevokeSessionsAsync(user.Id, "password-changed", context.CancellationToken, except: current).ConfigureAwait(false);
        return revoked.IsFailure ? revoked.Error! : new PasswordChangedResponse(user.Id, clock.GetUtcNow(), SessionsRevoked: true);
    }
}
