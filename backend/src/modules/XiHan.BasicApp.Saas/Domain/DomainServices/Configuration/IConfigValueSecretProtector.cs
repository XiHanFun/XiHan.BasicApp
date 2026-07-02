#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IConfigValueSecretProtector
// Guid:3f6b8c1d-9e42-47a5-b8d0-6c2f1a9e5b74
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/03 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 系统配置加密值保护器（IsEncrypted 行的 ConfigValue）
/// </summary>
/// <remarks>
/// <c>SysConfig.IsEncrypted</c> 行的 <c>ConfigValue</c> 必须可逆加密落库（写侧加密、读侧解密），
/// 消灭「仅 DTO 遮蔽」的假加密（如 Telegram webhook secret-token 明文落库/进缓存）。
/// <c>DefaultValue</c> 不参与加密（加密配置不应设默认值）。
/// Data Protection 实现，独立 Purpose 隔离密钥。解密失败即抛异常（fail-closed，无历史兼容）。
/// </remarks>
public interface IConfigValueSecretProtector
{
    /// <summary>
    /// 加密配置值（幂等：已是密文则原样返回；空值透传）
    /// </summary>
    /// <param name="plaintext">明文配置值</param>
    /// <returns>密文（带前缀标记）</returns>
    string? Protect(string? plaintext);

    /// <summary>
    /// 解密配置值（解密失败抛异常，fail-closed；空值透传）
    /// </summary>
    /// <param name="value">密文</param>
    /// <returns>明文配置值</returns>
    string? Unprotect(string? value);
}
