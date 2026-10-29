// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Runtime.Queries;
using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.BasicApp.Settings.Operations.Dicts;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Queries;

/// <summary>
/// 字典列表查询（SqlSugar 直查，平台库，读共享）。
/// </summary>
/// <remarks>
/// 关键字匹配编码、名称与分类；筛选 <c>code</c>、<c>name</c>、<c>category</c>、<c>status</c>、<c>owner</c>（<c>platform</c> / <c>tenant</c>）、<c>isBuiltIn</c>；
/// 排序 <c>code</c>、<c>name</c>、<c>category</c>、<c>order</c>、<c>createdAt</c>。
/// </remarks>
/// <param name="unitOfWork">SqlSugar 工作单元。</param>
public sealed class DictQueries(SqlSugarUnitOfWork unitOfWork)
{
    private const string FieldCode = "code";
    private const string FieldName = "name";
    private const string FieldCategory = "category";
    private const string FieldStatus = "status";
    private const string FieldOwner = "owner";
    private const string FieldBuiltIn = "isBuiltIn";
    private const string FieldOrder = "order";
    private const string FieldCreatedAt = "createdAt";

    /// <summary>
    /// 分页列出字典。
    /// </summary>
    public async ValueTask<Result<PageResult<DictResponse>>> ListAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var query = TenantScopedQuery.For<SysDict>(unitOfWork.ConnectionFor(typeof(SysDict)), context).OwnedByCurrentTenantOrPlatform(context);
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(d => d.Code.Contains(keyword) || d.Name.Contains(keyword) || (d.Category != null && d.Category.Contains(keyword)));
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
            query = query.OrderBy(d => d.Order, OrderByType.Asc).OrderBy(d => d.Category, OrderByType.Asc).OrderBy(d => d.Code, OrderByType.Asc);
        }

        foreach (var sort in request.Sorts)
        {
            var direction = sort.Descending ? OrderByType.Desc : OrderByType.Asc;
            var sorted = sort.Field switch
            {
                FieldCode => query.OrderBy(d => d.Code, direction),
                FieldName => query.OrderBy(d => d.Name, direction),
                FieldCategory => query.OrderBy(d => d.Category, direction),
                FieldOrder => query.OrderBy(d => d.Order, direction),
                FieldCreatedAt => query.OrderBy(d => d.CreatedAt, direction),
                _ => null,
            };
            if (sorted is null)
            {
                return Error.Validation(SettingsErrors.QueryUnsupported, SettingsMessages.SortUnsupported, MessageArguments.Of(("field", sort.Field)));
            }

            query = sorted;
        }

        var total = new RefAsync<int>();
        var rows = await query.ToPageListAsync(request.Page.Index, request.Page.Size, total, context.CancellationToken).ConfigureAwait(false);
        return new PageResult<DictResponse>(rows.Select(DictResponse.From).ToList(), total.Value, request.Page);
    }

    private static Result<ISugarQueryable<SysDict>> Apply(ISugarQueryable<SysDict> query, PageFilter filter)
    {
        var value = filter.Value ?? string.Empty;
        const string platform = TenantIds.Platform;
        return (filter.Field, filter.Operator) switch
        {
            (FieldCode, "eq") => Result.Success(query.Where(d => d.Code == value)),
            (FieldCode, "contains") => Result.Success(query.Where(d => d.Code.Contains(value))),
            (FieldName, "contains") => Result.Success(query.Where(d => d.Name.Contains(value))),
            (FieldCategory, "eq") => Result.Success(query.Where(d => d.Category == value)),
            (FieldStatus, "eq") when Enum.TryParse<SettingStatus>(value, ignoreCase: true, out var status) => Result.Success(query.Where(d => d.Status == status)),
            (FieldOwner, "eq") when value == "platform" => Result.Success(query.Where(d => d.OwnerTenantId == platform)),
            (FieldOwner, "eq") when value == "tenant" => Result.Success(query.Where(d => d.OwnerTenantId != platform)),
            (FieldBuiltIn, "eq") when bool.TryParse(value, out var builtIn) => Result.Success(query.Where(d => d.IsBuiltIn == builtIn)),
            _ => Error.Validation(SettingsErrors.QueryUnsupported, SettingsMessages.FilterUnsupported, MessageArguments.Of(("field", filter.Field), ("operator", filter.Operator))),
        };
    }
}
