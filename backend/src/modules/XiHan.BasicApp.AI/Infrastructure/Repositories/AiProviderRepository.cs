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
    /// <inheritdoc />
    public async Task<SysAiProvider?> GetByCodeAsync(string configCode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configCode);
        cancellationToken.ThrowIfCancellationRequested();

        var code = configCode.Trim();
        return await CreateQueryable()
            .Where(provider => provider.ConfigCode == code)
            .FirstAsync(cancellationToken);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task<SysAiProvider?> GetEnabledByCodeAsync(string configCode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configCode);
        cancellationToken.ThrowIfCancellationRequested();

        var code = configCode.Trim();
        return await CreateQueryable()
            .Where(provider => provider.ConfigCode == code && provider.IsEnabled && provider.Status == EnableStatus.Enabled)
            .FirstAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SysAiProvider?> GetDefaultAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(provider => provider.IsDefault && provider.IsEnabled && provider.Status == EnableStatus.Enabled)
            .OrderBy(provider => provider.Sort)
            .FirstAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SysAiProvider>> GetEnabledListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(provider => provider.IsEnabled && provider.Status == EnableStatus.Enabled)
            .OrderBy(provider => provider.Sort)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SysAiProvider>> GetOtherDefaultsAsync(long keepId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(provider => provider.IsDefault && provider.BasicId != keepId)
            .ToListAsync(cancellationToken);
    }
}
