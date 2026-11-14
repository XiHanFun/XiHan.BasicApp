// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.OpenPlatform.Domain.Entities;

/// <summary>
/// 用户对某个应用的授权同意
/// </summary>
/// <remarks>
/// 记下用户同意过某个应用的哪些范围：同意过的范围覆盖本次请求的就不再弹同意页；范围累积，同意新范围时并进这一行，不另起一行
/// 一个用户对一个应用一行，唯一是（用户、ClientId）的部分唯一索引：撤销是软删除，之后再授权可以重建
/// 撤销同意即删掉它并吊销这个应用拿到的本人全部令牌：令牌出自同意，撤了同意不能还留着可用的令牌
/// 落平台库、按 ClientId 关联应用，与应用、授权码同在一个库；应用删除后同意记录照常列出，只是没有应用名称
/// 只能本人同意或撤销，模仿会话不能做
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
