#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ITelegramBotRepository
// Guid:e53c61f0-505f-4981-9f2b-dc3fedeb28f2
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 18:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// Telegram 机器人仓储接口
/// </summary>
public interface ITelegramBotRepository : ISaasRepository<SysTelegramBot>
{
    /// <summary>
    /// 按机器人名称查询（租户内唯一）
    /// </summary>
    Task<SysTelegramBot?> GetByNameAsync(string botName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取全部启用的机器人列表
    /// </summary>
    Task<List<SysTelegramBot>> GetEnabledListAsync(CancellationToken cancellationToken = default);
}
