#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysTenantRepository
// Guid:f6a7b8c9-d0e1-2345-6789-012345f01234
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/30 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// 租户仓储接口
/// </summary>
public interface ISysTenantRepository : IAggregateRootRepository<SysTenant, long>
{
    /// <summary>
    /// 根据租户编码获取租户
    /// </summary>
    Task<SysTenant?> GetByTenantCodeAsync(string tenantCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据域名获取租户
    /// </summary>
    Task<SysTenant?> GetByDomainAsync(string domain, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有启用的租户
    /// </summary>
    Task<List<SysTenant>> GetActiveTenantsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查租户编码是否存在
    /// </summary>
    Task<bool> ExistsByTenantCodeAsync(string tenantCode, long? excludeTenantId = null, CancellationToken cancellationToken = default);
}
