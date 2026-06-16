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
    /// 注册账号（自助注册，落到默认租户）
    /// </summary>
    /// <param name="input">注册参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>注册结果</returns>
    Task<RegisterResultDto> RegisterAsync(RegisterRequestDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 申请找回密码（发送一次性重置链接邮件，不立即更改密码）
    /// </summary>
    /// <param name="input">找回密码参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>受理结果</returns>
    Task<PasswordResetResultDto> PasswordResetRequestAsync(PasswordResetRequestDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 消费一次性重置链接令牌并设置新密码
    /// </summary>
    /// <param name="input">令牌 + 新密码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>重置结果</returns>
    Task<PasswordResetConfirmResultDto> ConsumePasswordResetTokenAsync(PasswordResetConfirmDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 密码登录
    /// </summary>
    /// <param name="input">登录参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>登录响应</returns>
    Task<LoginResponseDto> LoginAsync(LoginRequestDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送邮箱登录验证码
    /// </summary>
    /// <param name="input">邮箱登录验证码请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>验证码下发结果</returns>
    Task<VerificationCodeResultDto> EmailLoginCodeAsync(EmailLoginCodeRequestDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 邮箱验证码登录
    /// </summary>
    /// <param name="input">邮箱验证码登录请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>登录令牌</returns>
    Task<LoginTokenDto> EmailLoginAsync(EmailLoginRequestDto input, CancellationToken cancellationToken = default);

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
    /// 切换租户 / 进入平台运维态，重新签发访问令牌
    /// </summary>
    /// <param name="input">切换参数（目标租户，空表示平台态）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>新的登录令牌</returns>
    Task<LoginTokenDto> SwitchTenantAsync(SwitchTenantRequestDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 退出登录
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task LogoutAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 第三方登录编排（登录/绑定）。非公开 API，仅由 OAuth 回调端点服务端调用。
    /// </summary>
    /// <param name="command">编排命令</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>编排结果</returns>
    Task<ExternalLoginResultDto> ExternalLoginAsync(ExternalLoginCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建第三方账号绑定一次性票据（已登录用户调用；浏览器跳转发起绑定时携带身份）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>票据令牌</returns>
    Task<string> CreateOAuthBindTicketAsync(CancellationToken cancellationToken = default);
}
