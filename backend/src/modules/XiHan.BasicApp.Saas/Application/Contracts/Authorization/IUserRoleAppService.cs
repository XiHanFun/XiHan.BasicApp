#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserRoleAppService
// Guid:28dfb3a5-f3b4-4902-822a-f15e4c83e21a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
