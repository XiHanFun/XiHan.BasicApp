// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Identity.Domain.Entities;

/// <summary>
/// 账号登记的通行密钥
/// </summary>
/// <remarks>
/// 平台库，跟着账号走。凭据 ID 由认证器随机生成、全局唯一。只存公钥与签名计数，私钥不离开认证器
/// </remarks>
[ActiveUniqueIndex(nameof(CredentialId))]
[DiffLogIgnored]
public sealed class SysUserPasskey : AppAuditedEntity
{
    /// <summary>
    /// 用户
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 用户的注册地（平台账号为 <c>platform</c>）
    /// </summary>
    [StringLength(64)]
    public string HomeTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 本人起的名字
    /// </summary>
    [StringLength(64)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 凭据 ID（base64url）
    /// </summary>
    /// <remarks>
    /// 规范上限 1023 字节，编码后不超过 1364 个字符
    /// </remarks>
    [StringLength(1400)]
    public string CredentialId { get; set; } = string.Empty;

    /// <summary>
    /// COSE 编码的公钥（base64url）
    /// </summary>
    [StringLength(2048)]
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// 签名算法的 COSE 编号
    /// </summary>
    public int Algorithm { get; set; }

    /// <summary>
    /// 最近一次核对过的签名计数
    /// </summary>
    /// <remarks>
    /// 认证器给的是 32 位无符号数；同步的通行密钥恒为 0
    /// </remarks>
    public long SignCount { get; set; }

    /// <summary>
    /// 认证器型号（AAGUID）
    /// </summary>
    /// <remarks>
    /// 不要求证明，认证器可以给全零
    /// </remarks>
    public Guid Aaguid { get; set; }

    /// <summary>
    /// 能否在设备之间同步
    /// </summary>
    public bool BackupEligible { get; set; }

    /// <summary>
    /// 是否已同步备份
    /// </summary>
    /// <remarks>
    /// 每次登录按认证器的回答刷新
    /// </remarks>
    public bool BackedUp { get; set; }

    /// <summary>
    /// 认证器支持的传输方式（逗号分隔，如 <c>internal,hybrid</c>）
    /// </summary>
    [StringLength(200)]
    public string? Transports { get; set; }

    /// <summary>
    /// 最近一次用它登录的时间
    /// </summary>
    public DateTimeOffset? LastUsedAt { get; set; }
}
