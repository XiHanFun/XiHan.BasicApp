// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 密码策略领域服务实现
/// </summary>
public sealed class PasswordPolicyDomainService : IPasswordPolicyDomainService
{
    /// <summary>
    /// 密码最小长度
    /// </summary>
    private const int MinLength = 8;

    /// <summary>
    /// 密码最大长度
    /// </summary>
    private const int MaxLength = 128;

    /// <inheritdoc />
    public string? ValidateStrength(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return "密码不能为空。";
        }

        if (password.Length < MinLength)
        {
            return $"密码长度不能少于 {MinLength} 位。";
        }

        if (password.Length > MaxLength)
        {
            return $"密码长度不能超过 {MaxLength} 位。";
        }

        var hasUpper = false;
        var hasLower = false;
        var hasDigit = false;
        var hasSpecial = false;

        foreach (var ch in password)
        {
            if (char.IsUpper(ch)) hasUpper = true;
            else if (char.IsLower(ch)) hasLower = true;
            else if (char.IsDigit(ch)) hasDigit = true;
            else hasSpecial = true;
        }

        var categoryCount = (hasUpper ? 1 : 0) + (hasLower ? 1 : 0) + (hasDigit ? 1 : 0) + (hasSpecial ? 1 : 0);
        if (categoryCount < 3)
        {
            return "密码必须包含大写字母、小写字母、数字、特殊字符中至少三种。";
        }

        return null;
    }

    /// <inheritdoc />
    public bool IsDuplicateWithHistory(string passwordHash, IEnumerable<string> historicalHashes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        return historicalHashes.Any(hash =>
            string.Equals(hash, passwordHash, StringComparison.Ordinal));
    }
}
