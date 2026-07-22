// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.AI.Domain.DomainServices;

/// <summary>
/// 知识文档领域服务接口
/// </summary>
public interface IKnowledgeDocumentDomainService
{
    /// <summary>
    /// 摄取一篇文档（落库元信息 + 切片嵌入入向量库；失败落 Failed 状态不抛）
    /// </summary>
    Task<KnowledgeDocumentCommandResult> IngestAsync(KnowledgeIngestCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 重建索引（清旧向量 + 用原文重新切片嵌入）
    /// </summary>
    Task<KnowledgeDocumentCommandResult> ReindexAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除文档（清向量 + 软删元信息）
    /// </summary>
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}
