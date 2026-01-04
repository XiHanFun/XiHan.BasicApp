#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysPermissionRepository
// Guid:fb2b3c4d-5e6f-7890-abcd-ef12345678b4
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 6:30:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Repositories.Permissions;

/// <summary>
/// 系统权限仓储实现
/// </summary>
public class SysPermissionRepository : SqlSugarRepositoryBase<SysPermission, long>, ISysPermissionRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public SysPermissionRepository(ISqlSugarDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据权限编码获取权限
    /// </summary>
    /// <param name="permissionCode">权限编码</param>
    /// <returns></returns>
    public async Task<SysPermission?> GetByPermissionCodeAsync(string permissionCode)
    {
        return await GetFirstAsync(p => p.PermissionCode == permissionCode);
    }

    /// <summary>
    /// 检查权限编码是否存在
    /// </summary>
    /// <param name="permissionCode">权限编码</param>
    /// <param name="excludeId">排除的权限ID</param>
    /// <returns></returns>
    public async Task<bool> ExistsByPermissionCodeAsync(string permissionCode, long? excludeId = null)
    {
        var query = _dbContext.GetClient().Queryable<SysPermission>().Where(p => p.PermissionCode == permissionCode);
        if (excludeId.HasValue)
        {
            query = query.Where(p => p.BasicId != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    /// <summary>
    /// 根据角色ID获取权限列表
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns></returns>
    public async Task<List<SysPermission>> GetByRoleIdAsync(long roleId)
    {
        return await _dbContext.GetClient()
            .Queryable<SysRolePermission>()
            .Where(rp => rp.RoleId == roleId)
            .LeftJoin<SysPermission>((rp, p) => rp.PermissionId == p.BasicId)
            .Select((rp, p) => p)
            .ToListAsync();
    }

    /// <summary>
    /// 根据用户ID获取权限列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    public async Task<List<SysPermission>> GetByUserIdAsync(long userId)
    {
        // 获取用户的角色ID列表
        var roleIds = await _dbContext.GetClient()
            .Queryable<SysUserRole>()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        if (!roleIds.Any())
        {
            return [];
        }

        // 通过角色获取权限
        return await _dbContext.GetClient()
            .Queryable<SysRolePermission>()
            .Where(rp => roleIds.Contains(rp.RoleId))
            .LeftJoin<SysPermission>((rp, p) => rp.PermissionId == p.BasicId)
            .Select((rp, p) => p)
            .Distinct()
            .ToListAsync();
    }
}
