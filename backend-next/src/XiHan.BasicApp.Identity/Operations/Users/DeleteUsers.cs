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
/// 批量删除用户（软删除）。
/// </summary>
/// <remarks>
/// <c>POST /api/users:delete</c>。不能删自己；被删用户的会话一并吊销。
/// </remarks>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号域服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = IdentityPermissions.UsersDelete)]
public sealed class DeleteUsers(IRepository<SysUser, Guid> users, UserAccountService accounts) : IOperation<UserIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "users.delete";

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
            var revoked = await accounts.RevokeSessionsAsync(user.Id, "deleted", context.CancellationToken).ConfigureAwait(false);
            if (revoked.IsFailure)
            {
                return revoked.Error!;
            }
        }

        var outcomes = await users.RemoveRangeAsync(targets, context.CancellationToken).ConfigureAwait(false);
        return UserBatch.Combine(targets, outcomes, failed);
    }
}
