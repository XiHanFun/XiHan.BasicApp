// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.MultiTenancy;
using XiHan.BasicApp.Contracts.Paging;
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
/// 操作 <c>impersonation.candidates</c>。目标上下文里可被模仿的正常用户——注册地在该租户（或平台）的，加上该租户生效中的普通成员（运维支持不算）；不含自己。
/// 范围规则与 <c>impersonation.start</c> 相同：不填租户即发起人当前上下文，租户主体只能看自己的租户。
/// 注册地用户与成员在同一个查询里按登录名排序分页，带总数。
/// </remarks>
/// <param name="users">用户仓储。</param>
/// <param name="memberships">成员关系（Tenancy 实现）。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = IdentityPermissions.ImpersonationStart)]
public sealed class ListImpersonationCandidates(IRepository<SysUser, Guid> users, IEnumerable<ITenantMembershipResolver> memberships) : IOperation<ImpersonationCandidatesRequest, PageResult<ImpersonationCandidateResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "impersonation.candidates";

    /// <inheritdoc />
    public async ValueTask<Result<PageResult<ImpersonationCandidateResponse>>> HandleAsync(ImpersonationCandidatesRequest request, InvocationContext context)
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
        var memberIds = new List<Guid>();
        if (requested is not null)
        {
            foreach (var resolver in memberships)
            {
                memberIds.AddRange(await resolver.MembersOfAsync(requested, context.CancellationToken).ConfigureAwait(false));
            }
        }

        var spec = new Specification<SysUser>(Scope(home, memberIds.Distinct().ToList(), selfId, request.Keyword?.Trim()));
        var total = await users.CountAsync(EntityQuery.All<SysUser>().Where(spec), context.CancellationToken).ConfigureAwait(false);
        var page = new PageInfo { Index = request.PageIndex, Size = request.PageSize };
        var rows = await users.ListAsync(EntityQuery.All<SysUser>().Where(spec).OrderBy(static u => u.UserName).Page(page.Skip, page.Size), context.CancellationToken).ConfigureAwait(false);
        return new PageResult<ImpersonationCandidateResponse>(rows.Select(ImpersonationCandidateResponse.From).ToList(), total, page);
    }

    /// <summary>
    /// 候选的条件。
    /// </summary>
    /// <remarks>
    /// 启用、不是自己，注册地在目标上下文或是其成员；有关键字时再匹配登录名或显示名。
    /// </remarks>
    private static System.Linq.Expressions.Expression<Func<SysUser, bool>> Scope(string home, List<Guid> memberIds, Guid selfId, string? keyword) =>
        (memberIds.Count == 0, string.IsNullOrEmpty(keyword)) switch
        {
            (true, true) => u => u.Status == UserStatus.Active && u.Id != selfId && u.HomeTenantId == home,
            (true, false) => u => u.Status == UserStatus.Active && u.Id != selfId && u.HomeTenantId == home && (u.UserName.Contains(keyword!) || u.DisplayName.Contains(keyword!)),
            (false, true) => u => u.Status == UserStatus.Active && u.Id != selfId && (u.HomeTenantId == home || memberIds.Contains(u.Id)),
            (false, false) => u => u.Status == UserStatus.Active && u.Id != selfId && (u.HomeTenantId == home || memberIds.Contains(u.Id)) && (u.UserName.Contains(keyword!) || u.DisplayName.Contains(keyword!)),
        };
}
