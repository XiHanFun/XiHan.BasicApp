// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Identity.Domain.Entities;

/// <summary>
/// 密码历史记录
/// </summary>
/// <remarks>
/// 只追加的记录（<c>AppRecord</c>）：每次设置密码追加一行当时的散列，设新密码前取最近 <c>BasicApp:Auth:PasswordHistoryDepth</c> 个连同当前密码比对，不让换回最近用过的密码
/// 只存散列、不存明文，比对靠逐个校验；按用户与创建时间的索引正好取最近几条
/// 一批导入的新用户共用一个初始密码时散列只算一次、每人各记一行：同一个密码分别加盐，破解出一个也就知道了其余的，分开算只会让导入慢上百倍
/// 不修改、不删除，删除用户也不连带
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
