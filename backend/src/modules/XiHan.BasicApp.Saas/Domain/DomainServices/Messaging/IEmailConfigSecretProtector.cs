// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 邮件网关认证密码保护器
/// </summary>
/// <remarks>
/// <c>SysEmailConfig.Password</c> 为 SMTP 认证密码，必须可逆加密落库；
/// Data Protection 实现，独立 Purpose 隔离密钥。解密失败即抛异常（fail-closed，无历史兼容）。
/// </remarks>
public interface IEmailConfigSecretProtector
{
    /// <summary>
    /// 加密认证密码（幂等：已是密文则原样返回）
    /// </summary>
    /// <param name="plaintext">明文密码</param>
    /// <returns>密文（带前缀标记）</returns>
    string? Protect(string? plaintext);

    /// <summary>
    /// 解密认证密码（解密失败抛异常，fail-closed）
    /// </summary>
    /// <param name="value">密文</param>
    /// <returns>明文密码</returns>
    string? Unprotect(string? value);
}
