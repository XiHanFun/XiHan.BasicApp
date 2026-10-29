// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Cryptography;

namespace XiHan.BasicApp.Identity.Infrastructure;

/// <summary>
/// 图形验证码的码源。
/// </summary>
/// <remarks>
/// 缺省是密码学随机的数字；测试可以换成固定码。
/// </remarks>
public interface ICaptchaCodeSource
{
    /// <summary>
    /// 取一个指定长度的数字码。
    /// </summary>
    string Generate(int length);
}

/// <summary>
/// 密码学随机的数字码（<see cref="RandomNumberGenerator"/>）。
/// </summary>
public sealed class RandomCaptchaCodes : ICaptchaCodeSource
{
    /// <inheritdoc />
    public string Generate(int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);
        return string.Create(length, 0, static (span, _) =>
        {
            for (var i = 0; i < span.Length; i++)
            {
                span[i] = (char)('0' + RandomNumberGenerator.GetInt32(10));
            }
        });
    }
}
