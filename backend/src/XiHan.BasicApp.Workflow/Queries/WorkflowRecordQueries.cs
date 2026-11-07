// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.BasicApp.Workflow.Domain.Entities;
using XiHan.BasicApp.Workflow.Domain.Services;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Workflow.Queries;

/// <summary>
/// 审批记录的分页（我已审批、抄送我的）
/// </summary>
/// <remarks>
/// 主体锁定为调用方，办理、转办、加签都算「我已审批」，抄送归「抄送我的」；关键字匹配任务标题、实例标题与关联 ID，按时间降序，没有筛选与排序
/// </remarks>
/// <param name="rows">按侧读写</param>
public sealed class WorkflowRecordQueries(SideRows rows)
{
    /// <summary>
    /// 分页列出调用方的记录
    /// </summary>
    /// <param name="request">分页请求</param>
    /// <param name="copied"><c>true</c> 是抄送我的，<c>false</c> 是我已审批</param>
    /// <param name="context">调用上下文</param>
    /// <returns>一页记录</returns>
    public async ValueTask<Result<PageResult<SysWorkflowTaskRecord>>> ListAsync(PageRequest request, bool copied, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (request.Sorts.Count > 0)
        {
            return Error.Validation(WorkflowErrors.QueryUnsupported, WorkflowMessages.SortUnsupported, MessageArguments.Of(("field", request.Sorts[0].Field)));
        }

        if (request.Filters.Count > 0)
        {
            return Error.Validation(WorkflowErrors.QueryUnsupported, WorkflowMessages.FilterUnsupported, MessageArguments.Of(("field", request.Filters[0].Field)));
        }

        var caller = WorkflowCaller.Of(context);
        if (caller.UserId is not { } me)
        {
            return new PageResult<SysWorkflowTaskRecord>([], 0, request.Page);
        }

        var query = rows.Records<SysWorkflowTaskRecord>(caller.Side).Where(r => r.UserId == me);
        query = copied
            ? query.Where(static r => r.Action == WorkflowTaskRecordAction.CarbonCopied)
            : query.Where(static r => r.Action != WorkflowTaskRecordAction.CarbonCopied);
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(r => r.Title.Contains(keyword) || r.InstanceTitle.Contains(keyword) || (r.CorrelationId != null && r.CorrelationId.Contains(keyword)));
        }

        var total = new RefAsync<int>();
        var page = await query.OrderBy(static r => r.CreatedAt, OrderByType.Desc).ToPageListAsync(request.Page.Index, request.Page.Size, total, context.CancellationToken).ConfigureAwait(false);
        return new PageResult<SysWorkflowTaskRecord>(page, total.Value, request.Page);
    }
}
