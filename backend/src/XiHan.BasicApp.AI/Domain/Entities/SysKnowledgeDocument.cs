// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.AI.Domain.Entities;

/// <summary>
/// 知识文档
/// </summary>
/// <remarks>
/// 租户库实体，平台一侧租户为 <c>platform</c>（方案 A2：租户维护自己的知识）：知识随租户分库隔离
/// 向量按侧分集合（<c>knowledge.platform</c>、<c>knowledge.&lt;租户 ID&gt;</c>），检索只在调用方一侧的集合里做：框架的元数据过滤在进程内完成，不能当隔离边界
/// 原文留在库里供重建；切块与向量在向量库，块元数据存标题与来源，行上只记块数、索引状态与最近一次失败的原因
/// 摄入与重建有网络 I/O（嵌入与向量库），不放在调用的事务里：行的写入各自在独立的工作单元里提交，索引在两次写入之间做；进程中断即停在 <see cref="KnowledgeIndexStatus.Pending"/>、可以重建，回写失败即补偿删除刚写入的向量，库与向量库不留孤儿
/// 索引失败以 <see cref="KnowledgeIndexStatus.Failed"/> 保存并成功返回：文档先留下，原因记在行上，可以重建；重建由框架先写后删，失败时旧块照常可检索
/// 删除先删向量再软删行，向量库不可达即不删行；没写入过向量的不碰向量库，没配向量库也删得掉
/// 嵌入提供商按编码引用（为空即缺省提供商），删提供商前逐侧按它查引用
/// </remarks>
[SugarTable(TableName = "sys_knowledge_document", TableDescription = "知识文档")]
[SugarIndex("ix_sys_knowledge_document_tenant_id_created_at", nameof(TenantId), OrderByType.Asc, nameof(CreatedAt), OrderByType.Desc)]
[SugarIndex("ix_sys_knowledge_document_tenant_id_status", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
[SugarIndex("ix_sys_knowledge_document_embedding_provider_code", nameof(EmbeddingProviderCode), OrderByType.Asc)]
public sealed class SysKnowledgeDocument : TenantScopedAuditedEntity
{
    /// <summary>
    /// 标题
    /// </summary>
    [SugarColumn(ColumnName = "title", Length = 200, IsNullable = false, ColumnDescription = "标题")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 来源类型
    /// </summary>
    [SugarColumn(ColumnName = "source_type", IsNullable = false, ColumnDescription = "来源类型")]
    public KnowledgeSourceType SourceType { get; set; }

    /// <summary>
    /// 来源标识（如文件名）
    /// </summary>
    [SugarColumn(ColumnName = "source", Length = 500, IsNullable = true, ColumnDescription = "来源标识（如文件名）")]
    public string? Source { get; set; }

    /// <summary>
    /// 原文
    /// </summary>
    [SugarColumn(ColumnName = "raw_content", IsNullable = false, ColumnDescription = "原文")]
    [LongText]
    public string RawContent { get; set; } = string.Empty;

    /// <summary>
    /// 向量库里的块数
    /// </summary>
    [SugarColumn(ColumnName = "chunk_count", IsNullable = false, ColumnDescription = "向量库里的块数")]
    public int ChunkCount { get; set; }

    /// <summary>
    /// 嵌入提供商的编码（为空即目录的缺省提供商）
    /// </summary>
    [SugarColumn(ColumnName = "embedding_provider_code", Length = 64, IsNullable = true, ColumnDescription = "嵌入提供商的编码（为空即目录的缺省提供商）")]
    public string? EmbeddingProviderCode { get; set; }

    /// <summary>
    /// 索引状态
    /// </summary>
    [SugarColumn(ColumnName = "status", IsNullable = false, ColumnDescription = "索引状态")]
    public KnowledgeIndexStatus Status { get; set; }

    /// <summary>
    /// 最近一次索引失败的错误码
    /// </summary>
    [SugarColumn(ColumnName = "error_code", Length = 64, IsNullable = true, ColumnDescription = "最近一次索引失败的错误码")]
    public string? ErrorCode { get; set; }

    /// <summary>
    /// 最近一次索引失败的原因（发起人的区域）
    /// </summary>
    [SugarColumn(ColumnName = "error_message", Length = 1000, IsNullable = true, ColumnDescription = "最近一次索引失败的原因（发起人的区域）")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    [SugarColumn(ColumnName = "order", IsNullable = false, ColumnDescription = "排序号")]
    public int Order { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "remark", Length = 500, IsNullable = true, ColumnDescription = "备注")]
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
