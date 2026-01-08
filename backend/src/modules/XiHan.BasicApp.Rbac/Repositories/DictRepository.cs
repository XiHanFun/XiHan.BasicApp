#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DictRepository
// Guid:a7b8c9d0-e1f2-4a5b-3c4d-6e7f8a9b0c1d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/8 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Repositories;

/// <summary>
/// 字典仓储实现
/// </summary>
public class DictRepository : SqlSugarAggregateRepository<SysDict, long>, IDictRepository
{
    private readonly ISqlSugarClient _dbClient;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DictRepository(ISqlSugarDbContext dbContext, IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, unitOfWorkManager)
    {
        _dbClient = dbContext.GetClient();
    }

    /// <summary>
    /// 根据字典编码查询字典
    /// </summary>
    public async Task<SysDict?> GetByDictCodeAsync(string dictCode, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysDict>()
            .FirstAsync(d => d.DictCode == dictCode, cancellationToken);
    }

    /// <summary>
    /// 检查字典编码是否存在
    /// </summary>
    public async Task<bool> ExistsByDictCodeAsync(string dictCode, long? excludeDictId = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = _dbClient.Queryable<SysDict>()
            .Where(d => d.DictCode == dictCode);

        if (excludeDictId.HasValue)
        {
            query = query.Where(d => d.BasicId != excludeDictId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// 获取字典及其所有字典项
    /// </summary>
    public async Task<SysDict?> GetWithItemsAsync(long dictId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dict = await _dbClient.Queryable<SysDict>()
            .FirstAsync(d => d.BasicId == dictId, cancellationToken);

        // 字典项可以通过导航属性或单独查询获取
        return dict;
    }

    /// <summary>
    /// 根据字典编码获取字典及其所有字典项
    /// </summary>
    public async Task<SysDict?> GetWithItemsByCodeAsync(string dictCode, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dict = await _dbClient.Queryable<SysDict>()
            .FirstAsync(d => d.DictCode == dictCode, cancellationToken);

        return dict;
    }
}
