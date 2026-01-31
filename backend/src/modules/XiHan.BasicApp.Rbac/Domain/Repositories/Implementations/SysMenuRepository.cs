#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysMenuRepository
// Guid:b8c9d0e1-f2a3-4567-89ab-cdef12345678
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/31 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Domain.Repositories.Implementations;

/// <summary>
/// 系统菜单仓储实现
/// </summary>
public class SysMenuRepository : SqlSugarAggregateRepository<SysMenu, long>, ISysMenuRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysMenuRepository(ISqlSugarDbContext dbContext, IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, unitOfWorkManager)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据菜单编码获取菜单
    /// </summary>
    public async Task<SysMenu?> GetByMenuCodeAsync(string menuCode, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysMenu>()
            .Where(m => m.MenuCode == menuCode)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 检查菜单编码是否存在
    /// </summary>
    public async Task<bool> IsMenuCodeExistsAsync(string menuCode, long? excludeMenuId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.GetClient().Queryable<SysMenu>()
            .Where(m => m.MenuCode == menuCode);

        if (excludeMenuId.HasValue)
        {
            query = query.Where(m => m.BasicId != excludeMenuId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户的菜单树
    /// </summary>
    public async Task<List<SysMenu>> GetMenuTreeByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        // 通过用户角色获取菜单
        return await _dbContext.GetClient().Queryable<SysMenu>()
            .InnerJoin<SysRoleMenu>((m, rm) => m.BasicId == rm.MenuId)
            .InnerJoin<SysUserRole>((m, rm, ur) => rm.RoleId == ur.RoleId)
            .Where((m, rm, ur) => ur.UserId == userId && ur.Status == YesOrNo.Yes)
            .Where((m, rm, ur) => m.Status == YesOrNo.Yes && m.IsVisible)
            .Select((m, rm, ur) => m)
            .Distinct()
            .OrderBy(m => m.Sort)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取角色的菜单列表
    /// </summary>
    public async Task<List<SysMenu>> GetMenusByRoleIdAsync(long roleId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysMenu>()
            .InnerJoin<SysRoleMenu>((m, rm) => m.BasicId == rm.MenuId)
            .Where((m, rm) => rm.RoleId == roleId && rm.Status == YesOrNo.Yes)
            .Where((m, rm) => m.Status == YesOrNo.Yes)
            .Select((m, rm) => m)
            .OrderBy(m => m.Sort)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取父级菜单下的子菜单列表
    /// </summary>
    public async Task<List<SysMenu>> GetChildrenMenusAsync(long? parentId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysMenu>()
            .Where(m => m.ParentId == parentId)
            .Where(m => m.Status == YesOrNo.Yes)
            .OrderBy(m => m.Sort)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取租户下的菜单树
    /// </summary>
    public async Task<List<SysMenu>> GetMenuTreeByTenantAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysMenu>()
            .Where(m => m.TenantId == tenantId || m.TenantId == null)
            .Where(m => m.Status == YesOrNo.Yes)
            .OrderBy(m => m.Sort)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 保存菜单
    /// </summary>
    public async Task<SysMenu> SaveAsync(SysMenu menu, CancellationToken cancellationToken = default)
    {
        if (menu.IsTransient())
        {
            return await AddAsync(menu, cancellationToken);
        }
        else
        {
            return await UpdateAsync(menu, cancellationToken);
        }
    }

    /// <summary>
    /// 启用菜单
    /// </summary>
    public async Task EnableMenuAsync(long menuId, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Updateable<SysMenu>()
            .SetColumns(m => m.Status == YesOrNo.Yes)
            .Where(m => m.BasicId == menuId)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 禁用菜单
    /// </summary>
    public async Task DisableMenuAsync(long menuId, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Updateable<SysMenu>()
            .SetColumns(m => m.Status == YesOrNo.No)
            .Where(m => m.BasicId == menuId)
            .ExecuteCommandAsync(cancellationToken);
    }
}
