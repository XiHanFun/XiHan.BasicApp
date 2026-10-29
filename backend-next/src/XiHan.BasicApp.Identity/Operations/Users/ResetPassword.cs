// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Runtime.Queries;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Users;

/// <summary>
/// 管理员重置密码。
/// </summary>
/// <remarks>
/// <c>POST /api/users/{id}:reset-password</c>。设新密码并标记下次登录必须改密，吊销该用户全部会话。
/// </remarks>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="clock">时间源。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = IdentityPermissions.UsersResetPassword)]
public sealed class ResetPassword(IRepository<SysUser, Guid> users, UserAccountService accounts, TimeProvider clock) : IOperation<ResetPasswordRequest, PasswordChangedResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "users.reset-password";

    /// <inheritdoc />
    public async ValueTask<Result<PasswordChangedResponse>> HandleAsync(ResetPasswordRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var tenantId = context.GetSecurity().Tenant?.Id;
        if (!(await users.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var user) || (tenantId is not null && !string.Equals(user.HomeTenantId, tenantId, StringComparison.Ordinal)) || !(await DataScopeQuery.ResolveAsync(context).ConfigureAwait(false)).Allows(user.Id))
        {
            return Error.NotFound(IdentityErrors.UserNotFound, IdentityMessages.UserNotFound, MessageArguments.Of(("id", request.Id)));
        }

        var securityResult = await accounts.SecurityOfAsync(user.Id, context.CancellationToken).ConfigureAwait(false);
        if (!securityResult.TryGetValue(out var security))
        {
            return securityResult.Error!;
        }

        var set = await accounts.SetPasswordAsync(security, request.NewPassword, mustChange: true, context.CancellationToken).ConfigureAwait(false);
        if (set.IsFailure)
        {
            return set.Error!;
        }

        var revoked = await accounts.RevokeSessionsAsync(user.Id, "password-reset", context.CancellationToken).ConfigureAwait(false);
        return revoked.IsFailure ? revoked.Error! : new PasswordChangedResponse(user.Id, clock.GetUtcNow(), SessionsRevoked: true);
    }
}
