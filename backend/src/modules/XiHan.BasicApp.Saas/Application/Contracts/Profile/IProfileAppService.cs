#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IProfileAppService
// Guid:9c3e9d90-1778-476a-a6a1-f9f6cb66484c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/04 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 当前用户个人中心应用服务接口
/// </summary>
public interface IProfileAppService : IApplicationService
{
    /// <summary>
    /// 获取当前用户个人资料
    /// </summary>
    Task<UserProfileDto> GetProfileAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新当前用户个人资料
    /// </summary>
    Task<UserProfileDto> UpdateProfileAsync(ProfileUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 修改当前用户密码
    /// </summary>
    Task ChangePasswordAsync(ProfileChangePasswordDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 修改当前用户名
    /// </summary>
    Task ChangeUserNameAsync(ProfileChangeUserNameDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送邮箱验证验证码
    /// </summary>
    Task<ProfileVerificationCodeResultDto> SendEmailVerifyCodeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送手机验证验证码
    /// </summary>
    Task<ProfileVerificationCodeResultDto> SendPhoneVerifyCodeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证当前邮箱
    /// </summary>
    Task VerifyEmailAsync(ProfileVerificationCodeDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证当前手机号
    /// </summary>
    Task VerifyPhoneAsync(ProfileVerificationCodeDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送邮箱换绑验证码
    /// </summary>
    Task<ProfileVerificationCodeResultDto> SendChangeEmailCodeAsync(ProfileChangeEmailDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送手机换绑验证码
    /// </summary>
    Task<ProfileVerificationCodeResultDto> SendChangePhoneCodeAsync(ProfileChangePhoneDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 确认邮箱换绑
    /// </summary>
    Task ConfirmChangeEmailAsync(ProfileVerificationCodeDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 确认手机换绑
    /// </summary>
    Task ConfirmChangePhoneAsync(ProfileVerificationCodeDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 初始化 TOTP 双因素认证
    /// </summary>
    Task<ProfileTwoFactorSetupDto> Setup2FAAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送双因素设置验证码
    /// </summary>
    Task<ProfileVerificationCodeResultDto> Send2FASetupCodeAsync(ProfileTwoFactorMethodDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 启用双因素认证
    /// </summary>
    Task Enable2FAAsync(ProfileTwoFactorVerifyDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 禁用双因素认证
    /// </summary>
    Task Disable2FAAsync(ProfileTwoFactorVerifyDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前用户会话列表
    /// </summary>
    Task<List<ProfileSessionDto>> GetSessionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销指定会话
    /// </summary>
    Task RevokeSessionAsync(ProfileSessionRevokeDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销其他会话
    /// </summary>
    Task RevokeOtherSessionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前用户登录日志
    /// </summary>
    Task<ProfileLoginLogPageDto> GetLoginLogsAsync(int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前用户第三方账号绑定
    /// </summary>
    Task<List<ProfileExternalLoginDto>> GetLinkedAccountsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前用户活跃度统计
    /// </summary>
    Task<ProfileActivityDto> GetActivityAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 解除第三方账号绑定
    /// </summary>
    Task UnlinkAccountAsync(ProfileUnlinkAccountDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 停用当前账号
    /// </summary>
    Task DeactivateAccountAsync(ProfilePasswordConfirmDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 注销当前账号
    /// </summary>
    Task DeleteAccountAsync(ProfilePasswordConfirmDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前用户通知偏好
    /// </summary>
    Task<ProfileNotificationPreferenceDto> GetNotificationPreferenceAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新当前用户通知偏好
    /// </summary>
    Task<ProfileNotificationPreferenceDto> UpdateNotificationPreferenceAsync(ProfileNotificationPreferenceDto input, CancellationToken cancellationToken = default);
}
