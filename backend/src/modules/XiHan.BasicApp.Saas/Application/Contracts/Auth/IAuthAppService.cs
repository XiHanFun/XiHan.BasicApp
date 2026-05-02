#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IAuthAppService
// Guid:1fb43b6b-d2cb-4761-91d2-fabf2beef247
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/02 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 认证应用服务接口
/// </summary>
public interface IAuthAppService : IApplicationService
{
    /// <summary>
    /// 获取登录配置
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>登录配置</returns>
    Task<LoginConfigDto> GetLoginConfigAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 密码登录
    /// </summary>
    /// <param name="input">登录参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>登录响应</returns>
    Task<LoginResponseDto> LoginAsync(LoginRequestDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 刷新访问令牌
    /// </summary>
    /// <param name="input">刷新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>新令牌</returns>
    Task<LoginTokenDto> RefreshTokenAsync(RefreshTokenRequestDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>当前用户信息</returns>
    Task<UserInfoDto> GetUserInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前用户权限
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>当前用户权限</returns>
    Task<PermissionInfoDto> GetPermissionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 退出登录
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task LogoutAsync(CancellationToken cancellationToken = default);
}
