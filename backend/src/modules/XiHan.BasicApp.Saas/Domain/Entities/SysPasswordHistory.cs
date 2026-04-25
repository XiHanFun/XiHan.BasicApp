#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysPasswordHistory
// Guid:c9d0e1f2-a3b4-5678-4567-901234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 密码历史实体
/// 记录用户历史密码哈希，用于密码修改时防止重复使用近期旧密码
/// </summary>
/// <remarks>
/// 关联：
/// - UserId → SysUser
///
/// 写入：
/// - 每次密码修改成功后追加一条（旧密码哈希）
/// - 服务层在修改密码前查询最近 N 条历史，校验新密码不与历史重复
///
/// 查询：
/// - 密码校验：IX_TeId_UsId + WHERE UserId=? ORDER BY ChangedTime DESC LIMIT N
///
/// 删除：
/// - 硬删；可按保留策略清理超过 N 条的历史记录
/// </remarks>
[SugarTable("SysPasswordHistory", "密码历史表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_UsId", nameof(TenantId), OrderByType.Asc, nameof(UserId), OrderByType.Asc)]
public partial class SysPasswordHistory : BasicAppCreationEntity
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "用户ID", IsNullable = false)]
    public virtual long UserId { get; set; }

    /// <summary>
    /// 密码哈希（历史密码的哈希值）
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(ColumnDescription = "密码哈希", Length = 200, IsNullable = false)]
    public virtual string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// 密码修改时间
    /// </summary>
    [SugarColumn(ColumnDescription = "密码修改时间")]
    public virtual DateTimeOffset ChangedTime { get; set; }
}
