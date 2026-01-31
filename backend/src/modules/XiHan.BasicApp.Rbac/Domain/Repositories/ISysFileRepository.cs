#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysFileRepository
// Guid:a3b4c5d6-e7f8-9abc-def1-234567890123
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/31 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 系统文件仓储接口
/// </summary>
public interface ISysFileRepository : IAggregateRootRepository<SysFile, long>
{
    /// <summary>
    /// 根据文件哈希获取文件
    /// </summary>
    /// <param name="fileHash">文件哈希</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件实体</returns>
    Task<SysFile?> GetByFileHashAsync(string fileHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据文件名获取文件
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件实体</returns>
    Task<SysFile?> GetByFileNameAsync(string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取租户下的文件列表
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件列表</returns>
    Task<List<SysFile>> GetFilesByTenantAsync(long tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取过期的临时文件列表
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件列表</returns>
    Task<List<SysFile>> GetExpiredTemporaryFilesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存文件
    /// </summary>
    /// <param name="file">文件实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>保存的文件实体</returns>
    Task<SysFile> SaveAsync(SysFile file, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新文件访问统计
    /// </summary>
    /// <param name="fileId">文件ID</param>
    /// <param name="isDownload">是否为下载（false 为浏览）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UpdateAccessStatisticsAsync(long fileId, bool isDownload, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理过期的临时文件
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task CleanupExpiredFilesAsync(CancellationToken cancellationToken = default);
}
