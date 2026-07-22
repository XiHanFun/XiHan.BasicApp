// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 存储配置密钥保护器：对 <c>SysStorageConfig.SecretAccessKey</c> 等敏感字段做可逆加密落库
/// </summary>
/// <remarks>
/// 加密结果带固定前缀标记，便于与历史明文区分；<see cref="Unprotect"/> 对历史明文/解密失败均原样返回，保证平滑过渡。
/// </remarks>
public interface IStorageSecretProtector
{
    /// <summary>
    /// 加密（null/空原样返回）
    /// </summary>
    string? Protect(string? plaintext);

    /// <summary>
    /// 解密（历史明文或解密失败按原值返回）
    /// </summary>
    string? Unprotect(string? value);
}
