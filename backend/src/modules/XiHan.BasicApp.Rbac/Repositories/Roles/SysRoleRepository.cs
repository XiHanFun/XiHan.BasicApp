#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysRoleRepository
// Guid:eb2b3c4d-5e6f-7890-abcd-ef12345678b3
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 6:25:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Repositories.Roles;

/// <summary>
/// 系统角色仓储实现
/// </summary>
public class SysRoleRepository : SqlSugarRepositoryBase<SysRole, long>, ISysRoleRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public SysRoleRepository(ISqlSugarDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据角色编码获取角色
    /// </summary>
    /// <param name="roleCode">角色编码</param>
    /// <returns></returns>
    public async Task<SysRole?> GetByRoleCodeAsync(string roleCode)
    {
        return await GetFirstAsync(r => r.RoleCode == roleCode);
    }

    /// <summary>
    /// 检查角色编码是否存在
    /// </summary>
    /// <param name="roleCode">角色编码</param>
    /// <param name="excludeId">排除的角色ID</param>
    /// <returns></returns>
    public async Task<bool> ExistsByRoleCodeAsync(string roleCode, long? excludeId = null)
    {
        var query = _dbContext.GetClient().Queryable<SysRole>().Where(r => r.RoleCode == roleCode);
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
        return await _dbContext.GetClient()
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
        return await _dbContext.GetClient()
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
        return await _dbContext.GetClient()
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
        return await _dbContext.GetClient()
            .Queryable<SysUserRole>()
            .Where(ur => ur.UserId == userId)
            .LeftJoin<SysRole>((ur, r) => ur.RoleId == r.BasicId)
            .Select((ur, r) => r)
            .ToListAsync();
    }

    /// <summary>
    /// 获取角色的所有父角色ID（递归查询继承链）
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns>父角色ID列表（包括所有祖先角色）</returns>
    public async Task<List<long>> GetParentRoleIdsAsync(long roleId)
    {
        var parentRoleIds = new List<long>();
        await GetParentRoleIdsRecursiveAsync(roleId, parentRoleIds);
        return parentRoleIds;
    }

    /// <summary>
    /// 获取角色的所有子角色ID（递归查询继承链）
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns>子角色ID列表（包括所有后代角色）</returns>
    public async Task<List<long>> GetChildRoleIdsAsync(long roleId)
    {
        var childRoleIds = new List<long>();
        await GetChildRoleIdsRecursiveAsync(roleId, childRoleIds);
        return childRoleIds;
    }

    /// <summary>
    /// 获取角色的所有父角色（递归查询继承链）
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns>父角色列表（包括所有祖先角色）</returns>
    public async Task<List<SysRole>> GetParentRolesAsync(long roleId)
    {
        var parentRoleIds = await GetParentRoleIdsAsync(roleId);
        if (!parentRoleIds.Any())
        {
            return [];
        }

        var roles = await GetListAsync(r => parentRoleIds.Contains(r.BasicId));
        return roles.ToList();
    }

    /// <summary>
    /// 获取角色的所有子角色（递归查询继承链）
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns>子角色列表（包括所有后代角色）</returns>
    public async Task<List<SysRole>> GetChildRolesAsync(long roleId)
    {
        var childRoleIds = await GetChildRoleIdsAsync(roleId);
        if (!childRoleIds.Any())
        {
            return [];
        }

        var roles = await GetListAsync(r => childRoleIds.Contains(r.BasicId));
        return roles.ToList();
    }

    /// <summary>
    /// 检查是否会形成循环继承
    /// </summary>
    /// <param name="roleId">当前角色ID</param>
    /// <param name="parentRoleId">要设置的父角色ID</param>
    /// <returns>是否会形成循环</returns>
    public async Task<bool> WouldCreateCycleAsync(long roleId, long parentRoleId)
    {
        // 如果父角色ID等于当前角色ID，直接返回true
        if (roleId == parentRoleId)
        {
            return true;
        }

        // 检查parentRoleId是否是roleId的子孙角色
        var childRoleIds = await GetChildRoleIdsAsync(roleId);
        return childRoleIds.Contains(parentRoleId);
    }

    /// <summary>
    /// 获取角色树（包含子角色）
    /// </summary>
    /// <param name="parentRoleId">父角色ID，null表示获取根角色</param>
    /// <returns>角色树</returns>
    public async Task<List<SysRole>> GetRoleTreeAsync(long? parentRoleId = null)
    {
        if (parentRoleId == null)
        {
            // 获取所有根角色（没有父角色的角色）
            var rootRoles = await GetListAsync(r => r.ParentRoleId == null);
            return rootRoles.ToList();
        }

        // 获取指定父角色下的所有子角色
        var childRoles = await GetListAsync(r => r.ParentRoleId == parentRoleId);
        return childRoles.ToList();
    }

    /// <summary>
    /// 递归获取父角色ID
    /// </summary>
    private async Task GetParentRoleIdsRecursiveAsync(long roleId, List<long> result)
    {
        var role = await GetByIdAsync(roleId);
        if (role?.ParentRoleId != null && role.ParentRoleId.Value != default(long))
        {
            if (!result.Contains(role.ParentRoleId.Value))
            {
                result.Add(role.ParentRoleId.Value);
                await GetParentRoleIdsRecursiveAsync(role.ParentRoleId.Value, result);
            }
        }
    }

    /// <summary>
    /// 递归获取子角色ID
    /// </summary>
    private async Task GetChildRoleIdsRecursiveAsync(long roleId, List<long> result)
    {
        var childRoles = await GetListAsync(r => r.ParentRoleId == roleId);
        foreach (var child in childRoles)
        {
            if (!result.Contains(child.BasicId))
            {
                result.Add(child.BasicId);
                await GetChildRoleIdsRecursiveAsync(child.BasicId, result);
            }
        }
    }
}
