// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 权限委托命令应用服务接口
/// </summary>
public interface IPermissionDelegationAppService : IApplicationService
{
    /// <summary>
    /// 创建权限委托
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限委托详情</returns>
    Task<PermissionDelegationDetailDto> CreatePermissionDelegationAsync(PermissionDelegationCreateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新权限委托
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限委托详情</returns>
    Task<PermissionDelegationDetailDto> UpdatePermissionDelegationAsync(PermissionDelegationUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新权限委托状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限委托详情</returns>
    Task<PermissionDelegationDetailDto> UpdatePermissionDelegationStatusAsync(PermissionDelegationStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销权限委托
    /// </summary>
    /// <param name="id">权限委托主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeletePermissionDelegationAsync(long id, CancellationToken cancellationToken = default);
}
