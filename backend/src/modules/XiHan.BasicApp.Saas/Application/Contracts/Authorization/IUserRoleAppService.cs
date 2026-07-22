// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 用户角色命令应用服务接口
/// </summary>
public interface IUserRoleAppService : IApplicationService
{
    /// <summary>
    /// 授予用户角色
    /// </summary>
    /// <param name="input">授权参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户角色详情</returns>
    Task<UserRoleDetailDto> CreateUserRoleAsync(UserRoleGrantDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户角色
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户角色详情</returns>
    Task<UserRoleDetailDto> UpdateUserRoleAsync(UserRoleUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户角色状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户角色详情</returns>
    Task<UserRoleDetailDto> UpdateUserRoleStatusAsync(UserRoleStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销用户角色
    /// </summary>
    /// <param name="id">用户角色绑定主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteUserRoleAsync(long id, CancellationToken cancellationToken = default);
}
