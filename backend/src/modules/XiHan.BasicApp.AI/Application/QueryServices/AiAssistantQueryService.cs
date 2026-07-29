// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
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
/// AI 助手查询应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.AI", GroupName = "AI 服务", Tag = "AI助手")]
public sealed class AiAssistantQueryService : AiApplicationService, IAiAssistantQueryService
{
    private readonly IAiAssistantRepository _assistantRepository;

    /// <summary>
    /// 字段级安全（排序/过滤门控）
    /// </summary>
    private readonly IFieldSecurityService _fieldSecurity;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AiAssistantQueryService(IAiAssistantRepository assistantRepository, IFieldSecurityService fieldSecurityService)
    {
        _assistantRepository = assistantRepository;
        _fieldSecurity = fieldSecurityService;
    }

    /// <inheritdoc />
    [PermissionAuthorize(AiAssistantPermissionCodes.Read)]
    [HttpPost]
    public async Task<PageResultDtoBase<AiAssistantListItemDto>> GetPageAsync(AiAssistantPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildPageRequest(input);

        // 排序：前端选择优先，FLS 门控剔除不可读/已脱敏字段；无有效排序回退默认排序
        await _fieldSecurity.GuardSortsAsync(request.Conditions, nameof(SysAiAssistant), cancellationToken);
        // 过滤：前端区间/多选下发，FLS 门控剔除不可读/已脱敏字段
        await _fieldSecurity.GuardFiltersAsync(request.Conditions, nameof(SysAiAssistant), cancellationToken);
        if (request.Conditions.Sorts.Count == 0)
        {
            ApplyAssistantSorts(request);
        }

        var assistantPage = await _assistantRepository.GetPagedAsync(request, cancellationToken);
        if (assistantPage.Items.Count == 0)
        {
            return new PageResultDtoBase<AiAssistantListItemDto>([], assistantPage.Page)
            {
                ExtendDatas = assistantPage.ExtendDatas
            };
        }

        var items = assistantPage.Items
            .Select(AiAssistantApplicationMapper.ToListItemDto)
            .ToList();
        return new PageResultDtoBase<AiAssistantListItemDto>(items, assistantPage.Page)
        {
            ExtendDatas = assistantPage.ExtendDatas
        };
    }

    /// <inheritdoc />
    [PermissionAuthorize(AiAssistantPermissionCodes.Read)]
    public async Task<AiAssistantDetailDto?> GetDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "助手主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var assistant = await _assistantRepository.GetByIdAsync(id, cancellationToken);
        return assistant is null ? null : AiAssistantApplicationMapper.ToDetailDto(assistant);
    }

    /// <inheritdoc />
    /// <remarks>
    /// 聊天页人人可见，只按登录态门控：助手管理权限属后台配置，不能拿它挡住普通用户使用助手。
    /// </remarks>
    [Authorize]
    public async Task<IReadOnlyList<AiAssistantOptionDto>> GetAvailableAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var assistants = await _assistantRepository.GetEnabledListAsync(cancellationToken);
        return [.. assistants.Select(AiAssistantApplicationMapper.ToOptionDto)];
    }

    /// <summary>
    /// 构建助手分页请求
    /// </summary>
    private static BasicAppPRDto BuildPageRequest(AiAssistantPageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Conditions = new QueryConditions()
        };

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword<SysAiAssistant>(
                input.Keyword.Trim(),
                assistant => assistant.AssistantCode,
                assistant => assistant.AssistantName,
                assistant => assistant.Description);
        }

        if (input.IsDefault.HasValue)
        {
            request.Conditions.AddFilter((SysAiAssistant assistant) => assistant.IsDefault, input.IsDefault.Value);
        }

        if (input.IsEnabled.HasValue)
        {
            request.Conditions.AddFilter((SysAiAssistant assistant) => assistant.IsEnabled, input.IsEnabled.Value);
        }

        if (input.Status.HasValue)
        {
            request.Conditions.AddFilter((SysAiAssistant assistant) => assistant.Status, input.Status.Value);
        }

        // 前端选择的排序原样带入（FLS 门控与默认兜底在调用方 GetPageAsync 处理）
        if (input.Conditions?.Sorts is { Count: > 0 } sorts)
        {
            _ = request.Conditions.AddSorts(sorts);
        }

        // 前端下发的区间/多选过滤原样带入（FLS 门控在调用方 GetPageAsync 处理）
        if (input.Conditions?.Filters is { Count: > 0 } filters)
        {
            _ = request.Conditions.AddFilters(filters);
        }

        return request;
    }

    /// <summary>
    /// 应用助手默认排序
    /// </summary>
    private static void ApplyAssistantSorts(BasicAppPRDto request)
    {
        request.Conditions.AddSort((SysAiAssistant assistant) => assistant.Sort, SortDirection.Ascending, 0);
        request.Conditions.AddSort((SysAiAssistant assistant) => assistant.CreatedTime, SortDirection.Descending, 1);
    }
}
