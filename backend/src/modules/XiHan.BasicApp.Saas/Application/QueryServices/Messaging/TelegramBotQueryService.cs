#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TelegramBotQueryService
// Guid:3f010af0-2dca-40de-9894-f3ffecc34496
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 18:00:00
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
/// Telegram 机器人查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "Telegram机器人")]
public sealed class TelegramBotQueryService
    : SaasApplicationService, ITelegramBotQueryService
{
    /// <summary>
    /// Telegram 机器人仓储
    /// </summary>
    private readonly ITelegramBotRepository _telegramBotRepository;

    /// <summary>
    /// 字段级安全（排序/过滤门控）
    /// </summary>
    private readonly IFieldSecurityService _fieldSecurity;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TelegramBotQueryService(
        ITelegramBotRepository telegramBotRepository,
        IFieldSecurityService fieldSecurityService)
    {
        _telegramBotRepository = telegramBotRepository;
        _fieldSecurity = fieldSecurityService;
    }

    /// <summary>
    /// 获取 Telegram 机器人分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Telegram 机器人分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.TelegramBot.Read)]
    [HttpPost]
    public async Task<PageResultDtoBase<TelegramBotListItemDto>> GetTelegramBotPageAsync(TelegramBotPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildTelegramBotPageRequest(input);

        // 排序：前端选择优先，FLS 门控剔除不可读/已脱敏字段；无有效排序回退默认排序
        await _fieldSecurity.GuardSortsAsync(request.Conditions, "SysTelegramBot", cancellationToken);
        // 过滤：FLS 门控剔除不可读/已脱敏字段
        await _fieldSecurity.GuardFiltersAsync(request.Conditions, "SysTelegramBot", cancellationToken);
        if (request.Conditions.Sorts.Count == 0)
        {
            ApplyTelegramBotSorts(request);
        }

        var botPage = await _telegramBotRepository.GetPagedAsync(request, cancellationToken);
        var items = botPage.Items
            .Select(TelegramBotApplicationMapper.ToListItemDto)
            .ToList();

        return new PageResultDtoBase<TelegramBotListItemDto>(items, botPage.Page);
    }

    /// <summary>
    /// 获取 Telegram 机器人详情
    /// </summary>
    /// <param name="id">Telegram 机器人主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Telegram 机器人详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.TelegramBot.Read)]
    public async Task<TelegramBotDetailDto?> GetTelegramBotDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Telegram 机器人主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var bot = await _telegramBotRepository.GetByIdAsync(id, cancellationToken);
        return bot is null ? null : TelegramBotApplicationMapper.ToDetailDto(bot);
    }

    /// <summary>
    /// 构建 Telegram 机器人分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>Telegram 机器人分页请求</returns>
    private static BasicAppPRDto BuildTelegramBotPageRequest(TelegramBotPageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Behavior = input.Behavior,
            Conditions = new QueryConditions()
        };

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword<SysTelegramBot>(
                input.Keyword.Trim(),
                bot => bot.BotName,
                bot => bot.Remark);
        }

        if (input.IsEnabled.HasValue)
        {
            request.Conditions.AddFilter((SysTelegramBot bot) => bot.IsEnabled, input.IsEnabled.Value);
        }

        // 前端选择的排序原样带入（FLS 门控与默认兜底在调用方 GetTelegramBotPageAsync 处理）
        if (input.Conditions?.Sorts is { Count: > 0 } sorts)
        {
            _ = request.Conditions.AddSorts(sorts);
        }

        // 前端选择的通用过滤原样带入（FLS 门控在调用方处理）
        if (input.Conditions?.Filters is { Count: > 0 } filters)
        {
            _ = request.Conditions.AddFilters(filters);
        }

        return request;
    }

    /// <summary>
    /// 施加 Telegram 机器人默认排序（无前端排序时的兜底）
    /// </summary>
    /// <param name="request">分页请求</param>
    private static void ApplyTelegramBotSorts(BasicAppPRDto request)
    {
        request.Conditions.AddSort((SysTelegramBot bot) => bot.Sort, SortDirection.Ascending, 0);
        request.Conditions.AddSort((SysTelegramBot bot) => bot.CreatedTime, SortDirection.Descending, 1);
    }
}
