// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Files.Domain.Entities;
using XiHan.BasicApp.Files.Operations.StorageConfigs;
using XiHan.BasicApp.Runtime.Queries;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;
using StorageType = XiHan.BasicApp.Files.Domain.Entities.StorageType;

namespace XiHan.BasicApp.Files.Queries;

/// <summary>
/// 存储配置列表（当前一侧）：关键字匹配编码、名称、桶与备注；按类型、状态多选，按是否缺省筛选；缺省按缺省在前、再按排序与创建时间（4.x 同）。
/// </summary>
/// <param name="unitOfWork">工作单元。</param>
public sealed class StorageConfigQueries(SqlSugarUnitOfWork unitOfWork)
{
    private const string FieldCode = "code";
    private const string FieldName = "name";
    private const string FieldType = "type";
    private const string FieldStatus = "status";
    private const string FieldIsDefault = "isDefault";
    private const string FieldOrder = "order";
    private const string FieldCreatedAt = "createdAt";

    /// <summary>
    /// 分页列出。
    /// </summary>
    /// <param name="request">分页请求。</param>
    /// <param name="context">调用上下文。</param>
    /// <returns>一页配置。</returns>
    public async ValueTask<Result<PageResult<StorageConfigResponse>>> ListAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var query = TenantScopedQuery.For<SysStorageConfig>(unitOfWork.ConnectionFor(typeof(SysStorageConfig)), context).OwnedByCurrentTenant(context);
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(c => c.Code.Contains(keyword) || c.Name.Contains(keyword) || c.Bucket.Contains(keyword) || (c.Remark != null && c.Remark.Contains(keyword)));
        }

        foreach (var filter in request.Filters)
        {
            var value = filter.Value ?? string.Empty;
            ISugarQueryable<SysStorageConfig>? filtered = (filter.Field, filter.Operator) switch
            {
                (FieldType, "eq" or "in") when Enums.Of<StorageType>(value) is { } types => query.Where(c => types.Contains(c.Type)),
                (FieldStatus, "eq" or "in") when Enums.Of<StorageConfigStatus>(value) is { } statuses => query.Where(c => statuses.Contains(c.Status)),
                (FieldIsDefault, "eq") when bool.TryParse(value, out var isDefault) => query.Where(c => c.IsDefault == isDefault),
                _ => null,
            };
            if (filtered is null)
            {
                return Error.Validation(FilesErrors.QueryUnsupported, FilesMessages.FilterUnsupported, MessageArguments.Of(("field", filter.Field), ("operator", filter.Operator)));
            }

            query = filtered;
        }

        if (request.Sorts.Count == 0)
        {
            query = query.OrderBy(c => c.IsDefault, OrderByType.Desc).OrderBy(c => c.Order, OrderByType.Asc).OrderBy(c => c.CreatedAt, OrderByType.Desc);
        }

        foreach (var sort in request.Sorts)
        {
            var direction = sort.Descending ? OrderByType.Desc : OrderByType.Asc;
            var sorted = sort.Field switch
            {
                FieldCode => query.OrderBy(c => c.Code, direction),
                FieldName => query.OrderBy(c => c.Name, direction),
                FieldType => query.OrderBy(c => c.Type, direction),
                FieldOrder => query.OrderBy(c => c.Order, direction),
                FieldCreatedAt => query.OrderBy(c => c.CreatedAt, direction),
                _ => null,
            };
            if (sorted is null)
            {
                return Error.Validation(FilesErrors.QueryUnsupported, FilesMessages.SortUnsupported, MessageArguments.Of(("field", sort.Field)));
            }

            query = sorted;
        }

        var total = new RefAsync<int>();
        var page = await query.ToPageListAsync(request.Page.Index, request.Page.Size, total, context.CancellationToken).ConfigureAwait(false);
        return new PageResult<StorageConfigResponse>([.. page.Select(StorageConfigResponse.From)], total.Value, request.Page);
    }
}
