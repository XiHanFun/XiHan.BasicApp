// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Organization.Domain.Entities;
using XiHan.BasicApp.Organization.Operations.Positions;
using XiHan.BasicApp.Runtime.Queries;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Organization.Queries;

/// <summary>
/// 岗位列表查询（SqlSugar 直查，租户库）：关键字匹配编码与名称；筛选 <c>code</c>、<c>name</c>、<c>status</c>；排序 <c>code</c>、<c>name</c>、<c>order</c>、<c>createdAt</c>。
/// </summary>
/// <param name="unitOfWork">SqlSugar 工作单元。</param>
public sealed class PositionQueries(SqlSugarUnitOfWork unitOfWork)
{
    private const string FieldCode = "code";
    private const string FieldName = "name";
    private const string FieldStatus = "status";
    private const string FieldOrder = "order";
    private const string FieldCreatedAt = "createdAt";

    /// <summary>
    /// 分页列出岗位。
    /// </summary>
    public async ValueTask<Result<PageResult<PositionResponse>>> ListAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var query = TenantScopedQuery.For<SysPosition>(unitOfWork.ConnectionFor(typeof(SysPosition)), context);
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(p => p.Code.Contains(keyword) || p.Name.Contains(keyword));
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
            query = query.OrderBy(p => p.Order, OrderByType.Asc).OrderBy(p => p.Code, OrderByType.Asc);
        }

        foreach (var sort in request.Sorts)
        {
            var direction = sort.Descending ? OrderByType.Desc : OrderByType.Asc;
            var sorted = sort.Field switch
            {
                FieldCode => query.OrderBy(p => p.Code, direction),
                FieldName => query.OrderBy(p => p.Name, direction),
                FieldOrder => query.OrderBy(p => p.Order, direction),
                FieldCreatedAt => query.OrderBy(p => p.CreatedAt, direction),
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
        return new PageResult<PositionResponse>(rows.Select(PositionResponse.From).ToList(), total.Value, request.Page);
    }

    private static Result<ISugarQueryable<SysPosition>> Apply(ISugarQueryable<SysPosition> query, PageFilter filter)
    {
        var value = filter.Value ?? string.Empty;
        return (filter.Field, filter.Operator) switch
        {
            (FieldCode, "eq") => Result.Success(query.Where(p => p.Code == value)),
            (FieldCode, "contains") => Result.Success(query.Where(p => p.Code.Contains(value))),
            (FieldName, "contains") => Result.Success(query.Where(p => p.Name.Contains(value))),
            (FieldStatus, "eq") when Enum.TryParse<OrganizationStatus>(value, ignoreCase: true, out var status) => Result.Success(query.Where(p => p.Status == status)),
            _ => Error.Validation(OrganizationErrors.QueryUnsupported, OrganizationMessages.FilterUnsupported, MessageArguments.Of(("field", filter.Field), ("operator", filter.Operator))),
        };
    }
}
