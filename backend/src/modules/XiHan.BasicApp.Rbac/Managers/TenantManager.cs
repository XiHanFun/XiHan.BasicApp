#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantManager
// Guid:cb2b3c4d-5e6f-7890-abcd-ef12345678b1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 6:15:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Repositories.Abstractions;
using XiHan.Framework.Domain.Services;

namespace XiHan.BasicApp.Rbac.Managers;

/// <summary>
/// 租户领域管理器
/// </summary>
public class TenantManager : DomainService
{
    private readonly ISysTenantRepository _tenantRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="tenantRepository">租户仓储</param>
    public TenantManager(ISysTenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    /// <summary>
    /// 验证租户编码是否唯一
    /// </summary>
    /// <param name="tenantCode">租户编码</param>
    /// <param name="excludeId">排除的租户ID</param>
    /// <returns></returns>
    public async Task<bool> IsTenantCodeUniqueAsync(string tenantCode, RbacIdType? excludeId = null)
    {
        return !await _tenantRepository.ExistsByTenantCodeAsync(tenantCode, excludeId);
    }

    /// <summary>
    /// 验证域名是否唯一
    /// </summary>
    /// <param name="domain">域名</param>
    /// <param name="excludeId">排除的租户ID</param>
    /// <returns></returns>
    public async Task<bool> IsDomainUniqueAsync(string domain, RbacIdType? excludeId = null)
    {
        return !await _tenantRepository.ExistsByDomainAsync(domain, excludeId);
    }

    /// <summary>
    /// 检查租户是否可用
    /// </summary>
    /// <param name="tenant">租户</param>
    /// <returns></returns>
    public bool IsAvailable(SysTenant tenant)
    {
        if (tenant.Status != YesOrNo.Yes)
        {
            return false;
        }

        if (tenant.TenantStatus == TenantStatus.Disabled)
        {
            return false;
        }

        if (tenant.ExpireTime.HasValue && tenant.ExpireTime.Value < DateTimeOffset.Now)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 检查租户用户数是否超限
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns></returns>
    public async Task<bool> IsUserLimitExceededAsync(RbacIdType tenantId)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId);
        if (tenant == null || !tenant.UserLimit.HasValue)
        {
            return false;
        }

        var userCount = await _tenantRepository.GetTenantUserCountAsync(tenantId);
        return userCount >= tenant.UserLimit.Value;
    }

    /// <summary>
    /// 检查租户存储空间是否超限
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns></returns>
    public async Task<bool> IsStorageLimitExceededAsync(RbacIdType tenantId)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId);
        if (tenant == null || !tenant.StorageLimit.HasValue)
        {
            return false;
        }

        var usedStorage = await _tenantRepository.GetTenantUsedStorageAsync(tenantId);
        return usedStorage >= tenant.StorageLimit.Value;
    }
}