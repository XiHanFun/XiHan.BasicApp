// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Organization.Domain.Entities;
using XiHan.BasicApp.Organization.Domain.Services;
using XiHan.BasicApp.Organization.Operations.Memberships;
using XiHan.BasicApp.Runtime.Queries;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Organization.Queries;

/// <summary>
/// 归属列表查询（SqlSugar 直查，租户库）：筛选 <c>departmentId</c> / <c>userId</c> / <c>positionId</c>（eq）、<c>isPrimary</c>（eq）；排序 <c>order</c>、<c>createdAt</c>、<c>hiredAt</c>；名称由 <see cref="MembershipService.ComposeAsync"/> 按主键补齐。
/// </summary>
/// <param name="unitOfWork">SqlSugar 工作单元。</param>
/// <param name="service">归属域服务。</param>
public sealed class MembershipQueries(SqlSugarUnitOfWork unitOfWork, MembershipService service)
{
    private const string FieldDepartmentId = "departmentId";
    private const string FieldUserId = "userId";
    private const string FieldPositionId = "positionId";
    private const string FieldIsPrimary = "isPrimary";
    private const string FieldOrder = "order";
    private const string FieldCreatedAt = "createdAt";
    private const string FieldHiredAt = "hiredAt";

    /// <summary>
    /// 分页列出归属。
    /// </summary>
    public async ValueTask<Result<PageResult<MembershipResponse>>> ListAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var query = TenantScopedQuery.For<SysUserDepartment>(unitOfWork.ConnectionFor(typeof(SysUserDepartment)), context);
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            return Error.Validation(OrganizationErrors.QueryUnsupported, OrganizationMessages.FilterUnsupported, MessageArguments.Of(("field", "keyword"), ("operator", "contains")));
        }

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
            query = query.OrderBy(m => m.Order, OrderByType.Asc).OrderBy(m => m.CreatedAt, OrderByType.Asc);
        }

        foreach (var sort in request.Sorts)
        {
            var direction = sort.Descending ? OrderByType.Desc : OrderByType.Asc;
            var sorted = sort.Field switch
            {
                FieldOrder => query.OrderBy(m => m.Order, direction),
                FieldCreatedAt => query.OrderBy(m => m.CreatedAt, direction),
                FieldHiredAt => query.OrderBy(m => m.HiredAt, direction),
                _ => null,
            };
            if (sorted is null)
            {
                return Error.Validation(OrganizationErrors.QueryUnsupported, OrganizationMessages.SortUnsupported, MessageArguments.Of(("field", sort.Field)));
            }

            query = sorted;
        }

        var total = new RefAsync<int>();
        var rows = await query.ToPageListAsync(request.Page.Index, request.Page.Size, total, context.CancellationToken).ConfigureAwait(false);
        return new PageResult<MembershipResponse>(await service.ComposeAsync(rows, context.CancellationToken).ConfigureAwait(false), total.Value, request.Page);
    }

    private static Result<ISugarQueryable<SysUserDepartment>> Apply(ISugarQueryable<SysUserDepartment> query, PageFilter filter)
    {
        var value = filter.Value ?? string.Empty;
        return (filter.Field, filter.Operator) switch
        {
            (FieldDepartmentId, "eq") when Guid.TryParse(value, out var departmentId) => Result.Success(query.Where(m => m.DepartmentId == departmentId)),
            (FieldUserId, "eq") when Guid.TryParse(value, out var userId) => Result.Success(query.Where(m => m.UserId == userId)),
            (FieldPositionId, "eq") when Guid.TryParse(value, out var positionId) => Result.Success(query.Where(m => m.PositionId == positionId)),
            (FieldIsPrimary, "eq") when bool.TryParse(value, out var isPrimary) => Result.Success(query.Where(m => m.IsPrimary == isPrimary)),
            _ => Error.Validation(OrganizationErrors.QueryUnsupported, OrganizationMessages.FilterUnsupported, MessageArguments.Of(("field", filter.Field), ("operator", filter.Operator))),
        };
    }
}
