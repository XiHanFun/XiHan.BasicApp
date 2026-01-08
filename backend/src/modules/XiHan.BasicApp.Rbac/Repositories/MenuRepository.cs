#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MenuRepository
// Guid:d4e5f6a7-b8c9-4d5e-0f1a-3b4c5d6e7f8a
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
/// 菜单仓储实现
/// </summary>
public class MenuRepository : SqlSugarAggregateRepository<SysMenu, long>, IMenuRepository
{
    private readonly ISqlSugarClient _dbClient;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MenuRepository(ISqlSugarDbContext dbContext, IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, unitOfWorkManager)
    {
        _dbClient = dbContext.GetClient();
    }

    /// <summary>
    /// 根据菜单编码查询菜单
    /// </summary>
    public async Task<SysMenu?> GetByMenuCodeAsync(string menuCode, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysMenu>()
            .FirstAsync(m => m.MenuCode == menuCode, cancellationToken);
    }

    /// <summary>
    /// 检查菜单编码是否存在
    /// </summary>
    public async Task<bool> ExistsByMenuCodeAsync(string menuCode, long? excludeMenuId = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = _dbClient.Queryable<SysMenu>()
            .Where(m => m.MenuCode == menuCode);

        if (excludeMenuId.HasValue)
        {
            query = query.Where(m => m.BasicId != excludeMenuId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户的所有菜单
    /// </summary>
    public async Task<List<SysMenu>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 通过用户角色获取菜单
        return await _dbClient.Queryable<SysMenu>()
            .LeftJoin<SysRoleMenu>((m, rm) => m.BasicId == rm.MenuId)
            .LeftJoin<SysUserRole>((m, rm, ur) => rm.RoleId == ur.RoleId)
            .Where((m, rm, ur) => ur.UserId == userId)
            .Select((m, rm, ur) => m)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取角色的所有菜单
    /// </summary>
    public async Task<List<SysMenu>> GetByRoleIdAsync(long roleId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysMenu>()
            .LeftJoin<SysRoleMenu>((m, rm) => m.BasicId == rm.MenuId)
            .Where((m, rm) => rm.RoleId == roleId)
            .Select((m, rm) => m)
            .OrderBy(m => m.Sort)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取父菜单的所有子菜单
    /// </summary>
    public async Task<List<SysMenu>> GetByParentIdAsync(long? parentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysMenu>()
            .Where(m => m.ParentId == parentId)
            .OrderBy(m => m.Sort)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取所有根菜单
    /// </summary>
    public async Task<List<SysMenu>> GetRootMenusAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysMenu>()
            .Where(m => m.ParentId == null || m.ParentId == 0)
            .OrderBy(m => m.Sort)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取菜单树（包含所有子菜单）
    /// </summary>
    public async Task<List<SysMenu>> GetMenuTreeAsync(long? parentId = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 获取所有菜单
        var allMenus = await _dbClient.Queryable<SysMenu>()
            .OrderBy(m => m.Sort)
            .ToListAsync(cancellationToken);

        // 递归构建树形结构（这里返回扁平列表，树形结构应在服务层构建）
        return allMenus;
    }

    /// <summary>
    /// 检查是否有子菜单
    /// </summary>
    public async Task<bool> HasChildrenAsync(long menuId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysMenu>()
            .Where(m => m.ParentId == menuId)
            .AnyAsync(cancellationToken);
    }
}
