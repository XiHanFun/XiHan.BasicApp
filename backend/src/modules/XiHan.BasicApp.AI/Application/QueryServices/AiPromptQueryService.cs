// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
/// AI 提示词查询应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.AI", GroupName = "AI 服务", Tag = "提示词")]
public sealed class AiPromptQueryService : AiApplicationService, IAiPromptQueryService
{
    private readonly IAiPromptRepository _promptRepository;

    /// <summary>
    /// 字段级安全（排序/过滤门控）
    /// </summary>
    private readonly IFieldSecurityService _fieldSecurity;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AiPromptQueryService(IAiPromptRepository promptRepository, IFieldSecurityService fieldSecurityService)
    {
        _promptRepository = promptRepository;
        _fieldSecurity = fieldSecurityService;
    }

    /// <inheritdoc />
    [PermissionAuthorize(AiPromptPermissionCodes.Read)]
    [HttpPost]
    public async Task<PageResultDtoBase<AiPromptListItemDto>> GetPageAsync(AiPromptPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildPageRequest(input);

        await _fieldSecurity.GuardSortsAsync(request.Conditions, "SysAiPrompt", cancellationToken);
        await _fieldSecurity.GuardFiltersAsync(request.Conditions, "SysAiPrompt", cancellationToken);
        if (request.Conditions.Sorts.Count == 0)
        {
            ApplyPromptSorts(request);
        }

        var promptPage = await _promptRepository.GetPagedAsync(request, cancellationToken);
        if (promptPage.Items.Count == 0)
        {
            return new PageResultDtoBase<AiPromptListItemDto>([], promptPage.Page)
            {
                ExtendDatas = promptPage.ExtendDatas
            };
        }

        var items = promptPage.Items
            .Select(AiPromptApplicationMapper.ToListItemDto)
            .ToList();
        return new PageResultDtoBase<AiPromptListItemDto>(items, promptPage.Page)
        {
            ExtendDatas = promptPage.ExtendDatas
        };
    }

    /// <inheritdoc />
    [PermissionAuthorize(AiPromptPermissionCodes.Read)]
    public async Task<AiPromptDetailDto?> GetDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "提示词主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var prompt = await _promptRepository.GetByIdAsync(id, cancellationToken);
        return prompt is null ? null : AiPromptApplicationMapper.ToDetailDto(prompt);
    }

    /// <summary>
    /// 构建提示词分页请求
    /// </summary>
    private static BasicAppPRDto BuildPageRequest(AiPromptPageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Conditions = new QueryConditions()
        };

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword<SysAiPrompt>(
                input.Keyword.Trim(),
                prompt => prompt.PromptCode,
                prompt => prompt.PromptName);
        }

        if (!string.IsNullOrWhiteSpace(input.Category))
        {
            request.Conditions.AddFilter((SysAiPrompt prompt) => prompt.Category, input.Category.Trim());
        }

        if (input.IsEnabled.HasValue)
        {
            request.Conditions.AddFilter((SysAiPrompt prompt) => prompt.IsEnabled, input.IsEnabled.Value);
        }

        if (input.Status.HasValue)
        {
            request.Conditions.AddFilter((SysAiPrompt prompt) => prompt.Status, input.Status.Value);
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
    /// 应用提示词默认排序
    /// </summary>
    private static void ApplyPromptSorts(BasicAppPRDto request)
    {
        request.Conditions.AddSort((SysAiPrompt prompt) => prompt.Sort, SortDirection.Ascending, 0);
        request.Conditions.AddSort((SysAiPrompt prompt) => prompt.CreatedTime, SortDirection.Descending, 1);
    }
}
