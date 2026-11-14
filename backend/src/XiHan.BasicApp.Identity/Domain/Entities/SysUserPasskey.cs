// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Identity.Domain.Entities;

/// <summary>
/// 账号登记的通行密钥
/// </summary>
/// <remarks>
/// 跟着账号走、落平台库；只存公钥、签名计数与认证器属性，私钥不离开认证器，拿到库也冒用不了
/// 凭据 ID 由认证器随机生成，部分唯一索引 <c>ux_sys_user_passkey_credential_id</c> 在全局约束：免用户名登录在认出人之前只有凭据 ID，要按它全局找到通行密钥再认出账号
/// 登录进的注册地取自所属用户；行上的 <see cref="HomeTenantId"/> 只随登记写下，登录不读它
/// 每次登录按认证器的回答刷新 <see cref="SignCount"/> 与 <see cref="BackedUp"/>，签名计数由框架核对
/// 每人最多 10 个，本人的列表按用户取、没有单独的索引；标 <c>[DiffLogIgnored]</c> 不进数据变更日志
/// 宿主配了 <c>XiHan:Security:WebAuthn</c>、系统参数 <c>identity.login-methods</c> 也开放了才可用
/// </remarks>
[SugarTable(TableName = "sys_user_passkey", TableDescription = "账号登记的通行密钥")]
[ActiveUniqueIndex(nameof(CredentialId), Name = "ux_sys_user_passkey_credential_id")]
[DiffLogIgnored]
public sealed class SysUserPasskey : AppAuditedEntity
{
    /// <summary>
    /// 用户
    /// </summary>
    [SugarColumn(ColumnName = "user_id", IsNullable = false, ColumnDescription = "用户")]
    public Guid UserId { get; set; }

    /// <summary>
    /// 用户的注册地（平台账号为 <c>platform</c>）
    /// </summary>
    [SugarColumn(ColumnName = "home_tenant_id", Length = 64, IsNullable = false, ColumnDescription = "用户的注册地（平台账号为 platform）")]
    public string HomeTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 本人起的名字
    /// </summary>
    [SugarColumn(ColumnName = "name", Length = 64, IsNullable = false, ColumnDescription = "本人起的名字")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 凭据 ID（base64url）
    /// </summary>
    /// <remarks>
    /// 规范上限 1023 字节，编码后不超过 1364 个字符
    /// </remarks>
    [SugarColumn(ColumnName = "credential_id", Length = 1400, IsNullable = false, ColumnDescription = "凭据 ID（base64url）")]
    public string CredentialId { get; set; } = string.Empty;

    /// <summary>
    /// COSE 编码的公钥（base64url）
    /// </summary>
    [SugarColumn(ColumnName = "public_key", Length = 2048, IsNullable = false, ColumnDescription = "COSE 编码的公钥（base64url）")]
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// 签名算法的 COSE 编号
    /// </summary>
    [SugarColumn(ColumnName = "algorithm", IsNullable = false, ColumnDescription = "签名算法的 COSE 编号")]
    public int Algorithm { get; set; }

    /// <summary>
    /// 最近一次核对过的签名计数
    /// </summary>
    /// <remarks>
    /// 认证器给的是 32 位无符号数；同步的通行密钥恒为 0
    /// </remarks>
    [SugarColumn(ColumnName = "sign_count", IsNullable = false, ColumnDescription = "最近一次核对过的签名计数")]
    public long SignCount { get; set; }

    /// <summary>
    /// 认证器型号（AAGUID）
    /// </summary>
    /// <remarks>
    /// 不要求证明，认证器可以给全零
    /// </remarks>
    [SugarColumn(ColumnName = "aaguid", IsNullable = false, ColumnDescription = "认证器型号（AAGUID）")]
    public Guid Aaguid { get; set; }

    /// <summary>
    /// 能否在设备之间同步
    /// </summary>
    [SugarColumn(ColumnName = "backup_eligible", IsNullable = false, ColumnDescription = "能否在设备之间同步")]
    public bool BackupEligible { get; set; }

    /// <summary>
    /// 是否已同步备份
    /// </summary>
    /// <remarks>
    /// 每次登录按认证器的回答刷新
    /// </remarks>
    [SugarColumn(ColumnName = "backed_up", IsNullable = false, ColumnDescription = "是否已同步备份")]
    public bool BackedUp { get; set; }

    /// <summary>
    /// 认证器支持的传输方式（逗号分隔，如 <c>internal,hybrid</c>）
    /// </summary>
    [SugarColumn(ColumnName = "transports", Length = 200, IsNullable = true, ColumnDescription = "认证器支持的传输方式（逗号分隔，如 internal,hybrid）")]
    public string? Transports { get; set; }

    /// <summary>
    /// 最近一次用它登录的时间
    /// </summary>
    [SugarColumn(ColumnName = "last_used_at", IsNullable = true, ColumnDescription = "最近一次用它登录的时间")]
    public DateTimeOffset? LastUsedAt { get; set; }
}
