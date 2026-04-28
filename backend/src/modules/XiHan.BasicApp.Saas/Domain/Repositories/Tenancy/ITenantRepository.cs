#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ITenantRepository
// Guid:aa884bc0-7369-4c8d-a6a0-d94a51ef5d7f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 租户仓储接口
/// </summary>
public interface ITenantRepository : ISaasAggregateRepository<SysTenant>
{
    /// <summary>
    /// 根据租户编码获取租户
    /// </summary>
    Task<SysTenant?> GetByCodeAsync(string tenantCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据域名获取租户
    /// </summary>
    Task<SysTenant?> GetByDomainAsync(string domain, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查租户编码是否存在
    /// </summary>
    Task<bool> ExistsTenantCodeAsync(string tenantCode, long? excludeId = null, CancellationToken cancellationToken = default);
}
