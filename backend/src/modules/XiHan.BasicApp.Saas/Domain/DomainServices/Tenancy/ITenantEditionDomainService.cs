// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
    /// 批量变更租户版本权限（一次性提交授予、撤销与启停）
    /// </summary>
    /// <param name="command">批量变更命令</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>本次实际发生的条数</returns>
    Task<TenantEditionPermissionBatchUpdateResult> BatchUpdateTenantEditionPermissionsAsync(TenantEditionPermissionBatchUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销租户版本权限
    /// </summary>
    /// <param name="id">租户版本权限绑定主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>被撤销的版本权限映射（调用方据此回收受影响租户的越界授权）</returns>
    Task<TenantEditionPermissionCommandResult> RevokeTenantEditionPermissionAsync(long id, CancellationToken cancellationToken = default);

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
