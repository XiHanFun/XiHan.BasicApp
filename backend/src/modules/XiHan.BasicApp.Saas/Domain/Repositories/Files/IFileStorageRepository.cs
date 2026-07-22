// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 文件存储仓储接口
/// </summary>
public interface IFileStorageRepository : ISaasRepository<SysFileStorage>
{
    /// <summary>
    /// 获取文件的全部存储副本
    /// </summary>
    Task<IReadOnlyList<SysFileStorage>> GetByFileIdAsync(long fileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取文件主存储
    /// </summary>
    Task<SysFileStorage?> GetPrimaryByFileIdAsync(long fileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据存储提供商获取列表
    /// </summary>
    Task<IReadOnlyList<SysFileStorage>> GetByProviderAsync(string provider, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清除文件的主存储标记
    /// </summary>
    Task<bool> ClearPrimaryAsync(long fileId, long? excludeStorageId = null, CancellationToken cancellationToken = default);
}
