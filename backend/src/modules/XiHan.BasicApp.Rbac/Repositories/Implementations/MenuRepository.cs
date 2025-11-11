#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MenuRepository
// Guid:0c2b3c4d-5e6f-7890-abcd-ef12345678b5
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 6:35:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.Abstractions;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Repositories.Implementations;

/// <summary>
/// 菜单仓储实现
/// </summary>
public class MenuRepository : SqlSugarRepositoryBase<SysMenu, RbacIdType>, IMenuRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public MenuRepository(ISqlSugarDbContext dbContext) : base(dbContext)
    {
    }

    /// <summary>
    /// 根据菜单编码获取菜单
    /// </summary>
    /// <param name="menuCode">菜单编码</param>
    /// <returns></returns>
    public async Task<SysMenu?> GetByMenuCodeAsync(string menuCode)
    {
        return await QueryAsync(m => m.MenuCode == menuCode);
    }

    /// <summary>
    /// 检查菜单编码是否存在
    /// </summary>
    /// <param name="menuCode">菜单编码</param>
    /// <param name="excludeId">排除的菜单ID</param>
    /// <returns></returns>
    public async Task<bool> ExistsByMenuCodeAsync(string menuCode, long? excludeId = null)
    {
        var query = Queryable().Where(m => m.MenuCode == menuCode);
        if (excludeId.HasValue)
        {
            query = query.Where(m => m.BasicId != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    /// <summary>
    /// 获取所有根菜单
    /// </summary>
    /// <returns></returns>
    public async Task<List<SysMenu>> GetRootMenusAsync()
    {
        return await QueryListAsync(m => m.ParentId == null || m.ParentId == 0);
    }

    /// <summary>
    /// 根据父级ID获取子菜单
    /// </summary>
    /// <param name="parentId">父级菜单ID</param>
    /// <returns></returns>
    public async Task<List<SysMenu>> GetChildrenAsync(long parentId)
    {
        return await QueryListAsync(m => m.ParentId == parentId);
    }

    /// <summary>
    /// 根据角色ID获取菜单列表
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns></returns>
    public async Task<List<SysMenu>> GetByRoleIdAsync(long roleId)
    {
        return await DbContext.GetClient()
            .Queryable<SysRoleMenu>()
            .Where(rm => rm.RoleId == roleId)
            .LeftJoin<SysMenu>((rm, m) => rm.MenuId == m.BasicId)
            .Select((rm, m) => m)
            .ToListAsync();
    }

    /// <summary>
    /// 根据用户ID获取菜单列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    public async Task<List<SysMenu>> GetByUserIdAsync(long userId)
    {
        // 获取用户的角色ID列表
        var roleIds = await DbContext.GetClient()
            .Queryable<SysUserRole>()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        if (!roleIds.Any())
        {
            return [];
        }

        // 通过角色获取菜单
        return await DbContext.GetClient()
            .Queryable<SysRoleMenu>()
            .Where(rm => roleIds.Contains(rm.RoleId))
            .LeftJoin<SysMenu>((rm, m) => rm.MenuId == m.BasicId)
            .Select((rm, m) => m)
            .Distinct()
            .ToListAsync();
    }
}

