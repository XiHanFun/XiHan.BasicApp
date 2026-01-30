#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserRelationRepository
// Guid:a5b6c7d8-e9f0-1234-5678-901234a90123
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

namespace XiHan.BasicApp.Rbac.Repositories;

/// <summary>
/// 用户关系映射仓储实现
/// </summary>
public class SysUserRelationRepository : ISysUserRelationRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysUserRelationRepository(ISqlSugarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // ========== 用户角色关系 ==========

    /// <summary>
    /// 批量添加用户角色
    /// </summary>
    public async Task AddUserRolesAsync(IEnumerable<SysUserRole> userRoles, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Insertable(userRoles.ToArray()).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 删除用户角色
    /// </summary>
    public async Task DeleteUserRolesAsync(long userId, IEnumerable<long> roleIds, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Deleteable<SysUserRole>()
            .Where(ur => ur.UserId == userId && roleIds.Contains(ur.RoleId))
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 删除用户所有角色
    /// </summary>
    public async Task DeleteUserAllRolesAsync(long userId, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Deleteable<SysUserRole>()
            .Where(ur => ur.UserId == userId)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户角色列表
    /// </summary>
    public async Task<List<SysUserRole>> GetUserRolesAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysUserRole>()
            .Where(ur => ur.UserId == userId && ur.Status == YesOrNo.Yes)
            .ToListAsync(cancellationToken);
    }

    // ========== 用户权限关系 ==========

    /// <summary>
    /// 批量添加用户权限
    /// </summary>
    public async Task AddUserPermissionsAsync(IEnumerable<SysUserPermission> userPermissions, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Insertable(userPermissions.ToArray()).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 删除用户权限
    /// </summary>
    public async Task DeleteUserPermissionsAsync(long userId, IEnumerable<long> permissionIds, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Deleteable<SysUserPermission>()
            .Where(up => up.UserId == userId && permissionIds.Contains(up.PermissionId))
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 删除用户所有权限
    /// </summary>
    public async Task DeleteUserAllPermissionsAsync(long userId, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Deleteable<SysUserPermission>()
            .Where(up => up.UserId == userId)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户权限列表
    /// </summary>
    public async Task<List<SysUserPermission>> GetUserPermissionsAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysUserPermission>()
            .Where(up => up.UserId == userId && up.Status == YesOrNo.Yes)
            .ToListAsync(cancellationToken);
    }

    // ========== 用户部门关系 ==========

    /// <summary>
    /// 批量添加用户部门
    /// </summary>
    public async Task AddUserDepartmentsAsync(IEnumerable<SysUserDepartment> userDepartments, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Insertable(userDepartments.ToArray()).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 删除用户部门
    /// </summary>
    public async Task DeleteUserDepartmentsAsync(long userId, IEnumerable<long> departmentIds, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Deleteable<SysUserDepartment>()
            .Where(ud => ud.UserId == userId && departmentIds.Contains(ud.DepartmentId))
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 删除用户所有部门
    /// </summary>
    public async Task DeleteUserAllDepartmentsAsync(long userId, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Deleteable<SysUserDepartment>()
            .Where(ud => ud.UserId == userId)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户部门列表
    /// </summary>
    public async Task<List<SysUserDepartment>> GetUserDepartmentsAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysUserDepartment>()
            .Where(ud => ud.UserId == userId && ud.Status == YesOrNo.Yes)
            .ToListAsync(cancellationToken);
    }

    // ========== 会话角色关系 ==========

    /// <summary>
    /// 添加会话角色
    /// </summary>
    public async Task<SysSessionRole> AddSessionRoleAsync(SysSessionRole sessionRole, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Insertable(sessionRole).ExecuteReturnEntityAsync();
    }

    /// <summary>
    /// 删除会话角色
    /// </summary>
    public async Task DeleteSessionRolesAsync(long sessionId, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Deleteable<SysSessionRole>()
            .Where(sr => sr.SessionId == sessionId)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 获取会话角色列表
    /// </summary>
    public async Task<List<SysSessionRole>> GetSessionRolesAsync(long sessionId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysSessionRole>()
            .Where(sr => sr.SessionId == sessionId && sr.Status == SessionRoleStatus.Active)
            .ToListAsync(cancellationToken);
    }
}
