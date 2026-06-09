#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:LoginAuthenticationResult
// Guid:5c4b1df7-52d7-4f67-a2aa-e0e3bfb9c7e1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/17 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 登录认证结果
/// </summary>
public sealed record LoginAuthenticationResult
{
    /// <summary>
    /// 是否认证通过
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// 是否需要双因素认证
    /// </summary>
    public bool RequiresTwoFactor { get; init; }

    /// <summary>
    /// 认证用户
    /// </summary>
    public SysUser? User { get; init; }

    /// <summary>
    /// 用户安全配置
    /// </summary>
    public SysUserSecurity? Security { get; init; }

    /// <summary>
    /// 失败结果
    /// </summary>
    public LoginResult FailureResult { get; init; } = LoginResult.Failed;

    /// <summary>
    /// 失败消息
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    /// <param name="user">用户</param>
    /// <param name="security">安全配置</param>
    /// <returns>登录认证结果</returns>
    public static LoginAuthenticationResult Success(SysUser user, SysUserSecurity? security)
    {
        return new LoginAuthenticationResult
        {
            Succeeded = true,
            User = user,
            Security = security
        };
    }

    /// <summary>
    /// 创建双因素挑战结果
    /// </summary>
    /// <param name="user">用户</param>
    /// <param name="security">安全配置</param>
    /// <returns>登录认证结果</returns>
    public static LoginAuthenticationResult TwoFactorRequired(SysUser user, SysUserSecurity security)
    {
        return new LoginAuthenticationResult
        {
            RequiresTwoFactor = true,
            User = user,
            Security = security,
            FailureResult = LoginResult.RequiresTwoFactor,
            ErrorMessage = "需要双因素认证。"
        };
    }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    /// <param name="failureResult">失败结果</param>
    /// <param name="errorMessage">失败消息</param>
    /// <returns>登录认证结果</returns>
    public static LoginAuthenticationResult Failed(LoginResult failureResult, string? errorMessage)
    {
        return new LoginAuthenticationResult
        {
            FailureResult = failureResult,
            ErrorMessage = errorMessage
        };
    }
}
