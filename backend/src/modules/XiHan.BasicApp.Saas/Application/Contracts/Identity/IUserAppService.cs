// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 用户命令应用服务接口
/// </summary>
public interface IUserAppService : IApplicationService
{
    /// <summary>
    /// 创建用户
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户详情</returns>
    Task<UserDetailDto> CreateUserAsync(UserCreateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户资料
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户详情</returns>
    Task<UserDetailDto> UpdateUserAsync(UserUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户详情</returns>
    Task<UserDetailDto> UpdateUserStatusAsync(UserStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除用户
    /// </summary>
    /// <param name="id">用户主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteUserAsync(long id, CancellationToken cancellationToken = default);
}
