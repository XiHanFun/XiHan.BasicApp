// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.AI.Application.Dtos;
using XiHan.BasicApp.AI.Domain.DomainServices;
using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.Framework.AI.Abstractions.Rag.Models;

namespace XiHan.BasicApp.AI.Application.Mappers;

/// <summary>
/// 知识库应用层映射器（手写静态映射）
/// </summary>
public static class KnowledgeApplicationMapper
{
    /// <summary>
    /// 映射摄取命令
    /// </summary>
    public static KnowledgeIngestCommand ToIngestCommand(KnowledgeIngestDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new KnowledgeIngestCommand(
            input.Title,
            input.SourceType,
            input.Source,
            input.Text,
            input.EmbeddingProviderCode,
            input.Remark);
    }

    /// <summary>
    /// 实体 → 列表项 DTO
    /// </summary>
    public static KnowledgeListItemDto ToListItemDto(SysKnowledgeDocument entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new KnowledgeListItemDto
        {
            BasicId = entity.BasicId,
            Title = entity.Title,
            SourceType = entity.SourceType,
            Source = entity.Source,
            ChunkCount = entity.ChunkCount,
            EmbeddingProviderCode = entity.EmbeddingProviderCode,
            Status = entity.Status,
            ErrorMessage = entity.ErrorMessage,
            Sort = entity.Sort,
            CreatedTime = entity.CreatedTime,
            ModifiedTime = entity.ModifiedTime
        };
    }

    /// <summary>
    /// 实体 → 详情 DTO（含原文）
    /// </summary>
    public static KnowledgeDetailDto ToDetailDto(SysKnowledgeDocument entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var item = ToListItemDto(entity);
        return new KnowledgeDetailDto
        {
            BasicId = item.BasicId,
            Title = item.Title,
            SourceType = item.SourceType,
            Source = item.Source,
            ChunkCount = item.ChunkCount,
            EmbeddingProviderCode = item.EmbeddingProviderCode,
            Status = item.Status,
            ErrorMessage = item.ErrorMessage,
            Sort = item.Sort,
            CreatedTime = item.CreatedTime,
            ModifiedTime = item.ModifiedTime,
            RawContent = entity.RawContent,
            Remark = entity.Remark,
            CreatedId = entity.CreatedId,
            CreatedBy = entity.CreatedBy,
            ModifiedId = entity.ModifiedId,
            ModifiedBy = entity.ModifiedBy
        };
    }

    /// <summary>
    /// 检索片段 → 引用 DTO
    /// </summary>
    public static KnowledgeCitationDto ToCitationDto(RetrievedChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);

        return new KnowledgeCitationDto
        {
            DocumentId = chunk.DocumentId,
            Index = chunk.Index,
            Title = chunk.Title,
            Source = chunk.Source,
            Score = chunk.Score,
            Text = chunk.Text
        };
    }
}
