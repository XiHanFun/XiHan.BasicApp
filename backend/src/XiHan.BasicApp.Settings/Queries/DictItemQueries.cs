// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Runtime.Queries;
using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.BasicApp.Settings.Domain.Services;
using XiHan.BasicApp.Settings.Operations.DictItems;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Queries;

/// <summary>
/// 字典项列表查询（SqlSugar 直查，平台库，读共享）
/// </summary>
/// <remarks>
/// 关键字匹配编码、名称与附加值；筛选 <c>dictId</c>、<c>parentId</c>（eq）、<c>code</c>（eq / contains）、<c>name</c>（contains）、<c>status</c>、<c>isDefault</c>（eq）；
/// 排序 <c>code</c>、<c>name</c>、<c>value</c>、<c>order</c>、<c>createdAt</c>，缺省按排序号再编码
/// 层级要看整本字典：每页命中的字典各取一次全部项算层级，一本字典的项以百计
/// </remarks>
/// <param name="unitOfWork">SqlSugar 工作单元</param>
/// <param name="service">字典域服务</param>
public sealed class DictItemQueries(SqlSugarUnitOfWork unitOfWork, DictService service)
{
    private const string FieldDictId = "dictId";
    private const string FieldParentId = "parentId";
    private const string FieldCode = "code";
    private const string FieldName = "name";
    private const string FieldValue = "value";
    private const string FieldStatus = "status";
    private const string FieldIsDefault = "isDefault";
    private const string FieldOrder = "order";
    private const string FieldCreatedAt = "createdAt";

    /// <summary>
    /// 分页列出字典项
    /// </summary>
    public async ValueTask<Result<PageResult<DictItemResponse>>> ListAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var query = TenantScopedQuery.For<SysDictItem>(unitOfWork.ConnectionFor(typeof(SysDictItem)), context).OwnedByCurrentTenantOrPlatform(context);
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(i => i.Code.Contains(keyword) || i.Name.Contains(keyword) || (i.Value != null && i.Value.Contains(keyword)));
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
            query = query.OrderBy(i => i.Order, OrderByType.Asc).OrderBy(i => i.Code, OrderByType.Asc);
        }

        foreach (var sort in request.Sorts)
        {
            var direction = sort.Descending ? OrderByType.Desc : OrderByType.Asc;
            var sorted = sort.Field switch
            {
                FieldCode => query.OrderBy(i => i.Code, direction),
                FieldName => query.OrderBy(i => i.Name, direction),
                FieldValue => query.OrderBy(i => i.Value, direction),
                FieldOrder => query.OrderBy(i => i.Order, direction),
                FieldCreatedAt => query.OrderBy(i => i.CreatedAt, direction),
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
        var depths = new Dictionary<Guid, IReadOnlyList<SysDictItem>>();
        var items = new List<DictItemResponse>(rows.Count);
        foreach (var row in rows)
        {
            if (!depths.TryGetValue(row.DictId, out var all))
            {
                all = await service.ItemsOfAsync(row.DictId, context.CancellationToken).ConfigureAwait(false);
                depths[row.DictId] = all;
            }

            items.Add(DictItemResponse.From(row, DictService.DepthOf(row, all)));
        }

        return new PageResult<DictItemResponse>(items, total.Value, request.Page);
    }

    private static Result<ISugarQueryable<SysDictItem>> Apply(ISugarQueryable<SysDictItem> query, PageFilter filter)
    {
        var value = filter.Value ?? string.Empty;
        return (filter.Field, filter.Operator) switch
        {
            (FieldDictId, "eq") when Guid.TryParse(value, out var dictId) => Result.Success(query.Where(i => i.DictId == dictId)),
            (FieldParentId, "eq") when Guid.TryParse(value, out var parentId) => Result.Success(query.Where(i => i.ParentId == parentId)),
            (FieldCode, "eq") => Result.Success(query.Where(i => i.Code == value)),
            (FieldCode, "contains") => Result.Success(query.Where(i => i.Code.Contains(value))),
            (FieldName, "contains") => Result.Success(query.Where(i => i.Name.Contains(value))),
            (FieldStatus, "eq") when Enum.TryParse<SettingStatus>(value, ignoreCase: true, out var status) => Result.Success(query.Where(i => i.Status == status)),
            (FieldIsDefault, "eq") when bool.TryParse(value, out var isDefault) => Result.Success(query.Where(i => i.IsDefault == isDefault)),
            _ => Error.Validation(SettingsErrors.QueryUnsupported, SettingsMessages.FilterUnsupported, MessageArguments.Of(("field", filter.Field), ("operator", filter.Operator))),
        };
    }
}
