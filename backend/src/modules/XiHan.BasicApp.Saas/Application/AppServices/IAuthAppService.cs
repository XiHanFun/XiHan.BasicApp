#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IAuthAppService
// Guid:3f99078f-71c5-4fa0-a685-c8c3ad90addf
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/03 15:35:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.UseCases.Commands;
using XiHan.BasicApp.Saas.Application.UseCases.Queries;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 认证应用服务
/// </summary>
public interface IAuthAppService : IApplicationService
{
    /// <summary>
    /// 获取登录配置
    /// </summary>
    Task<LoginConfigDto> GetLoginConfigAsync();

    /// <summary>
    /// 登录（返回令牌或双因素验证挑战）
    /// </summary>
    Task<LoginResponseDto> LoginAsync(UserLoginCommand command);

    /// <summary>
    /// 用户注册
    /// </summary>
    Task RegisterAsync(UserRegisterCommand command);

    /// <summary>
    /// 发送手机登录验证码
    /// </summary>
    Task<AuthVerificationCodeDto> SendPhoneLoginCodeAsync(SendPhoneLoginCodeCommand command);

    /// <summary>
    /// 手机验证码登录
    /// </summary>
    Task<AuthTokenDto> PhoneLoginAsync(PhoneLoginCommand command);

    /// <summary>
    /// 申请重置密码
    /// </summary>
    Task<PasswordResetResultDto> RequestPasswordResetAsync(RequestPasswordResetCommand command);

    /// <summary>
    /// 刷新令牌
    /// </summary>
    Task<AuthTokenDto> RefreshTokenAsync(RefreshTokenCommand command);

    /// <summary>
    /// 获取当前用户
    /// </summary>
    Task<CurrentUserDto> GetCurrentUserAsync();

    /// <summary>
    /// 获取权限上下文
    /// </summary>
    Task<AuthPermissionDto> GetPermissionsAsync();

    /// <summary>
    /// 退出登录
    /// </summary>
    Task LogoutAsync();

    /// <summary>
    /// 修改密码
    /// </summary>
    Task ChangePasswordAsync(ChangePasswordCommand command);

    /// <summary>
    /// 获取用户权限编码
    /// </summary>
    Task<IReadOnlyCollection<string>> GetPermissionCodesAsync(UserPermissionQuery query);

    /// <summary>
    /// 获取用户数据范围部门ID
    /// </summary>
    /// <remarks>
    /// 空集合表示不限部门（全量数据范围）。
    /// </remarks>
    Task<IReadOnlyCollection<long>> GetDataScopeDepartmentIdsAsync(UserDataScopeQuery query);

    /// <summary>
    /// 获取当前用户完整档案
    /// </summary>
    Task<UserProfileDto> GetProfileAsync();

    /// <summary>
    /// 更新当前用户个人资料
    /// </summary>
    Task UpdateProfileAsync(UpdateProfileCommand command);

    /// <summary>
    /// 获取当前用户活跃会话列表
    /// </summary>
    Task<IReadOnlyList<UserSessionItemDto>> GetSessionsAsync();

    /// <summary>
    /// 撤销指定会话
    /// </summary>
    Task RevokeSessionAsync(RevokeSessionCommand command);

    /// <summary>
    /// 撤销当前用户其他所有会话
    /// </summary>
    Task RevokeOtherSessionsAsync();

    /// <summary>
    /// 初始化双因素认证（生成密钥和二维码URI，尚未启用）
    /// </summary>
    Task<TwoFactorSetupResultDto> Setup2FAAsync();

    /// <summary>
    /// 验证并启用双因素认证
    /// </summary>
    Task Enable2FAAsync(Enable2FACommand command);

    /// <summary>
    /// 验证并禁用双因素认证
    /// </summary>
    Task Disable2FAAsync(Disable2FACommand command);

    /// <summary>
    /// 处理第三方登录（查找或自动创建用户，签发令牌）
    /// </summary>
    Task<AuthTokenDto> ExternalLoginAsync(ExternalLoginCommand command);

    /// <summary>
    /// 发送邮箱验证码
    /// </summary>
    Task<AuthVerificationCodeDto> SendEmailVerifyCodeAsync();

    /// <summary>
    /// 验证邮箱
    /// </summary>
    Task VerifyEmailAsync(VerifyEmailCommand command);

    /// <summary>
    /// 停用当前账号
    /// </summary>
    Task DeactivateAccountAsync(DeactivateAccountCommand command);

    /// <summary>
    /// 注销当前账号（永久删除）
    /// </summary>
    Task DeleteAccountAsync(DeleteAccountCommand command);
}
