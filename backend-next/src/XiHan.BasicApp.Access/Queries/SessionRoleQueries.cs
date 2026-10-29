// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Operations.SessionRoles;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Runtime.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Queries;

/// <summary>
/// 会话角色列表查询。
/// </summary>
/// <remarks>
/// SqlSugar 直查，作用侧 = 当前租户或平台。
/// 筛选 <c>sessionId / userId / roleId</c> 的 <c>eq</c> 与 <c>status</c> 的 <c>eq / in</c>；
/// 排序 <c>activatedAt / deactivatedAt / status</c>，缺省按激活时间倒序。用户与角色补上名称。
/// </remarks>
/// <param name="unitOfWork">SqlSugar 工作单元。</param>
/// <param name="rows">会话角色仓储。</param>
/// <param name="users">用户仓储。</param>
/// <param name="roles">角色仓储。</param>
public sealed class SessionRoleQueries(SqlSugarUnitOfWork unitOfWork, IRepository<SysSessionRole, Guid> rows, IRepository<SysUser, Guid> users, IRepository<SysRole, Guid> roles)
{
    private const string FieldSessionId = "sessionId";
    private const string FieldUserId = "userId";
    private const string FieldRoleId = "roleId";
    private const string FieldStatus = "status";
    private const string FieldActivatedAt = "activatedAt";
    private const string FieldDeactivatedAt = "deactivatedAt";

    /// <summary>
    /// 分页列出。
    /// </summary>
    public async ValueTask<Result<PageResult<SessionRoleResponse>>> ListAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var query = TenantScopedQuery.For<SysSessionRole>(unitOfWork.ConnectionFor(typeof(SysSessionRole)), context).OwnedByCurrentTenant(context);
        foreach (var filter in request.Filters)
        {
            var filtered = Apply(query, filter);
            if (!filtered.TryGetValue(out query))
            {
                return filtered.Error!;
            }
        }

        if (request.Sorts.Count == 0)
        {
            query = query.OrderBy(r => r.ActivatedAt, OrderByType.Desc);
        }

        foreach (var sort in request.Sorts)
        {
            var direction = sort.Descending ? OrderByType.Desc : OrderByType.Asc;
            var sorted = sort.Field switch
            {
                FieldActivatedAt => query.OrderBy(r => r.ActivatedAt, direction),
                FieldDeactivatedAt => query.OrderBy(r => r.DeactivatedAt, direction),
                FieldStatus => query.OrderBy(r => r.Status, direction),
                _ => null,
            };
            if (sorted is null)
            {
                return Error.Validation(AccessErrors.QueryUnsupported, AccessMessages.SortUnsupported, MessageArguments.Of(("field", sort.Field)));
            }

            query = sorted;
        }

        var total = new RefAsync<int>();
        var page = await query.ToPageListAsync(request.Page.Index, request.Page.Size, total, context.CancellationToken).ConfigureAwait(false);
        return new PageResult<SessionRoleResponse>(await ProjectAsync(page, context.CancellationToken).ConfigureAwait(false), total.Value, request.Page);
    }

    /// <summary>
    /// 按 ID 取这一侧的一行会话角色。
    /// </summary>
    /// <returns>会话角色；没有为空。</returns>
    public async Task<SessionRoleResponse?> GetAsync(Guid id, string owner, CancellationToken cancellationToken)
    {
        if (!(await rows.FindAsync(id, cancellationToken).ConfigureAwait(false)).TryGetValue(out var row) || !string.Equals(row.OwnerTenantId, owner, StringComparison.Ordinal))
        {
            return null;
        }

        return (await ProjectAsync([row], cancellationToken).ConfigureAwait(false))[0];
    }

    private async Task<IReadOnlyList<SessionRoleResponse>> ProjectAsync(List<SysSessionRole> page, CancellationToken cancellationToken)
    {
        if (page.Count == 0)
        {
            return [];
        }

        var userNames = (await users.FindManyAsync([.. page.Select(static r => r.UserId).Distinct()], cancellationToken).ConfigureAwait(false)).ToDictionary(static u => u.Id, static u => u.UserName);
        var roleRows = (await roles.FindManyAsync([.. page.Select(static r => r.RoleId).Distinct()], cancellationToken).ConfigureAwait(false)).ToDictionary(static r => r.Id);
        return [.. page.Select(r => new SessionRoleResponse(
            r.Id, r.SessionId, r.UserId, userNames.GetValueOrDefault(r.UserId), r.RoleId,
            roleRows.TryGetValue(r.RoleId, out var role) ? role.Name : null,
            roleRows.TryGetValue(r.RoleId, out var found) ? found.Code : null,
            r.Status, r.ActivatedAt, r.DeactivatedAt, r.Reason))];
    }

    private static Result<ISugarQueryable<SysSessionRole>> Apply(ISugarQueryable<SysSessionRole> query, PageFilter filter)
    {
        var value = filter.Value ?? string.Empty;
        return (filter.Field, filter.Operator) switch
        {
            (FieldSessionId, "eq") when Guid.TryParse(value, out var sessionId) => Result.Success(query.Where(r => r.SessionId == sessionId)),
            (FieldUserId, "eq") when Guid.TryParse(value, out var userId) => Result.Success(query.Where(r => r.UserId == userId)),
            (FieldRoleId, "eq") when Guid.TryParse(value, out var roleId) => Result.Success(query.Where(r => r.RoleId == roleId)),
            (FieldStatus, "eq" or "in") when StatusesOf(value) is { } statuses => Result.Success(query.Where(r => statuses.Contains(r.Status))),
            _ => Error.Validation(AccessErrors.QueryUnsupported, AccessMessages.FilterUnsupported, MessageArguments.Of(("field", filter.Field), ("operator", filter.Operator))),
        };
    }

    private static List<SessionRoleStatus>? StatusesOf(string value)
    {
        var parsed = new List<SessionRoleStatus>();
        foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!Enum.TryParse<SessionRoleStatus>(part, ignoreCase: true, out var status) || !Enum.IsDefined(status))
            {
                return null;
            }

            parsed.Add(status);
        }

        return parsed.Count == 0 ? null : parsed;
    }
}
