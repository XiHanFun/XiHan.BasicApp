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

namespace XiHan.BasicApp.Identity.Operations.Auth;

/// <summary>
/// 当前主体 <c>auth.me</c>（自助操作，只要求已认证）：按令牌 <c>sub</c> 取用户资料，附当前租户、会话与角色。类名不用 <c>Me</c>（VB 关键字，CA1716）。
/// </summary>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号域服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class CurrentUser(IRepository<SysUser, Guid> users, UserAccountService accounts) : IOperation<MeRequest, MeResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "auth.me";

    /// <inheritdoc />
    public async ValueTask<Result<MeResponse>> HandleAsync(MeRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var security = context.GetSecurity();
        if (!Guid.TryParse(security.Principal.Id, out var userId) || !(await users.FindAsync(userId, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var user))
        {
            return Error.NotFound(IdentityErrors.UserNotFound, IdentityMessages.UserNotFound, MessageArguments.Of(("id", security.Principal.Id)));
        }

        var account = await accounts.SecurityOfAsync(user.Id, context.CancellationToken).ConfigureAwait(false);
        if (!account.TryGetValue(out var accountSecurity))
        {
            return account.Error!;
        }

        var sid = security.Principal.ClaimValues(IdentityClaims.Session).FirstOrDefault();
        return new MeResponse(
            UserResponse.From(user),
            security.Tenant?.Id,
            sid is not null && Guid.TryParse(sid, out var sessionId) ? sessionId : null,
            security.Principal.ClaimValues(IdentityClaims.Role).ToList(),
            accountSecurity.MustChangePassword);
    }
}
