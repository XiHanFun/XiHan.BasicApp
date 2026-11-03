// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.AI.Domain.Services;
using XiHan.BasicApp.AI.Infrastructure;
using XiHan.BasicApp.AI.Queries;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.AI.Operations.KnowledgeDocuments;

/// <summary>
/// 知识文档列表
/// </summary>
/// <remarks>
/// <c>POST /api/knowledge-documents:list</c>。只看调用方一侧，不带原文；筛选与排序见 <see cref="KnowledgeDocumentQueries"/>
/// </remarks>
/// <param name="queries">知识文档查询</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AIPermissions.KnowledgeDocumentsRead)]
public sealed class ListKnowledgeDocuments(KnowledgeDocumentQueries queries) : IOperation<PageRequest, PageResult<KnowledgeDocumentResponse>>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "knowledge-documents.list";

    /// <inheritdoc />
    public ValueTask<Result<PageResult<KnowledgeDocumentResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.ListAsync(request, context);
    }
}

/// <summary>
/// 知识文档详情
/// </summary>
/// <remarks>
/// <c>GET /api/knowledge-documents/{id}</c>。带原文；别的一侧的文档按不存在处理
/// </remarks>
/// <param name="service">知识文档服务</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AIPermissions.KnowledgeDocumentsRead)]
public sealed class GetKnowledgeDocument(KnowledgeDocumentService service) : IOperation<KnowledgeDocumentIdRequest, KnowledgeDocumentDetailResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "knowledge-documents.get";

    /// <inheritdoc />
    public async ValueTask<Result<KnowledgeDocumentDetailResponse>> HandleAsync(KnowledgeDocumentIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(request.Id, context).ConfigureAwait(false);
        return found.TryGetValue(out var document) ? KnowledgeDocumentDetailResponse.From(document) : found.Error!;
    }
}

/// <summary>
/// 摄入知识文档
/// </summary>
/// <remarks>
/// <c>POST /api/knowledge-documents:ingest</c>。只收文本、请求内同步（方案 A5，照 4.x）；指定的嵌入提供商不存在或已停用 <c>AI_VALIDATION_102</c>
/// 索引失败（没配向量库、模型不可达、维度不符）照 4.x 以 <c>Failed</c> 保存并成功返回，原因记在文档上，可以重建
/// </remarks>
/// <param name="service">知识文档服务</param>
/// <param name="catalog">提供商目录</param>
/// <param name="ids">标识生成器</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AIPermissions.KnowledgeDocumentsCreate)]
public sealed class IngestKnowledgeDocument(KnowledgeDocumentService service, AiProviderCatalog catalog, IIdGenerator<Guid> ids) : IOperation<IngestKnowledgeDocumentRequest, KnowledgeDocumentDetailResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "knowledge-documents.ingest";

    /// <inheritdoc />
    public async ValueTask<Result<KnowledgeDocumentDetailResponse>> HandleAsync(IngestKnowledgeDocumentRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var provider = AiProviderService.Trimmed(request.EmbeddingProviderCode);
        if (provider is not null && !catalog.Find(provider).HasValue)
        {
            return Error.Validation(AIErrors.KnowledgeProviderUnavailable, AIMessages.KnowledgeProviderUnavailable, MessageArguments.Of(("code", provider)));
        }

        var document = new SysKnowledgeDocument
        {
            Id = ids.NewId(),
            TenantId = SideRows.SideOf(context),
            Title = request.Title.Trim(),
            SourceType = request.SourceType,
            Source = AiProviderService.Trimmed(request.Source),
            RawContent = request.Text.Trim(),
            EmbeddingProviderCode = provider,
            Status = KnowledgeIndexStatus.Pending,
            Order = request.Order,
            Remark = AiProviderService.Trimmed(request.Remark),
        };
        var ingested = await service.IngestAsync(document, context).ConfigureAwait(false);
        return ingested.IsSuccess ? KnowledgeDocumentDetailResponse.From(document) : ingested.Error!;
    }
}

/// <summary>
/// 重建知识文档的索引
/// </summary>
/// <remarks>
/// <c>POST /api/knowledge-documents/{id}:reindex</c>。用原文重新切块嵌入，先写后删；失败照摄入记在文档上并成功返回，旧块照常可检索
/// </remarks>
/// <param name="service">知识文档服务</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AIPermissions.KnowledgeDocumentsUpdate)]
public sealed class ReindexKnowledgeDocument(KnowledgeDocumentService service) : IOperation<KnowledgeDocumentIdRequest, KnowledgeDocumentDetailResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "knowledge-documents.reindex";

    /// <inheritdoc />
    public async ValueTask<Result<KnowledgeDocumentDetailResponse>> HandleAsync(KnowledgeDocumentIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(request.Id, context).ConfigureAwait(false);
        if (!found.TryGetValue(out var document))
        {
            return found.Error!;
        }

        var reindexed = await service.ReindexAsync(document, context).ConfigureAwait(false);
        return reindexed.IsSuccess ? KnowledgeDocumentDetailResponse.From(document) : reindexed.Error!;
    }
}

/// <summary>
/// 批量删除知识文档
/// </summary>
/// <remarks>
/// <c>DELETE /api/knowledge-documents?ids=...</c>。逐项：不存在 <c>AI_NOTFOUND_103</c>，向量库不可达照框架的错误码、行不删
/// 先删向量再软删行：向量库的网络 I/O 都在数据库事务开始之前
/// </remarks>
/// <param name="service">知识文档服务</param>
/// <param name="rows">按侧读写</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AIPermissions.KnowledgeDocumentsDelete)]
public sealed class DeleteKnowledgeDocuments(KnowledgeDocumentService service, SideRows rows) : IOperation<KnowledgeDocumentIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "knowledge-documents.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(KnowledgeDocumentIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var removable = new List<SysKnowledgeDocument>();
        var failed = new List<BatchFailure>();
        foreach (var id in request.Ids.Distinct())
        {
            var found = await service.FindAsync(id, context).ConfigureAwait(false);
            if (!found.TryGetValue(out var document))
            {
                failed.Add(new BatchFailure(id, AIErrors.KnowledgeDocumentNotFound));
                continue;
            }

            var removed = await service.RemoveVectorsAsync(document, context.CancellationToken).ConfigureAwait(false);
            if (removed.IsFailure)
            {
                failed.Add(new BatchFailure(id, removed.Error!.Code));
                continue;
            }

            removable.Add(document);
        }

        foreach (var document in removable)
        {
            await rows.SoftDeleteAsync(document, context.CancellationToken).ConfigureAwait(false);
        }

        return new BatchResult(removable.Select(static d => d.Id).ToList(), failed);
    }
}

/// <summary>
/// 知识问答
/// </summary>
/// <remarks>
/// <c>GET /api/knowledge-documents:query</c>。只在调用方一侧检索；要答案且有命中时生成带引用编号的答案，<c>[n]</c> 对应第 n 个片段（照 4.x）
/// 没配向量库 <c>AI_UNAVAILABLE_002</c>，提供商未配置 <c>AI_NOTFOUND_001</c>，原样返回框架的错误
/// </remarks>
/// <param name="service">知识文档服务</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = AIPermissions.KnowledgeDocumentsExecute)]
public sealed class QueryKnowledgeDocuments(KnowledgeDocumentService service) : IOperation<QueryKnowledgeDocumentsRequest, KnowledgeAnswerResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "knowledge-documents.query";

    /// <inheritdoc />
    public async ValueTask<Result<KnowledgeAnswerResponse>> HandleAsync(QueryKnowledgeDocumentsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var question = request.Question.Trim();
        var provider = AiProviderService.Trimmed(request.Provider);
        var retrieved = await service.RetrieveAsync(question, request.Top, provider, context).ConfigureAwait(false);
        if (!retrieved.TryGetValue(out var chunks))
        {
            return retrieved.Error!;
        }

        var citations = chunks.Select(KnowledgeCitationResponse.From).ToList();
        if (!request.Answer || chunks.Count == 0)
        {
            return new KnowledgeAnswerResponse(null, citations);
        }

        var answered = await service.AnswerAsync(question, chunks, provider, context).ConfigureAwait(false);
        return answered.TryGetValue(out var answer) ? new KnowledgeAnswerResponse(answer, citations) : answered.Error!;
    }
}

/// <summary>
/// 检索知识库
/// </summary>
/// <remarks>
/// <c>GET /api/knowledge-documents:search</c>。只检索、返回片段，也发布为 MCP 与 AI 工具（取代 4.x 的 <c>knowledge_retrieve</c> 技能），以调用方身份在调用方一侧执行
/// </remarks>
/// <param name="service">知识文档服务</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http | Expose.Mcp | Expose.Ai, Permission = AIPermissions.KnowledgeDocumentsExecute)]
public sealed class SearchKnowledgeDocuments(KnowledgeDocumentService service) : IOperation<SearchKnowledgeDocumentsRequest, KnowledgeSearchResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "knowledge-documents.search";

    /// <inheritdoc />
    public async ValueTask<Result<KnowledgeSearchResponse>> HandleAsync(SearchKnowledgeDocumentsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var retrieved = await service.RetrieveAsync(request.Question.Trim(), request.Top, AiProviderService.Trimmed(request.Provider), context).ConfigureAwait(false);
        return retrieved.TryGetValue(out var chunks)
            ? new KnowledgeSearchResponse(chunks.Select(KnowledgeCitationResponse.From).ToList())
            : retrieved.Error!;
    }
}
