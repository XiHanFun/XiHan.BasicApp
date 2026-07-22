// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 当前用户验证码应用服务
/// </summary>
public interface IProfileVerificationService
{
    /// <summary>
    /// 消费验证码并返回待确认值（一次性，消费即销毁）
    /// </summary>
    Task<string> ConsumeCodeAsync(long userId, ProfileVerificationPurpose purpose, string? code, CancellationToken cancellationToken = default);

    /// <summary>
    /// 校验双因素验证码
    /// </summary>
    Task EnsureTwoFactorCodeValidAsync(ProfileUserSecurityContext context, TwoFactorMethod method, string? code, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送验证码
    /// </summary>
    Task<ProfileVerificationCodeResultDto> SendCodeAsync(
        SysUser user,
        ProfileVerificationPurpose purpose,
        string? target,
        string title,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送登录双因素短信验证码（用途与安全操作共用 <see cref="ProfileVerificationPurpose.TwoFactorPhone"/>，
    /// 使用登录验证码短信模板；校验经 <see cref="ConsumeCodeAsync"/> 一次性消费）
    /// </summary>
    Task<ProfileVerificationCodeResultDto> SendLoginTwoFactorSmsAsync(
        SysUser user,
        string phone,
        CancellationToken cancellationToken = default);
}
