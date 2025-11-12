#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserRepository
// Guid:db2b3c4d-5e6f-7890-abcd-ef12345678b2
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 6:20:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.Abstractions;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Repositories.Implementations;

/// <summary>
/// 系统用户仓储实现
/// </summary>
public class SysUserRepository : SqlSugarRepositoryBase<SysUser, RbacIdType>, ISysUserRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public SysUserRepository(ISqlSugarDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <returns></returns>
    public async Task<SysUser?> GetByUserNameAsync(string userName)
    {
        return await GetFirstAsync(u => u.UserName == userName);
    }

    /// <summary>
    /// 根据邮箱获取用户
    /// </summary>
    /// <param name="email">邮箱</param>
    /// <returns></returns>
    public async Task<SysUser?> GetByEmailAsync(string email)
    {
        return await GetFirstAsync(u => u.Email == email);
    }

    /// <summary>
    /// 根据手机号获取用户
    /// </summary>
    /// <param name="phone">手机号</param>
    /// <returns></returns>
    public async Task<SysUser?> GetByPhoneAsync(string phone)
    {
        return await GetFirstAsync(u => u.Phone == phone);
    }

    /// <summary>
    /// 检查用户名是否存在
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="excludeId">排除的用户ID</param>
    /// <returns></returns>
    public async Task<bool> ExistsByUserNameAsync(string userName, RbacIdType? excludeId = null)
    {
        var query = _dbContext.GetClient().Queryable<SysUser>().Where(u => u.UserName == userName);
        if (excludeId.HasValue)
        {
            query = query.Where(u => u.BasicId != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    /// <summary>
    /// 检查邮箱是否存在
    /// </summary>
    /// <param name="email">邮箱</param>
    /// <param name="excludeId">排除的用户ID</param>
    /// <returns></returns>
    public async Task<bool> ExistsByEmailAsync(string email, RbacIdType? excludeId = null)
    {
        var query = _dbContext.GetClient().Queryable<SysUser>().Where(u => u.Email == email);
        if (excludeId.HasValue)
        {
            query = query.Where(u => u.BasicId != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    /// <summary>
    /// 检查手机号是否存在
    /// </summary>
    /// <param name="phone">手机号</param>
    /// <param name="excludeId">排除的用户ID</param>
    /// <returns></returns>
    public async Task<bool> ExistsByPhoneAsync(string phone, RbacIdType? excludeId = null)
    {
        var query = _dbContext.GetClient().Queryable<SysUser>().Where(u => u.Phone == phone);
        if (excludeId.HasValue)
        {
            query = query.Where(u => u.BasicId != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    /// <summary>
    /// 获取用户的角色ID列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    public async Task<List<RbacIdType>> GetUserRoleIdsAsync(RbacIdType userId)
    {
        return await _dbContext.GetClient()
            .Queryable<SysUserRole>()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();
    }

    /// <summary>
    /// 获取用户的部门ID列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    public async Task<List<RbacIdType>> GetUserDepartmentIdsAsync(RbacIdType userId)
    {
        return await _dbContext.GetClient()
            .Queryable<SysUserDepartment>()
            .Where(ud => ud.UserId == userId)
            .Select(ud => ud.DepartmentId)
            .ToListAsync();
    }

    /// <summary>
    /// 获取用户的权限编码列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    public async Task<List<string>> GetUserPermissionsAsync(RbacIdType userId)
    {
        // 获取用户的角色ID列表
        var roleIds = await GetUserRoleIdsAsync(userId);
        if (!roleIds.Any())
        {
            return [];
        }

        // 通过角色获取权限
        return await _dbContext.GetClient()
            .Queryable<SysRolePermission>()
            .Where(rp => roleIds.Contains(rp.RoleId))
            .LeftJoin<SysPermission>((rp, p) => rp.PermissionId == p.BasicId)
            .Select((rp, p) => p.PermissionCode)
            .Distinct()
            .ToListAsync();
    }
}
