#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:KnowledgeDocumentQueryService
// Guid:a11c0de0-5014-4a10-9a00-00000000ai5d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/05 16:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Mvc;
using XiHan.BasicApp.AI.Application.Contracts;
using XiHan.BasicApp.AI.Application.Dtos;
using XiHan.BasicApp.AI.Application.Mappers;
using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.AI.Domain.Permissions;
using XiHan.BasicApp.AI.Domain.Repositories;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Extensions;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.Domain.Shared.Paging.Models;

namespace XiHan.BasicApp.AI.Application.QueryServices;

/// <summary>
/// 知识文档查询应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.AI", GroupName = "AI 服务", Tag = "知识库")]
public sealed class KnowledgeDocumentQueryService : AiApplicationService, IKnowledgeDocumentQueryService
{
    private readonly IKnowledgeDocumentRepository _documentRepository;

    /// <summary>
    /// 字段级安全（排序/过滤门控）
    /// </summary>
    private readonly IFieldSecurityService _fieldSecurity;

    /// <summary>
    /// 构造函数
    /// </summary>
    public KnowledgeDocumentQueryService(IKnowledgeDocumentRepository documentRepository, IFieldSecurityService fieldSecurityService)
    {
        _documentRepository = documentRepository;
        _fieldSecurity = fieldSecurityService;
    }

    /// <inheritdoc />
    [PermissionAuthorize(KnowledgePermissionCodes.Read)]
    [HttpPost]
    public async Task<PageResultDtoBase<KnowledgeListItemDto>> GetPageAsync(KnowledgePageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildPageRequest(input);

        await _fieldSecurity.GuardSortsAsync(request.Conditions, "SysKnowledgeDocument", cancellationToken);
        await _fieldSecurity.GuardFiltersAsync(request.Conditions, "SysKnowledgeDocument", cancellationToken);
        if (request.Conditions.Sorts.Count == 0)
        {
            ApplyDocumentSorts(request);
        }

        var documentPage = await _documentRepository.GetPagedAsync(request, cancellationToken);
        if (documentPage.Items.Count == 0)
        {
            return new PageResultDtoBase<KnowledgeListItemDto>([], documentPage.Page)
            {
                ExtendDatas = documentPage.ExtendDatas
            };
        }

        var items = documentPage.Items
            .Select(KnowledgeApplicationMapper.ToListItemDto)
            .ToList();
        return new PageResultDtoBase<KnowledgeListItemDto>(items, documentPage.Page)
        {
            ExtendDatas = documentPage.ExtendDatas
        };
    }

    /// <inheritdoc />
    [PermissionAuthorize(KnowledgePermissionCodes.Read)]
    public async Task<KnowledgeDetailDto?> GetDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "知识文档主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var document = await _documentRepository.GetByIdAsync(id, cancellationToken);
        return document is null ? null : KnowledgeApplicationMapper.ToDetailDto(document);
    }

    /// <summary>
    /// 构建文档分页请求
    /// </summary>
    private static BasicAppPRDto BuildPageRequest(KnowledgePageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Behavior = input.Behavior,
            Conditions = new QueryConditions()
        };

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword<SysKnowledgeDocument>(
                input.Keyword.Trim(),
                document => document.Title,
                document => document.Source);
        }

        if (input.SourceType.HasValue)
        {
            request.Conditions.AddFilter((SysKnowledgeDocument document) => document.SourceType, input.SourceType.Value);
        }

        if (input.Status.HasValue)
        {
            request.Conditions.AddFilter((SysKnowledgeDocument document) => document.Status, input.Status.Value);
        }

        if (input.Conditions?.Sorts is { Count: > 0 } sorts)
        {
            _ = request.Conditions.AddSorts(sorts);
        }

        if (input.Conditions?.Filters is { Count: > 0 } filters)
        {
            _ = request.Conditions.AddFilters(filters);
        }
        return request;
    }

    /// <summary>
    /// 应用文档默认排序
    /// </summary>
    private static void ApplyDocumentSorts(BasicAppPRDto request)
    {
        request.Conditions.AddSort((SysKnowledgeDocument document) => document.Sort, SortDirection.Ascending, 0);
        request.Conditions.AddSort((SysKnowledgeDocument document) => document.CreatedTime, SortDirection.Descending, 1);
    }
}
