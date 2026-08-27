// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.DataProtection;
using XiHan.BasicApp.Saas.Domain.DomainServices;

namespace XiHan.BasicApp.Saas.Infrastructure.Security;

/// <summary>
/// 基于 ASP.NET Core Data Protection 的系统配置加密值保护器
/// </summary>
/// <remarks>
/// 多实例部署需共享 Data Protection 密钥环（持久化到共享存储），否则其它实例无法解密。
/// 使用独立 Purpose，与存储密钥/短信邮件网关密钥/机器人秘钥互不影响。
/// </remarks>
public sealed class DataProtectionConfigValueSecretProtector : IConfigValueSecretProtector
{
    private readonly IDataProtector _protector;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DataProtectionConfigValueSecretProtector(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector(SaasSecretProtectionPurposes.ConfigValue);
    }

    /// <summary>
    /// 加密配置值（幂等：已是密文则原样返回；空值透传）
    /// </summary>
    /// <param name="plaintext">明文配置值</param>
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
    /// 解密配置值（解密失败抛异常，fail-closed；空值透传）
    /// </summary>
    /// <param name="value">密文</param>
    /// <returns>明文配置值</returns>
    public string? Unprotect(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        // 先认前缀再解密：非本保护器写入的值（历史明文/截断脏值）一律按解密失败处理，
        // 抛可诊断的 CryptographicException，而不是在字符串切片处抛参数越界（fail-closed，不做旧明文兼容）
        return _protector.Unprotect(SaasSecretCipherText.StripPrefixOrThrow(value, "系统配置加密值"));
    }
}
