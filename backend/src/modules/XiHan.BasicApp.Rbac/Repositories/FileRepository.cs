#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FileRepository
// Guid:c9d0e1f2-a3b4-4c5d-5e6f-8a9b0c1d2e3f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/8 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Repositories;

/// <summary>
/// 文件仓储实现
/// </summary>
public class FileRepository : SqlSugarAggregateRepository<SysFile, long>, IFileRepository
{
    private readonly ISqlSugarClient _dbClient;

    /// <summary>
    /// 构造函数
    /// </summary>
    public FileRepository(ISqlSugarDbContext dbContext, IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, unitOfWorkManager)
    {
        _dbClient = dbContext.GetClient();
    }

    /// <summary>
    /// 根据文件哈希查询文件（用于文件去重）
    /// </summary>
    public async Task<SysFile?> GetByFileHashAsync(string fileHash, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysFile>()
            .FirstAsync(f => f.FileHash == fileHash, cancellationToken);
    }

    /// <summary>
    /// 根据存储路径查询文件
    /// </summary>
    public async Task<SysFile?> GetByStoragePathAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysFile>()
            .InnerJoin<SysFileStorage>((f, s) => f.BasicId == s.FileId)
            .Where((f, s) => s.StoragePath == storagePath)
            .Select((f, s) => f)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 检查文件哈希是否存在
    /// </summary>
    public async Task<bool> ExistsByFileHashAsync(string fileHash, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysFile>()
            .Where(f => f.FileHash == fileHash)
            .AnyAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户上传的文件列表
    /// </summary>
    public async Task<List<SysFile>> GetByUploadUserIdAsync(long uploadUserId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysFile>()
            .Where(f => f.CreatedId == uploadUserId)
            .OrderBy(f => f.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据文件类型获取文件列表
    /// </summary>
    public async Task<List<SysFile>> GetByFileTypeAsync(FileType fileType, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysFile>()
            .Where(f => f.FileType == fileType)
            .OrderBy(f => f.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取指定时间段内的文件列表
    /// </summary>
    public async Task<List<SysFile>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysFile>()
            .Where(f => f.CreatedTime >= startTime && f.CreatedTime <= endTime)
            .OrderBy(f => f.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取文件总大小（字节）
    /// </summary>
    public async Task<long> GetTotalFileSizeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysFile>()
            .SumAsync(f => f.FileSize);
    }
}
