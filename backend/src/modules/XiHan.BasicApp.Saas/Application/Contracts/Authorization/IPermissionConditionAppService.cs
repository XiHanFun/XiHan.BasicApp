// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 权限条件命令应用服务接口
/// </summary>
public interface IPermissionConditionAppService : IApplicationService
{
    /// <summary>
    /// 创建权限 ABAC 条件
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>ABAC 条件详情</returns>
    Task<PermissionConditionDetailDto> CreatePermissionConditionAsync(PermissionConditionCreateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新权限 ABAC 条件
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>ABAC 条件详情</returns>
    Task<PermissionConditionDetailDto> UpdatePermissionConditionAsync(PermissionConditionUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新权限 ABAC 条件状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>ABAC 条件详情</returns>
    Task<PermissionConditionDetailDto> UpdatePermissionConditionStatusAsync(PermissionConditionStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除权限 ABAC 条件
    /// </summary>
    /// <param name="id">ABAC 条件主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeletePermissionConditionAsync(long id, CancellationToken cancellationToken = default);
}
