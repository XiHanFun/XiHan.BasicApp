// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.AI.Domain.Repositories;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Infrastructure.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.AI.Infrastructure.Repositories;

/// <summary>
/// AI Provider 仓储实现
/// </summary>
public sealed class AiProviderRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysAiProvider>(clientResolver), IAiProviderRepository
{
    /// <summary>
    /// 按配置编码获取（任意状态，用于详情/唯一性）
    /// </summary>
    public async Task<SysAiProvider?> GetByCodeAsync(string configCode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configCode);
        cancellationToken.ThrowIfCancellationRequested();

        var code = configCode.Trim();
        return await CreateQueryable()
            .Where(provider => provider.ConfigCode == code)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 检查配置编码是否存在
    /// </summary>
    public async Task<bool> ExistsCodeAsync(string configCode, long? excludeId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configCode);
        cancellationToken.ThrowIfCancellationRequested();

        var code = configCode.Trim();
        var query = CreateQueryable().Where(provider => provider.ConfigCode == code);
        if (excludeId.HasValue)
        {
            query = query.Where(provider => provider.BasicId != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// 按配置编码获取启用记录（配置源运行路径）
    /// </summary>
    public async Task<SysAiProvider?> GetEnabledByCodeAsync(string configCode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configCode);
        cancellationToken.ThrowIfCancellationRequested();

        var code = configCode.Trim();
        return await CreateQueryable()
            .Where(provider => provider.ConfigCode == code && provider.IsEnabled && provider.Status == EnableStatus.Enabled)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 获取默认且启用的 provider（配置源默认解析）
    /// </summary>
    public async Task<SysAiProvider?> GetDefaultAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(provider => provider.IsDefault && provider.IsEnabled && provider.Status == EnableStatus.Enabled)
            .OrderBy(provider => provider.Sort)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 获取全部启用 provider（配置源枚举）
    /// </summary>
    public async Task<IReadOnlyList<SysAiProvider>> GetEnabledListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(provider => provider.IsEnabled && provider.Status == EnableStatus.Enabled)
            .OrderBy(provider => provider.Sort)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取其它标记为默认的 provider（用于单默认互斥清理）
    /// </summary>
    public async Task<IReadOnlyList<SysAiProvider>> GetOtherDefaultsAsync(long keepId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(provider => provider.IsDefault && provider.BasicId != keepId)
            .ToListAsync(cancellationToken);
    }
}
