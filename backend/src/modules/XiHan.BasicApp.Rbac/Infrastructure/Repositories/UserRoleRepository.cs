#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserRoleRepository
// Guid:456c51ba-d1a3-40cf-b133-22aa1fc4e426
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:54:46
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Rbac.Infrastructure.Repositories;

/// <summary>
/// 用户角色关系仓储实现
/// </summary>
public class UserRoleRepository : SqlSugarRepositoryBase<SysUserRole, long>, IUserRoleRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="clientProvider"></param>
    /// <param name="currentTenant"></param>
    /// <param name="serviceProvider"></param>
    public UserRoleRepository(
        ISqlSugarClientProvider clientProvider,
        ICurrentTenant currentTenant,
        IServiceProvider serviceProvider)
        : base(clientProvider, currentTenant, serviceProvider)
    {
    }

    /// <summary>
    /// 根据用户ID获取用户角色关系
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<IReadOnlyList<SysUserRole>> GetByUserIdAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        if (tenantId.HasValue)
        {
            return GetListAsync(mapping => mapping.UserId == userId && mapping.TenantId == tenantId.Value, cancellationToken);
        }

        return GetListAsync(mapping => mapping.UserId == userId, cancellationToken);
    }

    /// <summary>
    /// 根据用户ID删除用户角色关系
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<bool> RemoveByUserIdAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        if (tenantId.HasValue)
        {
            return DeleteAsync(mapping => mapping.UserId == userId && mapping.TenantId == tenantId.Value, cancellationToken);
        }

        return DeleteAsync(mapping => mapping.UserId == userId, cancellationToken);
    }
}
