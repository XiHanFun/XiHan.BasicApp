// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using XiHan.BasicApp.Contracts.Security;

namespace XiHan.BasicApp.OpenPlatform.Infrastructure;

/// <summary>
/// 签名开放接口的协议。
/// </summary>
/// <remarks>
/// 沿用 4.x 的请求头与规范串，接入方不用改：
/// 规范串为「方法大写 \n 路径 \n 规范查询串 \n 内容签名 \n X-Timestamp 原文 \n X-Nonce」，用凭证密钥（UTF-8）做 HMAC，签名写十六进制或 Base64。
/// 规范查询串：键按序数排序、同键的值也排序，键与值都做 <see cref="Uri.EscapeDataString(string)"/>，<c>&amp;</c> 连接，没有值写 <c>key=</c>。
/// 内容签名是请求体明文（加密的先解密）的 SHA256 / SHA512 小写十六进制，空体即空串的散列，GET 也要带。
/// 加密为 AES-CBC + PKCS7：密钥是凭证密钥的 UTF-8 字节（正好 16 / 24 / 32 字节直接用，否则取 SHA256），IV 依次取 <c>X-Encrypt-Iv</c>、信封的 <c>iv</c>、SHA256(随机串) 的前 16 字节。
/// </remarks>
public static class OpenApiSignature
{
    /// <summary>
    /// AppKey。
    /// </summary>
    public const string AccessKeyHeader = "X-Access-Key";

    /// <summary>
    /// Unix 秒。
    /// </summary>
    public const string TimestampHeader = "X-Timestamp";

    /// <summary>
    /// 随机串。
    /// </summary>
    public const string NonceHeader = "X-Nonce";

    /// <summary>
    /// 请求签名。
    /// </summary>
    public const string SignatureHeader = "X-Signature";

    /// <summary>
    /// 内容签名。
    /// </summary>
    public const string ContentSignHeader = "X-Content-Sign";

    /// <summary>
    /// 签名算法（可选，须与凭证的一致）。
    /// </summary>
    public const string SignAlgorithmHeader = "X-Sign-Algorithm";

    /// <summary>
    /// 内容签名算法（可选，<c>SHA256</c> 缺省或 <c>SHA512</c>）。
    /// </summary>
    public const string ContentSignAlgorithmHeader = "X-Content-Sign-Algorithm";

    /// <summary>
    /// 请求体加密算法（可选）。
    /// </summary>
    /// <remarks>
    /// <c>AES-CBC</c> 或 <c>NONE</c>；不带即明文。
    /// </remarks>
    public const string EncryptAlgorithmHeader = "X-Encrypt-Algorithm";

    /// <summary>
    /// 加密 IV（Base64，可选）。
    /// </summary>
    public const string EncryptIvHeader = "X-Encrypt-Iv";

    /// <summary>
    /// 要求加密响应。
    /// </summary>
    /// <remarks>
    /// 值为 <c>1</c> / <c>true</c> / <c>yes</c>；请求本身加密了也加密响应。
    /// </remarks>
    public const string EncryptResponseHeader = "X-Encrypt-Response";

    /// <summary>
    /// 响应已加密的标记头（值 <c>1</c>）。
    /// </summary>
    public const string SecureResponseHeader = "X-Secure-Response";

    /// <summary>
    /// 加密算法名。
    /// </summary>
    public const string AesCbc = "AES-CBC";

    /// <summary>
    /// 不加密。
    /// </summary>
    public const string None = "NONE";

    /// <summary>
    /// 内容签名 SHA256。
    /// </summary>
    public const string Sha256 = "SHA256";

    /// <summary>
    /// 内容签名 SHA512。
    /// </summary>
    public const string Sha512 = "SHA512";

    /// <summary>
    /// 规范查询串。
    /// </summary>
    public static string CanonicalQuery(IQueryCollection query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var pairs = new List<string>();
        foreach (var key in query.Keys.Order(StringComparer.Ordinal))
        {
            var values = query[key].Select(static v => v ?? string.Empty).Order(StringComparer.Ordinal).ToList();
            if (values.Count == 0)
            {
                pairs.Add(Uri.EscapeDataString(key) + "=");
                continue;
            }

            pairs.AddRange(values.Select(value => Uri.EscapeDataString(key) + "=" + Uri.EscapeDataString(value)));
        }

        return string.Join('&', pairs);
    }

    /// <summary>
    /// 规范串。
    /// </summary>
    public static string CanonicalString(string method, string path, string canonicalQuery, string contentSign, string timestamp, string nonce)
        => string.Join('\n', method.ToUpperInvariant(), path, canonicalQuery, contentSign, timestamp, nonce);

    /// <summary>
    /// 规整内容签名算法。
    /// </summary>
    /// <returns>规整后的算法；不认识的为 <see langword="null"/>。</returns>
    public static string? NormalizeContentAlgorithm(string? value)
        => string.IsNullOrWhiteSpace(value) ? Sha256
            : value.Trim().Replace("-", string.Empty, StringComparison.Ordinal).ToUpperInvariant() switch
            {
                Sha256 => Sha256,
                Sha512 => Sha512,
                _ => null,
            };

    /// <summary>
    /// 内容签名（小写十六进制）。
    /// </summary>
    public static string ContentHash(ReadOnlySpan<byte> body, string algorithm)
        => Convert.ToHexStringLower(algorithm == Sha512 ? SHA512.HashData(body) : SHA256.HashData(body));

    /// <summary>
    /// 按凭证的算法算签名。
    /// </summary>
    public static byte[] Sign(string secret, string canonical, string algorithm)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var data = Encoding.UTF8.GetBytes(canonical);
        return algorithm == ApiSignatureAlgorithms.HmacSha512 ? HMACSHA512.HashData(key, data) : HMACSHA256.HashData(key, data);
    }

    /// <summary>
    /// 判定请求带的签名与算出的是否一致。
    /// </summary>
    /// <remarks>
    /// 签名写十六进制（不区分大小写）或 Base64，恒定时间比较。
    /// </remarks>
    public static bool Matches(string presented, byte[] expected)
    {
        ArgumentNullException.ThrowIfNull(expected);
        var text = presented.Trim();
        byte[]? bytes = null;
        if (text.Length == expected.Length * 2)
        {
            try
            {
                bytes = Convert.FromHexString(text);
            }
            catch (FormatException)
            {
                bytes = null;
            }
        }

        if (bytes is null)
        {
            var buffer = new byte[expected.Length + 3];
            bytes = Convert.TryFromBase64String(text, buffer, out var written) ? buffer[..written] : [];
        }

        return CryptographicOperations.FixedTimeEquals(bytes, expected);
    }

    /// <summary>
    /// 内容签名是否一致（十六进制不区分大小写，恒定时间比较）。
    /// </summary>
    public static bool ContentMatches(string presented, string expected)
        => CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(presented.Trim().ToLowerInvariant()), Encoding.ASCII.GetBytes(expected));

    /// <summary>
    /// 规整加密算法。
    /// </summary>
    /// <returns>不带或 <c>NONE</c> 即不加密（<see cref="None"/>），<c>AES</c> / <c>AES-CBC</c> 即 <see cref="AesCbc"/>，其余为 <see langword="null"/>。</returns>
    public static string? NormalizeEncryption(string? value)
        => string.IsNullOrWhiteSpace(value) ? None
            : value.Trim().ToUpperInvariant() switch
            {
                None => None,
                "AES" or "AES-CBC" or "AESCBC" => AesCbc,
                _ => null,
            };

    /// <summary>
    /// 是否要求加密响应。
    /// </summary>
    public static bool IsTruthy(string? value)
        => value?.Trim().ToUpperInvariant() is "1" or "TRUE" or "YES";

    /// <summary>
    /// 随机串导出的缺省 IV。
    /// </summary>
    public static byte[] DefaultIv(string nonce) => SHA256.HashData(Encoding.UTF8.GetBytes(nonce))[..16];

    /// <summary>
    /// 解密。
    /// </summary>
    public static byte[] Decrypt(string secret, byte[] cipher, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = KeyOf(secret);
        return aes.DecryptCbc(cipher, iv, PaddingMode.PKCS7);
    }

    /// <summary>
    /// 加密。
    /// </summary>
    public static byte[] Encrypt(string secret, byte[] plain, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = KeyOf(secret);
        return aes.EncryptCbc(plain, iv, PaddingMode.PKCS7);
    }

    private static byte[] KeyOf(string secret)
    {
        var bytes = Encoding.UTF8.GetBytes(secret);
        return bytes.Length is 16 or 24 or 32 ? bytes : SHA256.HashData(bytes);
    }
}
