// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Domain.Services;

/// <summary>
/// 密码策略（沿用 4.x：最短 8 位，须同时含字母与数字）；业务规则返回 <see cref="Result"/>，不抛异常。
/// </summary>
public static class PasswordPolicy
{
    /// <summary>
    /// 校验新密码。
    /// </summary>
    public static Result Validate(string password, IdentityOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrEmpty(password) || password.Length < options.PasswordMinLength)
        {
            return Error.Validation(IdentityErrors.PasswordPolicy, IdentityMessages.PasswordTooShort, MessageArguments.Of(("min", options.PasswordMinLength)));
        }

        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
        {
            return Error.Validation(IdentityErrors.PasswordPolicy, IdentityMessages.PasswordTooSimple);
        }

        return Result.Success();
    }
}
