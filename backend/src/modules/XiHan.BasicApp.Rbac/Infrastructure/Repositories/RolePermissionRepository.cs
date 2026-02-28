#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RolePermissionRepository
// Guid:6ba17985-0cfe-4abd-8e06-2b4bfc9ebd0d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:55:02
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Rbac.Infrastructure.Repositories;

/// <summary>
/// 角色权限关系仓储实现
/// </summary>
public class RolePermissionRepository : SqlSugarRepositoryBase<SysRolePermission, long>, IRolePermissionRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="clientProvider"></param>
    /// <param name="currentTenant"></param>
    /// <param name="serviceProvider"></param>
    public RolePermissionRepository(
        ISqlSugarClientProvider clientProvider,
        ICurrentTenant currentTenant,
        IServiceProvider serviceProvider)
        : base(clientProvider, currentTenant, serviceProvider)
    {
    }

    /// <summary>
    /// 根据角色ID获取角色权限关系
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<IReadOnlyList<SysRolePermission>> GetByRoleIdAsync(long roleId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        if (tenantId.HasValue)
        {
            return GetListAsync(mapping => mapping.RoleId == roleId && mapping.TenantId == tenantId.Value, cancellationToken);
        }

        return GetListAsync(mapping => mapping.RoleId == roleId, cancellationToken);
    }

    /// <summary>
    /// 根据角色ID删除角色权限关系
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
