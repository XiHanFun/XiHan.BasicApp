#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISmsConfigSecretProtector
// Guid:6b8e4a19-3d72-4c05-9f6b-1e0a8c5d7f34
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 14:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 短信网关访问密钥保护器
/// </summary>
/// <remarks>
/// <c>SysSmsConfig.AccessKeySecret</c> 为服务商访问密钥，必须可逆加密落库；
/// Data Protection 实现，独立 Purpose 隔离密钥。解密失败即抛异常（fail-closed，无历史兼容）。
/// </remarks>
public interface ISmsConfigSecretProtector
{
    /// <summary>
    /// 加密访问密钥（幂等：已是密文则原样返回）
    /// </summary>
    /// <param name="plaintext">明文密钥</param>
    /// <returns>密文（带前缀标记）</returns>
    string? Protect(string? plaintext);

    /// <summary>
    /// 解密访问密钥（解密失败抛异常，fail-closed）
    /// </summary>
    /// <param name="value">密文</param>
    /// <returns>明文密钥</returns>
    string? Unprotect(string? value);
}
