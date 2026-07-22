// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 个人中心应用层映射器
/// </summary>
public static class ProfileApplicationMapper
{
    /// <summary>
    /// 映射密码修改命令
    /// </summary>
    public static ProfileChangePasswordCommand ToChangePasswordCommand(ProfileChangePasswordDto input, long userId)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new ProfileChangePasswordCommand(userId, input.OldPassword, input.NewPassword);
    }

    /// <summary>
    /// 映射用户名修改命令
    /// </summary>
    public static ProfileChangeUserNameCommand ToChangeUserNameCommand(ProfileChangeUserNameDto input, long userId)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new ProfileChangeUserNameCommand(userId, input.UserName, input.Password);
    }

    /// <summary>
    /// 映射联系方式换绑准备命令
    /// </summary>
    public static ProfileChangeContactPrepareCommand ToChangeContactPrepareCommand(
        long userId,
        ProfileContactKind contactKind,
        string? target,
        string? password)
    {
        return new ProfileChangeContactPrepareCommand(userId, contactKind, target, password);
    }

    /// <summary>
    /// 映射联系方式换绑确认命令
    /// </summary>
    public static ProfileConfirmContactCommand ToConfirmContactCommand(long userId, ProfileContactKind contactKind, string? target)
    {
        return new ProfileConfirmContactCommand(userId, contactKind, target);
    }

    /// <summary>
    /// 映射联系方式验证命令
    /// </summary>
    public static ProfileVerifyContactCommand ToVerifyContactCommand(long userId, ProfileContactKind contactKind)
    {
        return new ProfileVerifyContactCommand(userId, contactKind);
    }

    /// <summary>
    /// 映射双因素方式变更命令
    /// </summary>
    public static ProfileTwoFactorCommand ToTwoFactorCommand(long userId, TwoFactorMethod method)
    {
        return new ProfileTwoFactorCommand(userId, method);
    }

    /// <summary>
    /// 映射双因素初始化命令
    /// </summary>
    public static ProfileTwoFactorSetupCommand ToTwoFactorSetupCommand(long userId, string issuer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(issuer);

        return new ProfileTwoFactorSetupCommand(userId, issuer);
    }

    /// <summary>
    /// 映射会话撤销命令
    /// </summary>
    public static ProfileSessionRevokeCommand ToSessionRevokeCommand(
        ProfileSessionRevokeDto input,
        long userId,
        string? currentSessionId,
        long? operatorUserId)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new ProfileSessionRevokeCommand(userId, input.SessionId, currentSessionId, operatorUserId);
    }

    /// <summary>
    /// 映射其他会话撤销命令
    /// </summary>
    public static ProfileOtherSessionsRevokeCommand ToOtherSessionsRevokeCommand(
        long userId,
        string? currentSessionId,
        long? operatorUserId)
    {
        return new ProfileOtherSessionsRevokeCommand(userId, currentSessionId, operatorUserId);
    }

    /// <summary>
    /// 映射第三方账号解绑命令
    /// </summary>
    public static ProfileUnlinkAccountCommand ToUnlinkAccountCommand(ProfileUnlinkAccountDto input, long userId)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new ProfileUnlinkAccountCommand(userId, input.Provider);
    }

    /// <summary>
    /// 映射密码确认命令
    /// </summary>
    public static ProfilePasswordConfirmCommand ToPasswordConfirmCommand(
        ProfilePasswordConfirmDto input,
        long userId,
        long? operatorUserId)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new ProfilePasswordConfirmCommand(userId, input.Password, operatorUserId);
    }

    /// <summary>
    /// 映射个人资料更新命令
    /// </summary>
    public static ProfileUpdateCommand ToUpdateCommand(ProfileUpdateDto input, long userId)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new ProfileUpdateCommand(
            userId,
            input.NickName,
            input.RealName,
            input.Avatar,
            input.Gender,
            input.Birthday,
            input.TimeZone,
            input.Language,
            input.Country,
            input.Remark);
    }

    /// <summary>
    /// 映射双因素设置结果
    /// </summary>
    public static ProfileTwoFactorSetupDto ToTwoFactorSetupDto(ProfileTwoFactorSetupResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new ProfileTwoFactorSetupDto
        {
            SharedKey = result.SharedKey,
            AuthenticatorUri = result.AuthenticatorUri
        };
    }
}
