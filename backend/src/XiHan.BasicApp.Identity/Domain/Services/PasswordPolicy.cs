// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Domain.Services;

/// <summary>
/// 密码策略
/// </summary>
/// <remarks>
/// 规则来自系统参数 <see cref="PasswordSettings.ConfigKey"/>：最短长度与须含的字符类别，报第一条不满足的。业务规则返回 <see cref="Result"/>，不抛异常
/// 字母、大小写按 Unicode 判定，其他字符是字母与数字以外的可见字符
/// </remarks>
public static class PasswordPolicy
{
    /// <summary>
    /// 按系统参数的密码设置校验新密码
    /// </summary>
    /// <remarks>
    /// 参数写错即失败
    /// </remarks>
    public static async ValueTask<Result> CheckAsync(string password, InvocationContext context)
    {
        var configured = await PasswordSettings.ReadAsync(context).ConfigureAwait(false);
        return configured.TryGetValue(out var settings) ? Validate(password, settings) : configured.Error!;
    }

    /// <summary>
    /// 校验新密码
    /// </summary>
    public static Result Validate(string password, PasswordSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (string.IsNullOrEmpty(password) || password.Length < settings.MinLength)
        {
            return Error.Validation(IdentityErrors.PasswordPolicy, IdentityMessages.PasswordTooShort, MessageArguments.Of(("min", settings.MinLength)));
        }

        var missing = (settings.RequireLetter && !password.Any(char.IsLetter)) ? IdentityMessages.PasswordNeedsLetter
            : (settings.RequireDigit && !password.Any(char.IsDigit)) ? IdentityMessages.PasswordNeedsDigit
            : (settings.RequireUppercase && !password.Any(char.IsUpper)) ? IdentityMessages.PasswordNeedsUppercase
            : (settings.RequireLowercase && !password.Any(char.IsLower)) ? IdentityMessages.PasswordNeedsLowercase
            : (settings.RequireSymbol && !password.Any(static c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c) && !char.IsControl(c))) ? IdentityMessages.PasswordNeedsSymbol
            : null;
        return missing is null
            ? Result.Success()
            : Error.Validation(IdentityErrors.PasswordPolicy, missing);
    }
}
