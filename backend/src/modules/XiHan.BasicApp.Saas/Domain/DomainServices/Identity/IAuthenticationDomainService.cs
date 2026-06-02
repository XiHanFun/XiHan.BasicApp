#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IAuthenticationDomainService
// Guid:86fdd60b-70f6-4d07-9d0d-598630853cc6
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/17 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 认证领域服务
/// </summary>
public interface IAuthenticationDomainService
{
    /// <summary>
    /// 执行密码登录认证与应用级安全校验
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="password">密码</param>
    /// <param name="tenantId">租户标识</param>
    /// <param name="now">当前时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>登录认证结果</returns>
    Task<LoginAuthenticationResult> AuthenticatePasswordLoginAsync(
        string userName,
        string password,
        long? tenantId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 执行邮箱验证码登录的用户定位与应用级安全校验（验证码本身由应用层校验）
    /// </summary>
    /// <param name="email">邮箱地址</param>
    /// <param name="tenantId">租户标识</param>
    /// <param name="now">当前时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>登录认证结果</returns>
    Task<LoginAuthenticationResult> AuthenticateEmailLoginAsync(
        string email,
        long? tenantId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);
}
