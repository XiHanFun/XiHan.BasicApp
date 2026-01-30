#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysRoleRepository
// Guid:b0c1d2e3-f4a5-6789-0123-456789b45678
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
/// 角色聚合仓储实现
/// </summary>
public class SysRoleRepository : SqlSugarAggregateRepository<SysRole, long>, ISysRoleRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysRoleRepository(ISqlSugarDbContext dbContext, IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, unitOfWorkManager)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据角色编码获取角色
    /// </summary>
    public async Task<SysRole?> GetByRoleCodeAsync(string roleCode, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.GetClient().Queryable<SysRole>()
            .Where(r => r.RoleCode == roleCode && r.Status == YesOrNo.Yes);

        if (tenantId.HasValue)
        {
            query = query.Where(r => r.TenantId == tenantId.Value);
        }

        return await query.FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 获取角色及层级关系
    /// </summary>
    public async Task<SysRole?> GetWithHierarchyAsync(long roleId, CancellationToken cancellationToken = default)
    {
        return await GetByIdAsync(roleId, cancellationToken);
    }

    /// <summary>
    /// 获取角色及数据权限范围
    /// </summary>
    public async Task<SysRole?> GetWithDataScopeAsync(long roleId, CancellationToken cancellationToken = default)
    {
        return await GetByIdAsync(roleId, cancellationToken);
    }

    /// <summary>
    /// 获取用户的所有角色
    /// </summary>
    public async Task<List<SysRole>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysRole>()
            .InnerJoin<SysUserRole>((r, ur) => r.BasicId == ur.RoleId)
            .Where((r, ur) => ur.UserId == userId && r.Status == YesOrNo.Yes && ur.Status == YesOrNo.Yes)
            .Select(r => r)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取子角色列表
    /// </summary>
    public async Task<List<SysRole>> GetChildRolesAsync(long parentRoleId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysRole>()
            .InnerJoin<SysRoleHierarchy>((r, rh) => r.BasicId == rh.DescendantId)
            .Where((r, rh) => rh.AncestorId == parentRoleId && r.Status == YesOrNo.Yes)
            .Select(r => r)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 批量获取角色
    /// </summary>
    public async Task<List<SysRole>> GetByIdsAsync(IEnumerable<long> roleIds, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysRole>()
            .In(r => r.BasicId, roleIds.ToArray())
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 检查角色编码是否存在
    /// </summary>
    public async Task<bool> ExistsByRoleCodeAsync(string roleCode, long? excludeRoleId = null, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.GetClient().Queryable<SysRole>()
            .Where(r => r.RoleCode == roleCode);

        if (excludeRoleId.HasValue)
        {
            query = query.Where(r => r.BasicId != excludeRoleId.Value);
        }

        if (tenantId.HasValue)
        {
            query = query.Where(r => r.TenantId == tenantId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
