#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionRepository
// Guid:c3d4e5f6-a7b8-4c5d-9e0f-2a3b4c5d6e7f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/8 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Repositories;

/// <summary>
/// 权限仓储实现
/// </summary>
public class PermissionRepository : SqlSugarAggregateRepository<SysPermission, long>, IPermissionRepository
{
    private readonly ISqlSugarClient _dbClient;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PermissionRepository(ISqlSugarDbContext dbContext, IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, unitOfWorkManager)
    {
        _dbClient = dbContext.GetClient();
    }

    /// <summary>
    /// 根据权限编码查询权限
    /// </summary>
    public async Task<SysPermission?> GetByPermissionCodeAsync(string permissionCode, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysPermission>()
            .FirstAsync(p => p.PermissionCode == permissionCode, cancellationToken);
    }

    /// <summary>
    /// 检查权限编码是否存在
    /// </summary>
    public async Task<bool> ExistsByPermissionCodeAsync(string permissionCode, long? excludePermissionId = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = _dbClient.Queryable<SysPermission>()
            .Where(p => p.PermissionCode == permissionCode);

        if (excludePermissionId.HasValue)
        {
            query = query.Where(p => p.BasicId != excludePermissionId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户的所有权限
    /// </summary>
    public async Task<List<SysPermission>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 通过用户权限表直接获取
        var directPermissions = await _dbClient.Queryable<SysPermission>()
            .LeftJoin<SysUserPermission>((p, up) => p.BasicId == up.PermissionId)
            .Where((p, up) => up.UserId == userId)
            .Select((p, up) => p)
            .ToListAsync(cancellationToken);

        // 通过角色权限表获取
        var rolePermissions = await _dbClient.Queryable<SysPermission>()
            .LeftJoin<SysRolePermission>((p, rp) => p.BasicId == rp.PermissionId)
            .LeftJoin<SysUserRole>((p, rp, ur) => rp.RoleId == ur.RoleId)
            .Where((p, rp, ur) => ur.UserId == userId)
            .Select((p, rp, ur) => p)
            .ToListAsync(cancellationToken);

        // 合并并去重
        var allPermissions = directPermissions.Concat(rolePermissions)
            .GroupBy(p => p.BasicId)
            .Select(g => g.First())
            .ToList();

        return allPermissions;
    }

    /// <summary>
    /// 获取角色的所有权限
    /// </summary>
    public async Task<List<SysPermission>> GetByRoleIdAsync(long roleId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysPermission>()
            .LeftJoin<SysRolePermission>((p, rp) => p.BasicId == rp.PermissionId)
            .Where((p, rp) => rp.RoleId == roleId)
            .Select((p, rp) => p)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取资源的所有权限
    /// </summary>
    public async Task<List<SysPermission>> GetByResourceIdAsync(long resourceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysPermission>()
            .Where(p => p.ResourceId == resourceId)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据权限编码批量获取权限
    /// </summary>
    public async Task<List<SysPermission>> GetByCodesAsync(List<string> permissionCodes, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysPermission>()
            .Where(p => permissionCodes.Contains(p.PermissionCode))
            .ToListAsync(cancellationToken);
    }
}
