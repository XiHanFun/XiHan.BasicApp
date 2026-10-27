// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Security.Cryptography;

namespace XiHan.BasicApp.Identity.Domain.Services;

/// <summary>
/// 密码散列：PBKDF2-SHA256，16 字节随机盐、32 字节输出；格式 <c>pbkdf2-sha256$迭代$盐$散列</c>（Base64），迭代数写在散列里，提高后旧散列仍可校验。
/// </summary>
public static class PasswordHasher
{
    /// <summary>
    /// 算法标记。
    /// </summary>
    public const string Algorithm = "pbkdf2-sha256";

    /// <summary>
    /// 当前迭代次数。
    /// </summary>
    public const int Iterations = 100_000;

    private const int SaltSize = 16;
    private const int KeySize = 32;

    /// <summary>
    /// 生成散列。
    /// </summary>
    public static string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        return $"{Algorithm}${Iterations.ToString(CultureInfo.InvariantCulture)}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
    }

    /// <summary>
    /// 校验；散列为空或格式不对一律不通过（没有密码的账号不能登录）。
    /// </summary>
    public static bool Verify(string password, string? hash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
        {
            return false;
        }

        var parts = hash.Split('$');
        if (parts.Length != 4 || !string.Equals(parts[0], Algorithm, StringComparison.Ordinal) || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var iterations) || iterations <= 0)
        {
            return false;
        }

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expected = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
