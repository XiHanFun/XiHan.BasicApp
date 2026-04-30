#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserAppService
// Guid:384f77f8-9c58-4be5-86ae-4055757f165a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
