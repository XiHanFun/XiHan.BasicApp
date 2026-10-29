// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace XiHan.BasicApp.Identity.Domain.Services;

/// <summary>
/// 刷新令牌的生成与散列。
/// </summary>
/// <remarks>
/// 32 字节随机数的 Base64Url 文本交给客户端，库里只存 SHA-256 散列；每次刷新轮换。
/// </remarks>
public static class RefreshTokens
{
    /// <summary>
    /// 生成一枚新的刷新令牌（明文）。
    /// </summary>
    public static string Generate() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));

    /// <summary>
    /// 明文 → 库里存的散列（小写十六进制）。
    /// </summary>
    public static string Hash(string token)
    {
        ArgumentException.ThrowIfNullOrEmpty(token);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
