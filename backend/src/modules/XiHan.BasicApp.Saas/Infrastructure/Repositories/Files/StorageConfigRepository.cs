#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:StorageConfigRepository
// Guid:b2d8f4e6-7a19-4c53-8e0b-d9c5a3f17b28
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/12 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
    /// <inheritdoc />
    public async Task<SysStorageConfig?> GetByCodeAsync(string configCode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configCode);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(config => config.ConfigCode == configCode)
            .FirstAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SysStorageConfig?> GetDefaultAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(config => config.IsDefault && config.IsEnabled)
            .FirstAsync(cancellationToken);
    }
}
