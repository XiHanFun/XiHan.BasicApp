// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Cryptography;

namespace XiHan.BasicApp.Saas.Infrastructure.Security;

/// <summary>
/// 密文前缀契约（八个 Data Protection 保护器共用的解密前置校验）。
/// </summary>
/// <remarks>
/// 八个保护器写出的密文一律形如 <c>dp:{payload}</c>。解密前必须先确认这个前缀：
/// <list type="bullet">
/// <item>不校验就切片，遇到短于前缀的脏值（<c>"a"</c>、<c>"dp"</c>）会在字符串切片处抛
/// <see cref="ArgumentOutOfRangeException"/> —— 那是一条"参数越界"的错误，把排查方向指向调用方传参，
/// 而真实原因是库里那一行不是本保护器写的值；</item>
/// <item>对长度够但没有前缀的历史明文，切片会把前 3 个有效字符一起丢掉再去解密，
/// 抛出的虽然是加解密异常，但密文已被人为破坏，错误信息也说不清到底是"值不对"还是"密钥环不对"。</item>
/// </list>
/// 统一在此判前缀并抛 <see cref="CryptographicException"/>：既保持 fail-closed（绝不把可疑值当明文返回），
/// 又让"不是本保护器写的值"与"密钥环不匹配"落在同一族可捕获的异常上、且消息可诊断。
/// </remarks>
internal static class SaasSecretCipherText
{
    /// <summary>
    /// 校验密文前缀并剥掉它，得到可交给 Data Protection 解密的载荷。
    /// </summary>
    /// <param name="value">待解密的存量值（非空）。</param>
    /// <param name="secretDescription">密钥用途的中文描述，用于错误消息定位是哪一类密钥。</param>
    /// <returns>去掉 <c>dp:</c> 前缀后的密文载荷。</returns>
    /// <exception cref="CryptographicException">值不以 <c>dp:</c> 开头（历史明文、被截断的脏值或人工改写）。</exception>
    internal static string StripPrefixOrThrow(string value, string secretDescription)
    {
        if (!value.StartsWith(SaasSecretProtectionPurposes.CipherPrefix, StringComparison.Ordinal))
        {
            throw new CryptographicException(
                $"{secretDescription}不是有效密文：缺少 {SaasSecretProtectionPurposes.CipherPrefix} 前缀，" +
                "可能是历史明文、被截断的脏值或被人工改写过（fail-closed，不做旧明文兼容）。");
        }

        return value[SaasSecretProtectionPurposes.CipherPrefix.Length..];
    }
}
