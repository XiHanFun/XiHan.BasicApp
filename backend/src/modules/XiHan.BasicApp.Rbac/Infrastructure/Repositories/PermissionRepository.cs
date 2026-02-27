using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Infrastructure.Repositories;

/// <summary>
/// 权限仓储实现
/// </summary>
public class PermissionRepository : SqlSugarAggregateRepository<SysPermission, long>, IPermissionRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="unitOfWorkManager"></param>
    public PermissionRepository(ISqlSugarDbContext dbContext, IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, unitOfWorkManager)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据权限编码获取权限
    /// </summary>
    /// <param name="permissionCode"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SysPermission?> GetByPermissionCodeAsync(string permissionCode, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);
        var query = _dbContext.CreateQueryable<SysPermission>().Where(permission => permission.PermissionCode == permissionCode);

        if (tenantId.HasValue)
        {
            query = query.Where(permission => permission.TenantId == tenantId.Value);
        }
        else
        {
            query = query.Where(permission => permission.TenantId == null);
        }

        return await query.FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 根据用户ID获取权限
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<SysPermission>> GetUserPermissionsAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var rolePermissionQuery = _dbContext.GetClient().Queryable<SysUserRole, SysRolePermission>(
            (userRole, rolePermission) => userRole.RoleId == rolePermission.RoleId)
            .Where((userRole, rolePermission) =>
                userRole.UserId == userId &&
                userRole.Status == YesOrNo.Yes &&
                rolePermission.Status == YesOrNo.Yes);

        if (tenantId.HasValue)
        {
            rolePermissionQuery = rolePermissionQuery.Where((userRole, rolePermission) =>
                userRole.TenantId == tenantId.Value && rolePermission.TenantId == tenantId.Value);
        }

        var rolePermissionIds = await rolePermissionQuery
            .Select((userRole, rolePermission) => rolePermission.PermissionId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var directPermissionQuery = _dbContext.CreateQueryable<SysUserPermission>()
            .Where(mapping => mapping.UserId == userId && mapping.Status == YesOrNo.Yes);

        if (tenantId.HasValue)
        {
            directPermissionQuery = directPermissionQuery.Where(mapping => mapping.TenantId == tenantId.Value);
        }

        var directPermissions = await directPermissionQuery.ToListAsync(cancellationToken);

        var permissionIdSet = rolePermissionIds.ToHashSet();
        foreach (var mapping in directPermissions)
        {
            if (mapping.PermissionAction == PermissionAction.Grant)
            {
                permissionIdSet.Add(mapping.PermissionId);
            }
            else
            {
                permissionIdSet.Remove(mapping.PermissionId);
            }
        }

        if (permissionIdSet.Count == 0)
        {
            return [];
        }

        var query = _dbContext.CreateQueryable<SysPermission>()
            .Where(permission => permissionIdSet.Contains(permission.BasicId) && permission.Status == YesOrNo.Yes);

        if (tenantId.HasValue)
        {
            query = query.Where(permission => permission.TenantId == tenantId.Value);
        }

        return await query.OrderBy(permission => permission.Sort)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据角色ID获取权限
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<SysPermission>> GetRolePermissionsAsync(long roleId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var permissionIds = await _dbContext.CreateQueryable<SysRolePermission>()
            .Where(mapping => mapping.RoleId == roleId && mapping.Status == YesOrNo.Yes)
            .Select(mapping => mapping.PermissionId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (permissionIds.Count == 0)
        {
            return [];
        }

        var query = _dbContext.CreateQueryable<SysPermission>()
            .Where(permission => permissionIds.Contains(permission.BasicId) && permission.Status == YesOrNo.Yes);

        if (tenantId.HasValue)
        {
            query = query.Where(permission => permission.TenantId == tenantId.Value);
        }

        return await query.OrderBy(permission => permission.Sort)
            .ToListAsync(cancellationToken);
    }
}
