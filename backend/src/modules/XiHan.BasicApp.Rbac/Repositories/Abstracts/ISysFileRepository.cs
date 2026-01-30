#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysFileRepository
// Guid:b4c5d6e7-f8a9-0123-4567-890123b89012
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/30 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// 文件仓储接口
/// </summary>
/// <remarks>
/// 聚合范围：SysFile + SysFileStorage
/// </remarks>
public interface ISysFileRepository : IAggregateRootRepository<SysFile, long>
{
    /// <summary>
    /// 根据文件哈希获取文件
    /// </summary>
    Task<SysFile?> GetByFileHashAsync(string fileHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户文件列表
    /// </summary>
    Task<List<SysFile>> GetByUserIdAsync(long userId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取文件及存储信息
    /// </summary>
    Task<SysFile?> GetWithStorageAsync(long fileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量获取文件
    /// </summary>
    Task<List<SysFile>> GetByIdsAsync(IEnumerable<long> fileIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加文件存储信息
    /// </summary>
    Task<SysFileStorage> AddFileStorageAsync(SysFileStorage storage, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取文件存储信息
    /// </summary>
    Task<SysFileStorage?> GetFileStorageAsync(long fileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 统计用户存储空间使用量
    /// </summary>
    Task<long> GetUserStorageUsageAsync(long userId, CancellationToken cancellationToken = default);
}
