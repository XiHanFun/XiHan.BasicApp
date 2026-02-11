#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ITenantManagementService
// Guid:e7f8a9bc-def1-2345-6789-0abcdef12345
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/31 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Services.Abstracts;

namespace XiHan.BasicApp.Rbac.Domain.Services;

/// <summary>
/// 租户管理领域服务接口
/// </summary>
public interface ITenantManagementService : IDomainService
{
    /// <summary>
    /// 验证租户是否处于活跃状态
    /// </summary>
    /// <param name="tenant">租户实体</param>
    /// <returns>是否活跃</returns>
    bool IsTenantActive(SysTenant tenant);

    /// <summary>
    /// 检查租户是否已过期
    /// </summary>
    /// <param name="tenant">租户实体</param>
    /// <returns>是否已过期</returns>
    bool IsTenantExpired(SysTenant tenant);

    /// <summary>
    /// 检查租户用户数是否超限
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否超限</returns>
    Task<bool> IsUserLimitExceededAsync(long tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查租户存储空间是否超限
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否超限</returns>
    Task<bool> IsStorageLimitExceededAsync(long tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 初始化租户数据
    /// </summary>
    /// <param name="tenant">租户实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task InitializeTenantDataAsync(SysTenant tenant, CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证租户数据库配置
    /// </summary>
    /// <param name="tenant">租户实体</param>
    /// <returns>是否有效</returns>
    bool ValidateDatabaseConfiguration(SysTenant tenant);

    /// <summary>
    /// 生成租户连接字符串
    /// </summary>
    /// <param name="tenant">租户实体</param>
    /// <returns>连接字符串</returns>
    string GenerateConnectionString(SysTenant tenant);

    /// <summary>
    /// 检查租户编码是否重复
    /// </summary>
    /// <param name="tenantCode">租户编码</param>
    /// <param name="excludeTenantId">排除的租户ID（用于更新时检查）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否重复</returns>
    Task<bool> IsTenantCodeDuplicateAsync(string tenantCode, long? excludeTenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查域名是否重复
    /// </summary>
    /// <param name="domain">域名</param>
    /// <param name="excludeTenantId">排除的租户ID（用于更新时检查）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否重复</returns>
    Task<bool> IsDomainDuplicateAsync(string domain, long? excludeTenantId = null, CancellationToken cancellationToken = default);
}
