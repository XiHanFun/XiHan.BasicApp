// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Core.Entities;

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
/// - 用户"删除消息"通过 NotificationStatus=Deleted 状态标记，非物理删除；物理清理由定时任务按策略批量执行
///
/// 状态：
/// - NotificationStatus: Unread/Read/Deleted
///
/// 场景：
/// - 消息中心未读数、已读/未读切换、用户删除消息
/// </remarks>
[SugarTable(TableName = "Sys_User_Notification", TableDescription = "用户通知接收状态表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_NoId_UsId", nameof(TenantId), OrderByType.Asc, nameof(NotificationId), OrderByType.Asc, nameof(UserId), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_TeId_UsId_NoSt", nameof(TenantId), OrderByType.Asc, nameof(UserId), OrderByType.Asc, nameof(NotificationStatus), OrderByType.Asc)]
public partial class SysUserNotification : BasicAppCreationEntity
{
    /// <summary>
    /// 通知ID
    /// </summary>
    [SugarColumn(ColumnName = "Notification_Id", ColumnDescription = "通知ID")]
    public virtual long NotificationId { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnName = "User_Id", ColumnDescription = "用户ID")]
    public virtual long UserId { get; set; }

    /// <summary>
    /// 通知状态（未读/已读/已删除）
    /// </summary>
    [SugarColumn(ColumnName = "Notification_Status", ColumnDescription = "通知状态")]
    public virtual NotificationStatus NotificationStatus { get; set; } = NotificationStatus.Unread;

    /// <summary>
    /// 阅读时间
    /// </summary>
    [SugarColumn(ColumnName = "Read_Time", ColumnDescription = "阅读时间", IsNullable = true)]
    public virtual DateTimeOffset? ReadTime { get; set; }

    /// <summary>
    /// 确认时间
    /// </summary>
    [SugarColumn(ColumnName = "Confirm_Time", ColumnDescription = "确认时间", IsNullable = true)]
    public virtual DateTimeOffset? ConfirmTime { get; set; }

    /// <summary>
    /// 弹窗展示时间（登录后弹窗"仅弹一次"：非空即已弹过，不再弹）
    /// </summary>
    [SugarColumn(ColumnName = "Popup_Shown_Time", ColumnDescription = "弹窗展示时间", IsNullable = true)]
    public virtual DateTimeOffset? PopupShownTime { get; set; }
}
