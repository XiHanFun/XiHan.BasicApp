#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SmsConfigQueryService
// Guid:9c4f7e16-3a50-4d82-8b69-5e1a8f3d7c04
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 16:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
/// 短信配置查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "短信配置")]
public sealed class SmsConfigQueryService
    : SaasApplicationService, ISmsConfigQueryService
{
    /// <summary>
    /// 短信配置仓储
    /// </summary>
    private readonly ISmsConfigRepository _smsConfigRepository;

    /// <summary>
    /// 字段级安全（排序/过滤门控）
    /// </summary>
    private readonly IFieldSecurityService _fieldSecurity;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SmsConfigQueryService(
        ISmsConfigRepository smsConfigRepository,
        IFieldSecurityService fieldSecurityService)
    {
        _smsConfigRepository = smsConfigRepository;
        _fieldSecurity = fieldSecurityService;
    }

    /// <summary>
    /// 获取短信配置分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>短信配置分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.SmsConfig.Read)]
    [HttpPost]
    public async Task<PageResultDtoBase<SmsConfigListItemDto>> GetSmsConfigPageAsync(SmsConfigPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidatePageInput(input);

        var request = BuildSmsConfigPageRequest(input);

        // 排序：前端选择优先，FLS 门控剔除不可读/已脱敏字段；无有效排序回退默认排序
        await _fieldSecurity.GuardSortsAsync(request.Conditions, "SysSmsConfig", cancellationToken);
        // 过滤：FLS 门控剔除不可读/已脱敏字段（枚举多选 In）
        await _fieldSecurity.GuardFiltersAsync(request.Conditions, "SysSmsConfig", cancellationToken);
        if (request.Conditions.Sorts.Count == 0)
        {
            ApplySmsConfigSorts(request);
        }

        var configPage = await _smsConfigRepository.GetPagedAsync(request, cancellationToken);
        var items = configPage.Items
            .Select(SmsConfigApplicationMapper.ToListItemDto)
            .ToList();

        return new PageResultDtoBase<SmsConfigListItemDto>(items, configPage.Page);
    }

    /// <summary>
    /// 获取短信配置详情
    /// </summary>
    /// <param name="id">短信配置主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>短信配置详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.SmsConfig.Read)]
    public async Task<SmsConfigDetailDto?> GetSmsConfigDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "短信配置主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var config = await _smsConfigRepository.GetByIdAsync(id, cancellationToken);
        return config is null ? null : SmsConfigApplicationMapper.ToDetailDto(config);
    }

    /// <summary>
    /// 构建短信配置分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>短信配置分页请求</returns>
    private static BasicAppPRDto BuildSmsConfigPageRequest(SmsConfigPageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Conditions = new QueryConditions()
        };

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword<SysSmsConfig>(
                input.Keyword.Trim(),
                config => config.ConfigCode,
                config => config.ConfigName,
                config => config.SignName,
                config => config.Remark);
        }

        if (input.Provider.HasValue)
        {
            request.Conditions.AddFilter((SysSmsConfig config) => config.Provider, input.Provider.Value);
        }

        if (input.IsDefault.HasValue)
        {
            request.Conditions.AddFilter((SysSmsConfig config) => config.IsDefault, input.IsDefault.Value);
        }

        if (input.IsEnabled.HasValue)
        {
            request.Conditions.AddFilter((SysSmsConfig config) => config.IsEnabled, input.IsEnabled.Value);
        }

        // 前端选择的排序原样带入（FLS 门控与默认兜底在调用方 GetSmsConfigPageAsync 处理）
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
    /// 施加短信配置默认排序（无前端排序时的兜底）
    /// </summary>
    /// <param name="request">分页请求</param>
    private static void ApplySmsConfigSorts(BasicAppPRDto request)
    {
        request.Conditions.AddSort((SysSmsConfig config) => config.IsDefault, SortDirection.Descending, 0);
        request.Conditions.AddSort((SysSmsConfig config) => config.Sort, SortDirection.Ascending, 1);
        request.Conditions.AddSort((SysSmsConfig config) => config.CreatedTime, SortDirection.Descending, 2);
    }

    /// <summary>
    /// 校验分页参数
    /// </summary>
    /// <param name="input">查询参数</param>
    private static void ValidatePageInput(SmsConfigPageQueryDto input)
    {
        if (input.Provider.HasValue && !Enum.IsDefined(input.Provider.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(input), "短信服务商枚举值无效。");
        }
    }
}
