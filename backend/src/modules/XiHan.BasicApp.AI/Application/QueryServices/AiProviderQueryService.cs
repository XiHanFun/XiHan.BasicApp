#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AiProviderQueryService
// Guid:56110c01-4e88-4706-8949-79d7bf356eba
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/05 14:00:00
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
/// AI Provider 查询应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.AI", GroupName = "AI 服务", Tag = "AI提供商")]
public sealed class AiProviderQueryService : AiApplicationService, IAiProviderQueryService
{
    private readonly IAiProviderRepository _providerRepository;

    /// <summary>
    /// 字段级安全（排序/过滤门控）
    /// </summary>
    private readonly IFieldSecurityService _fieldSecurity;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AiProviderQueryService(IAiProviderRepository providerRepository, IFieldSecurityService fieldSecurityService)
    {
        _providerRepository = providerRepository;
        _fieldSecurity = fieldSecurityService;
    }

    /// <inheritdoc />
    [PermissionAuthorize(AiPermissionCodes.Read)]
    [HttpPost]
    public async Task<PageResultDtoBase<AiProviderListItemDto>> GetPageAsync(AiProviderPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildPageRequest(input);

        // 排序：前端选择优先，FLS 门控剔除不可读/已脱敏字段；无有效排序回退默认排序
        await _fieldSecurity.GuardSortsAsync(request.Conditions, "SysAiProvider", cancellationToken);
        // 过滤：前端区间/多选下发，FLS 门控剔除不可读/已脱敏字段
        await _fieldSecurity.GuardFiltersAsync(request.Conditions, "SysAiProvider", cancellationToken);
        if (request.Conditions.Sorts.Count == 0)
        {
            ApplyProviderSorts(request);
        }

        var providerPage = await _providerRepository.GetPagedAsync(request, cancellationToken);
        if (providerPage.Items.Count == 0)
        {
            return new PageResultDtoBase<AiProviderListItemDto>([], providerPage.Page)
            {
                ExtendDatas = providerPage.ExtendDatas
            };
        }

        var items = providerPage.Items
            .Select(AiProviderApplicationMapper.ToListItemDto)
            .ToList();
        return new PageResultDtoBase<AiProviderListItemDto>(items, providerPage.Page)
        {
            ExtendDatas = providerPage.ExtendDatas
        };
    }

    /// <inheritdoc />
    [PermissionAuthorize(AiPermissionCodes.Read)]
    public async Task<AiProviderDetailDto?> GetDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "provider 主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var provider = await _providerRepository.GetByIdAsync(id, cancellationToken);
        return provider is null ? null : AiProviderApplicationMapper.ToDetailDto(provider);
    }

    /// <summary>
    /// 构建 provider 分页请求
    /// </summary>
    private static BasicAppPRDto BuildPageRequest(AiProviderPageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Behavior = input.Behavior,
            Conditions = new QueryConditions()
        };

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword<SysAiProvider>(
                input.Keyword.Trim(),
                provider => provider.ConfigCode,
                provider => provider.ConfigName,
                provider => provider.Model);
        }

        if (!string.IsNullOrWhiteSpace(input.Provider))
        {
            request.Conditions.AddFilter((SysAiProvider provider) => provider.Provider, input.Provider.Trim());
        }

        if (input.IsDefault.HasValue)
        {
            request.Conditions.AddFilter((SysAiProvider provider) => provider.IsDefault, input.IsDefault.Value);
        }

        if (input.IsEnabled.HasValue)
        {
            request.Conditions.AddFilter((SysAiProvider provider) => provider.IsEnabled, input.IsEnabled.Value);
        }

        if (input.Status.HasValue)
        {
            request.Conditions.AddFilter((SysAiProvider provider) => provider.Status, input.Status.Value);
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
    /// 应用 provider 默认排序
    /// </summary>
    private static void ApplyProviderSorts(BasicAppPRDto request)
    {
        request.Conditions.AddSort((SysAiProvider provider) => provider.Sort, SortDirection.Ascending, 0);
        request.Conditions.AddSort((SysAiProvider provider) => provider.CreatedTime, SortDirection.Descending, 1);
    }
}
