// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 用户直授权限命令应用服务接口
/// </summary>
public interface IUserPermissionAppService : IApplicationService
{
    /// <summary>
    /// 授予用户直授权限
    /// </summary>
    /// <param name="input">授权参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户直授权限详情</returns>
    Task<UserPermissionDetailDto> CreateUserPermissionAsync(UserPermissionGrantDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户直授权限
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户直授权限详情</returns>
    Task<UserPermissionDetailDto> UpdateUserPermissionAsync(UserPermissionUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户直授权限状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户直授权限详情</returns>
    Task<UserPermissionDetailDto> UpdateUserPermissionStatusAsync(UserPermissionStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销用户直授权限
    /// </summary>
    /// <param name="id">用户直授权限绑定主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteUserPermissionAsync(long id, CancellationToken cancellationToken = default);
}
