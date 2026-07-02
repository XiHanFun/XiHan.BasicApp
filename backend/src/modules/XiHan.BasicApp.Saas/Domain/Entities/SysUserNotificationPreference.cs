#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserNotificationPreference
// Guid:b3e1f8a2-6c4d-4e9a-8f1b-2d5c7e9a0b34
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统用户通知偏好实体
/// SysUser 的 1:1 通知偏好扩展：承载个人通知偏好（接收渠道 × 通知类型），与安全/资料/通用设置解耦。
/// </summary>
/// <remarks>
/// 命名说明：
/// - 专指"通知偏好"，与通用的全场景设置同步实体 <see cref="SysUserSetting"/>（主题/布局/页面设置等）区分，避免歧义
///
/// 职责边界：
/// - 与 SysUser 一对一（UX_UsId），独立表便于偏好项独立演进，不污染主表
/// - 仅存"用户个人意愿开关"；实际是否发送还需结合系统策略（如安全告警强制下发）
///
/// 关联：
/// - UserId → SysUser（一对一）
///
/// 写入：
/// - UserId 唯一（UX_UsId）
/// - 首次读取时若无记录，由应用层按默认值惰性创建（默认开启，短信/机器人/营销默认关闭）
///
/// 查询：
/// - 个人中心通知偏好读取：按 UserId 查
///
/// 删除：
/// - 仅软删；随 SysUser 级联软删
///
/// 场景：
/// - 个人中心"通知偏好"设置：渠道（站内信/邮箱/短信/推送/机器人）× 类型（公告/任务/审批/安全/营销）
/// - 营销类可随时关闭（GDPR 合规）；安全告警建议始终开启
/// </remarks>
[SugarTable(TableName = "Sys_User_Notification_Preference", TableDescription = "系统用户通知偏好表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_UsId", nameof(UserId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc, true)]
public partial class SysUserNotificationPreference : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnName = "User_Id", ColumnDescription = "用户ID", IsNullable = false)]
    public virtual long UserId { get; set; }

    // ── 接收渠道总开关 ──────────────────────────────

    /// <summary>
    /// 站内信通知
    /// </summary>
    [SugarColumn(ColumnName = "Channel_In_App", ColumnDescription = "站内信通知")]
    public virtual bool ChannelInApp { get; set; } = true;

    /// <summary>
    /// 邮箱通知
    /// </summary>
    [SugarColumn(ColumnName = "Channel_Email", ColumnDescription = "邮箱通知")]
    public virtual bool ChannelEmail { get; set; } = true;

    /// <summary>
    /// 短信通知
    /// </summary>
    [SugarColumn(ColumnName = "Channel_Sms", ColumnDescription = "短信通知")]
    public virtual bool ChannelSms { get; set; } = false;

    /// <summary>
    /// 推送通知
    /// </summary>
    [SugarColumn(ColumnName = "Channel_Push", ColumnDescription = "推送通知")]
    public virtual bool ChannelPush { get; set; } = true;

    /// <summary>
    /// 机器人通知（钉钉/飞书/企微/Telegram，对应 <see cref="MessageChannel.Bot"/> 通道整体）
    /// 默认关闭：机器人投递需用户侧存在可达绑定才有意义
    /// </summary>
    [SugarColumn(ColumnName = "Channel_Bot", ColumnDescription = "机器人通知")]
    public virtual bool ChannelBot { get; set; } = false;

    // ── 通知类型开关 ──────────────────────────────

    /// <summary>
    /// 系统公告
    /// </summary>
    [SugarColumn(ColumnName = "Type_Announcement", ColumnDescription = "系统公告")]
    public virtual bool TypeAnnouncement { get; set; } = true;

    /// <summary>
    /// 任务提醒
    /// </summary>
    [SugarColumn(ColumnName = "Type_Task", ColumnDescription = "任务提醒")]
    public virtual bool TypeTask { get; set; } = true;

    /// <summary>
    /// 审批通知
    /// </summary>
    [SugarColumn(ColumnName = "Type_Approval", ColumnDescription = "审批通知")]
    public virtual bool TypeApproval { get; set; } = true;

    /// <summary>
    /// 安全告警
    /// </summary>
    [SugarColumn(ColumnName = "Type_Security", ColumnDescription = "安全告警")]
    public virtual bool TypeSecurity { get; set; } = true;

    /// <summary>
    /// 营销消息
    /// </summary>
    [SugarColumn(ColumnName = "Type_Marketing", ColumnDescription = "营销消息")]
    public virtual bool TypeMarketing { get; set; } = false;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "Remark", ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
