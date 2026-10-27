// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Tenancy.Domain.Entities;
using XiHan.BasicApp.Tenancy.Domain.Services;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Tenancy.Operations.TenantMembers;

/// <summary>
/// 加成员：<c>tenant-members.create</c>（平台侧）。租户须存在，用户须存在且注册地不是该租户；同一用户在同一租户只有一行。
/// </summary>
/// <param name="members">成员仓储。</param>
/// <param name="users">用户仓储。</param>
/// <param name="service">租户域服务。</param>
/// <param name="ids">标识生成器。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = TenancyPermissions.TenantMembersCreate)]
public sealed class AddTenantMember(IRepository<SysTenantMember, Guid> members, IRepository<SysUser, Guid> users, TenantService service, IIdGenerator<Guid> ids) : IOperation<AddTenantMemberRequest, TenantMemberResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "tenant-members.create";

    /// <inheritdoc />
    public async ValueTask<Result<TenantMemberResponse>> HandleAsync(AddTenantMemberRequest request, InvocationContext context)
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

        if (!(await users.FindAsync(request.UserId, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var user))
        {
            return Error.NotFound(TenancyErrors.MemberUserNotFound, TenancyMessages.MemberUserNotFound, MessageArguments.Of(("userId", request.UserId)));
        }

        var userId = user.Id;
        var exists = string.Equals(user.HomeTenantId, tenantId, StringComparison.Ordinal)
            || await members.CountAsync(EntityQuery.All<SysTenantMember>().Where(new Specification<SysTenantMember>(m => m.TenantId == tenantId && m.UserId == userId)), context.CancellationToken).ConfigureAwait(false) > 0;
        if (exists)
        {
            return Error.Conflict(TenancyErrors.MemberExists, TenancyMessages.MemberExists, MessageArguments.Of(("tenantId", tenantId), ("userId", userId)));
        }

        var member = new SysTenantMember { Id = ids.NewId(), TenantId = tenantId, UserId = userId, MemberType = request.MemberType };
        var added = await members.AddAsync(member, context.CancellationToken).ConfigureAwait(false);
        return added.IsFailure ? added.Error! : TenantMemberResponse.From(member, user);
    }
}
