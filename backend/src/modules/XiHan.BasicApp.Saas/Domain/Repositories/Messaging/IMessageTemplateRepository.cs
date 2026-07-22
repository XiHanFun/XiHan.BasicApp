// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 消息模板仓储接口
/// </summary>
public interface IMessageTemplateRepository : ISaasRepository<SysMessageTemplate>
{
    /// <summary>
    /// 按 渠道+编码 查找已启用模板（当前租户模板优先，回退平台全局模板）
    /// </summary>
    /// <param name="channel">消息渠道</param>
    /// <param name="templateCode">模板编码</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<SysMessageTemplate?> FindEnabledByCodeAsync(MessageChannel channel, string templateCode, CancellationToken cancellationToken = default);
}
