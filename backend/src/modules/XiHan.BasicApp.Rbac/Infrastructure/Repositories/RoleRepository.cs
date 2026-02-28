#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleRepository
// Guid:ca2dd776-5f0c-4617-9009-5bd860aa0943
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:52:12
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Infrastructure.Repositories;

/// <summary>
/// 角色仓储实现
/// </summary>
public class RoleRepository : SqlSugarAggregateRepository<SysRole, long>, IRoleRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="clientProvider"></param>
    /// <param name="currentTenant"></param>
    /// <param name="serviceProvider"></param>
    /// <param name="unitOfWorkManager"></param>
    public RoleRepository(
        ISqlSugarClientProvider clientProvider,
        ICurrentTenant currentTenant,
        IServiceProvider serviceProvider,
        IUnitOfWorkManager unitOfWorkManager)
        : base(clientProvider, currentTenant, serviceProvider, unitOfWorkManager)
    {
    }

    /// <summary>
    /// 根据角色编码获取角色
    /// </summary>
    /// <param name="roleCode"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SysRole?> GetByRoleCodeAsync(string roleCode, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleCode);
        var query = CreateTenantQueryable().Where(role => role.RoleCode == roleCode);

        if (tenantId.HasValue)
        {
            query = query.Where(role => role.TenantId == tenantId.Value);
        }
        else
        {
            query = query.Where(role => role.TenantId == null);
        }

        return await query.FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 判断角色编码是否存在
    /// </summary>
    /// <param name="roleCode"></param>
    /// <param name="excludeRoleId"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<bool> IsRoleCodeExistsAsync(
        string roleCode,
        long? excludeRoleId = null,
        long? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleCode);
        var query = CreateTenantQueryable().Where(role => role.RoleCode == roleCode);

        if (excludeRoleId.HasValue)
        {
            query = query.Where(role => role.BasicId != excludeRoleId.Value);
        }

        if (tenantId.HasValue)
        {
            query = query.Where(role => role.TenantId == tenantId.Value);
        }
        else
        {
            query = query.Where(role => role.TenantId == null);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
