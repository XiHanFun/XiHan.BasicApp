// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.AI.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.AI.Application.Contracts;

/// <summary>
/// 知识文档命令应用服务接口
/// </summary>
public interface IKnowledgeDocumentAppService : IApplicationService
{
    /// <summary>
    /// 摄取文档（切片嵌入入库）
    /// </summary>
    Task<KnowledgeDetailDto> IngestAsync(KnowledgeIngestDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 重建文档索引
    /// </summary>
    Task<KnowledgeDetailDto> ReindexAsync(KnowledgeActionDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除文档
    /// </summary>
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}

/// <summary>
/// 知识文档查询应用服务接口
/// </summary>
public interface IKnowledgeDocumentQueryService : IApplicationService
{
    /// <summary>
    /// 获取文档分页列表
    /// </summary>
    Task<PageResultDtoBase<KnowledgeListItemDto>> GetPageAsync(KnowledgePageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取文档详情
    /// </summary>
    Task<KnowledgeDetailDto?> GetDetailAsync(long id, CancellationToken cancellationToken = default);
}

/// <summary>
/// 知识检索/问答应用服务接口
/// </summary>
public interface IKnowledgeQueryAppService : IApplicationService
{
    /// <summary>
    /// 检索知识并（可选）生成带引用的答案
    /// </summary>
    Task<KnowledgeQueryResultDto> QueryAsync(KnowledgeQueryDto input, CancellationToken cancellationToken = default);
}
