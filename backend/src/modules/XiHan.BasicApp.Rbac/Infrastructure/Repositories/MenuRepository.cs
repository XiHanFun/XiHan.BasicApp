#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MenuRepository
// Guid:50645114-3d16-496e-a535-63c1756d8fb8
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:53:17
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Infrastructure.Repositories;

/// <summary>
/// 菜单仓储实现
/// </summary>
public class MenuRepository : SqlSugarAggregateRepository<SysMenu, long>, IMenuRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="clientProvider"></param>
    /// <param name="currentTenant"></param>
    /// <param name="serviceProvider"></param>
    /// <param name="unitOfWorkManager"></param>
    public MenuRepository(
        ISqlSugarClientProvider clientProvider,
        ICurrentTenant currentTenant,
        IServiceProvider serviceProvider,
        IUnitOfWorkManager unitOfWorkManager)
        : base(clientProvider, currentTenant, serviceProvider, unitOfWorkManager)
    {
    }

    /// <summary>
    /// 根据菜单编码获取菜单
    /// </summary>
    /// <param name="menuCode"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SysMenu?> GetByMenuCodeAsync(string menuCode, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(menuCode);
        var query = CreateTenantQueryable()
            .Where(menu => menu.MenuCode == menuCode);

        if (tenantId.HasValue)
        {
            query = query.Where(menu => menu.TenantId == tenantId.Value);
        }
        else
        {
            query = query.Where(menu => menu.TenantId == null);
        }

        return await query.FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 根据角色ID获取菜单
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<SysMenu>> GetRoleMenusAsync(long roleId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var menuIds = await CreateTenantQueryable<SysRoleMenu>()
            .Where(mapping => mapping.RoleId == roleId && mapping.Status == YesOrNo.Yes)
            .Select(mapping => mapping.MenuId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (menuIds.Count == 0)
        {
            return [];
        }

        var query = CreateTenantQueryable()
            .Where(menu => menuIds.Contains(menu.BasicId) && menu.Status == YesOrNo.Yes);

        if (tenantId.HasValue)
        {
            query = query.Where(menu => menu.TenantId == tenantId.Value);
        }

        return await query.OrderBy(menu => menu.Sort)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据用户ID获取菜单
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<SysMenu>> GetUserMenusAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var roleIds = await CreateTenantQueryable<SysUserRole>()
            .Where(mapping => mapping.UserId == userId && mapping.Status == YesOrNo.Yes)
            .Select(mapping => mapping.RoleId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (roleIds.Count == 0)
        {
            return [];
        }

        var menuIds = await CreateTenantQueryable<SysRoleMenu>()
            .Where(mapping => roleIds.Contains(mapping.RoleId) && mapping.Status == YesOrNo.Yes)
            .Select(mapping => mapping.MenuId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (menuIds.Count == 0)
        {
            return [];
        }

        var query = CreateTenantQueryable()
            .Where(menu => menuIds.Contains(menu.BasicId) && menu.Status == YesOrNo.Yes);

        if (tenantId.HasValue)
        {
            query = query.Where(menu => menu.TenantId == tenantId.Value);
        }

        return await query.OrderBy(menu => menu.Sort)
            .ToListAsync(cancellationToken);
    }
}
