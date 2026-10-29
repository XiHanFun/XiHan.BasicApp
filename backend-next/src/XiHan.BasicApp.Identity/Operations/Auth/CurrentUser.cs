// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.BasicApp.Identity.Operations.Users;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.MultiTenancy.Abstractions.Tenants;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Auth;

/// <summary>
/// 取当前主体的资料。
/// </summary>
/// <remarks>
/// 操作 <c>auth.me</c>（自助操作，只要求已认证）。按令牌 <c>sub</c> 取用户资料，附当前租户（含显示名）、会话、角色与模仿发起人。类名不用 <c>Me</c>（VB 关键字，CA1716）。
/// </remarks>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="tenants">框架租户目录（取当前租户的显示名）。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class CurrentUser(IRepository<SysUser, Guid> users, UserAccountService accounts, ITenantStore tenants) : IOperation<MeRequest, MeResponse>
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
        var tenantName = await TenantNames.ResolveAsync(tenants, security.Tenant?.Id, context.CancellationToken).ConfigureAwait(false);
        string? impersonator = null;
        if (security.Principal.ClaimValues(IdentityClaims.Actor).FirstOrDefault() is { } act && Guid.TryParse(act, out var actorId) && (await users.FindAsync(actorId, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var actor))
        {
            impersonator = actor.UserName;
        }

        return new MeResponse(
            UserResponse.From(user),
            security.Tenant?.Id,
            sid is not null && Guid.TryParse(sid, out var sessionId) ? sessionId : null,
            security.Principal.ClaimValues(IdentityClaims.Role).ToList(),
            accountSecurity.MustChangePassword,
            tenantName,
            impersonator);
    }
}
