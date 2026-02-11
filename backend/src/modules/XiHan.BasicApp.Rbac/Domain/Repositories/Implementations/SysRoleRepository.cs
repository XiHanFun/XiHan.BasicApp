#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysRoleRepository
// Guid:b2c3d4e5-f6a7-8901-2345-67890abcdef1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/31 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Domain.Repositories.Implementations;

/// <summary>
/// 系统角色仓储实现
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
            .Where(r => r.RoleCode == roleCode);

        if (tenantId.HasValue)
        {
            query = query.Where(r => r.TenantId == tenantId.Value);
        }

        return await query.FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 检查角色编码是否存在
    /// </summary>
    public async Task<bool> IsRoleCodeExistsAsync(string roleCode, long? excludeRoleId = null, long? tenantId = null, CancellationToken cancellationToken = default)
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

    /// <summary>
    /// 获取租户下的角色列表
    /// </summary>
    public async Task<List<SysRole>> GetRolesByTenantAsync(long tenantId, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.GetClient().Queryable<SysRole>()
            .Where(r => r.TenantId == tenantId);

        if (isActive.HasValue)
        {
            query = query.Where(r => r.Status == (isActive.Value ? YesOrNo.Yes : YesOrNo.No));
        }

        return await query.OrderBy(r => r.Sort).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户的角色列表
    /// </summary>
    public async Task<List<SysRole>> GetRolesByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysRole>()
            .InnerJoin<SysUserRole>((r, ur) => r.BasicId == ur.RoleId)
            .Where((r, ur) => ur.UserId == userId && ur.Status == YesOrNo.Yes)
            .Where((r, ur) => r.Status == YesOrNo.Yes)
            .Select((r, ur) => r)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 保存角色
    /// </summary>
    public async Task<SysRole> SaveAsync(SysRole role, CancellationToken cancellationToken = default)
    {
        if (role.IsTransient())
        {
            return await AddAsync(role, cancellationToken);
        }
        else
        {
            return await UpdateAsync(role, cancellationToken);
        }
    }

    /// <summary>
    /// 启用角色
    /// </summary>
    public async Task EnableRoleAsync(long roleId, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Updateable<SysRole>()
            .SetColumns(r => r.Status == YesOrNo.Yes)
            .Where(r => r.BasicId == roleId)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 禁用角色
    /// </summary>
    public async Task DisableRoleAsync(long roleId, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Updateable<SysRole>()
            .SetColumns(r => r.Status == YesOrNo.No)
            .Where(r => r.BasicId == roleId)
            .ExecuteCommandAsync(cancellationToken);
    }
}
