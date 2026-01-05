#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysDictItemRepository
// Guid:b9b2c3d4-e5f6-7890-abcd-ef1234567898
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Repositories.DictItems;

/// <summary>
/// 系统字典项仓储实现
/// </summary>
public class SysDictItemRepository : SqlSugarRepositoryBase<SysDictItem, long>, ISysDictItemRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public SysDictItemRepository(ISqlSugarDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据字典ID获取字典项列表
    /// </summary>
    public async Task<List<SysDictItem>> GetByDictIdAsync(long dictId)
    {
        var result = await GetListAsync(item => item.DictId == dictId);
        return [.. result.OrderBy(item => item.Sort)];
    }

    /// <summary>
    /// 根据字典编码获取字典项列表
    /// </summary>
    public async Task<List<SysDictItem>> GetByDictCodeAsync(string dictCode)
    {
        var result = await GetListAsync(item => item.DictCode == dictCode);
        return [.. result.OrderBy(item => item.Sort)];
    }

    /// <summary>
    /// 根据字典编码和字典项编码获取字典项
    /// </summary>
    public async Task<SysDictItem?> GetByCodeAsync(string dictCode, string itemCode)
    {
        return await GetFirstAsync(item => item.DictCode == dictCode && item.ItemCode == itemCode);
    }

    /// <summary>
    /// 根据父级ID获取子项列表
    /// </summary>
    public async Task<List<SysDictItem>> GetByParentIdAsync(long parentId)
    {
        var result = await GetListAsync(item => item.ParentId == parentId);
        return [.. result.OrderBy(item => item.Sort)];
    }
}
