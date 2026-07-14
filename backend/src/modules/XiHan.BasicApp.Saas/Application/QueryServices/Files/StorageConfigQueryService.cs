#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:StorageConfigQueryService
// Guid:f6a1c8d3-2e95-4b74-a0c6-9d5e3f8b1a47
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/12 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Extensions;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.Domain.Shared.Paging.Models;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 存储配置查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "存储配置")]
public sealed class StorageConfigQueryService
    : SaasApplicationService, IStorageConfigQueryService
{
    /// <summary>
    /// 存储配置仓储
    /// </summary>
    private readonly IStorageConfigRepository _storageConfigRepository;

    /// <summary>
    /// 字段级安全（排序门控）
    /// </summary>
    private readonly IFieldSecurityService _fieldSecurity;

    /// <summary>
    /// 构造函数
    /// </summary>
    public StorageConfigQueryService(
        IStorageConfigRepository storageConfigRepository,
        IFieldSecurityService fieldSecurityService)
    {
        _storageConfigRepository = storageConfigRepository;
        _fieldSecurity = fieldSecurityService;
    }

    /// <summary>
    /// 获取存储配置分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>存储配置分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.StorageConfig.Read)]
    [HttpPost]
    public async Task<PageResultDtoBase<StorageConfigListItemDto>> GetStorageConfigPageAsync(StorageConfigPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidatePageInput(input);

        var request = BuildStorageConfigPageRequest(input);

        // 排序：前端选择优先，FLS 门控剔除不可读/已脱敏字段；无有效排序回退默认排序
        await _fieldSecurity.GuardSortsAsync(request.Conditions, "SysStorageConfig", cancellationToken);
        // 过滤：FLS 门控剔除不可读/已脱敏字段（枚举多选 In）
        await _fieldSecurity.GuardFiltersAsync(request.Conditions, "SysStorageConfig", cancellationToken);
        if (request.Conditions.Sorts.Count == 0)
        {
            ApplyStorageConfigSorts(request);
        }

        var configPage = await _storageConfigRepository.GetPagedAsync(request, cancellationToken);
        var items = configPage.Items
            .Select(StorageConfigApplicationMapper.ToListItemDto)
            .ToList();

        return new PageResultDtoBase<StorageConfigListItemDto>(items, configPage.Page);
    }

    /// <summary>
    /// 获取存储配置详情
    /// </summary>
    /// <param name="id">存储配置主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>存储配置详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.StorageConfig.Read)]
    public async Task<StorageConfigDetailDto?> GetStorageConfigDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "存储配置主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var config = await _storageConfigRepository.GetByIdAsync(id, cancellationToken);
        return config is null ? null : StorageConfigApplicationMapper.ToDetailDto(config);
    }

    /// <summary>
    /// 构建存储配置分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>存储配置分页请求</returns>
    private static BasicAppPRDto BuildStorageConfigPageRequest(StorageConfigPageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Conditions = new QueryConditions()
        };

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword<SysStorageConfig>(
                input.Keyword.Trim(),
                config => config.ConfigCode,
                config => config.ConfigName,
                config => config.Remark);
        }

        if (input.StorageType.HasValue)
        {
            request.Conditions.AddFilter((SysStorageConfig config) => config.StorageType, input.StorageType.Value);
        }

        if (input.IsDefault.HasValue)
        {
            request.Conditions.AddFilter((SysStorageConfig config) => config.IsDefault, input.IsDefault.Value);
        }

        if (input.IsEnabled.HasValue)
        {
            request.Conditions.AddFilter((SysStorageConfig config) => config.IsEnabled, input.IsEnabled.Value);
        }

        // 前端选择的排序原样带入（FLS 门控与默认兜底在调用方 GetStorageConfigPageAsync 处理）
        if (input.Conditions?.Sorts is { Count: > 0 } sorts)
        {
            _ = request.Conditions.AddSorts(sorts);
        }

        // 前端选择的通用过滤原样带入（枚举多选 In；FLS 门控在调用方处理）
        if (input.Conditions?.Filters is { Count: > 0 } filters)
        {
            _ = request.Conditions.AddFilters(filters);
        }

        return request;
    }

    /// <summary>
    /// 施加存储配置默认排序（无前端排序时的兜底）
    /// </summary>
    /// <param name="request">分页请求</param>
    private static void ApplyStorageConfigSorts(BasicAppPRDto request)
    {
        request.Conditions.AddSort((SysStorageConfig config) => config.IsDefault, SortDirection.Descending, 0);
        request.Conditions.AddSort((SysStorageConfig config) => config.Sort, SortDirection.Ascending, 1);
        request.Conditions.AddSort((SysStorageConfig config) => config.CreatedTime, SortDirection.Descending, 2);
    }

    /// <summary>
    /// 校验分页参数
    /// </summary>
    /// <param name="input">查询参数</param>
    private static void ValidatePageInput(StorageConfigPageQueryDto input)
    {
        if (input.StorageType.HasValue && !Enum.IsDefined(input.StorageType.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(input), "存储类型枚举值无效。");
        }
    }
}
