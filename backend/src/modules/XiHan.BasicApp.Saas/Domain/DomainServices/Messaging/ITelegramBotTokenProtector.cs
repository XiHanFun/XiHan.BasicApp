// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// Telegram 机器人 Token 保护器
/// </summary>
/// <remarks>
/// <c>SysTelegramBot.Token</c> 为 Bot Token，必须可逆加密落库；
/// Data Protection 实现，与机器人配置秘钥独立 Purpose 隔离。解密失败即抛异常（fail-closed，无历史兼容）。
/// </remarks>
public interface ITelegramBotTokenProtector
{
    /// <summary>
    /// 加密 Token（幂等：已是密文则原样返回）
    /// </summary>
    /// <param name="plaintext">明文 Token</param>
    /// <returns>密文（带前缀标记）</returns>
    string? Protect(string? plaintext);

    /// <summary>
    /// 解密 Token（解密失败抛异常，fail-closed）
    /// </summary>
    /// <param name="value">密文</param>
    /// <returns>明文 Token</returns>
    string? Unprotect(string? value);
}
