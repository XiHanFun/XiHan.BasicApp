#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysRolePermissionRepository
// Guid:e0b14028-aefc-40d9-9ed9-579900a0cb82
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/11 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Domain.Repositories.Implementations;

/// <summary>
/// 系统角色权限仓储实现
/// </summary>
public class SysRolePermissionRepository : SqlSugarReadOnlyRepository<SysRolePermission, long>, ISysRolePermissionRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysRolePermissionRepository(ISqlSugarDbContext dbContext)
        : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据角色ID获取权限列表
    /// </summary>
    public async Task<List<SysRolePermission>> GetByRoleIdAsync(long roleId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysRolePermission>()
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据权限ID获取角色权限列表
    /// </summary>
    public async Task<List<SysRolePermission>> GetByPermissionIdAsync(long permissionId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysRolePermission>()
            .Where(rp => rp.PermissionId == permissionId)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 删除角色的所有权限
    /// </summary>
    public async Task DeleteByRoleIdAsync(long roleId, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Deleteable<SysRolePermission>()
            .Where(rp => rp.RoleId == roleId)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 保存角色权限
    /// </summary>
    public async Task<SysRolePermission> SaveAsync(SysRolePermission rolePermission, CancellationToken cancellationToken = default)
    {
        if (rolePermission.BasicId == 0)
        {
            // 新增
            await _dbContext.GetClient().Insertable(rolePermission).ExecuteCommandAsync(cancellationToken);
        }
        else
        {
            // 更新
            await _dbContext.GetClient().Updateable(rolePermission).ExecuteCommandAsync(cancellationToken);
        }

        return rolePermission;
    }
}
