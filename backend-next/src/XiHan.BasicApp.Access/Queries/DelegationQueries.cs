// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq.Expressions;
using SqlSugar;
using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Access.Operations.Delegations;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Runtime.Queries;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Queries;

/// <summary>
/// 委托列表查询（当前一侧）。
/// </summary>
/// <remarks>
/// 关键字匹配权限码与原因；筛选状态（按撤销时间与有效期换算成条件）、委托人、被委托人、权限码与角色；排序创建、生效与到期时间。
/// </remarks>
/// <param name="unitOfWork">工作单元（取委托所在的连接）。</param>
/// <param name="service">委托服务（显示名）。</param>
/// <param name="clock">时间源。</param>
public sealed class DelegationQueries(SqlSugarUnitOfWork unitOfWork, DelegationService service, TimeProvider clock)
{
    private const string FieldStatus = "status";
    private const string FieldDelegatorId = "delegatorId";
    private const string FieldDelegateeId = "delegateeId";
    private const string FieldPermission = "permission";
    private const string FieldRoleId = "roleId";
    private const string FieldEffectiveAt = "effectiveAt";
    private const string FieldExpiresAt = "expiresAt";
    private const string FieldCreatedAt = "createdAt";

    /// <summary>
    /// 分页列出。
    /// </summary>
    public async ValueTask<Result<PageResult<DelegationResponse>>> ListAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var now = clock.GetUtcNow();
        var query = TenantScopedQuery.For<SysPermissionDelegation>(unitOfWork.ConnectionFor(typeof(SysPermissionDelegation)), context).OwnedByCurrentTenant(context);
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(d => (d.Permission != null && d.Permission.Contains(keyword)) || (d.Reason != null && d.Reason.Contains(keyword)));
        }

        foreach (var filter in request.Filters)
        {
            var filtered = Apply(query, filter, now);
            if (!filtered.TryGetValue(out query))
            {
                return filtered.Error!;
            }
        }

        if (request.Sorts.Count == 0)
        {
            query = query.OrderBy(d => d.CreatedAt, OrderByType.Desc);
        }

        foreach (var sort in request.Sorts)
        {
            var direction = sort.Descending ? OrderByType.Desc : OrderByType.Asc;
            var sorted = sort.Field switch
            {
                FieldEffectiveAt => query.OrderBy(d => d.EffectiveAt, direction),
                FieldExpiresAt => query.OrderBy(d => d.ExpiresAt, direction),
                FieldCreatedAt => query.OrderBy(d => d.CreatedAt, direction),
                _ => null,
            };
            if (sorted is null)
            {
                return Error.Validation(AccessErrors.QueryUnsupported, AccessMessages.SortUnsupported, MessageArguments.Of(("field", sort.Field)));
            }

            query = sorted;
        }

        var total = new RefAsync<int>();
        var rows = await query.ToPageListAsync(request.Page.Index, request.Page.Size, total, context.CancellationToken).ConfigureAwait(false);
        var (users, roles) = await service.NamesAsync(rows, RoleService.OwnerOf(context), context.CancellationToken).ConfigureAwait(false);
        return new PageResult<DelegationResponse>(rows.Select(d => DelegationResponse.From(d, now, users, roles)).ToList(), total.Value, request.Page);
    }

    private static Result<ISugarQueryable<SysPermissionDelegation>> Apply(ISugarQueryable<SysPermissionDelegation> query, PageFilter filter, DateTimeOffset now)
    {
        var value = filter.Value ?? string.Empty;
        return (filter.Field, filter.Operator) switch
        {
            (FieldStatus, "eq" or "in") when StatusesOf(value) is { } statuses => Result.Success(query.Where(StatusPredicate(statuses, now))),
            (FieldDelegatorId, "eq") when Guid.TryParse(value, out var delegatorId) => Result.Success(query.Where(d => d.DelegatorId == delegatorId)),
            (FieldDelegateeId, "eq") when Guid.TryParse(value, out var delegateeId) => Result.Success(query.Where(d => d.DelegateeId == delegateeId)),
            (FieldPermission, "eq") => Result.Success(query.Where(d => d.Permission == value)),
            (FieldPermission, "contains") => Result.Success(query.Where(d => d.Permission != null && d.Permission.Contains(value))),
            (FieldRoleId, "eq") when Guid.TryParse(value, out var roleId) => Result.Success(query.Where(d => d.RoleId == roleId)),
            _ => Error.Validation(AccessErrors.QueryUnsupported, AccessMessages.FilterUnsupported, MessageArguments.Of(("field", filter.Field), ("operator", filter.Operator))),
        };
    }

    // 状态不落库：各状态换算成撤销时间与有效期上的条件，多个状态取或（照 Runtime 数据范围的写法拼表达式树）。
    // SqlSugar 把「!可空.HasValue || …」译丢 OR，生效中写成 !(HasValue && 生效时间 > now)；可空时间不取 .Value，否则绕过列转换器
    private static Expression<Func<SysPermissionDelegation, bool>> StatusPredicate(IReadOnlyCollection<DelegationStatus> statuses, DateTimeOffset now)
    {
        Expression<Func<SysPermissionDelegation, bool>>? combined = null;
        foreach (var status in statuses.OrderByDescending(static s => s))
        {
            Expression<Func<SysPermissionDelegation, bool>> next = status switch
            {
                DelegationStatus.Revoked => d => d.RevokedAt.HasValue,
                DelegationStatus.Expired => d => !d.RevokedAt.HasValue && d.ExpiresAt <= now,
                DelegationStatus.Pending => d => !d.RevokedAt.HasValue && d.ExpiresAt > now && d.EffectiveAt.HasValue && d.EffectiveAt > now,
                _ => d => !d.RevokedAt.HasValue && d.ExpiresAt > now && !(d.EffectiveAt.HasValue && d.EffectiveAt > now),
            };
            combined = combined is null
                ? next
                : Expression.Lambda<Func<SysPermissionDelegation, bool>>(Expression.OrElse(combined.Body, new ParameterReplacer(next.Parameters[0], combined.Parameters[0]).Visit(next.Body)), combined.Parameters[0]);
        }

        return combined!;
    }

    private static List<DelegationStatus>? StatusesOf(string value)
    {
        var parsed = new List<DelegationStatus>();
        foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!Enum.TryParse<DelegationStatus>(part, ignoreCase: true, out var status) || !Enum.IsDefined(status))
            {
                return null;
            }

            parsed.Add(status);
        }

        return parsed.Count == 0 ? null : parsed.Distinct().ToList();
    }

    private sealed class ParameterReplacer(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : base.VisitParameter(node);
    }
}
