// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 机器人配置领域服务（Webhook 型：钉钉/飞书/企业微信）
/// </summary>
public interface IBotConfigDomainService
{
    /// <summary>
    /// 创建机器人配置
    /// </summary>
    Task<BotConfigCommandResult> CreateBotConfigAsync(BotConfigCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新机器人配置
    /// </summary>
    Task<BotConfigCommandResult> UpdateBotConfigAsync(BotConfigUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新机器人配置启用状态
    /// </summary>
    Task<BotConfigCommandResult> UpdateBotConfigStatusAsync(BotConfigStatusChangeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置默认机器人配置（同租户同服务商内互斥）
    /// </summary>
    Task<BotConfigCommandResult> SetDefaultBotConfigAsync(BotConfigDefaultChangeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除机器人配置
    /// </summary>
    Task<BotConfigCommandResult> DeleteBotConfigAsync(long id, CancellationToken cancellationToken = default);
}
