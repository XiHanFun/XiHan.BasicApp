#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DataProtectionStorageSecretProtector
// Guid:d4e5f6a7-b8c9-4a31-bd4e-5f6a7b8c9d0e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.DataProtection;
using XiHan.BasicApp.Saas.Domain.DomainServices;

namespace XiHan.BasicApp.Saas.Infrastructure.Security;

/// <summary>
/// 基于 ASP.NET Core Data Protection 的存储密钥保护器
/// </summary>
/// <remarks>
/// 多实例部署需共享 Data Protection 密钥环（持久化到共享存储），否则其它实例无法解密。
/// 与 OAuth 回调 state 使用同一套 Data Protection 设施。
/// </remarks>
public sealed class DataProtectionStorageSecretProtector : IStorageSecretProtector
{
    private readonly IDataProtector _protector;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DataProtectionStorageSecretProtector(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector(SaasSecretProtectionPurposes.StorageSecretAccessKey);
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
