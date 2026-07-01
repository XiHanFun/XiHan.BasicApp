#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ITenantConnectionSecretProtector
// Guid:1b6d4e08-9a37-4c52-8f1d-3e7b2a6c5d90
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 租户数据库连接字符串保护器
/// </summary>
/// <remarks>
/// 库隔离场景下 <c>SysTenant.ConnectionString</c> 含数据库账号密码，必须可逆加密落库；
/// 与存储密钥保护器同为 ASP.NET Core Data Protection 实现，但使用独立 Purpose 隔离密钥。
/// </remarks>
public interface ITenantConnectionSecretProtector
{
    /// <summary>
    /// 加密连接字符串（幂等：已是密文则原样返回）
    /// </summary>
    /// <param name="plaintext">明文连接字符串</param>
    /// <returns>密文（带前缀标记）</returns>
    string? Protect(string? plaintext);

    /// <summary>
    /// 解密连接字符串（历史明文原样返回，解密失败兜底返回原值）
    /// </summary>
    /// <param name="value">密文或历史明文</param>
    /// <returns>明文连接字符串</returns>
    string? Unprotect(string? value);
}
