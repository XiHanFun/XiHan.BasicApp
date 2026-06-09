#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserPermissionAppService
// Guid:4a5b6c7d-8e9f-4a0b-1c2d-3e4f5a6b7c8d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
