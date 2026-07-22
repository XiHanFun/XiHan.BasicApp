// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 当前用户个人中心领域服务
/// </summary>
public interface IProfileDomainService
{
    /// <summary>
    /// 更新当前用户个人资料
    /// </summary>
    Task<ProfileUserSecurityResult> UpdateProfileAsync(ProfileUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 修改当前用户密码
    /// </summary>
    Task<ProfileUserSecurityResult> ChangePasswordAsync(ProfileChangePasswordCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 修改当前用户名
    /// </summary>
    Task<ProfileUserSecurityResult> ChangeUserNameAsync(ProfileChangeUserNameCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 准备联系方式换绑
    /// </summary>
    Task<ProfileContactPrepareResult> PrepareChangeContactAsync(ProfileChangeContactPrepareCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 确认联系方式换绑
    /// </summary>
    Task<ProfileUserSecurityResult> ConfirmContactAsync(ProfileConfirmContactCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证当前联系方式
    /// </summary>
    Task<ProfileUserSecurityResult> VerifyContactAsync(ProfileVerifyContactCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 初始化 TOTP 双因素认证
    /// </summary>
    Task<ProfileTwoFactorSetupResult> SetupTwoFactorAsync(ProfileTwoFactorSetupCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 启用双因素认证方式
    /// </summary>
    Task EnableTwoFactorAsync(ProfileTwoFactorCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 禁用双因素认证方式
    /// </summary>
    Task DisableTwoFactorAsync(ProfileTwoFactorCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销指定会话
    /// </summary>
    Task<ProfileSessionRevokeResult> RevokeSessionAsync(ProfileSessionRevokeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销其他会话
    /// </summary>
    Task<ProfileSessionRevokeResult> RevokeOtherSessionsAsync(ProfileOtherSessionsRevokeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 解除第三方账号绑定
    /// </summary>
    Task UnlinkAccountAsync(ProfileUnlinkAccountCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 停用当前账号
    /// </summary>
    Task<ProfileSessionRevokeResult> DeactivateAccountAsync(ProfilePasswordConfirmCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 注销当前账号
    /// </summary>
    Task<ProfileSessionRevokeResult> DeleteAccountAsync(ProfilePasswordConfirmCommand command, CancellationToken cancellationToken = default);
}
