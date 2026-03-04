#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DictRepository
// Guid:a5375509-02c2-43e3-9ee2-dea8bc59d2c3
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:54:29
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Data.SqlSugar.SplitTables;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Infrastructure.Repositories;

/// <summary>
/// 字典仓储实现
/// </summary>
public class DictRepository : SqlSugarAggregateRepository<SysDict, long>, IDictRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="splitTableExecutor"></param>
    /// <param name="serviceProvider"></param>
    /// <param name="unitOfWorkManager"></param>
    public DictRepository(
        ISqlSugarDbContext dbContext,
        ISqlSugarSplitTableExecutor splitTableExecutor,
        IServiceProvider serviceProvider,
        IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, splitTableExecutor, serviceProvider, unitOfWorkManager)
    {
    }

    /// <summary>
    /// 根据字典编码获取字典
    /// </summary>
    /// <param name="dictCode"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SysDict?> GetByDictCodeAsync(string dictCode, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dictCode);
        var query = CreateTenantQueryable()
            .Where(dict => dict.DictCode == dictCode);

        if (tenantId.HasValue)
        {
            query = query.Where(dict => dict.TenantId == tenantId.Value);
        }
        else
        {
            query = query.Where(dict => dict.TenantId == null);
        }

        return await query.FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 根据字典ID获取字典项
    /// </summary>
    /// <param name="dictId"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<SysDictItem>> GetDictItemsAsync(long dictId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = CreateTenantQueryable<SysDictItem>()
            .Where(item => item.DictId == dictId);

        if (tenantId.HasValue)
        {
            query = query.Where(item => item.TenantId == tenantId.Value);
        }

        return await query.OrderBy(item => item.Sort).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据字典项ID获取字典项
    /// </summary>
    /// <param name="dictItemId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SysDictItem?> GetDictItemByIdAsync(long dictItemId, CancellationToken cancellationToken = default)
    {
        return await CreateTenantQueryable<SysDictItem>()
            .Where(item => item.BasicId == dictItemId)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 根据字典ID和字典项编码获取字典项
    /// </summary>
    /// <param name="dictId"></param>
    /// <param name="itemCode"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SysDictItem?> GetDictItemByCodeAsync(long dictId, string itemCode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemCode);
        return await CreateTenantQueryable<SysDictItem>()
            .Where(item => item.DictId == dictId && item.ItemCode == itemCode)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 新增字典项
    /// </summary>
    /// <param name="dictItem"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SysDictItem> AddDictItemAsync(SysDictItem dictItem, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dictItem);
        cancellationToken.ThrowIfCancellationRequested();
        return await DbClient.Insertable(dictItem).ExecuteReturnEntityAsync();
    }

    /// <summary>
    /// 更新字典项
    /// </summary>
    /// <param name="dictItem"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SysDictItem> UpdateDictItemAsync(SysDictItem dictItem, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dictItem);
        cancellationToken.ThrowIfCancellationRequested();
        await DbClient.Updateable(dictItem).ExecuteCommandAsync(cancellationToken);
        return dictItem;
    }

    /// <summary>
    /// 删除字典项
    /// </summary>
    /// <param name="dictItem"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<bool> DeleteDictItemAsync(SysDictItem dictItem, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dictItem);
        cancellationToken.ThrowIfCancellationRequested();
        var affectedRows = await DbClient.Deleteable<SysDictItem>()
            .Where(item => item.BasicId == dictItem.BasicId)
            .ExecuteCommandAsync(cancellationToken);
        return affectedRows > 0;
    }
}
