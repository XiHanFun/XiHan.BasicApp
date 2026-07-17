#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserApiCredentialSecretProtector
// Guid:9f3c1a26-7d84-4e0b-9c15-6a2f8d4b3e70
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 用户 OpenAPI 凭证密钥（AppSecret）保护器。
/// </summary>
/// <remarks>
/// 可逆加密（非单向哈希）：开放接口 HMAC 验签需还原明文密钥参与签名运算。
/// 独立 Purpose 隔离密钥环，解密失败即抛（fail-closed，无历史明文兼容）。
/// </remarks>
public interface IUserApiCredentialSecretProtector
{
    /// <summary>
    /// 加密明文密钥（幂等：已是密文则原样返回）
    /// </summary>
    string? Protect(string? plaintext);

    /// <summary>
    /// 解密还原明文密钥
    /// </summary>
    string? Unprotect(string? value);
}
