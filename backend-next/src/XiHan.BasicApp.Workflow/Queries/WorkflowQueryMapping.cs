// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Workflow.Domain.Services;
using XiHan.Framework.Primitives;
using XiHan.Framework.Workflow.Abstractions;
using XiHan.Framework.Workflow.Abstractions.Definitions;
using XiHan.Framework.Workflow.Abstractions.Instances;

namespace XiHan.BasicApp.Workflow.Queries;

/// <summary>
/// 列表请求到框架查询的换算。
/// </summary>
/// <remarks>
/// 过滤、分页与计数都由框架在存储里做（框架第八批 G1、G2，ADR-0096）；这里只把分页请求换成框架的查询，筛选字段是白名单，越界即
/// <see cref="WorkflowErrors.QueryUnsupported"/>。排序由框架固定（定义按名字升序、版本降序，实例按创建时间降序），请求里带了排序同样是这个错误。
/// 关键字照框架的口径（与 4.x 同）：定义匹配名字、显示名与说明，实例匹配标题与关联 ID，按字面匹配（<c>%</c>、<c>_</c> 不是通配符）。
/// </remarks>
public static class WorkflowQueryMapping
{
    /// <summary>
    /// 定义列表。
    /// </summary>
    /// <remarks>
    /// 筛选：<c>name</c>、<c>category</c>、<c>status</c>（都是 <c>eq</c>）。
    /// </remarks>
    /// <param name="request">分页请求。</param>
    /// <returns>框架查询。</returns>
    public static Result<WorkflowDefinitionQuery> Definitions(PageRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var sorted = RequireNoSort(request);
        if (sorted.IsFailure)
        {
            return sorted.Error!;
        }

        WorkflowName? name = null;
        string? category = null;
        WorkflowDefinitionStatus? status = null;
        foreach (var filter in request.Filters)
        {
            var value = filter.Value ?? string.Empty;
            switch (filter.Field, filter.Operator)
            {
                case ("name", "eq") when WorkflowName.IsValid(value):
                    name = new WorkflowName(value);
                    break;
                case ("category", "eq"):
                    category = value;
                    break;
                case ("status", "eq") when Enum.TryParse<WorkflowDefinitionStatus>(value, ignoreCase: true, out var parsed):
                    status = parsed;
                    break;
                default:
                    return Unsupported(filter);
            }
        }

        return new WorkflowDefinitionQuery
        {
            Keyword = request.Keyword,
            Name = name,
            Category = category,
            Status = status,
            Skip = request.Page.Skip,
            Limit = request.Page.Size,
        };
    }

    /// <summary>
    /// 实例列表。
    /// </summary>
    /// <remarks>
    /// 筛选：<c>status</c>（<c>eq</c> / <c>in</c>，逗号连接）、<c>definitionName</c>（<c>eq</c>）、<c>correlationId</c>（<c>eq</c> / <c>in</c>）、
    /// <c>starterId</c>（<c>eq</c>）、<c>createdAt</c>（<c>ge</c> / <c>le</c>，按时刻含两端）。
    /// </remarks>
    /// <param name="request">分页请求。</param>
    /// <returns>框架查询。</returns>
    public static Result<WorkflowInstanceQuery> Instances(PageRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var sorted = RequireNoSort(request);
        if (sorted.IsFailure)
        {
            return sorted.Error!;
        }

        var query = new WorkflowInstanceQuery { Keyword = request.Keyword, Skip = request.Page.Skip, Limit = request.Page.Size };
        foreach (var filter in request.Filters)
        {
            var value = filter.Value ?? string.Empty;
            switch (filter.Field, filter.Operator)
            {
                case ("status", "eq" or "in") when TryParseStatuses(value, out var statuses):
                    query = query with { Statuses = statuses };
                    break;
                case ("definitionName", "eq") when WorkflowName.IsValid(value):
                    query = query with { DefinitionName = new WorkflowName(value) };
                    break;
                case ("correlationId", "eq" or "in"):
                    query = query with { CorrelationIds = Split(value) };
                    break;
                case ("starterId", "eq") when Guid.TryParse(value, out var starter):
                    query = query with { StarterId = WorkflowIds.ToEngineUser(starter) };
                    break;
                case ("createdAt", "ge") when TryParseTime(value, out var from):
                    query = query with { CreatedFrom = from };
                    break;
                case ("createdAt", "le") when TryParseTime(value, out var to):
                    // 框架的上界不含，请求的上界含
                    query = query with { CreatedTo = to.AddTicks(1) };
                    break;
                default:
                    return Unsupported(filter);
            }
        }

        return query;
    }

    private static Result RequireNoSort(PageRequest request)
        => request.Sorts.Count == 0
            ? Result.Success()
            : Error.Validation(WorkflowErrors.QueryUnsupported, WorkflowMessages.SortUnsupported, MessageArguments.Of(("field", request.Sorts[0].Field)));

    private static Error Unsupported(PageFilter filter)
        => Error.Validation(WorkflowErrors.QueryUnsupported, WorkflowMessages.FilterUnsupported, MessageArguments.Of(("field", filter.Field)));

    private static List<string> Split(string value) => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private static bool TryParseStatuses(string value, out IReadOnlyList<WorkflowInstanceStatus> statuses)
    {
        var parsed = new List<WorkflowInstanceStatus>();
        foreach (var part in Split(value))
        {
            if (!Enum.TryParse<WorkflowInstanceStatus>(part, ignoreCase: true, out var status))
            {
                statuses = [];
                return false;
            }

            parsed.Add(status);
        }

        statuses = parsed;
        return parsed.Count > 0;
    }

    private static bool TryParseTime(string value, out DateTimeOffset time)
        => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out time);
}
