// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Tenancy.Domain.Entities;
using XiHan.BasicApp.Tenancy.Domain.Services;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Tenancy.Operations.TenantMembers;

/// <summary>
/// 某租户的成员：<c>tenant-members.list</c>（平台侧）。成员行在平台库、用户也在平台库，按主键一次取回补登录名与显示名。
/// </summary>
/// <param name="members">成员仓储。</param>
/// <param name="users">用户仓储。</param>
/// <param name="service">租户域服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = TenancyPermissions.TenantMembersRead)]
public sealed class ListTenantMembers(IRepository<SysTenantMember, Guid> members, IRepository<SysUser, Guid> users, TenantService service) : IOperation<TenantMembersRequest, TenantMembersResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "tenant-members.list";

    /// <inheritdoc />
    public async ValueTask<Result<TenantMembersResponse>> HandleAsync(TenantMembersRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (TenantService.RequirePlatform(context) is { } denied)
        {
            return denied;
        }

        var tenantId = request.TenantId.Trim();
        if (!(await service.FindByTenantIdAsync(tenantId, context.CancellationToken).ConfigureAwait(false)).HasValue)
        {
            return Error.NotFound(TenancyErrors.TenantNotFound, TenancyMessages.TenantNotFound, MessageArguments.Of(("id", tenantId)));
        }

        var rows = await members.ListAsync(
            EntityQuery.All<SysTenantMember>().Where(new Specification<SysTenantMember>(m => m.TenantId == tenantId)).OrderBy(static m => m.CreatedAt),
            context.CancellationToken).ConfigureAwait(false);
        var people = (await users.FindManyAsync(rows.Select(static m => m.UserId).Distinct().ToList(), context.CancellationToken).ConfigureAwait(false)).ToDictionary(static u => u.Id);
        var items = new List<TenantMemberResponse>(rows.Count);
        foreach (var row in rows)
        {
            // 用户已删除的成员行不再展示（删除用户时成员行随后续批次一并清理）
            if (people.TryGetValue(row.UserId, out var user))
            {
                items.Add(TenantMemberResponse.From(row, user));
            }
        }

        return new TenantMembersResponse(items);
    }
}
