#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserSecurityAppService
// Guid:1d2c3b4a-5e6f-4a7b-8c9d-0e1f2a3b4c5d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 用户安全命令应用服务接口
/// </summary>
public interface IUserSecurityAppService : IApplicationService
{
    /// <summary>
    /// 重置用户密码
    /// </summary>
    /// <param name="input">密码重置参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户安全详情</returns>
    Task<UserSecurityDetailDto> ResetUserPasswordAsync(UserPasswordResetDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 重置用户双因素认证（清除 OTP 绑定）
    /// </summary>
    /// <param name="input">双因素重置参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户安全详情</returns>
    Task<UserSecurityDetailDto> ResetUserTwoFactorAsync(UserTwoFactorResetDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户锁定状态
    /// </summary>
    /// <param name="input">锁定状态参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户安全详情</returns>
    Task<UserSecurityDetailDto> UpdateUserLockAsync(UserLockUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户登录策略
    /// </summary>
    /// <param name="input">登录策略参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户安全详情</returns>
    Task<UserSecurityDetailDto> UpdateUserLoginPolicyAsync(UserLoginPolicyUpdateDto input, CancellationToken cancellationToken = default);
}
