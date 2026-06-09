#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ITenantEditionDomainService
// Guid:384f7acf-c1af-45b9-b090-a1c8fd088bd7
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 租户版本领域服务
/// </summary>
public interface ITenantEditionDomainService
{
    /// <summary>
    /// 创建租户版本
    /// </summary>
    Task<TenantEditionCommandResult> CreateTenantEditionAsync(TenantEditionCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 授予租户版本权限
    /// </summary>
    Task<TenantEditionPermissionCommandResult> GrantTenantEditionPermissionAsync(TenantEditionPermissionGrantCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销租户版本权限
    /// </summary>
    Task RevokeTenantEditionPermissionAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置默认租户版本
    /// </summary>
    Task<TenantEditionCommandResult> UpdateDefaultTenantEditionAsync(TenantEditionDefaultChangeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新租户版本
    /// </summary>
    Task<TenantEditionCommandResult> UpdateTenantEditionAsync(TenantEditionUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新租户版本权限状态
    /// </summary>
    Task<TenantEditionPermissionCommandResult> UpdateTenantEditionPermissionStatusAsync(TenantEditionPermissionStatusChangeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新租户版本状态
    /// </summary>
    Task<TenantEditionCommandResult> UpdateTenantEditionStatusAsync(TenantEditionStatusChangeCommand command, CancellationToken cancellationToken = default);
}
