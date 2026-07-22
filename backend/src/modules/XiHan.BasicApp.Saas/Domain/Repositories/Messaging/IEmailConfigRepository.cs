// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 邮件网关配置仓储接口
/// </summary>
public interface IEmailConfigRepository : ISaasRepository<SysEmailConfig>
{
    /// <summary>
    /// 按配置编码查询（租户内唯一）
    /// </summary>
    Task<SysEmailConfig?> GetByCodeAsync(string configCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取默认且启用的邮件网关配置
    /// </summary>
    Task<SysEmailConfig?> GetDefaultAsync(CancellationToken cancellationToken = default);
}
