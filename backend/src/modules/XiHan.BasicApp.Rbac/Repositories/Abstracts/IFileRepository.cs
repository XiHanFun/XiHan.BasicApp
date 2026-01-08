#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IFileRepository
// Guid:c9d0e1f2-a3b4-4c5d-5e6f-8a9b0c1d2e3f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// 文件仓储接口
/// </summary>
public interface IFileRepository : IAggregateRootRepository<SysFile, long>
{
    /// <summary>
    /// 根据文件哈希查询文件（用于文件去重）
    /// </summary>
    /// <param name="fileHash">文件哈希</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件实体</returns>
    Task<SysFile?> GetByFileHashAsync(string fileHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据存储路径查询文件
    /// </summary>
    /// <param name="storagePath">存储路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件实体</returns>
    Task<SysFile?> GetByStoragePathAsync(string storagePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查文件哈希是否存在
    /// </summary>
    /// <param name="fileHash">文件哈希</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    Task<bool> ExistsByFileHashAsync(string fileHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户上传的文件列表
    /// </summary>
    /// <param name="uploadUserId">上传用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件列表</returns>
    Task<List<SysFile>> GetByUploadUserIdAsync(long uploadUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据文件类型获取文件列表
    /// </summary>
    /// <param name="fileType">文件类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件列表</returns>
    Task<List<SysFile>> GetByFileTypeAsync(FileType fileType, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定时间段内的文件列表
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件列表</returns>
    Task<List<SysFile>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取文件总大小（字节）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件总大小</returns>
    Task<long> GetTotalFileSizeAsync(CancellationToken cancellationToken = default);
}
