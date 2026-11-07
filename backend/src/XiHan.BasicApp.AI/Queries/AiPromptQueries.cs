// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.AI.Operations.AiPrompts;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Runtime.Queries;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.AI.Queries;

/// <summary>
/// 提示词的列表查询
/// </summary>
/// <remarks>
/// 关键字匹配编码与名称；筛选：状态（<c>eq</c> / <c>in</c>）、分类（<c>eq</c>）；
/// 排序：名称、编码、分类、状态、排序号、创建时间，没给即排序号再名称升序。别的筛选与排序是 <c>AI_VALIDATION_101</c>
/// </remarks>
/// <param name="unitOfWork">工作单元</param>
public sealed class AiPromptQueries(SqlSugarUnitOfWork unitOfWork)
{
    private const string FieldName = "name";
    private const string FieldCode = "code";
    private const string FieldCategory = "category";
    private const string FieldStatus = "status";
    private const string FieldOrder = "order";
    private const string FieldCreatedAt = "createdAt";

    /// <summary>
    /// 分页列出提示词
    /// </summary>
    /// <param name="request">分页请求</param>
    /// <param name="context">调用上下文</param>
    public async ValueTask<Result<PageResult<AiPromptResponse>>> ListAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var query = TenantScopedQuery.For<SysAiPrompt>(unitOfWork.ConnectionFor(typeof(SysAiPrompt)), context);
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
            query = query.OrderBy(p => p.Order, OrderByType.Asc).OrderBy(p => p.Name, OrderByType.Asc);
        }

        foreach (var sort in request.Sorts)
        {
            var direction = sort.Descending ? OrderByType.Desc : OrderByType.Asc;
            var sorted = sort.Field switch
            {
                FieldName => query.OrderBy(p => p.Name, direction),
                FieldCode => query.OrderBy(p => p.Code, direction),
                FieldCategory => query.OrderBy(p => p.Category, direction),
                FieldStatus => query.OrderBy(p => p.Status, direction),
                FieldOrder => query.OrderBy(p => p.Order, direction),
                FieldCreatedAt => query.OrderBy(p => p.CreatedAt, direction),
                _ => null,
            };
            if (sorted is null)
            {
                return Error.Validation(AIErrors.QueryUnsupported, AIMessages.SortUnsupported, MessageArguments.Of(("field", sort.Field)));
            }

            query = sorted;
        }

        var total = new RefAsync<int>();
        var rows = await query.ToPageListAsync(request.Page.Index, request.Page.Size, total, context.CancellationToken).ConfigureAwait(false);
        return new PageResult<AiPromptResponse>(rows.Select(AiPromptResponse.From).ToList(), total.Value, request.Page);
    }

    private static Result<ISugarQueryable<SysAiPrompt>> Apply(ISugarQueryable<SysAiPrompt> query, PageFilter filter)
    {
        var value = filter.Value ?? string.Empty;
        return (filter.Field, filter.Operator) switch
        {
            (FieldStatus, "eq" or "in") when AiProviderQueries.EnumsOf<AiStatus>(value) is { } statuses => Result.Success(query.Where(p => statuses.Contains(p.Status))),
            (FieldCategory, "eq") => Result.Success(query.Where(p => p.Category == value)),
            _ => Error.Validation(AIErrors.QueryUnsupported, AIMessages.FilterUnsupported, MessageArguments.Of(("field", filter.Field), ("operator", filter.Operator))),
        };
    }
}
