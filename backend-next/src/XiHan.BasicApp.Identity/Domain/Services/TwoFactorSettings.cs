// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Identity.Domain.Entities;

namespace XiHan.BasicApp.Identity.Domain.Services;

/// <summary>
/// 账号的双因素设置：开了哪些方式、登录时哪些方式真能用。
/// </summary>
public static class TwoFactorSettings
{
    private static readonly TwoFactorMethod[] _order = [TwoFactorMethod.Totp, TwoFactorMethod.Email, TwoFactorMethod.Phone];

    /// <summary>
    /// 按位组合展开成方式列表（认证器、邮箱、短信的顺序）。
    /// </summary>
    public static IReadOnlyList<TwoFactorMethod> MethodsOf(TwoFactorMethods methods)
        => _order.Where(m => methods.HasFlag(FlagOf(m))).ToList();

    /// <summary>
    /// 单个方式对应的位。
    /// </summary>
    public static TwoFactorMethods FlagOf(TwoFactorMethod method) => (TwoFactorMethods)(int)method;

    /// <summary>
    /// 登录第二步可用的方式：认证器要有密钥；邮箱与短信要等 Notification 能发码，目前一律不可用。
    /// </summary>
    public static IReadOnlyList<TwoFactorMethod> UsableAt(SysUserSecurity security)
    {
        ArgumentNullException.ThrowIfNull(security);
        if (!security.TwoFactorEnabled)
        {
            return [];
        }

        return MethodsOf(security.TwoFactorMethods).Where(m => m == TwoFactorMethod.Totp && !string.IsNullOrEmpty(security.TwoFactorSecret)).ToList();
    }
}
