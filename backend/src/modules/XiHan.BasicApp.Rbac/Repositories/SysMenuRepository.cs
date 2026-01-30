#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysMenuRepository
// Guid:d2e3f4a5-b6c7-8901-2345-678901d67890
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
/// 菜单仓储实现
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
    /// 获取用户菜单树
    /// </summary>
    public async Task<List<SysMenu>> GetUserMenuTreeAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysMenu>()
            .InnerJoin<SysRoleMenu>((m, rm) => m.BasicId == rm.MenuId)
            .InnerJoin<SysUserRole>((m, rm, ur) => rm.RoleId == ur.RoleId)
            .Where((m, rm, ur) => ur.UserId == userId && m.Status == YesOrNo.Yes && rm.Status == YesOrNo.Yes && ur.Status == YesOrNo.Yes)
            .Select(m => m)
            .Distinct()
            .OrderBy(m => m.Sort)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取角色菜单
    /// </summary>
    public async Task<List<SysMenu>> GetByRoleIdAsync(long roleId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysMenu>()
            .InnerJoin<SysRoleMenu>((m, rm) => m.BasicId == rm.MenuId)
            .Where((m, rm) => rm.RoleId == roleId && m.Status == YesOrNo.Yes && rm.Status == YesOrNo.Yes)
            .Select(m => m)
            .OrderBy(m => m.Sort)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取子菜单
    /// </summary>
    public async Task<List<SysMenu>> GetChildrenAsync(long parentId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysMenu>()
            .Where(m => m.ParentId == parentId && m.Status == YesOrNo.Yes)
            .OrderBy(m => m.Sort)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取所有启用菜单树
    /// </summary>
    public async Task<List<SysMenu>> GetActiveMenuTreeAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysMenu>()
            .Where(m => m.Status == YesOrNo.Yes)
            .OrderBy(m => m.Sort)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 批量获取菜单
    /// </summary>
    public async Task<List<SysMenu>> GetByIdsAsync(IEnumerable<long> menuIds, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysMenu>()
            .In(m => m.BasicId, menuIds.ToArray())
            .ToListAsync(cancellationToken);
    }
}
