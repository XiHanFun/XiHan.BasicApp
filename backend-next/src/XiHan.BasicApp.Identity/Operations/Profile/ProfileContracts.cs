// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Identity.Operations.Users;

namespace XiHan.BasicApp.Identity.Operations.Profile;

/// <summary>
/// <c>profile.get</c> 与 <c>profile.setup-totp</c> 的请求：没有输入。
/// </summary>
public sealed record ProfileRequest;

/// <summary>
/// 本人的资料与账号安全概况（个人中心）。资料、联系方式验证、设备等随个人中心批次补全。
/// </summary>
/// <param name="User">用户资料。</param>
/// <param name="TwoFactorEnabled">是否开启了双因素。</param>
/// <param name="TwoFactorMethods">开启的双因素方式。</param>
/// <param name="PasswordChangedAt">最近一次改密时间。</param>
/// <param name="MustChangePassword">是否必须先改密。</param>
/// <param name="FailedAccessCount">自上次成功登录以来的连续失败次数（含双因素码错误）。</param>
/// <param name="LockoutEndAt">登录失败锁定的截止时间；没在锁定期为 <see langword="null"/>。</param>
public sealed record ProfileResponse(UserResponse User, bool TwoFactorEnabled, IReadOnlyList<TwoFactorMethod> TwoFactorMethods, DateTimeOffset? PasswordChangedAt, bool MustChangePassword, int FailedAccessCount, DateTimeOffset? LockoutEndAt)
{
    /// <summary>
    /// 由账号与账号安全构造。
    /// </summary>
    public static ProfileResponse From(SysUser user, SysUserSecurity security)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(security);
        return new ProfileResponse(UserResponse.From(user), security.TwoFactorEnabled, TwoFactorSettings.MethodsOf(security.TwoFactorMethods), security.PasswordChangedAt, security.MustChangePassword, security.FailedAccessCount, security.LockoutEndAt);
    }
}

/// <summary>
/// 绑定认证器用的新密钥：用认证器扫链接（或手输密钥），再用它算出的码调 <c>profile.enable-two-factor</c> 确认。
/// </summary>
/// <param name="SharedKey">密钥（Base32，每 4 个字符一组便于手输）。</param>
/// <param name="AuthenticatorUri"><c>otpauth://</c> 链接（二维码内容）。</param>
/// <param name="ExpiresAt">这个密钥待确认的截止时间（UTC）；过期须重新生成。</param>
public sealed record TotpSetupResponse(string SharedKey, string AuthenticatorUri, DateTimeOffset ExpiresAt);

/// <summary>
/// <c>profile.enable-two-factor</c> 与 <c>profile.disable-two-factor</c> 的请求：用该方式的验证码确认。
/// </summary>
/// <param name="Method">方式。</param>
/// <param name="Code">验证码：开启认证器时是新绑定的认证器算出的码，关闭时是当前认证器的码。</param>
public sealed record TwoFactorChangeRequest(
    TwoFactorMethod Method,
    [Required, StringLength(16, MinimumLength = 1)] string Code);
