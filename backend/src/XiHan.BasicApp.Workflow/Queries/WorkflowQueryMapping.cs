// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Workflow.Domain.Services;
using XiHan.Framework.Primitives;
using XiHan.Framework.Workflow.Abstractions;
using XiHan.Framework.Workflow.Abstractions.Definitions;
using XiHan.Framework.Workflow.Abstractions.Instances;
using XiHan.Framework.Workflow.Abstractions.Tasks;

namespace XiHan.BasicApp.Workflow.Queries;

/// <summary>
/// 列表请求到框架查询的换算
/// </summary>
/// <remarks>
/// 过滤、排序、分页与计数都由框架在存储里做（框架第八批 G1、G2，ADR-0096；第十一至十三批的排序，ADR-0102～ADR-0104）；这里只把分页请求换成框架的查询，
/// 筛选与排序字段是白名单，越界即 <see cref="WorkflowErrors.QueryUnsupported"/>。定义与实例照 4.x 多列排序：请求的排序项按顺序映射，第一项是主排序，
/// 之后是次级排序，同值由框架补的次序键定；列名照响应的字段名，同一列给两次或超过可排序列数由框架报 <c>WORKFLOW_VALIDATION_002</c>
/// 没给排序时是框架的缺省（定义按名字升序、版本降序，实例按创建时间降序）。状态按生命周期的先后排（同 4.x）。我发起的与待办顺序固定
/// 关键字照框架的口径（与 4.x 同）：定义匹配名字、显示名与说明，实例匹配标题与关联 ID，按字面匹配（<c>%</c>、<c>_</c> 不是通配符）
/// </remarks>
public static class WorkflowQueryMapping
{
    private static readonly Dictionary<string, WorkflowDefinitionSortField> _definitionSorts = new(StringComparer.Ordinal)
    {
        ["status"] = WorkflowDefinitionSortField.Status,
        ["name"] = WorkflowDefinitionSortField.Name,
        ["displayName"] = WorkflowDefinitionSortField.Title,
        ["version"] = WorkflowDefinitionSortField.Version,
        ["createdAt"] = WorkflowDefinitionSortField.CreatedAt,
        ["publishedAt"] = WorkflowDefinitionSortField.PublishedAt,
    };

    private static readonly Dictionary<string, WorkflowInstanceSortField> _instanceSorts = new(StringComparer.Ordinal)
    {
        ["status"] = WorkflowInstanceSortField.Status,
        ["definitionName"] = WorkflowInstanceSortField.DefinitionName,
        ["title"] = WorkflowInstanceSortField.Title,
        ["createdAt"] = WorkflowInstanceSortField.CreatedAt,
        ["endedAt"] = WorkflowInstanceSortField.EndedAt,
    };

    /// <summary>
    /// 定义列表
    /// </summary>
    /// <remarks>
    /// 筛选：<c>name</c>、<c>category</c>、<c>status</c>（都是 <c>eq</c>）、<c>createdAt</c>（<c>ge</c> / <c>le</c>，按时刻含两端）
    /// 排序：<c>status</c>、<c>name</c>、<c>displayName</c>、<c>version</c>、<c>createdAt</c>、<c>publishedAt</c>（4.x 可排序的六列）
    /// </remarks>
    /// <param name="request">分页请求</param>
    /// <returns>框架查询</returns>
    public static Result<WorkflowDefinitionQuery> Definitions(PageRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var sorted = SortsOf(request, _definitionSorts, static (field, descending) => new WorkflowDefinitionSort(field, descending));
        if (!sorted.TryGetValue(out var sorts))
        {
            return sorted.Error!;
        }

        WorkflowName? name = null;
        string? category = null;
        WorkflowDefinitionStatus? status = null;
        DateTimeOffset? createdFrom = null;
        DateTimeOffset? createdTo = null;
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
                case ("createdAt", "ge") when TryParseTime(value, out var from):
                    createdFrom = from;
                    break;
                case ("createdAt", "le") when TryParseTime(value, out var to):
                    // 框架的上界不含，请求的上界含
                    createdTo = to.AddTicks(1);
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
            CreatedFrom = createdFrom,
            CreatedTo = createdTo,
            Sorts = sorts,
            Skip = request.Page.Skip,
            Limit = request.Page.Size,
        };
    }

    /// <summary>
    /// 实例列表
    /// </summary>
    /// <remarks>
    /// 筛选：<c>status</c>（<c>eq</c> / <c>in</c>，逗号连接）、<c>definitionName</c>（<c>eq</c>）、<c>correlationId</c>（<c>eq</c> / <c>in</c>）、
    /// <c>starterId</c>（<c>eq</c>）、<c>createdAt</c>（<c>ge</c> / <c>le</c>，按时刻含两端）
    /// 排序：<c>status</c>、<c>definitionName</c>、<c>title</c>、<c>createdAt</c>、<c>endedAt</c>（4.x 可排序的五列）
    /// </remarks>
    /// <param name="request">分页请求</param>
    /// <returns>框架查询</returns>
    public static Result<WorkflowInstanceQuery> Instances(PageRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var sorted = SortsOf(request, _instanceSorts, static (field, descending) => new WorkflowInstanceSort(field, descending));
        if (!sorted.TryGetValue(out var sorts))
        {
            return sorted.Error!;
        }

        var query = new WorkflowInstanceQuery { Keyword = request.Keyword, Sorts = sorts, Skip = request.Page.Skip, Limit = request.Page.Size };

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

    /// <summary>
    /// 我发起的
    /// </summary>
    /// <remarks>
    /// 照 4.x：发起人锁定为调用方、只取顶层实例；关键字（标题或关联 ID），筛选只有 <c>status</c>（<c>eq</c> / <c>in</c>）
    /// </remarks>
    /// <param name="request">分页请求</param>
    /// <param name="starter">调用方（引擎用户 ID）</param>
    /// <returns>框架查询</returns>
    public static Result<WorkflowInstanceQuery> Started(PageRequest request, string starter)
    {
        ArgumentNullException.ThrowIfNull(request);
        var sorted = RequireNoSort(request);
        if (sorted.IsFailure)
        {
            return sorted.Error!;
        }

        var query = new WorkflowInstanceQuery { Keyword = request.Keyword, StarterId = starter, TopLevelOnly = true, Skip = request.Page.Skip, Limit = request.Page.Size };
        foreach (var filter in request.Filters)
        {
            if (filter is not { Field: "status", Operator: "eq" or "in" } || !TryParseStatuses(filter.Value ?? string.Empty, out var statuses))
            {
                return Unsupported(filter);
            }

            query = query with { Statuses = statuses };
        }

        return query;
    }

    /// <summary>
    /// 我的待办
    /// </summary>
    /// <remarks>
    /// 受理人锁定为调用方；关键字匹配任务标题、实例标题、定义名与关联 ID（与 4.x 同），按创建时间降序，没有筛选
    /// </remarks>
    /// <param name="request">分页请求</param>
    /// <param name="assignee">调用方（引擎用户 ID）</param>
    /// <returns>框架查询</returns>
    public static Result<UserTaskQuery> Tasks(PageRequest request, string assignee)
    {
        ArgumentNullException.ThrowIfNull(request);
        var sorted = RequireNoSort(request);
        if (sorted.IsFailure)
        {
            return sorted.Error!;
        }

        if (request.Filters.Count > 0)
        {
            return Unsupported(request.Filters[0]);
        }

        return new UserTaskQuery { AssigneeId = assignee, Keyword = request.Keyword, Descending = true, Skip = request.Page.Skip, Limit = request.Page.Size };
    }

    private static Result<List<TSort>> SortsOf<TField, TSort>(PageRequest request, Dictionary<string, TField> fields, Func<TField, bool, TSort> create)
        where TField : struct, Enum
    {
        var sorts = new List<TSort>(request.Sorts.Count);
        foreach (var sort in request.Sorts)
        {
            if (!fields.TryGetValue(sort.Field, out var field))
            {
                return Error.Validation(WorkflowErrors.QueryUnsupported, WorkflowMessages.SortUnsupported, MessageArguments.Of(("field", sort.Field)));
            }

            sorts.Add(create(field, sort.Descending));
        }

        return sorts;
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
