// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.MultiTenancy;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Users;

/// <summary>
/// 批量停用用户。
/// </summary>
/// <remarks>
/// <c>POST /api/users:disable</c>（照 4.x 的启停）。状态置为 <c>Disabled</c>、吊销全部会话，之后登录给 <c>IDENTITY_DENIED_018</c>；
/// 不能停用自己，租户的所有者要先转移所有权。锁定中的用户停用后也是停用（解锁不再适用，要启用）。
/// </remarks>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号服务（吊销会话）。</param>
/// <param name="memberships">租户成员解析（找出所有者）。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = IdentityPermissions.UsersDisable)]
public sealed class DisableUsers(IRepository<SysUser, Guid> users, UserAccountService accounts, IEnumerable<ITenantMembershipResolver> memberships) : IOperation<UserIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "users.disable";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(UserIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var (targets, failed) = await UserBatch.ResolveAsync(users, request.Ids, context, rejectSelf: true).ConfigureAwait(false);
        await UserBatch.ExcludeOwnersAsync(targets, failed, memberships, context.CancellationToken).ConfigureAwait(false);
        if (targets.Count == 0)
        {
            return new BatchResult([], failed);
        }

        foreach (var user in targets)
        {
            user.Status = UserStatus.Disabled;
            var revoked = await accounts.RevokeSessionsAsync(user.Id, "disabled", context.CancellationToken).ConfigureAwait(false);
            if (revoked.IsFailure)
            {
                return revoked.Error!;
            }
        }

        var outcomes = await users.UpdateRangeAsync(targets, context.CancellationToken).ConfigureAwait(false);
        return UserBatch.Combine(targets, outcomes, failed);
    }
}

/// <summary>
/// 批量启用用户。
/// </summary>
/// <remarks>
/// <c>POST /api/users:enable</c>。状态由 <c>Disabled</c> 回到 <c>Active</c>；本来就是正常的用户视为已启用、算成功，锁定中的不动（要解锁）。
/// </remarks>
/// <param name="users">用户仓储。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = IdentityPermissions.UsersDisable)]
public sealed class EnableUsers(IRepository<SysUser, Guid> users) : IOperation<UserIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "users.enable";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(UserIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var (targets, failed) = await UserBatch.ResolveAsync(users, request.Ids, context, rejectSelf: false).ConfigureAwait(false);
        var disabled = targets.Where(static u => u.Status == UserStatus.Disabled).ToList();
        var untouched = targets.Where(static u => u.Status == UserStatus.Active).Select(static u => u.Id).ToList();
        var locked = targets.Where(static u => u.Status == UserStatus.Locked)
            .Select(static u => new BatchFailure(u.Id, IdentityErrors.UserLocked))
            .ToList();
        foreach (var user in disabled)
        {
            user.Status = UserStatus.Active;
        }

        var outcomes = disabled.Count == 0 ? [] : await users.UpdateRangeAsync(disabled, context.CancellationToken).ConfigureAwait(false);
        var result = UserBatch.Combine(disabled, outcomes, [.. failed, .. locked]);
        return result with { Succeeded = [.. result.Succeeded, .. untouched] };
    }
}
