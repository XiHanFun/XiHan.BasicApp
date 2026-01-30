#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysFileRepository
// Guid:b2c3d4e5-f6a7-8901-2345-678901b67890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/30 0:00:00
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
public class SysFileRepository : SqlSugarAggregateRepository<SysFile, long>, ISysFileRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysFileRepository(ISqlSugarDbContext dbContext, IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, unitOfWorkManager)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据文件哈希获取文件
    /// </summary>
    public async Task<SysFile?> GetByFileHashAsync(string fileHash, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysFile>()
            .Where(f => f.FileHash == fileHash && f.Status == FileStatus.Normal)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户文件列表
    /// </summary>
    public async Task<List<SysFile>> GetByUserIdAsync(long userId, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysFile>()
            .Where(f => f.CreatedId == userId && f.Status == FileStatus.Normal)
            .OrderByDescending(f => f.CreatedTime)
            .ToPageListAsync(pageIndex, pageSize, cancellationToken);
    }

    /// <summary>
    /// 获取文件及存储信息
    /// </summary>
    public async Task<SysFile?> GetWithStorageAsync(long fileId, CancellationToken cancellationToken = default)
    {
        return await GetByIdAsync(fileId, cancellationToken);
    }

    /// <summary>
    /// 批量获取文件
    /// </summary>
    public async Task<List<SysFile>> GetByIdsAsync(IEnumerable<long> fileIds, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysFile>()
            .In(f => f.BasicId, fileIds.ToArray())
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 添加文件存储信息
    /// </summary>
    public async Task<SysFileStorage> AddFileStorageAsync(SysFileStorage storage, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Insertable(storage).ExecuteReturnEntityAsync();
    }

    /// <summary>
    /// 获取文件存储信息
    /// </summary>
    public async Task<SysFileStorage?> GetFileStorageAsync(long fileId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysFileStorage>()
            .Where(fs => fs.FileId == fileId && fs.Status == StorageStatus.Normal)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 统计用户存储空间使用量
    /// </summary>
    public async Task<long> GetUserStorageUsageAsync(long userId, CancellationToken cancellationToken = default)
    {
        var result = await _dbContext.GetClient().Queryable<SysFile>()
            .Where(f => f.CreatedId == userId && f.Status == FileStatus.Normal)
            .SumAsync(f => f.FileSize);

        return result;
    }
}
