// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Users;

/// <summary>
/// 批量锁定用户：<c>POST /api/users:lock</c>。状态置为 <c>Locked</c>、吊销全部会话；不能锁自己。
/// </summary>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号域服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = IdentityPermissions.UsersLock)]
public sealed class LockUsers(IRepository<SysUser, Guid> users, UserAccountService accounts) : IOperation<UserIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "users.lock";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(UserIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var (targets, failed) = await UserBatch.ResolveAsync(users, request.Ids, context, rejectSelf: true).ConfigureAwait(false);
        if (targets.Count == 0)
        {
            return new BatchResult([], failed);
        }

        foreach (var user in targets)
        {
            user.Status = UserStatus.Locked;
            var revoked = await accounts.RevokeSessionsAsync(user.Id, "locked", context.CancellationToken).ConfigureAwait(false);
            if (revoked.IsFailure)
            {
                return revoked.Error!;
            }
        }

        var outcomes = await users.UpdateRangeAsync(targets, context.CancellationToken).ConfigureAwait(false);
        return UserBatch.Combine(targets, outcomes, failed);
    }
}
