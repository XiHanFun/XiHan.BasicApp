// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 权限申请命令应用服务接口
/// </summary>
public interface IPermissionRequestAppService : IApplicationService
{
    /// <summary>
    /// 创建权限申请
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限申请详情</returns>
    Task<PermissionRequestDetailDto> CreatePermissionRequestAsync(PermissionRequestCreateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新权限申请
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限申请详情</returns>
    Task<PermissionRequestDetailDto> UpdatePermissionRequestAsync(PermissionRequestUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新权限申请状态（手工驳回或撤回，同时中止进行中的审批流程）
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限申请详情</returns>
    Task<PermissionRequestDetailDto> UpdatePermissionRequestStatusAsync(PermissionRequestStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤回权限申请（同时撤回进行中的审批流程）
    /// </summary>
    /// <param name="id">权限申请主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeletePermissionRequestAsync(long id, CancellationToken cancellationToken = default);
}
