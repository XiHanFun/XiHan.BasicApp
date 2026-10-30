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
    /// 方式列表合成按位组合。
    /// </summary>
    public static TwoFactorMethods FlagsOf(IEnumerable<TwoFactorMethod> methods)
        => methods.Aggregate(TwoFactorMethods.None, (flags, method) => flags | FlagOf(method));

    /// <summary>
    /// 邮箱与短信方式收码用的联系方式。
    /// </summary>
    /// <remarks>
    /// 认证器不发码，为 <see langword="null"/>。
    /// </remarks>
    public static ContactChannel? ChannelOf(TwoFactorMethod method) => method switch
    {
        TwoFactorMethod.Email => ContactChannel.Email,
        TwoFactorMethod.Phone => ContactChannel.Phone,
        _ => null,
    };

    /// <summary>
    /// 联系方式对应的双因素方式。
    /// </summary>
    public static TwoFactorMethod MethodOf(ContactChannel channel)
        => channel == ContactChannel.Email ? TwoFactorMethod.Email : TwoFactorMethod.Phone;

    /// <summary>
    /// 登录第二步可用的方式。
    /// </summary>
    /// <remarks>
    /// 认证器要有密钥；邮箱与短信要绑定且已验证（管理员改了联系方式即复位，须本人重新验证）。
    /// </remarks>
    public static IReadOnlyList<TwoFactorMethod> UsableAt(SysUser user, SysUserSecurity security)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(security);
        if (!security.TwoFactorEnabled)
        {
            return [];
        }

        return MethodsOf(security.TwoFactorMethods).Where(m => m switch
        {
            TwoFactorMethod.Totp => !string.IsNullOrEmpty(security.TwoFactorSecret),
            TwoFactorMethod.Email => user.EmailVerified && !string.IsNullOrWhiteSpace(user.Email),
            TwoFactorMethod.Phone => user.PhoneVerified && !string.IsNullOrWhiteSpace(user.PhoneNumber),
            _ => false,
        }).ToList();
    }

    /// <summary>
    /// 挑战里预先选定的方式。
    /// </summary>
    /// <remarks>
    /// 只有一种方式且是认证器时就是它；邮箱与短信要先调 <c>auth.send-two-factor-code</c> 发码，不预先选定。
    /// </remarks>
    public static TwoFactorMethod? PreselectedOf(IReadOnlyList<TwoFactorMethod> methods)
    {
        ArgumentNullException.ThrowIfNull(methods);
        return methods is [TwoFactorMethod.Totp] ? TwoFactorMethod.Totp : null;
    }
}
