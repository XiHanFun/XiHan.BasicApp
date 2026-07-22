// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 机器人配置签名秘钥保护器（Webhook 型：钉钉/飞书）
/// </summary>
/// <remarks>
/// <c>SysBotConfig.Secret</c> 为签名秘钥，必须可逆加密落库；
/// Data Protection 实现，独立 Purpose 隔离密钥。解密失败即抛异常（fail-closed，无历史兼容）。
/// </remarks>
public interface IBotConfigSecretProtector
{
    /// <summary>
    /// 加密签名秘钥（幂等：已是密文则原样返回）
    /// </summary>
    /// <param name="plaintext">明文秘钥</param>
    /// <returns>密文（带前缀标记）</returns>
    string? Protect(string? plaintext);

    /// <summary>
    /// 解密签名秘钥（解密失败抛异常，fail-closed）
    /// </summary>
    /// <param name="value">密文</param>
    /// <returns>明文秘钥</returns>
    string? Unprotect(string? value);
}
