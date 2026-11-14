// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Identity.Domain.Entities;

/// <summary>
/// 密码历史记录
/// </summary>
/// <remarks>
/// 平台库，表 <c>sys_password_history</c>，只增。每次设置密码追加一行，改密时禁止与最近 N 个相同
/// </remarks>
[SugarTable(TableName = "sys_password_history", TableDescription = "密码历史记录")]
[SugarIndex("ix_sys_password_history_user_id_created_at", nameof(UserId), OrderByType.Asc, nameof(CreatedAt), OrderByType.Asc)]
public sealed class SysPasswordHistory : AppRecord
{
    /// <summary>
    /// 用户 ID
    /// </summary>
    [SugarColumn(ColumnName = "user_id", IsNullable = false, ColumnDescription = "用户 ID")]
    public Guid UserId { get; set; }

    /// <summary>
    /// 当时的密码散列
    /// </summary>
    [SugarColumn(ColumnName = "password_hash", Length = 512, IsNullable = false, ColumnDescription = "当时的密码散列")]
    public string PasswordHash { get; set; } = string.Empty;
}
