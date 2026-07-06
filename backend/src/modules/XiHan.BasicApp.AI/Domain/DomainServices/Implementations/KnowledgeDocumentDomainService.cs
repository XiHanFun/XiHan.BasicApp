#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:KnowledgeDocumentDomainService
// Guid:12f7b230-aedc-4feb-91ae-47b649c9dc4d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/05 16:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.AI.Domain.Enums;
using XiHan.BasicApp.AI.Domain.Repositories;
using XiHan.Framework.AI.Abstractions.Rag;

namespace XiHan.BasicApp.AI.Domain.DomainServices.Implementations;

/// <summary>
/// 知识文档领域服务实现
/// </summary>
/// <remarks>
/// 摄取/重建含外部 I/O（嵌入 API + 向量库），不套 UnitOfWork：先落库 Pending，索引后独立提交 Indexed/Failed，
/// 避免网络 I/O 期间长事务；进程中断则停留 Pending，可重建恢复。
/// </remarks>
public sealed class KnowledgeDocumentDomainService : IKnowledgeDocumentDomainService
{
    private readonly IKnowledgeDocumentRepository _documentRepository;
    private readonly IKnowledgeIngestor _ingestor;
    private readonly ILogger<KnowledgeDocumentDomainService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public KnowledgeDocumentDomainService(
        IKnowledgeDocumentRepository documentRepository,
        IKnowledgeIngestor ingestor,
        ILogger<KnowledgeDocumentDomainService> logger)
    {
        _documentRepository = documentRepository;
        _ingestor = ingestor;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<KnowledgeDocumentCommandResult> IngestAsync(KnowledgeIngestCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureEnum(command.SourceType, nameof(command.SourceType));
        var text = command.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("文档内容不能为空。");
        }

        var document = new SysKnowledgeDocument
        {
            Title = Required(command.Title, 200, nameof(command.Title), "文档标题不能超过 200 个字符。"),
            SourceType = command.SourceType,
            Source = Optional(command.Source, 500, nameof(command.Source), "来源标识不能超过 500 个字符。"),
            RawContent = text,
            EmbeddingProviderCode = Optional(command.EmbeddingProviderCode, 100, nameof(command.EmbeddingProviderCode), "嵌入 provider 编码不能超过 100 个字符。"),
            Status = KnowledgeIndexStatus.Pending,
            ChunkCount = 0,
            Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。")
        };

        document = await _documentRepository.AddAsync(document, cancellationToken);
        await IndexAsync(document, cancellationToken);
        return new KnowledgeDocumentCommandResult(document);
    }

    /// <inheritdoc />
    public async Task<KnowledgeDocumentCommandResult> ReindexAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var document = await GetDocumentOrThrowAsync(id, cancellationToken);

        // 先清旧向量（按当前已入库切片数），再用原文重新索引
        await _ingestor.RemoveDocumentAsync(document.BasicId.ToString(), document.ChunkCount, cancellationToken);
        await IndexAsync(document, cancellationToken);
        return new KnowledgeDocumentCommandResult(document);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var document = await GetDocumentOrThrowAsync(id, cancellationToken);
        await _ingestor.RemoveDocumentAsync(document.BasicId.ToString(), document.ChunkCount, cancellationToken);
        if (!await _documentRepository.DeleteAsync(document, cancellationToken))
        {
            throw new InvalidOperationException("知识文档删除失败。");
        }
    }

    /// <summary>
    /// 切片嵌入入库并回写状态（失败落 Failed，不抛）
    /// </summary>
    /// <remarks>
    /// 回写切片数的 DB 更新若失败，补偿清理刚写入的向量——否则 DB 记 0、向量库有 N，
    /// 后续按 ChunkCount(=0) 无法清理，孤儿向量会持续污染检索。
    /// </remarks>
    private async Task IndexAsync(SysKnowledgeDocument document, CancellationToken cancellationToken)
    {
        var writtenChunkCount = 0;
        try
        {
            writtenChunkCount = await _ingestor.IngestAsync(new KnowledgeIngestRequest
            {
                DocumentId = document.BasicId.ToString(),
                Text = document.RawContent,
                TenantId = document.TenantId,
                Title = document.Title,
                Source = document.Source,
                Provider = document.EmbeddingProviderCode
            }, cancellationToken);

            document.ChunkCount = writtenChunkCount;
            document.Status = writtenChunkCount > 0 ? KnowledgeIndexStatus.Indexed : KnowledgeIndexStatus.Failed;
            document.ErrorMessage = writtenChunkCount > 0 ? null : "未产生任何切片。";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            document.ChunkCount = 0;
            document.Status = KnowledgeIndexStatus.Failed;
            document.ErrorMessage = Truncate(ex.Message, 1000);
        }

        try
        {
            await _documentRepository.UpdateAsync(document, cancellationToken);
        }
        catch
        {
            // 持久化切片数失败：补偿清理已写入向量，保证 DB 与向量库一致（避免孤儿向量污染检索）
            if (writtenChunkCount > 0)
            {
                try
                {
                    await _ingestor.RemoveDocumentAsync(document.BasicId.ToString(), writtenChunkCount, CancellationToken.None);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogError(cleanupEx, "知识文档 {DocumentId} 回写切片数失败后补偿清理向量亦失败，可能残留 {ChunkCount} 条孤儿向量。", document.BasicId, writtenChunkCount);
                }
            }

            throw;
        }
    }

    private static void EnsureEnum<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "枚举值无效。");
        }
    }

    private static string? Optional(string? value, int maxLength, string paramName, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }

        return normalized;
    }

    private static string Required(string? value, int maxLength, string paramName, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }

        return normalized;
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private async Task<SysKnowledgeDocument> GetDocumentOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "知识文档主键必须大于 0。");
        }

        return await _documentRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("知识文档不存在。");
    }
}
