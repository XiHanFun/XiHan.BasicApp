#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserPermissionRepository
// Guid:bc2b3c4d-5e6f-7890-abcd-ef123456789b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 19:25:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Repositories.UserPermissions;

/// <summary>
/// 系统用户权限仓储实现
/// </summary>
public class SysUserPermissionRepository : SqlSugarRepositoryBase<SysUserPermission, XiHanBasicAppIdType>, ISysUserPermissionRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public SysUserPermissionRepository(ISqlSugarDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据用户ID获取用户权限列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    public async Task<List<SysUserPermission>> GetByUserIdAsync(XiHanBasicAppIdType userId)
    {
        return await _dbContext.GetClient()
            .Queryable<SysUserPermission>()
            .Where(up => up.UserId == userId)
            .ToListAsync();
    }

    /// <summary>
    /// 根据用户ID和权限操作类型获取用户权限列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="permissionAction">权限操作类型</param>
    /// <returns></returns>
    public async Task<List<SysUserPermission>> GetByUserIdAndActionAsync(XiHanBasicAppIdType userId, PermissionAction permissionAction)
    {
        return await _dbContext.GetClient()
            .Queryable<SysUserPermission>()
            .Where(up => up.UserId == userId && up.PermissionAction == permissionAction)
            .ToListAsync();
    }

    /// <summary>
    /// 根据权限ID获取用户权限列表
    /// </summary>
    /// <param name="permissionId">权限ID</param>
    /// <returns></returns>
    public async Task<List<SysUserPermission>> GetByPermissionIdAsync(XiHanBasicAppIdType permissionId)
    {
        return await _dbContext.GetClient()
            .Queryable<SysUserPermission>()
            .Where(up => up.PermissionId == permissionId)
            .ToListAsync();
    }

    /// <summary>
    /// 检查用户是否有指定权限的直授记录
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="permissionId">权限ID</param>
    /// <returns></returns>
    public async Task<SysUserPermission?> GetByUserAndPermissionAsync(XiHanBasicAppIdType userId, XiHanBasicAppIdType permissionId)
    {
        return await GetFirstAsync(up => up.UserId == userId && up.PermissionId == permissionId);
    }

    /// <summary>
    /// 获取用户的有效权限（未过期）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    public async Task<List<SysUserPermission>> GetEffectivePermissionsAsync(XiHanBasicAppIdType userId)
    {
        var now = DateTimeOffset.Now;
        return await _dbContext.GetClient()
            .Queryable<SysUserPermission>()
            .Where(up => up.UserId == userId)
            .Where(up => up.Status == YesOrNo.Yes)
            .Where(up => (up.EffectiveTime == null || up.EffectiveTime <= now) &&
                        (up.ExpirationTime == null || up.ExpirationTime > now))
            .ToListAsync();
    }

    /// <summary>
    /// 批量删除用户的权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="permissionIds">权限ID列表</param>
    /// <returns></returns>
    public async Task<int> DeleteByUserAndPermissionsAsync(XiHanBasicAppIdType userId, List<XiHanBasicAppIdType> permissionIds)
    {
        return await _dbContext.GetClient()
            .Deleteable<SysUserPermission>()
            .Where(up => up.UserId == userId && permissionIds.Contains(up.PermissionId))
            .ExecuteCommandAsync();
    }
}

