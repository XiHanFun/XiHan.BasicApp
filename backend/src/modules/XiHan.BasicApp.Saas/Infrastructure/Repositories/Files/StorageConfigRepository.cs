// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 存储配置仓储实现
/// </summary>
public sealed class StorageConfigRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysStorageConfig>(clientResolver), IStorageConfigRepository
{
    /// <summary>
    /// 按配置编码查询（租户内唯一）
    /// </summary>
    public async Task<SysStorageConfig?> GetByCodeAsync(string configCode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configCode);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(config => config.ConfigCode == configCode)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 获取默认且启用的存储配置
    /// </summary>
    public async Task<SysStorageConfig?> GetDefaultAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(config => config.IsDefault && config.IsEnabled)
            .FirstAsync(cancellationToken);
    }
}
