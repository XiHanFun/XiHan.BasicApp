#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserNotification
// Guid:c5a8e2d1-3b7f-49c6-a12d-8e6f9c4b5d03
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/11 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 用户通知接收状态实体
/// SysNotification × SysUser 的投递关系：承载已读/确认/已删除等个人状态
/// </summary>
/// <remarks>
/// 关联：
/// - NotificationId → SysNotification；UserId → SysUser
///
/// 写入：
/// - TenantId + NotificationId + UserId 唯一（UX_TeId_NoId_UsId）
/// - 由 SysNotification 发布触发批量 INSERT；建议用批处理/分页避免大事务
/// - 用户点击已读时更新 NotificationStatus=Read + ReadTime
///
/// 查询：
/// - 用户未读计数：IX_TeId_UsId_NoSt + WHERE NotificationStatus=Unread
/// - 用户消息中心列表：按 UserId + CreatedTime DESC 分页
///
/// 删除：
/// - 硬删；用户"删除消息"仅软删本表记录，不影响其他用户
///
/// 状态：
/// - NotificationStatus: Unread/Read/Confirmed/Archived/Deleted
///
/// 场景：
/// - 消息中心未读数、已读/未读切换、用户删除消息
/// </remarks>
[SugarTable("SysUserNotification", "用户通知接收状态表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_NoId_UsId", nameof(TenantId), OrderByType.Asc, nameof(NotificationId), OrderByType.Asc, nameof(UserId), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_TeId_UsId_NoSt", nameof(TenantId), OrderByType.Asc, nameof(UserId), OrderByType.Asc, nameof(NotificationStatus), OrderByType.Asc)]
public partial class SysUserNotification : BasicAppCreationEntity
{
    /// <summary>
    /// 通知ID
    /// </summary>
    [SugarColumn(ColumnDescription = "通知ID")]
    public virtual long NotificationId { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "用户ID")]
    public virtual long UserId { get; set; }

    /// <summary>
    /// 通知状态（未读/已读/已删除）
    /// </summary>
    [SugarColumn(ColumnDescription = "通知状态")]
    public virtual NotificationStatus NotificationStatus { get; set; } = NotificationStatus.Unread;

    /// <summary>
    /// 阅读时间
    /// </summary>
    [SugarColumn(ColumnDescription = "阅读时间", IsNullable = true)]
    public virtual DateTimeOffset? ReadTime { get; set; }

    /// <summary>
    /// 确认时间
    /// </summary>
    [SugarColumn(ColumnDescription = "确认时间", IsNullable = true)]
    public virtual DateTimeOffset? ConfirmTime { get; set; }
}
