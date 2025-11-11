#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleRepository
// Guid:eb2b3c4d-5e6f-7890-abcd-ef12345678b3
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 6:25:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.Abstractions;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Repositories.Implementations;

/// <summary>
/// 角色仓储实现
/// </summary>
public class RoleRepository : SqlSugarRepositoryBase<SysRole, RbacIdType>, IRoleRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public RoleRepository(ISqlSugarDbContext dbContext) : base(dbContext)
    {
    }

    /// <summary>
    /// 根据角色编码获取角色
    /// </summary>
    /// <param name="roleCode">角色编码</param>
    /// <returns></returns>
    public async Task<SysRole?> GetByRoleCodeAsync(string roleCode)
    {
        return await QueryAsync(r => r.RoleCode == roleCode);
    }

    /// <summary>
    /// 检查角色编码是否存在
    /// </summary>
    /// <param name="roleCode">角色编码</param>
    /// <param name="excludeId">排除的角色ID</param>
    /// <returns></returns>
    public async Task<bool> ExistsByRoleCodeAsync(string roleCode, long? excludeId = null)
    {
        var query = Queryable().Where(r => r.RoleCode == roleCode);
        if (excludeId.HasValue)
        {
            query = query.Where(r => r.BasicId != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    /// <summary>
    /// 获取角色的菜单ID列表
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns></returns>
    public async Task<List<long>> GetRoleMenuIdsAsync(long roleId)
    {
        return await DbContext.GetClient()
            .Queryable<SysRoleMenu>()
            .Where(rm => rm.RoleId == roleId)
            .Select(rm => rm.MenuId)
            .ToListAsync();
    }

    /// <summary>
    /// 获取角色的权限ID列表
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns></returns>
    public async Task<List<long>> GetRolePermissionIdsAsync(long roleId)
    {
        return await DbContext.GetClient()
            .Queryable<SysRolePermission>()
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.PermissionId)
            .ToListAsync();
    }

    /// <summary>
    /// 获取角色的用户数量
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns></returns>
    public async Task<int> GetRoleUserCountAsync(long roleId)
    {
        return await DbContext.GetClient()
            .Queryable<SysUserRole>()
            .Where(ur => ur.RoleId == roleId)
            .CountAsync();
    }

    /// <summary>
    /// 根据用户ID获取角色列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    public async Task<List<SysRole>> GetByUserIdAsync(long userId)
    {
        return await DbContext.GetClient()
            .Queryable<SysUserRole>()
            .Where(ur => ur.UserId == userId)
            .LeftJoin<SysRole>((ur, r) => ur.RoleId == r.BasicId)
            .Select((ur, r) => r)
            .ToListAsync();
    }
}

