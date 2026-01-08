#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleRepository
// Guid:b2c3d4e5-f6a7-4b5c-8d9e-1f2a3b4c5d6e
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
/// 角色仓储实现
/// </summary>
public class RoleRepository : SqlSugarAggregateRepository<SysRole, long>, IRoleRepository
{
    private readonly ISqlSugarClient _dbClient;

    /// <summary>
    /// 构造函数
    /// </summary>
    public RoleRepository(ISqlSugarDbContext dbContext, IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, unitOfWorkManager)
    {
        _dbClient = dbContext.GetClient();
    }

    /// <summary>
    /// 根据角色编码查询角色
    /// </summary>
    public async Task<SysRole?> GetByRoleCodeAsync(string roleCode, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysRole>()
            .FirstAsync(r => r.RoleCode == roleCode, cancellationToken);
    }

    /// <summary>
    /// 检查角色编码是否存在
    /// </summary>
    public async Task<bool> ExistsByRoleCodeAsync(string roleCode, long? excludeRoleId = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = _dbClient.Queryable<SysRole>()
            .Where(r => r.RoleCode == roleCode);

        if (excludeRoleId.HasValue)
        {
            query = query.Where(r => r.BasicId != excludeRoleId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// 获取角色及其权限
    /// </summary>
    public async Task<SysRole?> GetWithPermissionsAsync(long roleId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysRole>()
            .FirstAsync(r => r.BasicId == roleId, cancellationToken);
    }

    /// <summary>
    /// 获取角色及其菜单
    /// </summary>
    public async Task<SysRole?> GetWithMenusAsync(long roleId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysRole>()
            .FirstAsync(r => r.BasicId == roleId, cancellationToken);
    }

    /// <summary>
    /// 获取角色的所有用户
    /// </summary>
    public async Task<List<SysUser>> GetUsersByRoleIdAsync(long roleId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysUser>()
            .LeftJoin<SysUserRole>((u, ur) => u.BasicId == ur.UserId)
            .Where((u, ur) => ur.RoleId == roleId)
            .Select((u, ur) => u)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取角色的父角色（用于角色继承）
    /// </summary>
    public async Task<List<SysRole>> GetParentRolesAsync(long roleId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysRole>()
            .LeftJoin<SysRoleHierarchy>((r, rh) => r.BasicId == rh.ParentRoleId)
            .Where((r, rh) => rh.ChildRoleId == roleId)
            .Select((r, rh) => r)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取角色的子角色（用于角色继承）
    /// </summary>
    public async Task<List<SysRole>> GetChildRolesAsync(long roleId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysRole>()
            .LeftJoin<SysRoleHierarchy>((r, rh) => r.BasicId == rh.ChildRoleId)
            .Where((r, rh) => rh.ParentRoleId == roleId)
            .Select((r, rh) => r)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据用户ID获取角色列表
    /// </summary>
    public async Task<List<SysRole>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysRole>()
            .LeftJoin<SysUserRole>((r, ur) => r.BasicId == ur.RoleId)
            .Where((r, ur) => ur.UserId == userId)
            .Select((r, ur) => r)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 检查设置父角色是否会形成循环继承
    /// </summary>
    public async Task<bool> WouldCreateCycleAsync(long roleId, long parentRoleId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 简单检查：如果父角色的父级链中包含当前角色，则会形成循环
        var currentId = parentRoleId;
        var visited = new HashSet<long> { roleId };

        while (currentId != 0)
        {
            if (visited.Contains(currentId))
            {
                return true; // 检测到循环
            }

            visited.Add(currentId);

            var parent = await _dbClient.Queryable<SysRoleHierarchy>()
                .Where(rh => rh.ChildRoleId == currentId)
                .FirstAsync(cancellationToken);

            if (parent == null)
            {
                break;
            }

            currentId = parent.ParentRoleId;
        }

        return false;
    }
}
