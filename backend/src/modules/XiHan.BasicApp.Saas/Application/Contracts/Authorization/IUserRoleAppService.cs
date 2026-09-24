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
    /// 批量变更用户角色（一次性提交授予与撤销，单事务）
    /// </summary>
    /// <param name="input">批量变更参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task BatchUpdateUserRolesAsync(UserRoleBatchUpdateDto input, CancellationToken cancellationToken = default);

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
}
