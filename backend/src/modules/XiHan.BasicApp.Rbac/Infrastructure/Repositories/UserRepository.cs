#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserRepository
// Guid:7dcf161c-69ea-4fa5-ab72-8c524954d825
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:51:54
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
/// 用户仓储实现
/// </summary>
public class UserRepository : SqlSugarAggregateRepository<SysUser, long>, IUserRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="clientProvider"></param>
    /// <param name="currentTenant"></param>
    /// <param name="serviceProvider"></param>
    /// <param name="unitOfWorkManager"></param>
    public UserRepository(
        ISqlSugarClientProvider clientProvider,
        ICurrentTenant currentTenant,
        IServiceProvider serviceProvider,
        IUnitOfWorkManager unitOfWorkManager)
        : base(clientProvider, currentTenant, serviceProvider, unitOfWorkManager)
    {
    }

    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    /// <param name="userName"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SysUser?> GetByUserNameAsync(string userName, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);

        var query = CreateTenantQueryable()
            .Where(user => user.UserName == userName);

        if (tenantId.HasValue)
        {
            query = query.Where(user => user.TenantId == tenantId.Value);
        }
        else
        {
            query = query.Where(user => user.TenantId == null);
        }

        return await query.FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 判断用户名是否存在
    /// </summary>
    /// <param name="userName"></param>
    /// <param name="excludeUserId"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<bool> IsUserNameExistsAsync(
        string userName,
        long? excludeUserId = null,
        long? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);

        var query = CreateTenantQueryable()
            .Where(user => user.UserName == userName);

        if (excludeUserId.HasValue)
        {
            query = query.Where(user => user.BasicId != excludeUserId.Value);
        }

        if (tenantId.HasValue)
        {
            query = query.Where(user => user.TenantId == tenantId.Value);
        }
        else
        {
            query = query.Where(user => user.TenantId == null);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
