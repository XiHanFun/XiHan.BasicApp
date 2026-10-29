// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace XiHan.BasicApp.Identity.Domain.Services;

/// <summary>
/// 认证器验证码（TOTP，RFC 6238）。
/// </summary>
/// <remarks>
/// HMAC-SHA1、6 位、30 秒一步，前后各容一步时钟偏差；密钥是 20 字节随机数的 Base32（RFC 4648，无填充）。
/// 只做算法，不碰存储：调用方保存最近一次通过的时间步并传进来，同一步（及更早的）码不再接受，防重放。
/// </remarks>
public static class Totp
{
    /// <summary>
    /// 验证码位数。
    /// </summary>
    public const int Digits = 6;

    /// <summary>
    /// 一步的秒数。
    /// </summary>
    public const int PeriodSeconds = 30;

    /// <summary>
    /// 前后各容几步的时钟偏差。
    /// </summary>
    public const int AllowedSkew = 1;

    private const int SecretBytes = 20;
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    /// <summary>
    /// 生成新密钥（Base32）。
    /// </summary>
    public static string GenerateSecret() => ToBase32(RandomNumberGenerator.GetBytes(SecretBytes));

    /// <summary>
    /// 时刻所在的时间步。
    /// </summary>
    public static long StepOf(DateTimeOffset now) => now.ToUnixTimeSeconds() / PeriodSeconds;

    /// <summary>
    /// 某一步的验证码（补零到 <see cref="Digits"/> 位）。
    /// </summary>
    [SuppressMessage("Security", "CA5350:Do Not Use Weak Cryptographic Algorithms", Justification = "RFC 6238 与主流认证器只认 HMAC-SHA1；HMAC 用法下 SHA-1 的碰撞问题不构成弱点。")]
    public static string CodeAt(string secret, long step)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        Span<byte> counter = stackalloc byte[8];
        System.Buffers.Binary.BinaryPrimitives.WriteInt64BigEndian(counter, step);
        Span<byte> hash = stackalloc byte[HMACSHA1.HashSizeInBytes];
        HMACSHA1.HashData(FromBase32(secret), counter, hash);
        var offset = hash[^1] & 0x0f;
        var binary = ((hash[offset] & 0x7f) << 24) | (hash[offset + 1] << 16) | (hash[offset + 2] << 8) | hash[offset + 3];
        return (binary % 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 校验验证码。
    /// </summary>
    /// <remarks>
    /// 在 <see cref="AllowedSkew"/> 的窗口里找匹配的步，且必须晚于 <paramref name="lastStep"/>。
    /// </remarks>
    /// <returns>通过即返回那一步（调用方存下来），否则为 <see langword="null"/>。</returns>
    public static long? Verify(string secret, string? code, DateTimeOffset now, long? lastStep)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        var normalized = code?.Trim();
        if (normalized is not { Length: Digits } || !normalized.All(char.IsAsciiDigit))
        {
            return null;
        }

        var current = StepOf(now);
        for (var step = current - AllowedSkew; step <= current + AllowedSkew; step++)
        {
            if (lastStep is { } used && step <= used)
            {
                continue;
            }

            if (CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(CodeAt(secret, step)), Encoding.ASCII.GetBytes(normalized)))
            {
                return step;
            }
        }

        return null;
    }

    /// <summary>
    /// 生成认证器扫码用的 <c>otpauth://</c> 链接。
    /// </summary>
    /// <remarks>
    /// 标签为「签发方:账号」，参数带上算法、位数与步长。
    /// </remarks>
    public static string AuthenticatorUri(string issuer, string account, string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(account);
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        var label = Uri.EscapeDataString(issuer) + ":" + Uri.EscapeDataString(account);
        return string.Create(CultureInfo.InvariantCulture, $"otpauth://totp/{label}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits={Digits}&period={PeriodSeconds}");
    }

    /// <summary>
    /// Base32 编码（大写、无填充）。
    /// </summary>
    public static string ToBase32(ReadOnlySpan<byte> bytes)
    {
        var builder = new StringBuilder((bytes.Length * 8 + 4) / 5);
        var buffer = 0;
        var bits = 0;
        foreach (var value in bytes)
        {
            buffer = (buffer << 8) | value;
            bits += 8;
            while (bits >= 5)
            {
                builder.Append(Alphabet[(buffer >> (bits - 5)) & 0x1f]);
                bits -= 5;
            }
        }

        if (bits > 0)
        {
            builder.Append(Alphabet[(buffer << (5 - bits)) & 0x1f]);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Base32 解码。
    /// </summary>
    /// <remarks>
    /// 忽略大小写、空格与填充。
    /// </remarks>
    /// <exception cref="FormatException">含非法字符时抛出。</exception>
    public static byte[] FromBase32(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var output = new List<byte>(value.Length * 5 / 8);
        var buffer = 0;
        var bits = 0;
        foreach (var raw in value)
        {
            if (raw is ' ' or '=' or '-')
            {
                continue;
            }

            var index = Alphabet.IndexOf(char.ToUpperInvariant(raw), StringComparison.Ordinal);
            if (index < 0)
            {
                throw new FormatException($"'{raw}' is not a Base32 character.");
            }

            buffer = (buffer << 5) | index;
            bits += 5;
            if (bits >= 8)
            {
                output.Add((byte)((buffer >> (bits - 8)) & 0xff));
                bits -= 8;
            }
        }

        return [.. output];
    }
}
