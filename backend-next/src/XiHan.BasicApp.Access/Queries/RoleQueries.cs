// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Operations.Roles;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Runtime.Queries;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Queries;

/// <summary>
/// 角色列表查询。
/// </summary>
/// <remarks>
/// SqlSugar 直查，作用侧 = 当前租户或平台。
/// 关键字匹配角色码与名称；筛选 <c>code</c>、<c>name</c>、<c>status</c>；排序 <c>code</c>、<c>name</c>、<c>order</c>、<c>createdAt</c>。
/// </remarks>
/// <param name="unitOfWork">SqlSugar 工作单元。</param>
public sealed class RoleQueries(SqlSugarUnitOfWork unitOfWork)
{
    private const string FieldCode = "code";
    private const string FieldName = "name";
    private const string FieldStatus = "status";
    private const string FieldOrder = "order";
    private const string FieldCreatedAt = "createdAt";

    /// <summary>
    /// 分页列出当前作用侧的角色。
    /// </summary>
    public async ValueTask<Result<PageResult<RoleResponse>>> ListAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var query = TenantScopedQuery.For<SysRole>(unitOfWork.ConnectionFor(typeof(SysRole)), context).OwnedByCurrentTenant(context);
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(r => r.Code.Contains(keyword) || r.Name.Contains(keyword));
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
            query = query.OrderBy(r => r.Order, OrderByType.Asc).OrderBy(r => r.Code, OrderByType.Asc);
        }

        foreach (var sort in request.Sorts)
        {
            var direction = sort.Descending ? OrderByType.Desc : OrderByType.Asc;
            var sorted = sort.Field switch
            {
                FieldCode => query.OrderBy(r => r.Code, direction),
                FieldName => query.OrderBy(r => r.Name, direction),
                FieldOrder => query.OrderBy(r => r.Order, direction),
                FieldCreatedAt => query.OrderBy(r => r.CreatedAt, direction),
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
        return new PageResult<RoleResponse>(rows.Select(RoleResponse.From).ToList(), total.Value, request.Page);
    }

    private static Result<ISugarQueryable<SysRole>> Apply(ISugarQueryable<SysRole> query, PageFilter filter)
    {
        var value = filter.Value ?? string.Empty;
        return (filter.Field, filter.Operator) switch
        {
            (FieldCode, "eq") => Result.Success(query.Where(r => r.Code == value)),
            (FieldCode, "contains") => Result.Success(query.Where(r => r.Code.Contains(value))),
            (FieldName, "contains") => Result.Success(query.Where(r => r.Name.Contains(value))),
            (FieldStatus, "eq") when Enum.TryParse<RoleStatus>(value, ignoreCase: true, out var status) => Result.Success(query.Where(r => r.Status == status)),
            _ => Error.Validation(AccessErrors.QueryUnsupported, AccessMessages.FilterUnsupported, MessageArguments.Of(("field", filter.Field), ("operator", filter.Operator))),
        };
    }
}
