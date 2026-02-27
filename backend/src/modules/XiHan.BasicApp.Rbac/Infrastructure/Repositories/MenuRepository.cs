using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Infrastructure.Repositories;

/// <summary>
/// 菜单仓储实现
/// </summary>
public class MenuRepository : SqlSugarAggregateRepository<SysMenu, long>, IMenuRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="unitOfWorkManager"></param>
    public MenuRepository(ISqlSugarDbContext dbContext, IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, unitOfWorkManager)
    {
        _dbContext = dbContext;
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
        var query = _dbContext.CreateQueryable<SysMenu>()
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
        var menuIds = await _dbContext.CreateQueryable<SysRoleMenu>()
            .Where(mapping => mapping.RoleId == roleId && mapping.Status == YesOrNo.Yes)
            .Select(mapping => mapping.MenuId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (menuIds.Count == 0)
        {
            return [];
        }

        var query = _dbContext.CreateQueryable<SysMenu>()
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
        var roleIds = await _dbContext.CreateQueryable<SysUserRole>()
            .Where(mapping => mapping.UserId == userId && mapping.Status == YesOrNo.Yes)
            .Select(mapping => mapping.RoleId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (roleIds.Count == 0)
        {
            return [];
        }

        var menuIds = await _dbContext.CreateQueryable<SysRoleMenu>()
            .Where(mapping => roleIds.Contains(mapping.RoleId) && mapping.Status == YesOrNo.Yes)
            .Select(mapping => mapping.MenuId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (menuIds.Count == 0)
        {
            return [];
        }

        var query = _dbContext.CreateQueryable<SysMenu>()
            .Where(menu => menuIds.Contains(menu.BasicId) && menu.Status == YesOrNo.Yes);

        if (tenantId.HasValue)
        {
            query = query.Where(menu => menu.TenantId == tenantId.Value);
        }

        return await query.OrderBy(menu => menu.Sort)
            .ToListAsync(cancellationToken);
    }
}
