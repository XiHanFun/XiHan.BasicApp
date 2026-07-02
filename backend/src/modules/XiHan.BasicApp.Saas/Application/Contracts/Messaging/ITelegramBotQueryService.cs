#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ITelegramBotQueryService
// Guid:b6de6f1c-80a8-4c37-9fd8-c559ecb4421c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 18:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// Telegram 机器人查询应用服务接口
/// </summary>
public interface ITelegramBotQueryService : IApplicationService
{
    /// <summary>
    /// 获取 Telegram 机器人分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Telegram 机器人分页列表</returns>
    Task<PageResultDtoBase<TelegramBotListItemDto>> GetTelegramBotPageAsync(TelegramBotPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取 Telegram 机器人详情
    /// </summary>
    /// <param name="id">Telegram 机器人主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Telegram 机器人详情</returns>
    Task<TelegramBotDetailDto?> GetTelegramBotDetailAsync(long id, CancellationToken cancellationToken = default);
}
