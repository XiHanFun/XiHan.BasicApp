// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Runtime.Queries;
using XiHan.BasicApp.Tenancy.Domain.Entities;
using XiHan.BasicApp.Tenancy.Operations.Tenants;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Tenancy.Queries;

/// <summary>
/// 租户列表查询（SqlSugar 直查，平台库）：关键字匹配租户 ID 与名称；筛选 <c>tenantId</c>、<c>name</c>、<c>status</c>；排序 <c>tenantId</c>、<c>name</c>、<c>order</c>、<c>createdAt</c>、<c>expiresAt</c>。
/// </summary>
/// <param name="unitOfWork">SqlSugar 工作单元。</param>
public sealed class TenantQueries(SqlSugarUnitOfWork unitOfWork)
{
    private const string FieldTenantId = "tenantId";
    private const string FieldName = "name";
    private const string FieldStatus = "status";
    private const string FieldOrder = "order";
    private const string FieldCreatedAt = "createdAt";
    private const string FieldExpiresAt = "expiresAt";

    /// <summary>
    /// 分页列出租户。
    /// </summary>
    public async ValueTask<Result<PageResult<TenantResponse>>> ListAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var query = TenantScopedQuery.For<SysTenant>(unitOfWork.ConnectionFor(typeof(SysTenant)), context);
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(t => t.TenantId.Contains(keyword) || t.Name.Contains(keyword));
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
            query = query.OrderBy(t => t.Order, OrderByType.Asc).OrderBy(t => t.TenantId, OrderByType.Asc);
        }

        foreach (var sort in request.Sorts)
        {
            var direction = sort.Descending ? OrderByType.Desc : OrderByType.Asc;
            var sorted = sort.Field switch
            {
                FieldTenantId => query.OrderBy(t => t.TenantId, direction),
                FieldName => query.OrderBy(t => t.Name, direction),
                FieldOrder => query.OrderBy(t => t.Order, direction),
                FieldCreatedAt => query.OrderBy(t => t.CreatedAt, direction),
                FieldExpiresAt => query.OrderBy(t => t.ExpiresAt, direction),
                _ => null,
            };
            if (sorted is null)
            {
                return Error.Validation(TenancyErrors.QueryUnsupported, TenancyMessages.SortUnsupported, MessageArguments.Of(("field", sort.Field)));
            }

            query = sorted;
        }

        var total = new RefAsync<int>();
        var rows = await query.ToPageListAsync(request.Page.Index, request.Page.Size, total, context.CancellationToken).ConfigureAwait(false);
        return new PageResult<TenantResponse>(rows.Select(TenantResponse.From).ToList(), total.Value, request.Page);
    }

    private static Result<ISugarQueryable<SysTenant>> Apply(ISugarQueryable<SysTenant> query, PageFilter filter)
    {
        var value = filter.Value ?? string.Empty;
        return (filter.Field, filter.Operator) switch
        {
            (FieldTenantId, "eq") => Result.Success(query.Where(t => t.TenantId == value)),
            (FieldTenantId, "contains") => Result.Success(query.Where(t => t.TenantId.Contains(value))),
            (FieldName, "contains") => Result.Success(query.Where(t => t.Name.Contains(value))),
            (FieldStatus, "eq") when Enum.TryParse<TenantStatus>(value, ignoreCase: true, out var status) => Result.Success(query.Where(t => t.Status == status)),
            _ => Error.Validation(TenancyErrors.QueryUnsupported, TenancyMessages.FilterUnsupported, MessageArguments.Of(("field", filter.Field), ("operator", filter.Operator))),
        };
    }
}
