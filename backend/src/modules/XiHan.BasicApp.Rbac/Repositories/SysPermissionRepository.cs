#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysPermissionRepository
// Guid:c1d2e3f4-a5b6-7890-1234-567890c56789
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
/// 权限聚合仓储实现
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
            .Where(p => p.PermissionCode == permissionCode && p.Status == YesOrNo.Yes)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 获取角色的所有权限
    /// </summary>
    public async Task<List<SysPermission>> GetByRoleIdAsync(long roleId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysPermission>()
            .InnerJoin<SysRolePermission>((p, rp) => p.BasicId == rp.PermissionId)
            .Where((p, rp) => rp.RoleId == roleId && p.Status == YesOrNo.Yes && rp.Status == YesOrNo.Yes)
            .Select(p => p)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户的所有权限（通过角色）
    /// </summary>
    public async Task<List<SysPermission>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysPermission>()
            .InnerJoin<SysRolePermission>((p, rp) => p.BasicId == rp.PermissionId)
            .InnerJoin<SysUserRole>((p, rp, ur) => rp.RoleId == ur.RoleId)
            .Where((p, rp, ur) => ur.UserId == userId && p.Status == YesOrNo.Yes && rp.Status == YesOrNo.Yes && ur.Status == YesOrNo.Yes)
            .Select(p => p)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取资源的所有权限
    /// </summary>
    public async Task<List<SysPermission>> GetByResourceIdAsync(long resourceId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysPermission>()
            .Where(p => p.ResourceId == resourceId && p.Status == YesOrNo.Yes)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 批量获取权限
    /// </summary>
    public async Task<List<SysPermission>> GetByIdsAsync(IEnumerable<long> permissionIds, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysPermission>()
            .In(p => p.BasicId, permissionIds.ToArray())
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 检查权限编码是否存在
    /// </summary>
    public async Task<bool> ExistsByPermissionCodeAsync(string permissionCode, long? excludePermissionId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.GetClient().Queryable<SysPermission>()
            .Where(p => p.PermissionCode == permissionCode);

        if (excludePermissionId.HasValue)
        {
            query = query.Where(p => p.BasicId != excludePermissionId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
