// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Runtime.Queries;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Users;

/// <summary>
/// 用户批量操作的共同骨架：一次主键 <c>IN</c> 取回目标行，逐项判租户归属与「不能对自己操作」，剩下的一次写入，逐项结果进 <see cref="BatchResult"/>。
/// </summary>
internal static class UserBatch
{
    /// <summary>
    /// 取回目标用户并逐项校验；返回可操作的用户与已判失败的项。
    /// </summary>
    public static async Task<(List<SysUser> Targets, List<BatchFailure> Failed)> ResolveAsync(IRepository<SysUser, Guid> users, IReadOnlyList<Guid> ids, InvocationContext context, bool rejectSelf)
    {
        var security = context.GetSecurity();
        var tenantId = security.Tenant?.Id;
        // 外部签发的令牌 sub 不一定是 GUID：解析失败即不是本应用用户，不会与任何目标相等
        var selfId = Guid.TryParse(security.Principal.Id, out var parsed) ? parsed : Guid.Empty;
        var found = (await users.FindManyAsync(ids, context.CancellationToken).ConfigureAwait(false)).ToDictionary(static u => u.Id);
        var scope = await DataScopeQuery.ResolveAsync(context).ConfigureAwait(false);
        var targets = new List<SysUser>(ids.Count);
        var failed = new List<BatchFailure>();
        foreach (var id in ids.Distinct())
        {
            if (!found.TryGetValue(id, out var user) || (tenantId is not null && !string.Equals(user.HomeTenantId, tenantId, StringComparison.Ordinal)) || !scope.Allows(id))
            {
                failed.Add(new BatchFailure(id, IdentityErrors.UserNotFound));
            }
            else if (rejectSelf && id == selfId)
            {
                failed.Add(new BatchFailure(id, IdentityErrors.SelfOperation));
            }
            else
            {
                targets.Add(user);
            }
        }

        return (targets, failed);
    }

    /// <summary>
    /// 把仓储的逐项结果并进批量结果。
    /// </summary>
    public static BatchResult Combine(IReadOnlyList<SysUser> targets, IReadOnlyList<Result> outcomes, List<BatchFailure> failed)
    {
        var succeeded = new List<Guid>(targets.Count);
        for (var i = 0; i < targets.Count; i++)
        {
            if (outcomes[i].IsSuccess)
            {
                succeeded.Add(targets[i].Id);
            }
            else
            {
                failed.Add(new BatchFailure(targets[i].Id, outcomes[i].Error!.Code));
            }
        }

        return new BatchResult(succeeded, failed);
    }
}
