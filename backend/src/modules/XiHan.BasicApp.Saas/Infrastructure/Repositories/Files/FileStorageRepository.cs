#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FileStorageRepository
// Guid:1c728ca0-061a-4812-832c-17e3878f53d6
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/29 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 文件存储仓储实现
/// </summary>
public sealed class FileStorageRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysFileStorage>(clientResolver), IFileStorageRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SysFileStorage>> GetByFileIdAsync(long fileId, CancellationToken cancellationToken = default)
    {
        if (fileId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fileId), "系统文件主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(storage => storage.FileId == fileId)
            .OrderByDescending(storage => storage.IsPrimary)
            .OrderBy(storage => storage.Sort)
            .OrderBy(storage => storage.StorageType)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SysFileStorage?> GetPrimaryByFileIdAsync(long fileId, CancellationToken cancellationToken = default)
    {
        if (fileId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fileId), "系统文件主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(storage => storage.FileId == fileId && storage.IsPrimary)
            .OrderBy(storage => storage.Sort)
            .FirstAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SysFileStorage>> GetByProviderAsync(string provider, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(storage => storage.StorageProvider == provider)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> ClearPrimaryAsync(long fileId, long? excludeStorageId = null, CancellationToken cancellationToken = default)
    {
        if (fileId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fileId), "系统文件主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        return excludeStorageId.HasValue
            ? UpdateAsync(
                storage => new SysFileStorage { IsPrimary = false },
                storage => storage.FileId == fileId && storage.BasicId != excludeStorageId.Value && storage.IsPrimary,
                cancellationToken)
            : UpdateAsync(
                storage => new SysFileStorage { IsPrimary = false },
                storage => storage.FileId == fileId && storage.IsPrimary,
                cancellationToken);
    }
}
