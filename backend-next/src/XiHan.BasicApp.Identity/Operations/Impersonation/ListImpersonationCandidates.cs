// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.MultiTenancy;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Impersonation;

/// <summary>
/// 列出目标上下文里可被模仿的用户。
/// </summary>
/// <remarks>
/// 操作 <c>impersonation.candidates</c>。目标上下文里可被模仿的正常用户——注册地在该租户（或平台）的，加上该租户的成员；不含自己。
/// 范围规则与 <c>impersonation.start</c> 相同：不填租户即发起人当前上下文，租户主体只能看自己的租户。最多 20 个。
/// </remarks>
/// <param name="users">用户仓储。</param>
/// <param name="memberships">成员关系（Tenancy 实现）。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = IdentityPermissions.ImpersonationStart)]
public sealed class ListImpersonationCandidates(IRepository<SysUser, Guid> users, IEnumerable<ITenantMembershipResolver> memberships) : IOperation<ImpersonationCandidatesRequest, ImpersonationCandidatesResponse>
{
    private const int Limit = 20;

    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "impersonation.candidates";

    /// <inheritdoc />
    public async ValueTask<Result<ImpersonationCandidatesResponse>> HandleAsync(ImpersonationCandidatesRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var security = context.GetSecurity();
        var actorTenant = security.Tenant?.Id;
        var requested = ImpersonationScope.Resolve(request.TenantId, actorTenant);
        if (actorTenant is not null && !string.Equals(actorTenant, requested, StringComparison.Ordinal))
        {
            return Error.Denied(IdentityErrors.TenantNotAccessible, IdentityMessages.TenantNotAccessible, MessageArguments.Of(("tenantId", requested ?? TenantIds.Platform)));
        }

        var selfId = Guid.TryParse(security.Principal.Id, out var parsed) ? parsed : Guid.Empty;
        var home = requested ?? TenantIds.Platform;
        var keyword = request.Keyword?.Trim();
        var spec = string.IsNullOrEmpty(keyword)
            ? new Specification<SysUser>(u => u.HomeTenantId == home && u.Status == UserStatus.Active)
            : new Specification<SysUser>(u => u.HomeTenantId == home && u.Status == UserStatus.Active && (u.UserName.Contains(keyword) || u.DisplayName.Contains(keyword)));
        var natives = await users.ListAsync(EntityQuery.All<SysUser>().Where(spec).OrderBy(static u => u.UserName).Page(0, Limit + 1), context.CancellationToken).ConfigureAwait(false);
        var candidates = natives.Where(u => u.Id != selfId).ToList();
        if (requested is not null)
        {
            var memberIds = new List<Guid>();
            foreach (var resolver in memberships)
            {
                memberIds.AddRange(await resolver.MembersOfAsync(requested, context.CancellationToken).ConfigureAwait(false));
            }

            var known = candidates.Select(static u => u.Id).ToHashSet();
            var wanted = memberIds.Distinct().Where(id => id != selfId && !known.Contains(id)).ToList();
            if (wanted.Count > 0)
            {
                var members = await users.FindManyAsync(wanted, context.CancellationToken).ConfigureAwait(false);
                candidates.AddRange(members.Where(u => u.Status == UserStatus.Active && (string.IsNullOrEmpty(keyword) || u.UserName.Contains(keyword, StringComparison.OrdinalIgnoreCase) || u.DisplayName.Contains(keyword, StringComparison.OrdinalIgnoreCase))));
            }
        }

        var items = candidates.OrderBy(static u => u.UserName, StringComparer.Ordinal).Take(Limit).Select(ImpersonationCandidateResponse.From).ToList();
        return new ImpersonationCandidatesResponse(items);
    }
}
