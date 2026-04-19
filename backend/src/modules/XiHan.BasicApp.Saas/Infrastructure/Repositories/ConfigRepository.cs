#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ConfigRepository
// Guid:e7ad7ee2-0689-40b2-b009-c4c60f849f35
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:54:09
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Repository;

using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 配置仓储实现
/// </summary>
public class ConfigRepository : SqlSugarAggregateRepository<SysConfig, long>, IConfigRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="clientResolver"></param>
    /// <param name="unitOfWorkManager"></param>
    public ConfigRepository(
        ISqlSugarClientResolver clientResolver,
        IUnitOfWorkManager unitOfWorkManager)
        : base(clientResolver, unitOfWorkManager)
    {
    }

    /// <summary>
    /// 根据配置键获取配置
    /// </summary>
    /// <param name="configKey"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SysConfig?> GetByConfigKeyAsync(string configKey, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configKey);
        var query = CreateQueryable()
            .Where(config => config.ConfigKey == configKey);

        query = tenantId.HasValue ? query.Where(config => config.TenantId == tenantId.Value) : query.Where(config => config.TenantId == 0);

        return await query.FirstAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SysConfig>> GetByGroupAsync(string? configGroup, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = CreateQueryable();

        query = tenantId.HasValue ? query.Where(config => config.TenantId == tenantId.Value) : query.Where(config => config.TenantId == 0);

        query = string.IsNullOrWhiteSpace(configGroup)
            ? query.Where(config => config.ConfigGroup == null || config.ConfigGroup == string.Empty)
            : query.Where(config => config.ConfigGroup == configGroup);

        var list = await query.ToListAsync(cancellationToken);
        return list;
    }
}
