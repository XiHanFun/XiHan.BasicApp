// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.AI.Domain.Entities;

/// <summary>
/// 知识文档
/// </summary>
/// <remarks>
/// 租户库实体，平台一侧租户为 <c>platform</c>（方案 A2：租户维护自己的知识）；向量按侧分集合，检索只在调用方一侧
/// 原文留在库里供重建；切块与向量在向量库，块元数据存标题与来源
/// </remarks>
[SugarIndex("ix_{table}_tenant_id_created_at", nameof(TenantId), OrderByType.Asc, nameof(CreatedAt), OrderByType.Desc)]
[SugarIndex("ix_{table}_tenant_id_status", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
[SugarIndex("ix_{table}_embedding_provider_code", nameof(EmbeddingProviderCode), OrderByType.Asc)]
public sealed class SysKnowledgeDocument : TenantScopedAuditedEntity
{
    /// <summary>
    /// 标题
    /// </summary>
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 来源类型
    /// </summary>
    public KnowledgeSourceType SourceType { get; set; }

    /// <summary>
    /// 来源标识（如文件名）
    /// </summary>
    [StringLength(500)]
    public string? Source { get; set; }

    /// <summary>
    /// 原文
    /// </summary>
    [LongText]
    public string RawContent { get; set; } = string.Empty;

    /// <summary>
    /// 向量库里的块数
    /// </summary>
    public int ChunkCount { get; set; }

    /// <summary>
    /// 嵌入提供商的编码（为空即目录的缺省提供商）
    /// </summary>
    [StringLength(64)]
    public string? EmbeddingProviderCode { get; set; }

    /// <summary>
    /// 索引状态
    /// </summary>
    public KnowledgeIndexStatus Status { get; set; }

    /// <summary>
    /// 最近一次索引失败的错误码
    /// </summary>
    [StringLength(64)]
    public string? ErrorCode { get; set; }

    /// <summary>
    /// 最近一次索引失败的原因（发起人的区域）
    /// </summary>
    [StringLength(1000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [StringLength(500)]
    public string? Remark { get; set; }
}

/// <summary>
/// 知识文档的来源类型
/// </summary>
public enum KnowledgeSourceType
{
    /// <summary>
    /// 粘贴的文本
    /// </summary>
    PasteText = 0,

    /// <summary>
    /// 上传的文件（前端读成文本提交）
    /// </summary>
    UploadFile = 1,
}

/// <summary>
/// 知识文档的索引状态
/// </summary>
public enum KnowledgeIndexStatus
{
    /// <summary>
    /// 待处理（进程在索引途中中断即停在这里，可以重建）
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 已索引
    /// </summary>
    Indexed = 1,

    /// <summary>
    /// 索引失败
    /// </summary>
    Failed = 2,
}
