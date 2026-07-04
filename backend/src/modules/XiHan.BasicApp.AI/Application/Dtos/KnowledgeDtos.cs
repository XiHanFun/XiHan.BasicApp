#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:KnowledgeDtos
// Guid:a11c0de0-5010-4a10-9a00-00000000ai59
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/05 16:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

#pragma warning disable CS1591

using XiHan.BasicApp.AI.Domain.Enums;
using XiHan.BasicApp.Core.Dtos;

namespace XiHan.BasicApp.AI.Application.Dtos;

/// <summary>
/// 知识文档摄取 DTO（前端读取文件为文本或直接粘贴，均以文本提交）
/// </summary>
public sealed class KnowledgeIngestDto
{
    public string Title { get; set; } = string.Empty;
    public KnowledgeSourceType SourceType { get; set; } = KnowledgeSourceType.PasteText;
    public string? Source { get; set; }
    public string Text { get; set; } = string.Empty;

    /// <summary>嵌入 provider 配置编码（空=默认 provider）</summary>
    public string? EmbeddingProviderCode { get; set; }

    public string? Remark { get; set; }
}

/// <summary>
/// 知识文档单体动作 DTO（重建索引，POST 携带主键）
/// </summary>
public sealed class KnowledgeActionDto : BasicAppDto
{
}

/// <summary>
/// 知识文档分页查询 DTO
/// </summary>
public sealed class KnowledgePageQueryDto : BasicAppPRDto
{
    public string? Keyword { get; set; }
    public KnowledgeSourceType? SourceType { get; set; }
    public KnowledgeIndexStatus? Status { get; set; }
}

/// <summary>
/// 知识文档列表项 DTO（不含原文）
/// </summary>
public class KnowledgeListItemDto : BasicAppDto
{
    public string Title { get; set; } = string.Empty;
    public KnowledgeSourceType SourceType { get; set; }
    public string? Source { get; set; }
    public int ChunkCount { get; set; }
    public string? EmbeddingProviderCode { get; set; }
    public KnowledgeIndexStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public int Sort { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset? ModifiedTime { get; set; }
}

/// <summary>
/// 知识文档详情 DTO（含原文）
/// </summary>
public sealed class KnowledgeDetailDto : KnowledgeListItemDto
{
    public string? RawContent { get; set; }
    public string? Remark { get; set; }
    public long? CreatedId { get; set; }
    public string? CreatedBy { get; set; }
    public long? ModifiedId { get; set; }
    public string? ModifiedBy { get; set; }
}

/// <summary>
/// 知识检索/问答请求 DTO
/// </summary>
public sealed class KnowledgeQueryDto
{
    public string Query { get; set; } = string.Empty;
    public int? TopK { get; set; }

    /// <summary>provider 配置编码（空=默认；同时用于嵌入与会话）</summary>
    public string? Provider { get; set; }

    /// <summary>是否在检索后调用会话生成答案（false 仅返回命中片段）</summary>
    public bool Answer { get; set; } = true;
}

/// <summary>
/// 知识检索命中片段 DTO
/// </summary>
public sealed class KnowledgeCitationDto
{
    public string DocumentId { get; set; } = string.Empty;
    public int Index { get; set; }
    public string? Title { get; set; }
    public string? Source { get; set; }
    public double? Score { get; set; }
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// 知识检索/问答结果 DTO
/// </summary>
public sealed class KnowledgeQueryResultDto
{
    public string? Answer { get; set; }
    public List<KnowledgeCitationDto> Citations { get; set; } = [];
}
