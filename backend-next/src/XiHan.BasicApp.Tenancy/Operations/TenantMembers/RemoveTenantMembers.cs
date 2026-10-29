// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Tenancy.Domain.Entities;
using XiHan.BasicApp.Tenancy.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Tenancy.Operations.TenantMembers;

/// <summary>
/// 批量移除成员。
/// </summary>
/// <remarks>
/// <c>tenant-members.delete</c>（平台侧）。移除后该用户不能再切换进该租户；已签发的会话到刷新时才按新关系校验。
/// </remarks>
/// <param name="members">成员仓储。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = TenancyPermissions.TenantMembersDelete)]
public sealed class RemoveTenantMembers(IRepository<SysTenantMember, Guid> members) : IOperation<TenantMemberIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "tenant-members.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(TenantMemberIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (TenantService.RequirePlatform(context) is { } denied)
        {
            return denied;
        }

        var found = (await members.FindManyAsync(request.Ids, context.CancellationToken).ConfigureAwait(false)).ToDictionary(static m => m.Id);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in request.Ids.Distinct())
        {
            if (!found.TryGetValue(id, out var member))
            {
                failed.Add(new BatchFailure(id, TenancyErrors.MemberNotFound));
                continue;
            }

            var removed = await members.RemoveAsync(member, context.CancellationToken).ConfigureAwait(false);
            if (removed.IsFailure)
            {
                return removed.Error!;
            }

            succeeded.Add(id);
        }

        return new BatchResult(succeeded, failed);
    }
}
