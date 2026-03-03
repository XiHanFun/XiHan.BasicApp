#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ITenantManager
// Guid:7d3cfa51-c64c-45b6-8733-b7e0ee03200d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:39:50
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Domain.Entities;

namespace XiHan.BasicApp.Rbac.Domain.DomainServices;

/// <summary>
/// 租户领域管理器
/// </summary>
public interface ITenantManager
{
    /// <summary>
    /// 创建租户
    /// </summary>
    Task<SysTenant> CreateAsync(SysTenant tenant, CancellationToken cancellationToken = default);

    /// <summary>
    /// 校验租户编码唯一性
    /// </summary>
    Task EnsureTenantCodeUniqueAsync(string tenantCode, long? excludeTenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 修改租户状态
    /// </summary>
    Task ChangeStatusAsync(SysTenant tenant, TenantStatus status, CancellationToken cancellationToken = default);
}
