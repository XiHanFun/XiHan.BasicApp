// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 短信网关配置仓储接口
/// </summary>
public interface ISmsConfigRepository : ISaasRepository<SysSmsConfig>
{
    /// <summary>
    /// 按配置编码查询（租户内唯一）
    /// </summary>
    Task<SysSmsConfig?> GetByCodeAsync(string configCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取默认且启用的短信网关配置
    /// </summary>
    Task<SysSmsConfig?> GetDefaultAsync(CancellationToken cancellationToken = default);
}
