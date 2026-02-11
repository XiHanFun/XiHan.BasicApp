#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysTenantRepository
// Guid:d4e5f6a7-b8c9-0123-4567-890abcdef123
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/31 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 系统租户仓储接口
/// </summary>
public interface ISysTenantRepository : IAggregateRootRepository<SysTenant, long>
{
    /// <summary>
    /// 根据租户编码获取租户
    /// </summary>
    /// <param name="tenantCode">租户编码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户实体</returns>
    Task<SysTenant?> GetByTenantCodeAsync(string tenantCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据域名获取租户
    /// </summary>
    /// <param name="domain">域名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户实体</returns>
    Task<SysTenant?> GetByDomainAsync(string domain, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查租户编码是否存在
    /// </summary>
    /// <param name="tenantCode">租户编码</param>
    /// <param name="excludeTenantId">排除的租户ID（用于更新时检查）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    Task<bool> IsTenantCodeExistsAsync(string tenantCode, long? excludeTenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有活跃租户
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户列表</returns>
    Task<List<SysTenant>> GetActivTenantsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取即将过期的租户列表
    /// </summary>
    /// <param name="days">提前天数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户列表</returns>
    Task<List<SysTenant>> GetExpiringTenantsAsync(int days, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存租户
    /// </summary>
    /// <param name="tenant">租户实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>保存的租户实体</returns>
    Task<SysTenant> SaveAsync(SysTenant tenant, CancellationToken cancellationToken = default);

    /// <summary>
    /// 启用租户
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task EnableTenantAsync(long tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 禁用租户
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DisableTenantAsync(long tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 续期租户
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="days">续期天数</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task RenewTenantAsync(long tenantId, int days, CancellationToken cancellationToken = default);
}
