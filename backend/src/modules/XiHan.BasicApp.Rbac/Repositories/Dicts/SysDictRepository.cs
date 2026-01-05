#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysDictRepository
// Guid:b8b2c3d4-e5f6-7890-abcd-ef1234567897
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Repositories.Dicts;

/// <summary>
/// 系统字典仓储实现
/// </summary>
public class SysDictRepository : SqlSugarRepositoryBase<SysDict, long>, ISysDictRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public SysDictRepository(ISqlSugarDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据字典编码获取字典
    /// </summary>
    public async Task<SysDict?> GetByCodeAsync(string dictCode)
    {
        return await GetFirstAsync(dict => dict.DictCode == dictCode);
    }

    /// <summary>
    /// 根据字典类型获取字典列表
    /// </summary>
    public async Task<List<SysDict>> GetByTypeAsync(string dictType)
    {
        var result = await GetListAsync(dict => dict.DictType == dictType);
        return [.. result.OrderBy(dict => dict.Sort)];
    }

    /// <summary>
    /// 检查字典编码是否存在
    /// </summary>
    public async Task<bool> ExistsByCodeAsync(string dictCode, long? excludeId = null)
    {
        var query = _dbContext.GetClient().Queryable<SysDict>().Where(dict => dict.DictCode == dictCode);
        if (excludeId.HasValue)
        {
            query = query.Where(dict => dict.BasicId != excludeId.Value);
        }
        return await query.AnyAsync();
    }
}
