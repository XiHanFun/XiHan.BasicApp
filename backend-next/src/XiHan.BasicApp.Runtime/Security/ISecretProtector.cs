// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics.CodeAnalysis;

namespace XiHan.BasicApp.Runtime.Security;

/// <summary>
/// 应用侧的可逆加密（方案 5.2 Runtime「数据保护」）：要落库、用时又要还原的秘密（加密配置值，之后的渠道密钥、存储密钥、分库连接串）。
/// 按用途（purpose）隔离：一个用途的密文换个用途解不开。密文带 <see cref="SecretProtection.CipherPrefix"/>；解不开按失败处理，不兼容历史明文。
/// </summary>
public interface ISecretProtector
{
    /// <summary>
    /// 加密。已是本保护器的密文（带前缀）时原样返回，重复保存不会套两层。
    /// </summary>
    /// <param name="purpose">用途，如 <c>XiHan.BasicApp.Settings.ConfigValue</c>。</param>
    /// <param name="plaintext">明文。</param>
    /// <returns>带前缀的密文。</returns>
    string Protect(string purpose, string plaintext);

    /// <summary>
    /// 解密。没有前缀、密文被改动、密钥环里没有对应密钥或用途不符都返回 <c>false</c>，由调用方报自己的错误码。
    /// </summary>
    /// <param name="purpose">加密时的用途。</param>
    /// <param name="ciphertext">带前缀的密文。</param>
    /// <param name="plaintext">解出的明文。</param>
    /// <returns>是否解开。</returns>
    bool TryUnprotect(string purpose, string ciphertext, [NotNullWhen(true)] out string? plaintext);
}
