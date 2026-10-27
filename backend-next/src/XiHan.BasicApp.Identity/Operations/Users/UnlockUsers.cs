// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Users;

/// <summary>
/// 批量解锁用户：<c>POST /api/users:unlock</c>。状态由 <c>Locked</c> 回到 <c>Active</c>；本来就不是锁定状态的用户视为已解锁，算成功。
/// </summary>
/// <param name="users">用户仓储。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = IdentityPermissions.UsersLock)]
public sealed class UnlockUsers(IRepository<SysUser, Guid> users) : IOperation<UserIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "users.unlock";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(UserIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var (targets, failed) = await UserBatch.ResolveAsync(users, request.Ids, context, rejectSelf: false).ConfigureAwait(false);
        var locked = targets.Where(static u => u.Status == UserStatus.Locked).ToList();
        var untouched = targets.Where(static u => u.Status != UserStatus.Locked).Select(static u => u.Id).ToList();
        foreach (var user in locked)
        {
            user.Status = UserStatus.Active;
        }

        var outcomes = locked.Count == 0 ? [] : await users.UpdateRangeAsync(locked, context.CancellationToken).ConfigureAwait(false);
        var result = UserBatch.Combine(locked, outcomes, failed);
        return result with { Succeeded = [.. result.Succeeded, .. untouched] };
    }
}
