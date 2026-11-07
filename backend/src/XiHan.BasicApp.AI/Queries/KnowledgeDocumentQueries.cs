// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.AI.Operations.KnowledgeDocuments;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.AI.Queries;

/// <summary>
/// 知识文档的列表查询
/// </summary>
/// <remarks>
/// 只看调用方一侧。关键字匹配标题与来源；筛选：来源类型、状态（<c>eq</c> / <c>in</c>）；
/// 排序：标题、块数、状态、排序号、创建时间，没给即排序号升序再创建时间降序。别的筛选与排序是 <c>AI_VALIDATION_101</c>；列表不取原文
/// </remarks>
/// <param name="rows">按侧读写</param>
public sealed class KnowledgeDocumentQueries(SideRows rows)
{
    private const string FieldTitle = "title";
    private const string FieldSourceType = "sourceType";
    private const string FieldChunkCount = "chunkCount";
    private const string FieldStatus = "status";
    private const string FieldOrder = "order";
    private const string FieldCreatedAt = "createdAt";

    /// <summary>
    /// 分页列出知识文档
    /// </summary>
    /// <param name="request">分页请求</param>
    /// <param name="context">调用上下文</param>
    public async ValueTask<Result<PageResult<KnowledgeDocumentResponse>>> ListAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var query = rows.Query<SysKnowledgeDocument>(SideRows.SideOf(context)).IgnoreColumns(d => d.RawContent);
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(d => d.Title.Contains(keyword) || (d.Source != null && d.Source.Contains(keyword)));
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
            query = query.OrderBy(d => d.Order, OrderByType.Asc).OrderBy(d => d.CreatedAt, OrderByType.Desc);
        }

        foreach (var sort in request.Sorts)
        {
            var direction = sort.Descending ? OrderByType.Desc : OrderByType.Asc;
            var sorted = sort.Field switch
            {
                FieldTitle => query.OrderBy(d => d.Title, direction),
                FieldChunkCount => query.OrderBy(d => d.ChunkCount, direction),
                FieldStatus => query.OrderBy(d => d.Status, direction),
                FieldOrder => query.OrderBy(d => d.Order, direction),
                FieldCreatedAt => query.OrderBy(d => d.CreatedAt, direction),
                _ => null,
            };
            if (sorted is null)
            {
                return Error.Validation(AIErrors.QueryUnsupported, AIMessages.SortUnsupported, MessageArguments.Of(("field", sort.Field)));
            }

            query = sorted;
        }

        var total = new RefAsync<int>();
        var items = await query.ToPageListAsync(request.Page.Index, request.Page.Size, total, context.CancellationToken).ConfigureAwait(false);
        return new PageResult<KnowledgeDocumentResponse>(items.Select(KnowledgeDocumentResponse.From).ToList(), total.Value, request.Page);
    }

    private static Result<ISugarQueryable<SysKnowledgeDocument>> Apply(ISugarQueryable<SysKnowledgeDocument> query, PageFilter filter)
    {
        var value = filter.Value ?? string.Empty;
        return (filter.Field, filter.Operator) switch
        {
            (FieldStatus, "eq" or "in") when AiProviderQueries.EnumsOf<KnowledgeIndexStatus>(value) is { } statuses => Result.Success(query.Where(d => statuses.Contains(d.Status))),
            (FieldSourceType, "eq" or "in") when AiProviderQueries.EnumsOf<KnowledgeSourceType>(value) is { } types => Result.Success(query.Where(d => types.Contains(d.SourceType))),
            _ => Error.Validation(AIErrors.QueryUnsupported, AIMessages.FilterUnsupported, MessageArguments.Of(("field", filter.Field), ("operator", filter.Operator))),
        };
    }
}
