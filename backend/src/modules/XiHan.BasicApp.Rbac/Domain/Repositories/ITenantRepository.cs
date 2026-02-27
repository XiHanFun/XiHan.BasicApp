#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ITenantRepository
// Guid:a9695328-619b-496b-9783-4c71a971659a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:34:49
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 租户聚合仓储接口
/// </summary>
public interface ITenantRepository : IAggregateRootRepository<SysTenant, long>
{
    /// <summary>
    /// 根据租户编码获取租户
    /// </summary>
    Task<SysTenant?> GetByTenantCodeAsync(string tenantCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 校验租户编码是否已存在
    /// </summary>
    Task<bool> IsTenantCodeExistsAsync(string tenantCode, long? excludeTenantId = null, CancellationToken cancellationToken = default);
}
