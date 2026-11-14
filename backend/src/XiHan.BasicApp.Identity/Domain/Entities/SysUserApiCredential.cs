// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Identity.Domain.Entities;

/// <summary>
/// 开放接口凭证
/// </summary>
/// <remarks>
/// 平台库，跟着账号走，不分租户。个人中心「开放接口」自助创建，每人最多 5 个；签名开放接口按 AppKey 验签（OpenPlatform 经 Contracts 的凭证目录取）
/// 密钥经 <c>ISecretProtector</c> 加密存（验签要明文，不能存散列），明文只在创建与滚动时返回一次。签名算法与 IP 白名单在账号安全上，作用于本人全部凭证
/// </remarks>
[SugarTable(TableName = "sys_user_api_credential", TableDescription = "开放接口凭证")]
[ActiveUniqueIndex(nameof(AppKey), Name = "ux_sys_user_api_credential_app_key")]
[SugarIndex("ix_sys_user_api_credential_user_id", nameof(UserId), OrderByType.Asc)]
[DiffLogIgnored]
public sealed class SysUserApiCredential : AppAuditedEntity
{
    /// <summary>
    /// 归属人
    /// </summary>
    [SugarColumn(ColumnName = "user_id", IsNullable = false, ColumnDescription = "归属人")]
    public Guid UserId { get; set; }

    /// <summary>
    /// 名称
    /// </summary>
    [SugarColumn(ColumnName = "name", Length = 100, IsNullable = false, ColumnDescription = "名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// AppKey（<c>ak_</c> + 24 位字母数字，全局唯一）
    /// </summary>
    [SugarColumn(ColumnName = "app_key", Length = 64, IsNullable = false, ColumnDescription = "AppKey（ak_ + 24 位字母数字，全局唯一）")]
    public string AppKey { get; set; } = string.Empty;

    /// <summary>
    /// 密钥密文（明文为 <c>sk_</c> + 43 位字母数字）
    /// </summary>
    [SugarColumn(ColumnName = "secret_cipher", Length = 1024, IsNullable = false, ColumnDescription = "密钥密文（明文为 sk_ + 43 位字母数字）")]
    public string SecretCipher { get; set; } = string.Empty;

    /// <summary>
    /// 是否启用
    /// </summary>
    [SugarColumn(ColumnName = "is_enabled", IsNullable = false, ColumnDescription = "是否启用")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 最近一次验签通过的时间
    /// </summary>
    [SugarColumn(ColumnName = "last_used_at", IsNullable = true, ColumnDescription = "最近一次验签通过的时间")]
    public DateTimeOffset? LastUsedAt { get; set; }
}
