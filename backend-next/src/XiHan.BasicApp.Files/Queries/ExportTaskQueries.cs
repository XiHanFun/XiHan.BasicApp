// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Files.Domain.Entities;
using XiHan.BasicApp.Files.Domain.Services;
using XiHan.BasicApp.Files.Infrastructure;
using XiHan.BasicApp.Files.Operations.ExportTasks;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Files.Queries;

/// <summary>
/// 自己的导出任务列表：关键字匹配任务名称与列表操作 ID；按状态、格式多选，按列表操作筛选；缺省按提交时间倒序（4.x 同只看自己的）。
/// </summary>
/// <param name="rows">任务读写。</param>
public sealed class ExportTaskQueries(FileRows rows)
{
    private const string FieldStatus = "status";
    private const string FieldFormat = "format";
    private const string FieldBusinessType = "businessType";
    private const string FieldCreatedAt = "createdAt";
    private const string FieldTaskName = "taskName";

    /// <summary>
    /// 分页列出。
    /// </summary>
    /// <param name="request">分页请求。</param>
    /// <param name="context">调用上下文。</param>
    /// <returns>一页任务。</returns>
    public async ValueTask<Result<PageResult<ExportTaskResponse>>> ListAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var requester = ExportTaskService.RequesterOf(context) ?? Guid.Empty;
        var query = rows.Query<SysExportTask>(FileRows.SideOf(context)).Where(t => t.RequesterId == requester);
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(t => t.TaskName.Contains(keyword) || t.BusinessType.Contains(keyword));
        }

        foreach (var filter in request.Filters)
        {
            var value = filter.Value ?? string.Empty;
            ISugarQueryable<SysExportTask>? filtered = (filter.Field, filter.Operator) switch
            {
                (FieldStatus, "eq" or "in") when Enums.Of<ExportTaskStatus>(value) is { } statuses => query.Where(t => statuses.Contains(t.Status)),
                (FieldFormat, "eq" or "in") when Enums.Of<ExportFormat>(value) is { } formats => query.Where(t => formats.Contains(t.Format)),
                (FieldBusinessType, "eq") => query.Where(t => t.BusinessType == value),
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
            query = query.OrderBy(t => t.CreatedAt, OrderByType.Desc);
        }

        foreach (var sort in request.Sorts)
        {
            var direction = sort.Descending ? OrderByType.Desc : OrderByType.Asc;
            var sorted = sort.Field switch
            {
                FieldCreatedAt => query.OrderBy(t => t.CreatedAt, direction),
                FieldTaskName => query.OrderBy(t => t.TaskName, direction),
                FieldStatus => query.OrderBy(t => t.Status, direction),
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
        return new PageResult<ExportTaskResponse>([.. page.Select(ExportTaskResponse.From)], total.Value, request.Page);
    }
}
