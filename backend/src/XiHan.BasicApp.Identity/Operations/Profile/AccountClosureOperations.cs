// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.MultiTenancy;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Profile;

/// <summary>
/// <c>profile.deactivate-account</c> 与 <c>profile.delete-account</c> 的请求
/// </summary>
/// <param name="Password">当前密码</param>
public sealed record CloseAccountRequest([Required, StringLength(128, MinimumLength = 1)] string Password);

/// <summary>
/// 关闭账号的结果
/// </summary>
/// <param name="UserId">关闭的用户 ID</param>
public sealed record AccountClosedResponse(Guid UserId);

/// <summary>
/// 本人关闭账号的共同校验
/// </summary>
/// <remarks>
/// 模仿会话不能做，要当前密码；系统内置账号不能自助关闭，还是租户所有者的要先转移所有权（所有者跨全部租户查）
/// </remarks>
internal static class AccountClosure
{
    /// <summary>
    /// 校验并返回当前用户与账号安全
    /// </summary>
    public static async Task<Result<(SysUser User, SysUserSecurity Security)>> ResolveAsync(
        CloseAccountRequest request,
        IRepository<SysUser, Guid> users,
        UserAccountService accounts,
        IEnumerable<ITenantMembershipResolver> memberships,
        InvocationContext context)
    {
        var subject = await ProfileSubject.ResolveAsync(users, accounts, context, rejectImpersonation: true).ConfigureAwait(false);
        if (!subject.TryGetValue(out var found))
        {
            return subject.Error!;
        }

        var (user, security) = found;
        if (!PasswordHasher.Verify(request.Password, security.PasswordHash))
        {
            return Error.Denied(IdentityErrors.InvalidCredentials, IdentityMessages.InvalidCredentials);
        }

        if (user.IsSystemAccount)
        {
            return Error.Conflict(IdentityErrors.SystemAccount, IdentityMessages.SystemAccount);
        }

        foreach (var resolver in memberships)
        {
            if ((await resolver.OwnersAmongAsync([user.Id], context.CancellationToken).ConfigureAwait(false)).Contains(user.Id))
            {
                return Error.Conflict(IdentityErrors.TenantOwner, IdentityMessages.TenantOwner);
            }
        }

        return found;
    }
}

/// <summary>
/// 停用本人的账号
/// </summary>
/// <remarks>
/// <c>POST /api/profile:deactivate-account</c>。状态置为停用、吊销全部会话，之后登录给 <c>IDENTITY_DENIED_018</c>，
/// 可由管理员重新启用。校验见 <see cref="AccountClosure"/>
/// </remarks>
/// <param name="users">用户仓储</param>
/// <param name="securities">账号安全仓储</param>
/// <param name="accounts">账号域服务</param>
/// <param name="memberships">成员关系（Tenancy 实现：找出所有者）</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class DeactivateAccount(IRepository<SysUser, Guid> users, IRepository<SysUserSecurity, Guid> securities, UserAccountService accounts, IEnumerable<ITenantMembershipResolver> memberships) : IOperation<CloseAccountRequest, AccountClosedResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "profile.deactivate-account";

    /// <inheritdoc />
    public async ValueTask<Result<AccountClosedResponse>> HandleAsync(CloseAccountRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var resolved = await AccountClosure.ResolveAsync(request, users, accounts, memberships, context).ConfigureAwait(false);
        if (!resolved.TryGetValue(out var found))
        {
            return resolved.Error!;
        }

        var (user, security) = found;
        user.Status = UserStatus.Disabled;
        security.SecurityStamp = UserAccountService.NewStamp();
        var updated = await users.UpdateAsync(user, context.CancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        var stamped = await securities.UpdateAsync(security, context.CancellationToken).ConfigureAwait(false);
        if (stamped.IsFailure)
        {
            return stamped.Error!;
        }

        var revoked = await accounts.RevokeSessionsAsync(user.Id, "self-deactivated", context.CancellationToken).ConfigureAwait(false);
        return revoked.IsFailure ? revoked.Error! : new AccountClosedResponse(user.Id);
    }
}

/// <summary>
/// 注销本人的账号
/// </summary>
/// <remarks>
/// <c>POST /api/profile:delete-account</c>。账号与账号安全软删除、吊销全部会话，之后登录按账号不存在回答
/// 带密码不走 <c>DELETE</c>。校验见 <see cref="AccountClosure"/>
/// </remarks>
/// <param name="users">用户仓储</param>
/// <param name="securities">账号安全仓储</param>
/// <param name="accounts">账号域服务</param>
/// <param name="memberships">成员关系（Tenancy 实现：找出所有者）</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class DeleteAccount(IRepository<SysUser, Guid> users, IRepository<SysUserSecurity, Guid> securities, UserAccountService accounts, IEnumerable<ITenantMembershipResolver> memberships) : IOperation<CloseAccountRequest, AccountClosedResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "profile.delete-account";

    /// <inheritdoc />
    public async ValueTask<Result<AccountClosedResponse>> HandleAsync(CloseAccountRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var resolved = await AccountClosure.ResolveAsync(request, users, accounts, memberships, context).ConfigureAwait(false);
        if (!resolved.TryGetValue(out var found))
        {
            return resolved.Error!;
        }

        var (user, security) = found;
        var revoked = await accounts.RevokeSessionsAsync(user.Id, "self-deleted", context.CancellationToken).ConfigureAwait(false);
        if (revoked.IsFailure)
        {
            return revoked.Error!;
        }

        var removedSecurity = await securities.RemoveAsync(security, context.CancellationToken).ConfigureAwait(false);
        if (removedSecurity.IsFailure)
        {
            return removedSecurity.Error!;
        }

        var removed = await users.RemoveAsync(user, context.CancellationToken).ConfigureAwait(false);
        return removed.IsFailure ? removed.Error! : new AccountClosedResponse(user.Id);
    }
}
