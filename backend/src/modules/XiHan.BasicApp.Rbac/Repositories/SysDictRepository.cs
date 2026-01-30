#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysDictRepository
// Guid:a1b2c3d4-e5f6-7890-1234-567890a56789
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
/// 字典仓储实现
/// </summary>
public class SysDictRepository : SqlSugarAggregateRepository<SysDict, long>, ISysDictRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysDictRepository(ISqlSugarDbContext dbContext, IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, unitOfWorkManager)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据字典编码获取字典
    /// </summary>
    public async Task<SysDict?> GetByDictCodeAsync(string dictCode, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysDict>()
            .Where(d => d.DictCode == dictCode && d.Status == YesOrNo.Yes)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 获取字典及字典项
    /// </summary>
    public async Task<SysDict?> GetWithItemsAsync(long dictId, CancellationToken cancellationToken = default)
    {
        return await GetByIdAsync(dictId, cancellationToken);
    }

    /// <summary>
    /// 根据字典编码获取字典项列表
    /// </summary>
    public async Task<List<SysDictItem>> GetDictItemsByCodeAsync(string dictCode, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysDictItem>()
            .InnerJoin<SysDict>((di, d) => di.DictId == d.BasicId)
            .Where((di, d) => d.DictCode == dictCode && d.Status == YesOrNo.Yes && di.Status == YesOrNo.Yes)
            .Select((di, d) => di)
            .OrderBy(di => di.Sort)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据字典ID获取字典项列表
    /// </summary>
    public async Task<List<SysDictItem>> GetDictItemsAsync(long dictId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysDictItem>()
            .Where(di => di.DictId == dictId && di.Status == YesOrNo.Yes)
            .OrderBy(di => di.Sort)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 添加字典项
    /// </summary>
    public async Task<SysDictItem> AddDictItemAsync(SysDictItem dictItem, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Insertable(dictItem).ExecuteReturnEntityAsync();
    }

    /// <summary>
    /// 更新字典项
    /// </summary>
    public async Task UpdateDictItemAsync(SysDictItem dictItem, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Updateable(dictItem).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 删除字典项
    /// </summary>
    public async Task DeleteDictItemAsync(long dictItemId, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Deleteable<SysDictItem>()
            .Where(di => di.BasicId == dictItemId)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 检查字典编码是否存在
    /// </summary>
    public async Task<bool> ExistsByDictCodeAsync(string dictCode, long? excludeDictId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.GetClient().Queryable<SysDict>()
            .Where(d => d.DictCode == dictCode);

        if (excludeDictId.HasValue)
        {
            query = query.Where(d => d.BasicId != excludeDictId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
