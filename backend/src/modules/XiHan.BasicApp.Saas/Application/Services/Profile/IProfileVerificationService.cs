#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IProfileVerificationService
// Guid:8d1d89bc-268c-49ac-895b-b15c08f7f502
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
    /// 消费验证码并返回待确认值
    /// </summary>
    string ConsumeCode(long userId, ProfileVerificationPurpose purpose, string? code);

    /// <summary>
    /// 校验双因素验证码
    /// </summary>
    void EnsureTwoFactorCodeValid(ProfileUserSecurityContext context, TwoFactorMethod method, string? code);

    /// <summary>
    /// 发送验证码
    /// </summary>
    Task<ProfileVerificationCodeResultDto> SendCodeAsync(
        SysUser user,
        ProfileVerificationPurpose purpose,
        string? target,
        string title,
        CancellationToken cancellationToken = default);
}
