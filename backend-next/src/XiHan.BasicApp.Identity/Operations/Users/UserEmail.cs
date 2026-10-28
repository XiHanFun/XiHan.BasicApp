// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Identity.Operations.Users;

/// <summary>
/// 邮箱格式（<c>[EmailAddress]</c> 不折进 Schema，格式自己判）：只要求形如 local@domain，不做完整的 RFC 5322。
/// </summary>
internal static class UserEmail
{
    /// <summary>
    /// 是否形如 local@domain：恰好一个 <c>@</c>，前后都不为空。
    /// </summary>
    public static bool IsValid(string email)
    {
        ArgumentNullException.ThrowIfNull(email);
        var at = email.IndexOf('@', StringComparison.Ordinal);
        return at > 0 && at < email.Length - 1 && email.IndexOf('@', at + 1) < 0;
    }
}
