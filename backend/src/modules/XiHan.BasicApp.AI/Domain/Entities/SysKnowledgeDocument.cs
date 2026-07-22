// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.AI.Domain.Enums;
using XiHan.BasicApp.Core.Entities;

namespace XiHan.BasicApp.AI.Domain.Entities;

/// <summary>
/// 系统知识文档实体（RAG 知识库的文档元数据；切片向量存于向量库，本表只存文档元信息与原文）
/// </summary>
/// <remarks>
/// 切片/向量落在向量库（Qdrant），按 DocumentId(=本实体主键字符串) 关联；本表保留原文 RawContent 以支持重建索引。
/// Status 记录索引结果（Pending/Indexed/Failed）；ChunkCount 为已入库切片数，删除/重建据此清理向量。
/// </remarks>
[SugarTable(TableName = "Sys_Knowledge_Document", TableDescription = "系统知识文档表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
public partial class SysKnowledgeDocument : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 文档标题
    /// </summary>
    [SugarColumn(ColumnName = "Title", ColumnDescription = "文档标题", Length = 200, IsNullable = false)]
    public virtual string Title { get; set; } = string.Empty;

    /// <summary>
    /// 来源类型
    /// </summary>
    [SugarColumn(ColumnName = "Source_Type", ColumnDescription = "来源类型")]
    public virtual KnowledgeSourceType SourceType { get; set; } = KnowledgeSourceType.PasteText;

    /// <summary>
    /// 来源标识（文件名/标签，用于引用溯源）
    /// </summary>
    [SugarColumn(ColumnName = "Source", ColumnDescription = "来源标识", Length = 500, IsNullable = true)]
    public virtual string? Source { get; set; }

    /// <summary>
    /// 原文（用于重建索引，免重新上传）
    /// </summary>
    [SugarColumn(ColumnName = "Raw_Content", ColumnDescription = "原文", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = false)]
    public virtual string RawContent { get; set; } = string.Empty;

    /// <summary>
    /// 已入库切片数
    /// </summary>
    [SugarColumn(ColumnName = "Chunk_Count", ColumnDescription = "切片数")]
    public virtual int ChunkCount { get; set; } = 0;

    /// <summary>
    /// 嵌入 provider 配置编码（空=默认 provider）
    /// </summary>
    [SugarColumn(ColumnName = "Embedding_Provider_Code", ColumnDescription = "嵌入 provider 编码", Length = 100, IsNullable = true)]
    public virtual string? EmbeddingProviderCode { get; set; }

    /// <summary>
    /// 索引状态
    /// </summary>
    [SugarColumn(ColumnName = "Status", ColumnDescription = "索引状态")]
    public virtual KnowledgeIndexStatus Status { get; set; } = KnowledgeIndexStatus.Pending;

    /// <summary>
    /// 最近失败原因
    /// </summary>
    [SugarColumn(ColumnName = "Error_Message", ColumnDescription = "失败原因", Length = 1000, IsNullable = true)]
    public virtual string? ErrorMessage { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    [SugarColumn(ColumnName = "Sort", ColumnDescription = "排序")]
    public virtual int Sort { get; set; } = 0;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "Remark", ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
