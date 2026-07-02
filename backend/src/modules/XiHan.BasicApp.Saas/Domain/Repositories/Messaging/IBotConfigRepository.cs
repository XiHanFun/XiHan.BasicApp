#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IBotConfigRepository
// Guid:6daea692-c966-4fa1-80e5-83ae2a601092
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 18:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 机器人配置仓储接口（Webhook 型：钉钉/飞书/企业微信）
/// </summary>
public interface IBotConfigRepository : ISaasRepository<SysBotConfig>
{
    /// <summary>
    /// 按配置编码查询（租户内唯一）
    /// </summary>
    Task<SysBotConfig?> GetByCodeAsync(string configCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定服务商默认且启用的机器人配置
    /// </summary>
    Task<SysBotConfig?> GetDefaultAsync(BotProviderType provider, CancellationToken cancellationToken = default);
}
