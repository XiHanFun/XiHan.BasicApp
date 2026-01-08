#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ResourceRepository
// Guid:e5f6a7b8-c9d0-4e5f-1a2b-4c5d6e7f8a9b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/8 0:00:00
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
/// 资源仓储实现
/// </summary>
public class ResourceRepository : SqlSugarAggregateRepository<SysResource, long>, IResourceRepository
{
    private readonly ISqlSugarClient _dbClient;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ResourceRepository(ISqlSugarDbContext dbContext, IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, unitOfWorkManager)
    {
        _dbClient = dbContext.GetClient();
    }

    /// <summary>
    /// 根据资源编码查询资源
    /// </summary>
    public async Task<SysResource?> GetByResourceCodeAsync(string resourceCode, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysResource>()
            .FirstAsync(r => r.ResourceCode == resourceCode, cancellationToken);
    }

    /// <summary>
    /// 检查资源编码是否存在
    /// </summary>
    public async Task<bool> ExistsByResourceCodeAsync(string resourceCode, long? excludeResourceId = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = _dbClient.Queryable<SysResource>()
            .Where(r => r.ResourceCode == resourceCode);

        if (excludeResourceId.HasValue)
        {
            query = query.Where(r => r.BasicId != excludeResourceId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// 根据资源类型获取资源列表
    /// </summary>
    public async Task<List<SysResource>> GetByResourceTypeAsync(ResourceType resourceType, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysResource>()
            .Where(r => r.ResourceType == resourceType)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据路径查询资源
    /// </summary>
    public async Task<SysResource?> GetByApiPathAsync(string resourcePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = _dbClient.Queryable<SysResource>()
            .Where(r => r.ResourcePath == resourcePath);

        return await query.FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户可访问的资源
    /// </summary>
    public async Task<List<SysResource>> GetByUserIdAsync(long userId, ResourceType? resourceType = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 通过用户权限获取资源
        var query = _dbClient.Queryable<SysResource>()
            .LeftJoin<SysPermission>((r, p) => r.BasicId == p.ResourceId)
            .LeftJoin<SysUserPermission>((r, p, up) => p.BasicId == up.PermissionId)
            .Where((r, p, up) => up.UserId == userId);

        if (resourceType.HasValue)
        {
            query = query.Where((r, p, up) => r.ResourceType == resourceType.Value);
        }

        var directResources = await query
            .Select((r, p, up) => r)
            .ToListAsync(cancellationToken);

        // 通过角色权限获取资源
        var roleQuery = _dbClient.Queryable<SysResource>()
            .LeftJoin<SysPermission>((r, p) => r.BasicId == p.ResourceId)
            .LeftJoin<SysRolePermission>((r, p, rp) => p.BasicId == rp.PermissionId)
            .LeftJoin<SysUserRole>((r, p, rp, ur) => rp.RoleId == ur.RoleId)
            .Where((r, p, rp, ur) => ur.UserId == userId);

        if (resourceType.HasValue)
        {
            roleQuery = roleQuery.Where((r, p, rp, ur) => r.ResourceType == resourceType.Value);
        }

        var roleResources = await roleQuery
            .Select((r, p, rp, ur) => r)
            .ToListAsync(cancellationToken);

        // 合并并去重
        var allResources = directResources.Concat(roleResources)
            .GroupBy(r => r.BasicId)
            .Select(g => g.First())
            .ToList();

        return allResources;
    }

    /// <summary>
    /// 获取角色可访问的资源
    /// </summary>
    public async Task<List<SysResource>> GetByRoleIdAsync(long roleId, ResourceType? resourceType = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = _dbClient.Queryable<SysResource>()
            .LeftJoin<SysPermission>((r, p) => r.BasicId == p.ResourceId)
            .LeftJoin<SysRolePermission>((r, p, rp) => p.BasicId == rp.PermissionId)
            .Where((r, p, rp) => rp.RoleId == roleId);

        if (resourceType.HasValue)
        {
            query = query.Where((r, p, rp) => r.ResourceType == resourceType.Value);
        }

        return await query
            .Select((r, p, rp) => r)
            .ToListAsync(cancellationToken);
    }
}
