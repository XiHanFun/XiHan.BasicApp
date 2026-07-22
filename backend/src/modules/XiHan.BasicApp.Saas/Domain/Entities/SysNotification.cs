// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Core.Entities;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统通知实体
/// 站内通知内容主表：承载通知正文、发布状态、目标范围；每用户读取/确认状态由 SysUserNotification 独立维护
/// </summary>
/// <remarks>
/// 职责边界：
/// - 本表只有"一条通知内容"；"谁看过/是否已读"交由 SysUserNotification
/// - 避免循环："通知表 × 用户表"直接笛卡尔积（大租户会爆）
///
/// 关联：
/// - 反向：SysUserNotification.NotificationId
///
/// 写入：
/// - 编辑阶段 IsPublished=false；发布后 IsPublished=true + SendTime
/// - 发布时按 TargetType（All/Role/Department/User）展开生成 SysUserNotification 记录
/// - 可选 ExpirationTime 控制消息过期自动隐藏
///
/// 查询：
/// - 租户最近发布：IX_TeId_SeTi
/// - 按通知类型过滤：IX_NoTy
/// - 按目标类型过滤：IX_TaTy
///
/// 删除：
/// - 仅软删；删除时级联软删所有 SysUserNotification
///
/// 状态：
/// - IsPublished: false=草稿 / true=已发布
/// - NotificationType: System/Announcement/Activity/Personal 等
/// - TargetType: All=全员通知；Role/Department/User=定向通知
///
/// 场景：
/// - 系统公告、版本升级通知、营销活动、告警推送
/// </remarks>
[SugarTable(TableName = "Sys_Notification", TableDescription = "系统通知表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("IX_{table}_NoTy", nameof(NotificationType), OrderByType.Asc)]
[SugarIndex("IX_{table}_TaTy", nameof(TargetType), OrderByType.Asc)]
[SugarIndex("IX_{table}_IsPu", nameof(IsPublished), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_SeTi", nameof(TenantId), OrderByType.Asc, nameof(SendTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_TeId_IsMa", nameof(TenantId), OrderByType.Asc, nameof(IsMandatory), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsBa", nameof(TenantId), OrderByType.Asc, nameof(IsBanner), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsPo", nameof(TenantId), OrderByType.Asc, nameof(IsPopup), OrderByType.Asc)]
public partial class SysNotification : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 发送用户ID
    /// </summary>
    [SugarColumn(ColumnName = "Send_User_Id", ColumnDescription = "发送用户ID", IsNullable = true)]
    public virtual long? SendUserId { get; set; }

    /// <summary>
    /// 通知类型
    /// </summary>
    [SugarColumn(ColumnName = "Notification_Type", ColumnDescription = "通知类型")]
    public virtual NotificationType NotificationType { get; set; } = NotificationType.System;

    /// <summary>
    /// 优先级（与类型正交，决定排序权重/紧急置顶/分级推送）
    /// </summary>
    [SugarColumn(ColumnName = "Priority", ColumnDescription = "优先级")]
    public virtual NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    /// <summary>
    /// 正文格式（纯文本/Markdown/HTML，决定前端渲染方式）
    /// </summary>
    [SugarColumn(ColumnName = "Content_Format", ColumnDescription = "正文格式")]
    public virtual NotificationContentFormat ContentFormat { get; set; } = NotificationContentFormat.Markdown;

    /// <summary>
    /// 投递渠道（[Flags] 按位组合；必含站内信，可叠加邮箱/短信/机器人）
    /// </summary>
    /// <remarks>
    /// 发布时按此扇出：站内信=SysUserNotification 行；邮箱/短信=经用户偏好门控后落 SysEmail/SysSms 走发件箱异步发送；
    /// 机器人=通知级广播（无用户维度），UoW 提交后经框架 Bot 管道直发。
    /// </remarks>
    [SugarColumn(ColumnName = "Delivery_Channels", ColumnDescription = "投递渠道")]
    public virtual MessageChannel DeliveryChannels { get; set; } = MessageChannel.SiteNotification;

    /// <summary>
    /// 通知标题
    /// </summary>
    [SugarColumn(ColumnName = "Title", ColumnDescription = "通知标题", Length = 200, IsNullable = false)]
    public virtual string Title { get; set; } = string.Empty;

    /// <summary>
    /// 通知内容
    /// </summary>
    [SugarColumn(ColumnName = "Content", ColumnDescription = "通知内容", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? Content { get; set; }

    /// <summary>
    /// 通知图标
    /// </summary>
    [SugarColumn(ColumnName = "Icon", ColumnDescription = "通知图标", Length = 100, IsNullable = true)]
    public virtual string? Icon { get; set; }

    /// <summary>
    /// 跳转链接
    /// </summary>
    [SugarColumn(ColumnName = "Link", ColumnDescription = "跳转链接", Length = 500, IsNullable = true)]
    public virtual string? Link { get; set; }

    /// <summary>
    /// 业务类型
    /// </summary>
    [SugarColumn(ColumnName = "Business_Type", ColumnDescription = "业务类型", Length = 50, IsNullable = true)]
    public virtual string? BusinessType { get; set; }

    /// <summary>
    /// 业务ID
    /// </summary>
    [SugarColumn(ColumnName = "Business_Id", ColumnDescription = "业务ID", IsNullable = true)]
    public virtual long? BusinessId { get; set; }

    /// <summary>
    /// 发送时间
    /// </summary>
    [SugarColumn(ColumnName = "Send_Time", ColumnDescription = "发送时间")]
    public virtual DateTimeOffset SendTime { get; set; }

    /// <summary>
    /// 生效开始时间（有效期起点；null=发布即生效）
    /// </summary>
    [SugarColumn(ColumnName = "Start_Time", ColumnDescription = "生效开始时间", IsNullable = true)]
    public virtual DateTimeOffset? StartTime { get; set; }

    /// <summary>
    /// 过期时间（有效期终点；到期自动隐藏）
    /// </summary>
    [SugarColumn(ColumnName = "Expiration_Time", ColumnDescription = "过期时间", IsNullable = true)]
    public virtual DateTimeOffset? ExpirationTime { get; set; }

    /// <summary>
    /// 通知目标类型（All=全员, Role=角色, Department=部门, User=指定用户）
    /// </summary>
    [SugarColumn(ColumnName = "Target_Type", ColumnDescription = "通知目标类型")]
    public virtual NotificationTargetType TargetType { get; set; } = NotificationTargetType.All;

    /// <summary>
    /// 目标值（JSON格式，存储目标ID数组，如 [1,2,3]；TargetType=All时可为null）
    /// </summary>
    [SugarColumn(ColumnName = "Target_Value", ColumnDescription = "目标值", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? TargetValue { get; set; }

    /// <summary>
    /// 是否需要确认（可选确认，记录 ConfirmTime；不阻断进入系统）
    /// </summary>
    [SugarColumn(ColumnName = "Need_Confirm", ColumnDescription = "是否需要确认")]
    public virtual bool NeedConfirm { get; set; } = false;

    /// <summary>
    /// 是否强制阅读（必读公告：有未读则路由守卫/中间件拦截，须读毕方可进入系统）
    /// </summary>
    [SugarColumn(ColumnName = "Is_Mandatory", ColumnDescription = "是否强制阅读")]
    public virtual bool IsMandatory { get; set; } = false;

    /// <summary>
    /// 是否顶部横幅展示（系统维护/版本升级等，置于页面顶部通知条）
    /// </summary>
    [SugarColumn(ColumnName = "Is_Banner", ColumnDescription = "是否顶部横幅")]
    public virtual bool IsBanner { get; set; } = false;

    /// <summary>
    /// 是否登录后弹窗展示（重要公告，每用户仅弹一次，由 SysUserNotification.PopupShownTime 记录）
    /// </summary>
    [SugarColumn(ColumnName = "Is_Popup", ColumnDescription = "是否登录后弹窗")]
    public virtual bool IsPopup { get; set; } = false;

    /// <summary>
    /// 是否已发布（发布后不可编辑/删除）
    /// </summary>
    [SugarColumn(ColumnName = "Is_Published", ColumnDescription = "是否已发布")]
    public virtual bool IsPublished { get; set; } = false;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "Remark", ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
