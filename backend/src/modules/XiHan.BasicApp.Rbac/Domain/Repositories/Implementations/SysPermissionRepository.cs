#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysPermissionRepository
// Guid:c3d4e5f6-a7b8-9012-3456-7890abcdef12
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/31 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Domain.Repositories.Implementations;

/// <summary>
/// 系统权限仓储实现
/// </summary>
public class SysPermissionRepository : SqlSugarAggregateRepository<SysPermission, long>, ISysPermissionRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysPermissionRepository(ISqlSugarDbContext dbContext, IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, unitOfWorkManager)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据权限编码获取权限
    /// </summary>
    public async Task<SysPermission?> GetByPermissionCodeAsync(string permissionCode, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysPermission>()
            .Where(p => p.PermissionCode == permissionCode)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 根据资源ID和操作ID获取权限
    /// </summary>
    public async Task<SysPermission?> GetByResourceAndOperationAsync(long resourceId, long operationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysPermission>()
            .Where(p => p.ResourceId == resourceId && p.OperationId == operationId)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 检查权限编码是否存在
    /// </summary>
    public async Task<bool> IsPermissionCodeExistsAsync(string permissionCode, long? excludePermissionId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.GetClient().Queryable<SysPermission>()
            .Where(p => p.PermissionCode == permissionCode);

        if (excludePermissionId.HasValue)
        {
            query = query.Where(p => p.BasicId != excludePermissionId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// 获取角色的权限列表
    /// </summary>
    public async Task<List<SysPermission>> GetPermissionsByRoleIdAsync(long roleId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysPermission>()
            .InnerJoin<SysRolePermission>((p, rp) => p.BasicId == rp.PermissionId)
            .Where((p, rp) => rp.RoleId == roleId && rp.Status == YesOrNo.Yes)
            .Where((p, rp) => p.Status == YesOrNo.Yes)
            .Select((p, rp) => p)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户的权限列表（包含继承自角色的权限）
    /// </summary>
    public async Task<List<SysPermission>> GetPermissionsByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        // 通过用户角色获取权限
        var rolePermissions = await _dbContext.GetClient().Queryable<SysPermission>()
            .InnerJoin<SysRolePermission>((p, rp) => p.BasicId == rp.PermissionId)
            .InnerJoin<SysUserRole>((p, rp, ur) => rp.RoleId == ur.RoleId)
            .Where((p, rp, ur) => ur.UserId == userId && ur.Status == YesOrNo.Yes)
            .Where((p, rp, ur) => rp.Status == YesOrNo.Yes)
            .Where((p, rp, ur) => p.Status == YesOrNo.Yes)
            .Select((p, rp, ur) => p)
            .ToListAsync(cancellationToken);

        // 直接分配给用户的权限
        var userPermissions = await _dbContext.GetClient().Queryable<SysPermission>()
            .InnerJoin<SysUserPermission>((p, up) => p.BasicId == up.PermissionId)
            .Where((p, up) => up.UserId == userId)
            .Where((p, up) => p.Status == YesOrNo.Yes)
            .Select((p, up) => p)
            .ToListAsync(cancellationToken);

        // 合并去重
        return rolePermissions.Union(userPermissions).ToList();
    }

    /// <summary>
    /// 获取资源下的所有权限
    /// </summary>
    public async Task<List<SysPermission>> GetPermissionsByResourceIdAsync(long resourceId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysPermission>()
            .Where(p => p.ResourceId == resourceId)
            .Where(p => p.Status == YesOrNo.Yes)
            .OrderBy(p => p.Sort)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 保存权限
    /// </summary>
    public async Task<SysPermission> SaveAsync(SysPermission permission, CancellationToken cancellationToken = default)
    {
        if (permission.IsTransient())
        {
            return await AddAsync(permission, cancellationToken);
        }
        else
        {
            return await UpdateAsync(permission, cancellationToken);
        }
    }

    /// <summary>
    /// 启用权限
    /// </summary>
    public async Task EnablePermissionAsync(long permissionId, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Updateable<SysPermission>()
            .SetColumns(p => p.Status == YesOrNo.Yes)
            .Where(p => p.BasicId == permissionId)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 禁用权限
    /// </summary>
    public async Task DisablePermissionAsync(long permissionId, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Updateable<SysPermission>()
            .SetColumns(p => p.Status == YesOrNo.No)
            .Where(p => p.BasicId == permissionId)
            .ExecuteCommandAsync(cancellationToken);
    }
}
