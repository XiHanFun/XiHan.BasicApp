// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.AI.Infrastructure;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.AI.Abstractions.Chat;
using XiHan.Framework.AI.Abstractions.Knowledge;
using XiHan.Framework.Localization.Abstractions.Messages;
using XiHan.Framework.MultiTenancy.Abstractions.Sharding;
using XiHan.Framework.Persistence.Abstractions.UnitOfWork;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.AI.Domain.Services;

/// <summary>
/// 知识文档的索引与检索
/// </summary>
/// <remarks>
/// 摄入与重建有网络 I/O（嵌入与向量库），不放在调用的事务里：行的写入各自在独立的工作单元提交，索引在两次写入之间做；
/// 进程在途中中断即停在 <see cref="KnowledgeIndexStatus.Pending"/>，可以重建。回写失败时补偿删除刚写入的向量，库与向量库不留孤儿
/// 集合按侧分（平台 <c>knowledge.platform</c>，租户 <c>knowledge.&lt;租户 ID&gt;</c>），检索只在调用方一侧的集合里做：框架的元数据过滤在进程内完成，不能当隔离边界
/// 提供商的编码为空即目录的缺省提供商（框架问目录要缺省名）
/// </remarks>
/// <param name="rows">按侧读写</param>
/// <param name="ingestor">知识摄入</param>
/// <param name="retriever">知识检索</param>
/// <param name="augmenter">检索增强</param>
/// <param name="chat">聊天</param>
/// <param name="catalog">提供商目录</param>
/// <param name="scopes">作用域工厂</param>
/// <param name="logger">日志</param>
public sealed partial class KnowledgeDocumentService(
    SideRows rows,
    IKnowledgeIngestor ingestor,
    IKnowledgeRetriever retriever,
    IRagPromptAugmenter augmenter,
    IAIChat chat,
    AiProviderCatalog catalog,
    IServiceScopeFactory scopes,
    ILogger<KnowledgeDocumentService> logger)
{
    /// <summary>
    /// 一侧的向量集合
    /// </summary>
    /// <param name="side">侧</param>
    public static string CollectionOf(string side) => $"knowledge.{side}";

    /// <summary>
    /// 按 ID 取调用方一侧的文档
    /// </summary>
    /// <param name="id">文档 ID</param>
    /// <param name="context">调用上下文</param>
    public async Task<Result<SysKnowledgeDocument>> FindAsync(Guid id, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var found = await rows.FindAsync<SysKnowledgeDocument>(SideRows.SideOf(context), id, context.CancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var document)
            ? document
            : Error.NotFound(AIErrors.KnowledgeDocumentNotFound, AIMessages.KnowledgeDocumentNotFound, MessageArguments.Of(("id", id)));
    }

    /// <summary>
    /// 保存一篇新文档并索引
    /// </summary>
    /// <remarks>
    /// 先落一行 <see cref="KnowledgeIndexStatus.Pending"/>；索引失败以 <see cref="KnowledgeIndexStatus.Failed"/> 保存并成功返回，可以重建
    /// </remarks>
    /// <param name="document">文档（已填 ID 与所在一侧）</param>
    /// <param name="context">调用上下文</param>
    public async Task<Result<SysKnowledgeDocument>> IngestAsync(SysKnowledgeDocument document, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(context);
        await WriteAsync(document.TenantId, (side, ct) => side.InsertAsync(document, ct), context.CancellationToken).ConfigureAwait(false);
        return await IndexAsync(document, context).ConfigureAwait(false);
    }

    /// <summary>
    /// 用原文重建索引
    /// </summary>
    /// <remarks>
    /// 框架先写后删：新块写入成功之后才裁掉旧版本多出来的块；失败时旧块照常可检索，块数不变
    /// </remarks>
    /// <param name="document">文档</param>
    /// <param name="context">调用上下文</param>
    public Task<Result<SysKnowledgeDocument>> ReindexAsync(SysKnowledgeDocument document, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(context);
        return IndexAsync(document, context);
    }

    /// <summary>
    /// 删除文档的向量
    /// </summary>
    /// <remarks>
    /// 没写入过向量的（索引失败且块数为 0）不碰向量库：没配向量库时也删得掉这些文档
    /// </remarks>
    /// <param name="document">文档</param>
    /// <param name="cancellationToken">取消令牌</param>
    public ValueTask<Result> RemoveVectorsAsync(SysKnowledgeDocument document, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);
        return document is { Status: KnowledgeIndexStatus.Failed, ChunkCount: 0 }
            ? ValueTask.FromResult(Result.Success())
            : ingestor.RemoveAsync(CollectionOf(document.TenantId), document.Id.ToString(), cancellationToken);
    }

    /// <summary>
    /// 在调用方一侧检索
    /// </summary>
    /// <param name="question">问题</param>
    /// <param name="top">条数；为空即框架配置的缺省</param>
    /// <param name="provider">嵌入提供商的编码；为空即缺省提供商</param>
    /// <param name="context">调用上下文</param>
    /// <returns>命中的块，按得分从高到低；顺序就是增强模板里的编号</returns>
    public ValueTask<Result<IReadOnlyList<RetrievedChunk>>> RetrieveAsync(string question, int? top, string? provider, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(question);
        ArgumentNullException.ThrowIfNull(context);
        return retriever.RetrieveAsync(new KnowledgeQuery { Text = question, Collection = CollectionOf(SideRows.SideOf(context)), Top = top, Provider = provider }, context.CancellationToken);
    }

    /// <summary>
    /// 按命中的块生成答案
    /// </summary>
    /// <remarks>
    /// 用框架内置的增强模板（方案 D9），最大输出与温度取提供商的配置（方案 D2）
    /// </remarks>
    /// <param name="question">问题</param>
    /// <param name="chunks">命中的块</param>
    /// <param name="provider">聊天提供商的编码；为空即缺省提供商</param>
    /// <param name="context">调用上下文</param>
    /// <returns>答案</returns>
    public async Task<Result<string>> AnswerAsync(string question, IReadOnlyList<RetrievedChunk> chunks, string? provider, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(question);
        ArgumentNullException.ThrowIfNull(chunks);
        ArgumentNullException.ThrowIfNull(context);
        var replied = await chat.CompleteAsync(
            new AIChatRequest
            {
                Messages = [new ChatMessage(ChatRole.User, augmenter.Augment(question, chunks))],
                Provider = provider,
                Options = catalog.ChatOptionsOf(provider),
            },
            context.CancellationToken).ConfigureAwait(false);
        return replied.TryGetValue(out var response) ? response.Text : replied.Error!;
    }

    private async Task<Result<SysKnowledgeDocument>> IndexAsync(SysKnowledgeDocument document, InvocationContext context)
    {
        var collection = CollectionOf(document.TenantId);
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal) { [KnowledgeMetadataKeys.Title] = document.Title };
        if (document.Source is { } source)
        {
            metadata[KnowledgeMetadataKeys.Source] = source;
        }

        var outcome = await ingestor.IngestAsync(
            new KnowledgeIngestRequest
            {
                DocumentId = document.Id.ToString(),
                Text = document.RawContent,
                Collection = collection,
                Metadata = metadata,
                Provider = document.EmbeddingProviderCode,
            },
            context.CancellationToken).ConfigureAwait(false);
        var written = Record(document, outcome, context);
        try
        {
            await WriteAsync(document.TenantId, (side, ct) => side.UpdateAsync(document, ct), context.CancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (written > 0)
        {
            // 回写失败：删掉刚写入的向量，否则库里记的块数与向量库对不上，孤儿向量一直混在检索里
            var removed = await ingestor.RemoveAsync(collection, document.Id.ToString(), CancellationToken.None).ConfigureAwait(false);
            if (removed.IsFailure)
            {
                LogCompensationFailed(logger, exception, document.Id, written);
            }

            throw;
        }

        return document;
    }

    /// <summary>
    /// 把索引结果记到行上
    /// </summary>
    /// <returns>写入向量库的块数</returns>
    private static int Record(SysKnowledgeDocument document, Result<int> outcome, InvocationContext context)
    {
        if (!outcome.TryGetValue(out var count))
        {
            Fail(document, outcome.Error!, context);
            return 0;
        }

        if (count == 0)
        {
            document.ChunkCount = 0;
            Fail(document, Error.Validation(AIErrors.KnowledgeNoChunks, AIMessages.KnowledgeNoChunks), context);
            return 0;
        }

        document.ChunkCount = count;
        document.Status = KnowledgeIndexStatus.Indexed;
        document.ErrorCode = null;
        document.ErrorMessage = null;
        return count;
    }

    private static void Fail(SysKnowledgeDocument document, Error error, InvocationContext context)
    {
        var message = context.Localize(error);
        document.Status = KnowledgeIndexStatus.Failed;
        document.ErrorCode = error.Code;
        document.ErrorMessage = message.Length <= 1000 ? message : message[..1000];
    }

    /// <summary>
    /// 在独立的工作单元里写一侧的行
    /// </summary>
    /// <remarks>
    /// 独立库的租户先绑定分片，写进它自己的库；提交之后才返回，失败抛出
    /// </remarks>
    private async Task WriteAsync(string side, Func<SideRows, CancellationToken, Task> write, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        (await scope.ServiceProvider.GetRequiredService<TenantShardScope>().BindTenantAsync(side, cancellationToken).ConfigureAwait(false)).ToResult().ThrowIfFailure();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await unitOfWork.BeginAsync(new UnitOfWorkOptions(TenantId: side), cancellationToken).ConfigureAwait(false);
        try
        {
            await write(scope.ServiceProvider.GetRequiredService<SideRows>(), cancellationToken).ConfigureAwait(false);
            (await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false)).ThrowIfFailure();
        }
        catch
        {
            if (unitOfWork.State == UnitOfWorkState.Active)
            {
                await unitOfWork.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            }

            throw;
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Saving knowledge document {DocumentId} failed and removing its {ChunkCount} chunks failed too; the chunks may stay in the vector store.")]
    private static partial void LogCompensationFailed(ILogger logger, Exception exception, Guid documentId, int chunkCount);
}
