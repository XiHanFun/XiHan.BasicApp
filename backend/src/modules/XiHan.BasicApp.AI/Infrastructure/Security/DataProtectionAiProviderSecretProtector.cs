// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.DataProtection;
using XiHan.BasicApp.AI.Domain.DomainServices;
using XiHan.BasicApp.Saas.Infrastructure.Security;

namespace XiHan.BasicApp.AI.Infrastructure.Security;

/// <summary>
/// 基于 ASP.NET Core Data Protection 的 AI Provider 密钥保护器
/// </summary>
/// <remarks>
/// 多实例部署需共享 Data Protection 密钥环（持久化到共享存储），否则其它实例无法解密。
/// 使用独立 Purpose，与存储密钥/短信密钥/邮件密码/租户连接串的密钥互不影响。
/// </remarks>
public sealed class DataProtectionAiProviderSecretProtector : IAiProviderSecretProtector
{
    private readonly IDataProtector _protector;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DataProtectionAiProviderSecretProtector(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector(SaasSecretProtectionPurposes.AiProviderApiKey);
    }

    /// <summary>
    /// 加密密钥（幂等：已是密文则原样返回）
    /// </summary>
    /// <param name="plaintext">明文密钥</param>
    /// <returns>密文（带前缀标记）</returns>
    public string? Protect(string? plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
        {
            return plaintext;
        }

        // 已是密文则不重复加密（幂等）
        if (plaintext.StartsWith(SaasSecretProtectionPurposes.CipherPrefix, StringComparison.Ordinal))
        {
            return plaintext;
        }

        return SaasSecretProtectionPurposes.CipherPrefix + _protector.Protect(plaintext);
    }

    /// <summary>
    /// 解密密钥（解密失败抛异常，fail-closed）
    /// </summary>
    /// <param name="value">密文</param>
    /// <returns>明文密钥</returns>
    /// <exception cref="InvalidOperationException">值不带 <c>dp:</c> 前缀，即不是本保护器写入的密文。</exception>
    public string? Unprotect(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        // 先判前缀再切片：不带前缀的历史明文/脏值直接给出可读的错误语义。
        // 否则短于 3 字符的值会在切片处抛 ArgumentOutOfRangeException，
        // 够长但无前缀的值会被砍掉 3 个有效字符后抛 CryptographicException，
        // 两者都会把「不是本保护器写的值」误报成「密钥环不匹配」，误导排查方向。
        if (!value.StartsWith(SaasSecretProtectionPurposes.CipherPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("AI Provider 密钥不是有效密文（缺少 dp: 前缀）。");
        }

        // 密钥一律为本保护器写入的密文，去前缀直接解密；解密失败即抛（fail-closed，不做旧明文兼容）
        return _protector.Unprotect(value[SaasSecretProtectionPurposes.CipherPrefix.Length..]);
    }
}
