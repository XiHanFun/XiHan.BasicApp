// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 机器人配置查询应用服务接口（Webhook 型：钉钉/飞书/企业微信）
/// </summary>
public interface IBotConfigQueryService : IApplicationService
{
    /// <summary>
    /// 获取机器人配置分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>机器人配置分页列表</returns>
    Task<PageResultDtoBase<BotConfigListItemDto>> GetBotConfigPageAsync(BotConfigPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取机器人配置详情
    /// </summary>
    /// <param name="id">机器人配置主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>机器人配置详情</returns>
    Task<BotConfigDetailDto?> GetBotConfigDetailAsync(long id, CancellationToken cancellationToken = default);
}
