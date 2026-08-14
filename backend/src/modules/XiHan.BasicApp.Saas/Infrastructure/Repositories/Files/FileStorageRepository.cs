// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
    /// <summary>
    /// 获取文件的全部存储副本
    /// </summary>
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

    /// <summary>
    /// 获取文件主存储
    /// </summary>
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

    /// <summary>
    /// 根据存储提供商获取列表
    /// </summary>
    public async Task<IReadOnlyList<SysFileStorage>> GetByProviderAsync(string provider, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(storage => storage.StorageProvider == provider)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 清除文件的主存储标记
    /// </summary>
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
