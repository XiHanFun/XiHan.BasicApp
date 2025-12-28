#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysTenantRepository
// Guid:da2b3c4d-5e6f-7890-abcd-ef12345678a2
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 5:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Tenants;

/// <summary>
/// 系统租户仓储接口
/// </summary>
public interface ISysTenantRepository : IRepositoryBase<SysTenant, XiHanBasicAppIdType>
{
    /// <summary>
    /// 根据租户编码获取租户
    /// </summary>
    /// <param name="tenantCode">租户编码</param>
    /// <returns></returns>
    Task<SysTenant?> GetByTenantCodeAsync(string tenantCode);

    /// <summary>
    /// 根据域名获取租户
    /// </summary>
    /// <param name="domain">域名</param>
    /// <returns></returns>
    Task<SysTenant?> GetByDomainAsync(string domain);

    /// <summary>
    /// 检查租户编码是否存在
    /// </summary>
    /// <param name="tenantCode">租户编码</param>
    /// <param name="excludeId">排除的租户ID</param>
    /// <returns></returns>
    Task<bool> ExistsByTenantCodeAsync(string tenantCode, XiHanBasicAppIdType? excludeId = null);

    /// <summary>
    /// 检查域名是否存在
    /// </summary>
    /// <param name="domain">域名</param>
    /// <param name="excludeId">排除的租户ID</param>
    /// <returns></returns>
    Task<bool> ExistsByDomainAsync(string domain, XiHanBasicAppIdType? excludeId = null);

    /// <summary>
    /// 获取租户的用户数量
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns></returns>
    Task<int> GetTenantUserCountAsync(XiHanBasicAppIdType tenantId);

    /// <summary>
    /// 获取租户的已使用存储空间(MB)
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns></returns>
    Task<XiHanBasicAppIdType> GetTenantUsedStorageAsync(XiHanBasicAppIdType tenantId);
}
