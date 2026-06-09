#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IFileStorageRepository
// Guid:c9aaef7a-3771-45cf-af1a-88f4a1bb23c8
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
