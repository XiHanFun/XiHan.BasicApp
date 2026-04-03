#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IProfileAppService
// Guid:b1c2d3e4-f5a6-7b8c-9d0e-1f2a3b4c5d6e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/04 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.UseCases.Commands;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 个人中心应用服务
/// </summary>
public interface IProfileAppService : IApplicationService
{
    /// <summary>
    /// 获取当前用户完整档案
    /// </summary>
    Task<UserProfileDto> GetProfileAsync();

    /// <summary>
    /// 更新当前用户个人资料
    /// </summary>
    Task UpdateProfileAsync(UpdateProfileCommand command);

    /// <summary>
    /// 修改密码
    /// </summary>
    Task ChangePasswordAsync(ChangePasswordCommand command);

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
