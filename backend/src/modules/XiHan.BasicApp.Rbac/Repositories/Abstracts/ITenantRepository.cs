#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ITenantRepository
// Guid:f6a7b8c9-d0e1-4f5a-2b3c-5d6e7f8a9b0c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// 租户仓储接口
/// </summary>
public interface ITenantRepository : IAggregateRootRepository<SysTenant, long>
{
    /// <summary>
    /// 根据租户编码查询租户
    /// </summary>
    /// <param name="tenantCode">租户编码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户实体</returns>
    Task<SysTenant?> GetByTenantCodeAsync(string tenantCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据租户域名查询租户
    /// </summary>
    /// <param name="domain">域名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户实体</returns>
    Task<SysTenant?> GetByDomainAsync(string domain, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查租户编码是否存在
    /// </summary>
    /// <param name="tenantCode">租户编码</param>
    /// <param name="excludeTenantId">排除的租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    Task<bool> ExistsByTenantCodeAsync(string tenantCode, long? excludeTenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查域名是否存在
    /// </summary>
    /// <param name="domain">域名</param>
    /// <param name="excludeTenantId">排除的租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    Task<bool> ExistsByDomainAsync(string domain, long? excludeTenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取启用的租户列表
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户列表</returns>
    Task<List<SysTenant>> GetActiveTenantsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取租户的用户数量
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户数量</returns>
    Task<int> GetUserCountAsync(long tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取租户已使用的存储空间大小（MB）
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已使用的存储空间大小（MB）</returns>
    Task<long> GetUsedStorageAsync(long tenantId, CancellationToken cancellationToken = default);
}
