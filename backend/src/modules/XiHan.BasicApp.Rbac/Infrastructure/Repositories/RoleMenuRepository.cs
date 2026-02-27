#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleMenuRepository
// Guid:93e61c30-4298-42fc-8599-e7222b8c3fa2
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:55:18
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Infrastructure.Repositories;

/// <summary>
/// 角色菜单关系仓储实现
/// </summary>
public class RoleMenuRepository : SqlSugarRepositoryBase<SysRoleMenu, long>, IRoleMenuRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext"></param>
    public RoleMenuRepository(ISqlSugarDbContext dbContext)
        : base(dbContext)
    {
    }

    /// <summary>
    /// 根据角色ID获取角色菜单关系
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<IReadOnlyList<SysRoleMenu>> GetByRoleIdAsync(long roleId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        if (tenantId.HasValue)
        {
            return GetListAsync(mapping => mapping.RoleId == roleId && mapping.TenantId == tenantId.Value, cancellationToken);
        }

        return GetListAsync(mapping => mapping.RoleId == roleId, cancellationToken);
    }

    /// <summary>
    /// 根据角色ID删除角色菜单关系
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<bool> RemoveByRoleIdAsync(long roleId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        if (tenantId.HasValue)
        {
            return DeleteAsync(mapping => mapping.RoleId == roleId && mapping.TenantId == tenantId.Value, cancellationToken);
        }

        return DeleteAsync(mapping => mapping.RoleId == roleId, cancellationToken);
    }
}
