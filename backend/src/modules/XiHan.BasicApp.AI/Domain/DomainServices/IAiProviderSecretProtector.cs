#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IAiProviderSecretProtector
// Guid:a11c0de0-1003-4a10-9a00-00000000ai12
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/05 14:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.AI.Domain.DomainServices;

/// <summary>
/// AI Provider API 密钥保护器
/// </summary>
/// <remarks>
/// <c>SysAiProvider.ApiKey</c> 为外部模型访问密钥，必须可逆加密落库；
/// Data Protection 实现，独立 Purpose 隔离密钥。解密失败即抛异常（fail-closed，无历史明文兼容）。
/// </remarks>
public interface IAiProviderSecretProtector
{
    /// <summary>
    /// 加密密钥（幂等：已是密文则原样返回）
    /// </summary>
    /// <param name="plaintext">明文密钥</param>
    /// <returns>密文（带前缀标记）</returns>
    string? Protect(string? plaintext);

    /// <summary>
    /// 解密密钥（解密失败抛异常，fail-closed）
    /// </summary>
    /// <param name="value">密文</param>
    /// <returns>明文密钥</returns>
    string? Unprotect(string? value);
}
