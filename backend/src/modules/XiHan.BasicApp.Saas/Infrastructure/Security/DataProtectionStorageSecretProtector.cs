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
    /// <summary>
    /// 保护用途（隔离密钥）
    /// </summary>
    private const string Purpose = "XiHan.BasicApp.Saas.StorageConfig.SecretAccessKey.v1";

    /// <summary>
    /// 密文前缀标记：用于区分已加密值与历史明文
    /// </summary>
    private const string CipherPrefix = "dp:";

    private readonly IDataProtector _protector;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DataProtectionStorageSecretProtector(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector(Purpose);
    }

    /// <inheritdoc />
    public string? Protect(string? plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
        {
            return plaintext;
        }

        // 已是密文则不重复加密（幂等）
        if (plaintext.StartsWith(CipherPrefix, StringComparison.Ordinal))
        {
            return plaintext;
        }

        return CipherPrefix + _protector.Protect(plaintext);
    }

    /// <inheritdoc />
    public string? Unprotect(string? value)
    {
        if (string.IsNullOrEmpty(value) || !value.StartsWith(CipherPrefix, StringComparison.Ordinal))
        {
            // 历史明文：原样返回
            return value;
        }

        try
        {
            return _protector.Unprotect(value[CipherPrefix.Length..]);
        }
        catch
        {
            // 解密失败兜底（密钥环变更等），避免阻断；返回原值
            return value;
        }
    }
}
