// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.Framework.AI.Abstractions.Knowledge;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.AI.Operations.KnowledgeDocuments;

/// <summary>
/// 知识文档的列表项
/// </summary>
/// <remarks>
/// 不带原文
/// </remarks>
/// <param name="Id">主键</param>
/// <param name="Title">标题</param>
/// <param name="SourceType">来源类型</param>
/// <param name="Source">来源标识</param>
/// <param name="ChunkCount">向量库里的块数</param>
/// <param name="EmbeddingProviderCode">嵌入提供商的编码，为空即缺省提供商</param>
/// <param name="Status">索引状态</param>
/// <param name="ErrorCode">最近一次索引失败的错误码</param>
/// <param name="ErrorMessage">最近一次索引失败的原因</param>
/// <param name="Order">排序号</param>
/// <param name="Remark">备注</param>
/// <param name="CreatedAt">创建时间（UTC）</param>
/// <param name="ModifiedAt">修改时间（UTC）</param>
public sealed record KnowledgeDocumentResponse(
    Guid Id,
    string Title,
    KnowledgeSourceType SourceType,
    string? Source,
    int ChunkCount,
    string? EmbeddingProviderCode,
    KnowledgeIndexStatus Status,
    string? ErrorCode,
    string? ErrorMessage,
    int Order,
    string? Remark,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt)
{
    /// <summary>
    /// 从实体投影
    /// </summary>
    /// <param name="document">知识文档</param>
    public static KnowledgeDocumentResponse From(SysKnowledgeDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return new KnowledgeDocumentResponse(document.Id, document.Title, document.SourceType, document.Source, document.ChunkCount, document.EmbeddingProviderCode, document.Status, document.ErrorCode, document.ErrorMessage, document.Order, document.Remark, document.CreatedAt, document.ModifiedAt);
    }
}

/// <summary>
/// 知识文档的详情
/// </summary>
/// <param name="Id">主键</param>
/// <param name="Title">标题</param>
/// <param name="SourceType">来源类型</param>
/// <param name="Source">来源标识</param>
/// <param name="RawContent">原文</param>
/// <param name="ChunkCount">向量库里的块数</param>
/// <param name="EmbeddingProviderCode">嵌入提供商的编码，为空即缺省提供商</param>
/// <param name="Status">索引状态</param>
/// <param name="ErrorCode">最近一次索引失败的错误码</param>
/// <param name="ErrorMessage">最近一次索引失败的原因</param>
/// <param name="Order">排序号</param>
/// <param name="Remark">备注</param>
/// <param name="CreatedAt">创建时间（UTC）</param>
/// <param name="ModifiedAt">修改时间（UTC）</param>
public sealed record KnowledgeDocumentDetailResponse(
    Guid Id,
    string Title,
    KnowledgeSourceType SourceType,
    string? Source,
    string RawContent,
    int ChunkCount,
    string? EmbeddingProviderCode,
    KnowledgeIndexStatus Status,
    string? ErrorCode,
    string? ErrorMessage,
    int Order,
    string? Remark,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt)
{
    /// <summary>
    /// 从实体投影
    /// </summary>
    /// <param name="document">知识文档</param>
    public static KnowledgeDocumentDetailResponse From(SysKnowledgeDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return new KnowledgeDocumentDetailResponse(document.Id, document.Title, document.SourceType, document.Source, document.RawContent, document.ChunkCount, document.EmbeddingProviderCode, document.Status, document.ErrorCode, document.ErrorMessage, document.Order, document.Remark, document.CreatedAt, document.ModifiedAt);
    }
}

/// <summary>
/// <c>knowledge-documents.get</c> 与 <c>knowledge-documents.reindex</c> 的请求
/// </summary>
/// <param name="Id">知识文档 ID</param>
public sealed record KnowledgeDocumentIdRequest(Guid Id);

/// <summary>
/// <c>knowledge-documents.ingest</c> 的请求
/// </summary>
/// <remarks>
/// 只收文本（方案 A5）：文件由前端读成文本提交，请求内同步摄入
/// </remarks>
/// <param name="Title">标题</param>
/// <param name="Text">文本</param>
/// <param name="SourceType">来源类型</param>
/// <param name="Source">来源标识</param>
/// <param name="EmbeddingProviderCode">嵌入提供商的编码，为空即缺省提供商</param>
/// <param name="Order">排序号</param>
/// <param name="Remark">备注</param>
public sealed record IngestKnowledgeDocumentRequest(
    [Required, StringLength(200, MinimumLength = 1)] string Title,
    [Required] string Text,
    KnowledgeSourceType SourceType = KnowledgeSourceType.PasteText,
    [StringLength(500)] string? Source = null,
    [StringLength(64)] string? EmbeddingProviderCode = null,
    [Range(0, int.MaxValue)] int Order = 0,
    [StringLength(500)] string? Remark = null) : IValidatableObject
{
    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var (value, member) in new[] { (Title, nameof(Title)), (Text, nameof(Text)) })
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                yield return new ValidationResult(AIMessages.FieldBlank.L(), [member]);
            }
        }
    }
}

/// <summary>
/// <c>knowledge-documents.delete</c> 的请求：集合入参
/// </summary>
/// <param name="Ids">知识文档 ID，1 到 100 个</param>
public sealed record KnowledgeDocumentIdsRequest([Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids);

/// <summary>
/// <c>knowledge-documents.query</c> 的请求
/// </summary>
/// <param name="Question">问题</param>
/// <param name="Top">检索条数；不给即配置的缺省</param>
/// <param name="Provider">提供商的编码（同时用于嵌入与聊天），不给即缺省提供商</param>
/// <param name="Answer">是否生成答案；为否只返回命中的片段</param>
public sealed record QueryKnowledgeDocumentsRequest(
    [Required, StringLength(2000, MinimumLength = 1)] string Question,
    [Range(1, 20)] int? Top = null,
    [StringLength(64)] string? Provider = null,
    bool Answer = true) : IValidatableObject
{
    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Question))
        {
            yield return new ValidationResult(AIMessages.FieldBlank.L(), [nameof(Question)]);
        }
    }
}

/// <summary>
/// <c>knowledge-documents.search</c> 的请求
/// </summary>
/// <param name="Question">要检索的问题或关键词</param>
/// <param name="Top">检索条数；不给即配置的缺省</param>
/// <param name="Provider">嵌入提供商的编码，不给即缺省提供商</param>
public sealed record SearchKnowledgeDocumentsRequest(
    [Required, StringLength(2000, MinimumLength = 1)] string Question,
    [Range(1, 20)] int? Top = null,
    [StringLength(64)] string? Provider = null) : IValidatableObject
{
    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Question))
        {
            yield return new ValidationResult(AIMessages.FieldBlank.L(), [nameof(Question)]);
        }
    }
}

/// <summary>
/// 命中的片段
/// </summary>
/// <remarks>
/// 列表里的顺序就是答案里的引用编号：<c>[n]</c> 对应第 n 个片段
/// </remarks>
/// <param name="DocumentId">知识文档 ID</param>
/// <param name="Index">块序号</param>
/// <param name="Title">文档标题</param>
/// <param name="Source">来源标识</param>
/// <param name="Score">相似度得分（越大越相似）</param>
/// <param name="Text">片段正文</param>
public sealed record KnowledgeCitationResponse(string DocumentId, int Index, string? Title, string? Source, double Score, string Text)
{
    /// <summary>
    /// 从命中的块投影
    /// </summary>
    /// <param name="chunk">命中的块</param>
    public static KnowledgeCitationResponse From(RetrievedChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        return new KnowledgeCitationResponse(
            chunk.DocumentId,
            chunk.ChunkIndex,
            chunk.Metadata.GetValueOrDefault(KnowledgeMetadataKeys.Title),
            chunk.Metadata.GetValueOrDefault(KnowledgeMetadataKeys.Source),
            chunk.Score,
            chunk.Text);
    }
}

/// <summary>
/// <c>knowledge-documents.query</c> 的响应
/// </summary>
/// <param name="Answer">生成的答案；没要答案或没有命中为空</param>
/// <param name="Citations">命中的片段</param>
public sealed record KnowledgeAnswerResponse(string? Answer, IReadOnlyList<KnowledgeCitationResponse> Citations);

/// <summary>
/// <c>knowledge-documents.search</c> 的响应
/// </summary>
/// <param name="Citations">命中的片段</param>
public sealed record KnowledgeSearchResponse(IReadOnlyList<KnowledgeCitationResponse> Citations);
