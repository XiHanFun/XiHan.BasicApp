// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
/// 机器人配置查询应用服务（Webhook 型：钉钉/飞书/企业微信）
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "机器人配置")]
public sealed class BotConfigQueryService
    : SaasApplicationService, IBotConfigQueryService
{
    /// <summary>
    /// 机器人配置仓储
    /// </summary>
    private readonly IBotConfigRepository _botConfigRepository;

    /// <summary>
    /// 字段级安全（排序/过滤门控）
    /// </summary>
    private readonly IFieldSecurityService _fieldSecurity;

    /// <summary>
    /// 构造函数
    /// </summary>
    public BotConfigQueryService(
        IBotConfigRepository botConfigRepository,
        IFieldSecurityService fieldSecurityService)
    {
        _botConfigRepository = botConfigRepository;
        _fieldSecurity = fieldSecurityService;
    }

    /// <summary>
    /// 获取机器人配置分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>机器人配置分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.BotConfig.Read)]
    [HttpPost]
    public async Task<PageResultDtoBase<BotConfigListItemDto>> GetBotConfigPageAsync(BotConfigPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidatePageInput(input);

        var request = BuildBotConfigPageRequest(input);

        // 排序：前端选择优先，FLS 门控剔除不可读/已脱敏字段；无有效排序回退默认排序
        await _fieldSecurity.GuardSortsAsync(request.Conditions, "SysBotConfig", cancellationToken);
        // 过滤：FLS 门控剔除不可读/已脱敏字段（枚举多选 In）
        await _fieldSecurity.GuardFiltersAsync(request.Conditions, "SysBotConfig", cancellationToken);
        if (request.Conditions.Sorts.Count == 0)
        {
            ApplyBotConfigSorts(request);
        }

        var configPage = await _botConfigRepository.GetPagedAsync(request, cancellationToken);
        var items = configPage.Items
            .Select(BotConfigApplicationMapper.ToListItemDto)
            .ToList();

        return new PageResultDtoBase<BotConfigListItemDto>(items, configPage.Page);
    }

    /// <summary>
    /// 获取机器人配置详情
    /// </summary>
    /// <param name="id">机器人配置主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>机器人配置详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.BotConfig.Read)]
    public async Task<BotConfigDetailDto?> GetBotConfigDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "机器人配置主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var config = await _botConfigRepository.GetByIdAsync(id, cancellationToken);
        return config is null ? null : BotConfigApplicationMapper.ToDetailDto(config);
    }

    /// <summary>
    /// 构建机器人配置分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>机器人配置分页请求</returns>
    private static BasicAppPRDto BuildBotConfigPageRequest(BotConfigPageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Conditions = new QueryConditions()
        };

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword<SysBotConfig>(
                input.Keyword.Trim(),
                config => config.ConfigCode,
                config => config.ConfigName,
                config => config.Remark);
        }

        if (input.Provider.HasValue)
        {
            request.Conditions.AddFilter((SysBotConfig config) => config.Provider, input.Provider.Value);
        }

        if (input.IsDefault.HasValue)
        {
            request.Conditions.AddFilter((SysBotConfig config) => config.IsDefault, input.IsDefault.Value);
        }

        if (input.IsEnabled.HasValue)
        {
            request.Conditions.AddFilter((SysBotConfig config) => config.IsEnabled, input.IsEnabled.Value);
        }

        // 前端选择的排序原样带入（FLS 门控与默认兜底在调用方 GetBotConfigPageAsync 处理）
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
    /// 施加机器人配置默认排序（无前端排序时的兜底）
    /// </summary>
    /// <param name="request">分页请求</param>
    private static void ApplyBotConfigSorts(BasicAppPRDto request)
    {
        request.Conditions.AddSort((SysBotConfig config) => config.IsDefault, SortDirection.Descending, 0);
        request.Conditions.AddSort((SysBotConfig config) => config.Sort, SortDirection.Ascending, 1);
        request.Conditions.AddSort((SysBotConfig config) => config.CreatedTime, SortDirection.Descending, 2);
    }

    /// <summary>
    /// 校验分页参数
    /// </summary>
    /// <param name="input">查询参数</param>
    private static void ValidatePageInput(BotConfigPageQueryDto input)
    {
        if (input.Provider.HasValue && !Enum.IsDefined(input.Provider.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(input), "机器人服务商枚举值无效。");
        }
    }
}
