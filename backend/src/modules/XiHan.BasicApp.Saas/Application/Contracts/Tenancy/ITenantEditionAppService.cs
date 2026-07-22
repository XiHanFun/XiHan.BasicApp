// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 租户版本命令应用服务接口
/// </summary>
public interface ITenantEditionAppService : IApplicationService
{
    /// <summary>
    /// 创建租户版本
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户版本详情</returns>
    Task<TenantEditionDetailDto> CreateTenantEditionAsync(TenantEditionCreateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新租户版本
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户版本详情</returns>
    Task<TenantEditionDetailDto> UpdateTenantEditionAsync(TenantEditionUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新租户版本状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户版本详情</returns>
    Task<TenantEditionDetailDto> UpdateTenantEditionStatusAsync(TenantEditionStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置默认租户版本
    /// </summary>
    /// <param name="input">默认版本更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户版本详情</returns>
    Task<TenantEditionDetailDto> UpdateDefaultTenantEditionAsync(TenantEditionDefaultUpdateDto input, CancellationToken cancellationToken = default);

    #region EditionPermissions

    /// <summary>
    /// 授予租户版本权限
    /// </summary>
    /// <param name="input">授权参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户版本权限详情</returns>
    Task<TenantEditionPermissionDetailDto> GrantTenantEditionPermissionAsync(TenantEditionPermissionGrantDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新租户版本权限状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户版本权限详情</returns>
    Task<TenantEditionPermissionDetailDto> UpdateTenantEditionPermissionStatusAsync(TenantEditionPermissionStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销租户版本权限
    /// </summary>
    /// <param name="id">租户版本权限绑定主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task RevokeTenantEditionPermissionAsync(long id, CancellationToken cancellationToken = default);

    #endregion EditionPermissions
}
