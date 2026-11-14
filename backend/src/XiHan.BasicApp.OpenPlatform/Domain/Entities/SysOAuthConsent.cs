// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.OpenPlatform.Domain.Entities;

/// <summary>
/// 用户对某个应用的授权同意
/// </summary>
/// <remarks>
/// 平台库实体
/// 同意过的范围覆盖本次请求的就不再弹同意页；用户在个人中心撤销即删掉它并吊销这个应用的全部令牌
/// </remarks>
[SugarTable(TableName = "sys_oauth_consent", TableDescription = "用户对某个应用的授权同意")]
[ActiveUniqueIndex(nameof(UserId), nameof(ClientId), Name = "ux_sys_oauth_consent_user_id_client_id")]
[DiffLogIgnored]
public sealed class SysOAuthConsent : AppAuditedEntity
{
    /// <summary>
    /// 用户
    /// </summary>
    [SugarColumn(ColumnName = "user_id", IsNullable = false, ColumnDescription = "用户")]
    public Guid UserId { get; set; }

    /// <summary>
    /// ClientId
    /// </summary>
    [SugarColumn(ColumnName = "client_id", Length = 100, IsNullable = false, ColumnDescription = "ClientId")]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// 同意过的范围（空格分隔，累积）
    /// </summary>
    [SugarColumn(ColumnName = "scopes", Length = 200, IsNullable = false, ColumnDescription = "同意过的范围（空格分隔，累积）")]
    public string Scopes { get; set; } = string.Empty;

    /// <summary>
    /// 最近一次同意的时间
    /// </summary>
    [SugarColumn(ColumnName = "granted_at", IsNullable = false, ColumnDescription = "最近一次同意的时间")]
    public DateTimeOffset GrantedAt { get; set; }
}
