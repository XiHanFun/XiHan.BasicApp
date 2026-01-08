#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantDomainService
// Guid:f7a8b9c0-d1e2-4f5a-6b7c-8d9e0f1a2b3c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Domain.Services;

namespace XiHan.BasicApp.Rbac.Services.Domain;

/// <summary>
/// 租户领域服务
/// 处理租户相关的业务逻辑（租户验证、限额检查等）
/// </summary>
public class TenantDomainService : DomainService
{
    private readonly ITenantRepository _tenantRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TenantDomainService(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    /// <summary>
    /// 检查租户是否可用
    /// </summary>
    /// <param name="tenant">租户</param>
    /// <returns>是否可用</returns>
    public static bool IsAvailable(SysTenant tenant)
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
    /// 验证租户编码是否唯一
    /// </summary>
    /// <param name="tenantCode">租户编码</param>
    /// <param name="excludeId">排除的租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否唯一</returns>
    public async Task<bool> IsTenantCodeUniqueAsync(string tenantCode, long? excludeId = null, CancellationToken cancellationToken = default)
    {
        var exists = await _tenantRepository.ExistsByTenantCodeAsync(tenantCode, excludeId, cancellationToken);
        return !exists;
    }

    /// <summary>
    /// 验证域名是否唯一
    /// </summary>
    /// <param name="domain">域名</param>
    /// <param name="excludeId">排除的租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否唯一</returns>
    public async Task<bool> IsDomainUniqueAsync(string domain, long? excludeId = null, CancellationToken cancellationToken = default)
    {
        var exists = await _tenantRepository.ExistsByDomainAsync(domain, excludeId, cancellationToken);
        return !exists;
    }

    /// <summary>
    /// 检查租户用户数是否超限
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否超限</returns>
    public async Task<bool> IsUserLimitExceededAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        LogDomainOperation(nameof(IsUserLimitExceededAsync), new { tenantId });

        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null || !tenant.UserLimit.HasValue)
        {
            return false;
        }

        var userCount = await _tenantRepository.GetUserCountAsync(tenantId, cancellationToken);
        var isExceeded = userCount >= tenant.UserLimit.Value;

        if (isExceeded)
        {
            Logger.LogWarning("租户 {TenantId} 用户数已达上限: {UserCount}/{UserLimit}",
                tenantId, userCount, tenant.UserLimit.Value);
        }

        return isExceeded;
    }

    /// <summary>
    /// 检查租户存储空间是否超限
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否超限</returns>
    public async Task<bool> IsStorageLimitExceededAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        LogDomainOperation(nameof(IsStorageLimitExceededAsync), new { tenantId });

        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null || !tenant.StorageLimit.HasValue)
        {
            return false;
        }

        var usedStorage = await _tenantRepository.GetUsedStorageAsync(tenantId, cancellationToken);
        var isExceeded = usedStorage >= tenant.StorageLimit.Value;

        if (isExceeded)
        {
            Logger.LogWarning("租户 {TenantId} 存储空间已达上限: {UsedStorage}/{StorageLimit}",
                tenantId, usedStorage, tenant.StorageLimit.Value);
        }

        return isExceeded;
    }

    /// <summary>
    /// 获取租户剩余用户配额
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>剩余配额（null表示无限制）</returns>
    public async Task<int?> GetRemainingUserQuotaAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null || !tenant.UserLimit.HasValue)
        {
            return null; // 无限制
        }

        var userCount = await _tenantRepository.GetUserCountAsync(tenantId, cancellationToken);
        return Math.Max(0, tenant.UserLimit.Value - userCount);
    }

    /// <summary>
    /// 获取租户剩余存储配额（字节）
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>剩余配额（null表示无限制）</returns>
    public async Task<long?> GetRemainingStorageQuotaAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null || !tenant.StorageLimit.HasValue)
        {
            return null; // 无限制
        }

        var usedStorage = await _tenantRepository.GetUsedStorageAsync(tenantId, cancellationToken);
        return Math.Max(0, tenant.StorageLimit.Value - usedStorage);
    }

    /// <summary>
    /// 检查租户是否可以删除
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否可以删除</returns>
    public async Task<bool> CanDeleteAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        LogDomainOperation(nameof(CanDeleteAsync), new { tenantId });

        // 检查租户是否存在
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            throw new KeyNotFoundException($"租户 {tenantId} 不存在");
        }

        // 检查租户下是否还有用户
        var userCount = await _tenantRepository.GetUserCountAsync(tenantId, cancellationToken);
        if (userCount > 0)
        {
            throw new InvalidOperationException($"租户下还有 {userCount} 个用户，无法删除");
        }

        Logger.LogInformation("租户 {TenantId} 可以删除", tenantId);
        return true;
    }
}
