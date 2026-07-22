// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 文件记录查询服务
/// </summary>
public interface IFileRecordQueryService
{
    /// <summary>
    /// 根据主键获取文件
    /// </summary>
    Task<SysFile> GetFileOrThrowAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据哈希获取正常文件
    /// </summary>
    Task<SysFile?> GetNormalFileByHashAsync(string fileHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取文件全部存储副本
    /// </summary>
    Task<IReadOnlyList<SysFileStorage>> GetStoragesByFileIdAsync(long fileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取主存储
    /// </summary>
    Task<SysFileStorage> GetPrimaryStorageOrThrowAsync(long fileId, string errorMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取存储副本
    /// </summary>
    Task<SysFileStorage> GetStorageOrThrowAsync(long id, CancellationToken cancellationToken = default);
}
