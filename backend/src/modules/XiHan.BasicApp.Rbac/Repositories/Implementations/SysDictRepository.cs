#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysDictRepository
// Guid:b892f063-e137-47d4-8cca-4ff085eaf784
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/31 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Repositories.Implementations;

/// <summary>
/// 系统字典仓储实现
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
            .Where(d => d.DictCode == dictCode)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 检查字典编码是否存在
    /// </summary>
    public async Task<bool> IsDictCodeExistsAsync(string dictCode, long? excludeDictId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.GetClient().Queryable<SysDict>()
            .Where(d => d.DictCode == dictCode);

        if (excludeDictId.HasValue)
        {
            query = query.Where(d => d.BasicId != excludeDictId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// 根据字典类型获取字典列表
    /// </summary>
    public async Task<List<SysDict>> GetDictsByTypeAsync(string dictType, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysDict>()
            .Where(d => d.DictType == dictType)
            .Where(d => d.Status == YesOrNo.Yes)
            .OrderBy(d => d.Sort)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 保存字典
    /// </summary>
    public async Task<SysDict> SaveAsync(SysDict dict, CancellationToken cancellationToken = default)
    {
        if (dict.IsTransient())
        {
            return await AddAsync(dict, cancellationToken);
        }
        else
        {
            return await UpdateAsync(dict, cancellationToken);
        }
    }

    /// <summary>
    /// 启用字典
    /// </summary>
    public async Task EnableDictAsync(long dictId, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Updateable<SysDict>()
            .SetColumns(d => d.Status == YesOrNo.Yes)
            .Where(d => d.BasicId == dictId)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 禁用字典
    /// </summary>
    public async Task DisableDictAsync(long dictId, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Updateable<SysDict>()
            .SetColumns(d => d.Status == YesOrNo.No)
            .Where(d => d.BasicId == dictId)
            .ExecuteCommandAsync(cancellationToken);
    }
}
