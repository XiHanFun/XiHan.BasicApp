#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserSecurityRepository
// Guid:ca3b11b3-dc72-494a-9039-8477d2f91291
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:56:03
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Infrastructure.Repositories;

/// <summary>
/// 用户安全状态仓储实现
/// </summary>
public class UserSecurityRepository : SqlSugarRepositoryBase<SysUserSecurity, long>, IUserSecurityRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext"></param>
    public UserSecurityRepository(ISqlSugarDbContext dbContext)
        : base(dbContext)
    {
    }

    /// <summary>
    /// 根据用户ID获取用户安全状态
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<SysUserSecurity?> GetByUserIdAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        if (tenantId.HasValue)
        {
            return GetFirstAsync(entity => entity.UserId == userId && entity.TenantId == tenantId.Value, cancellationToken);
        }

        return GetFirstAsync(entity => entity.UserId == userId, cancellationToken);
    }
}
