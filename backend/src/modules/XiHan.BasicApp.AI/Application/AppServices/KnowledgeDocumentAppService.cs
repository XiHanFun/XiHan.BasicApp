// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using XiHan.BasicApp.AI.Application.Contracts;
using XiHan.BasicApp.AI.Application.Dtos;
using XiHan.BasicApp.AI.Application.Mappers;
using XiHan.BasicApp.AI.Domain.DomainServices;
using XiHan.BasicApp.AI.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;

namespace XiHan.BasicApp.AI.Application.AppServices;

/// <summary>
/// 知识文档命令应用服务
/// </summary>
/// <remarks>摄取/重建含外部 I/O（嵌入 + 向量库），不套 UnitOfWork（避免网络 I/O 期间长事务，见领域服务说明）。</remarks>
[DynamicApi(Group = "BasicApp.AI", GroupName = "AI 服务", Tag = "知识库")]
public sealed class KnowledgeDocumentAppService : AiApplicationService, IKnowledgeDocumentAppService
{
    private readonly IKnowledgeDocumentDomainService _documentDomainService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public KnowledgeDocumentAppService(IKnowledgeDocumentDomainService documentDomainService)
    {
        _documentDomainService = documentDomainService;
    }

    /// <summary>
    /// 摄取文档（切片嵌入入库）
    /// </summary>
    [PermissionAuthorize(KnowledgePermissionCodes.Create)]
    [HttpPost]
    public async Task<KnowledgeDetailDto> IngestAsync(KnowledgeIngestDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _documentDomainService.IngestAsync(KnowledgeApplicationMapper.ToIngestCommand(input), cancellationToken);
        return KnowledgeApplicationMapper.ToDetailDto(result.Document);
    }

    /// <summary>
    /// 重建文档索引
    /// </summary>
    [PermissionAuthorize(KnowledgePermissionCodes.Update)]
    [HttpPost]
    public async Task<KnowledgeDetailDto> ReindexAsync(KnowledgeActionDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _documentDomainService.ReindexAsync(input.BasicId, cancellationToken);
        return KnowledgeApplicationMapper.ToDetailDto(result.Document);
    }

    /// <summary>
    /// 删除文档
    /// </summary>
    [PermissionAuthorize(KnowledgePermissionCodes.Delete)]
    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _documentDomainService.DeleteAsync(id, cancellationToken);
    }
}
