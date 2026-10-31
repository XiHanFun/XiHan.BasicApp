// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Identity.Domain.Entities;

/// <summary>
/// 开放接口凭证。
/// </summary>
/// <remarks>
/// 平台库，跟着账号走，不分租户。个人中心「开放接口」自助创建，每人最多 5 个；签名开放接口按 AppKey 验签（OpenPlatform 经 Contracts 的凭证目录取）。
/// 密钥经 <c>ISecretProtector</c> 加密存（验签要明文，不能存散列），明文只在创建与滚动时返回一次。签名算法与 IP 白名单在账号安全上，作用于本人全部凭证。
/// </remarks>
[ActiveUniqueIndex(nameof(AppKey))]
[SugarIndex("ix_{table}_user_id", nameof(UserId), OrderByType.Asc)]
[DiffLogIgnored]
public sealed class SysUserApiCredential : AppAuditedEntity
{
    /// <summary>
    /// 归属人。
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 名称。
    /// </summary>
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// AppKey（<c>ak_</c> + 24 位字母数字，全局唯一）。
    /// </summary>
    [StringLength(64)]
    public string AppKey { get; set; } = string.Empty;

    /// <summary>
    /// 密钥密文（明文为 <c>sk_</c> + 43 位字母数字）。
    /// </summary>
    [StringLength(1024)]
    public string SecretCipher { get; set; } = string.Empty;

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 最近一次验签通过的时间。
    /// </summary>
    public DateTimeOffset? LastUsedAt { get; set; }
}
