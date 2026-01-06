#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysRoleHierarchyRepository
// Guid:6a2b3c4d-5e6f-7890-abcd-ef1234567805
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026-01-07 15:35:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Repositories.RoleHierarchies;

/// <summary>
/// 系统角色层次仓储实现
/// </summary>
public class SysRoleHierarchyRepository : SqlSugarRepositoryBase<SysRoleHierarchy, long>, ISysRoleHierarchyRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysRoleHierarchyRepository(ISqlSugarDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 获取角色的所有父角色层次
    /// </summary>
    public async Task<List<SysRoleHierarchy>> GetParentHierarchiesAsync(long childRoleId, bool includingIndirect = true)
    {
        var query = _dbContext.GetClient().Queryable<SysRoleHierarchy>()
            .Where(h => h.ChildRoleId == childRoleId && h.Status == YesOrNo.Yes);
        
        if (!includingIndirect)
        {
            query = query.Where(h => h.IsDirect == true);
        }

        return await query.ToListAsync();
    }

    /// <summary>
    /// 获取角色的所有子角色层次
    /// </summary>
    public async Task<List<SysRoleHierarchy>> GetChildHierarchiesAsync(long parentRoleId, bool includingIndirect = true)
    {
        var query = _dbContext.GetClient().Queryable<SysRoleHierarchy>()
            .Where(h => h.ParentRoleId == parentRoleId && h.Status == YesOrNo.Yes);
        
        if (!includingIndirect)
        {
            query = query.Where(h => h.IsDirect == true);
        }

        return await query.ToListAsync();
    }

    /// <summary>
    /// 获取直接父角色ID列表
    /// </summary>
    public async Task<List<long>> GetDirectParentRoleIdsAsync(long childRoleId)
    {
        return await _dbContext.GetClient().Queryable<SysRoleHierarchy>()
            .Where(h => h.ChildRoleId == childRoleId && h.IsDirect == true && h.Status == YesOrNo.Yes)
            .Select(h => h.ParentRoleId)
            .ToListAsync();
    }

    /// <summary>
    /// 获取直接子角色ID列表
    /// </summary>
    public async Task<List<long>> GetDirectChildRoleIdsAsync(long parentRoleId)
    {
        return await _dbContext.GetClient().Queryable<SysRoleHierarchy>()
            .Where(h => h.ParentRoleId == parentRoleId && h.IsDirect == true && h.Status == YesOrNo.Yes)
            .Select(h => h.ChildRoleId)
            .ToListAsync();
    }

    /// <summary>
    /// 获取所有父角色ID列表（包括间接父角色）
    /// </summary>
    public async Task<List<long>> GetAllParentRoleIdsAsync(long childRoleId)
    {
        return await _dbContext.GetClient().Queryable<SysRoleHierarchy>()
            .Where(h => h.ChildRoleId == childRoleId && h.Status == YesOrNo.Yes)
            .Select(h => h.ParentRoleId)
            .ToListAsync();
    }

    /// <summary>
    /// 获取所有子角色ID列表（包括间接子角色）
    /// </summary>
    public async Task<List<long>> GetAllChildRoleIdsAsync(long parentRoleId)
    {
        return await _dbContext.GetClient().Queryable<SysRoleHierarchy>()
            .Where(h => h.ParentRoleId == parentRoleId && h.Status == YesOrNo.Yes)
            .Select(h => h.ChildRoleId)
            .ToListAsync();
    }

    /// <summary>
    /// 检查角色层次关系是否存在
    /// </summary>
    public async Task<bool> ExistsHierarchyAsync(long parentRoleId, long childRoleId)
    {
        return await _dbContext.GetClient().Queryable<SysRoleHierarchy>()
            .Where(h => h.ParentRoleId == parentRoleId && h.ChildRoleId == childRoleId && h.Status == YesOrNo.Yes)
            .AnyAsync();
    }

    /// <summary>
    /// 添加角色层次关系
    /// </summary>
    public async Task AddHierarchyAsync(long parentRoleId, long childRoleId)
    {
        if (await ExistsHierarchyAsync(parentRoleId, childRoleId))
        {
            return;
        }

        var hierarchy = new SysRoleHierarchy
        {
            ParentRoleId = parentRoleId,
            ChildRoleId = childRoleId,
            Depth = 1,
            IsDirect = true,
            Status = YesOrNo.Yes
        };

        await AddAsync(hierarchy);
    }

    /// <summary>
    /// 移除角色层次关系
    /// </summary>
    public async Task RemoveHierarchyAsync(long parentRoleId, long childRoleId)
    {
        await _dbContext.GetClient().Deleteable<SysRoleHierarchy>()
            .Where(h => h.ParentRoleId == parentRoleId && h.ChildRoleId == childRoleId)
            .ExecuteCommandAsync();
    }

    /// <summary>
    /// 检查是否会形成循环继承
    /// </summary>
    public async Task<bool> WouldCreateCycleAsync(long parentRoleId, long childRoleId)
    {
        if (parentRoleId == childRoleId)
        {
            return true;
        }

        // 检查 childRoleId 是否是 parentRoleId 的祖先
        var childParents = await GetAllParentRoleIdsAsync(parentRoleId);
        return childParents.Contains(childRoleId);
    }
}
