#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IFileRecordQueryService
// Guid:615a6f41-d999-48d5-ae90-19299510ee04
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
