// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 配置仓储实现
/// </summary>
public sealed class ConfigRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysConfig>(clientResolver), IConfigRepository
{
    /// <summary>
    /// 根据配置键获取
    /// </summary>
    public async Task<SysConfig?> GetByKeyAsync(string configKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configKey);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(config => config.ConfigKey == configKey.Trim())
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 根据配置键获取当前租户有效配置，租户级配置优先，全局配置兜底。
    /// </summary>
    public async Task<SysConfig?> GetEffectiveByKeyAsync(string configKey, long? tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configKey);
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedKey = configKey.Trim();
        var configs = await CreateQueryable()
            .Where(config => config.ConfigKey == normalizedKey && config.Status == EnableStatus.Enabled)
            .ToListAsync(cancellationToken);
        if (configs.Count == 0)
        {
            return null;
        }

        // 当前作用域的配置优先，平台（0 号租户）的全局配置兜底；不存在「任取一条」的口径
        var scopeTenantId = tenantId ?? 0;
        return configs.FirstOrDefault(config => config.TenantId == scopeTenantId)
            ?? configs.FirstOrDefault(static config => config.TenantId == 0);
    }

    /// <summary>
    /// 检查配置键是否存在
    /// </summary>
    public async Task<bool> ExistsKeyAsync(string configKey, long? excludeId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configKey);
        cancellationToken.ThrowIfCancellationRequested();

        var query = CreateQueryable().Where(config => config.ConfigKey == configKey.Trim());
        if (excludeId.HasValue)
        {
            query = query.Where(config => config.BasicId != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
