#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IBotConfigAppService
// Guid:eeff51fb-d45e-4d9c-ba8b-a1e1c61c3210
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 18:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 机器人配置命令应用服务接口（Webhook 型：钉钉/飞书/企业微信）
/// </summary>
public interface IBotConfigAppService : IApplicationService
{
    /// <summary>
    /// 创建机器人配置
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>机器人配置详情</returns>
    Task<BotConfigDetailDto> CreateBotConfigAsync(BotConfigCreateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新机器人配置
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>机器人配置详情</returns>
    Task<BotConfigDetailDto> UpdateBotConfigAsync(BotConfigUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新机器人配置启停状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>机器人配置详情</returns>
    Task<BotConfigDetailDto> UpdateBotConfigStatusAsync(BotConfigStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置默认机器人配置（同租户同服务商内互斥）
    /// </summary>
    /// <param name="input">默认更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>机器人配置详情</returns>
    Task<BotConfigDetailDto> SetDefaultBotConfigAsync(BotConfigDefaultUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除机器人配置
    /// </summary>
    /// <param name="id">机器人配置主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteBotConfigAsync(long id, CancellationToken cancellationToken = default);
}
