#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DataProtectionUserApiCredentialSecretProtector
// Guid:2b7e04a9-53c1-4f6d-8a90-7e1c5b2d9f38
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.DataProtection;
using XiHan.BasicApp.Saas.Domain.DomainServices;

namespace XiHan.BasicApp.Saas.Infrastructure.Security;

/// <summary>
/// 基于 ASP.NET Core Data Protection 的用户 OpenAPI 凭证密钥保护器
/// </summary>
/// <remarks>
/// 多实例部署需共享 Data Protection 密钥环（持久化到共享存储），否则其它实例无法解密。
/// 使用独立 Purpose，与存储密钥/短信密钥/邮件密码/AI 密钥/租户连接串互不影响。
/// </remarks>
public sealed class DataProtectionUserApiCredentialSecretProtector : IUserApiCredentialSecretProtector
{
    private readonly IDataProtector _protector;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DataProtectionUserApiCredentialSecretProtector(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector(SaasSecretProtectionPurposes.UserApiCredentialSecret);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public string? Unprotect(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        // 密钥一律为本保护器写入的密文，去前缀直接解密；解密失败即抛（fail-closed，不做旧明文兼容）
        return _protector.Unprotect(value[SaasSecretProtectionPurposes.CipherPrefix.Length..]);
    }
}
