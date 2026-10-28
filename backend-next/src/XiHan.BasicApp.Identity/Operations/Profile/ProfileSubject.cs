// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Profile;

/// <summary>
/// 个人中心操作的主体：当前令牌的用户与他的账号安全。改账号安全的操作不许在模仿会话里做——模仿者不能替别人开关双因素。
/// </summary>
internal static class ProfileSubject
{
    /// <summary>
    /// 取当前用户与账号安全；<paramref name="rejectImpersonation"/> 时模仿会话直接拒绝。
    /// </summary>
    public static async Task<Result<(SysUser User, SysUserSecurity Security)>> ResolveAsync(IRepository<SysUser, Guid> users, UserAccountService accounts, InvocationContext context, bool rejectImpersonation)
    {
        var principal = context.GetSecurity().Principal;
        if (rejectImpersonation && principal.ClaimValues(IdentityClaims.Actor).Any())
        {
            return Error.Conflict(IdentityErrors.ImpersonationActive, IdentityMessages.ImpersonationActive);
        }

        if (!Guid.TryParse(principal.Id, out var userId) || !(await users.FindAsync(userId, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var user))
        {
            return Error.NotFound(IdentityErrors.UserNotFound, IdentityMessages.UserNotFound, MessageArguments.Of(("id", principal.Id)));
        }

        var security = await accounts.SecurityOfAsync(user.Id, context.CancellationToken).ConfigureAwait(false);
        return security.TryGetValue(out var row) ? (user, row) : security.Error!;
    }
}
