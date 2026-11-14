// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Notification.Domain.Entities;

/// <summary>
/// 通知公告（租户库实体，平台一侧租户为 <c>platform</c>）
/// </summary>
/// <remarks>
/// 落租户库（平台一侧租户为 <c>platform</c>）：通知与收件箱随租户分库隔离，收件箱查询能和它在同一个库里联表
/// 先存草稿，发布时按目标解析出此刻正常的接收人、一次展开成收件箱行：之后组织与角色变动不影响已发出的这条，所以发布后不能再改；目标 ID 只在展开时用，存成 JSON 列，接收人数冗余记在行上
/// 投递渠道按位组合、站内信必有：邮件、短信与机器人只是站内信之外的补充，经发件箱随发布一起提交；开了的外发渠道先核对能发，发不出即整次发布失败，不悄悄只发站内信
/// 别的包经 Contracts 的 <c>IInboxDelivery</c> 建一条指定用户的通知并立即发布（Workflow 的待办、Identity 的账号安全通知）：复用同一套收件箱与推送，不另建表
/// 列表的数据范围按创建人套用（方案 5.9），收件箱不受影响
/// 软删除；删除已发布的连同收件箱条目一起软删除，已入发件箱的外发照常发出；到期只是不再显示，没有清理作业
/// </remarks>
[SugarTable(TableName = "sys_notification", TableDescription = "通知公告（租户库实体，平台一侧租户为 platform）")]
[SugarIndex("ix_sys_notification_tenant_id_created_at", nameof(TenantId), OrderByType.Asc, nameof(CreatedAt), OrderByType.Asc)]
public sealed class SysNotification : TenantScopedAuditedEntity
{
    /// <summary>
    /// 标题
    /// </summary>
    [SugarColumn(ColumnName = "title", Length = 200, IsNullable = false, ColumnDescription = "标题")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 正文
    /// </summary>
    [SugarColumn(ColumnName = "content", IsNullable = true, ColumnDescription = "正文")]
    [LongText]
    public string? Content { get; set; }

    /// <summary>
    /// 正文格式
    /// </summary>
    [SugarColumn(ColumnName = "content_format", IsNullable = false, ColumnDescription = "正文格式")]
    public NotificationContentFormat ContentFormat { get; set; } = NotificationContentFormat.Markdown;

    /// <summary>
    /// 类型
    /// </summary>
    [SugarColumn(ColumnName = "notification_type", IsNullable = false, ColumnDescription = "类型")]
    public NotificationType NotificationType { get; set; } = NotificationType.System;

    /// <summary>
    /// 优先级
    /// </summary>
    [SugarColumn(ColumnName = "priority", IsNullable = false, ColumnDescription = "优先级")]
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    /// <summary>
    /// 图标（Iconify 名）
    /// </summary>
    [SugarColumn(ColumnName = "icon", Length = 100, IsNullable = true, ColumnDescription = "图标（Iconify 名）")]
    public string? Icon { get; set; }

    /// <summary>
    /// 点开后跳转的链接（站内路由或外部地址）
    /// </summary>
    [SugarColumn(ColumnName = "link", Length = 500, IsNullable = true, ColumnDescription = "点开后跳转的链接（站内路由或外部地址）")]
    public string? Link { get; set; }

    /// <summary>
    /// 关联的业务类型（如 <c>workflow.task</c>）
    /// </summary>
    [SugarColumn(ColumnName = "business_type", Length = 100, IsNullable = true, ColumnDescription = "关联的业务类型（如 workflow.task）")]
    public string? BusinessType { get; set; }

    /// <summary>
    /// 关联的业务 ID
    /// </summary>
    [SugarColumn(ColumnName = "business_id", Length = 100, IsNullable = true, ColumnDescription = "关联的业务 ID")]
    public string? BusinessId { get; set; }

    /// <summary>
    /// 投递渠道（按位组合，站内信必有）
    /// </summary>
    [SugarColumn(ColumnName = "delivery_channels", IsNullable = false, ColumnDescription = "投递渠道（按位组合，站内信必有）")]
    public MessageChannels DeliveryChannels { get; set; } = MessageChannels.InApp;

    /// <summary>
    /// 投递目标
    /// </summary>
    [SugarColumn(ColumnName = "target_type", IsNullable = false, ColumnDescription = "投递目标")]
    public NotificationTargetType TargetType { get; set; } = NotificationTargetType.All;

    /// <summary>
    /// 目标 ID（角色、部门或用户，JSON 数组）
    /// </summary>
    /// <remarks>
    /// <see cref="NotificationTargetType.All"/> 为空
    /// </remarks>
    [SugarColumn(ColumnName = "target_ids", IsNullable = true, ColumnDescription = "目标 ID（角色、部门或用户，JSON 数组）")]
    [LongText]
    public string? TargetIds { get; set; }

    /// <summary>
    /// 需要收件人确认
    /// </summary>
    [SugarColumn(ColumnName = "need_confirm", IsNullable = false, ColumnDescription = "需要收件人确认")]
    public bool NeedConfirm { get; set; }

    /// <summary>
    /// 强制阅读：未读前进入页面都被拦下
    /// </summary>
    [SugarColumn(ColumnName = "is_mandatory", IsNullable = false, ColumnDescription = "强制阅读：未读前进入页面都被拦下")]
    public bool IsMandatory { get; set; }

    /// <summary>
    /// 未读时在页面顶部显示横幅
    /// </summary>
    [SugarColumn(ColumnName = "is_banner", IsNullable = false, ColumnDescription = "未读时在页面顶部显示横幅")]
    public bool IsBanner { get; set; }

    /// <summary>
    /// 登录后弹窗（每人只弹一次）
    /// </summary>
    [SugarColumn(ColumnName = "is_popup", IsNullable = false, ColumnDescription = "登录后弹窗（每人只弹一次）")]
    public bool IsPopup { get; set; }

    /// <summary>
    /// 收件箱里从何时起可见
    /// </summary>
    /// <remarks>
    /// 为空即发布即可见
    /// </remarks>
    [SugarColumn(ColumnName = "starts_at", IsNullable = true, ColumnDescription = "收件箱里从何时起可见")]
    public DateTimeOffset? StartsAt { get; set; }

    /// <summary>
    /// 到期时间（不含）
    /// </summary>
    /// <remarks>
    /// 之后收件箱里不再显示。为空即长期有效
    /// </remarks>
    [SugarColumn(ColumnName = "expires_at", IsNullable = true, ColumnDescription = "到期时间（不含）")]
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// 是否已发布
    /// </summary>
    [SugarColumn(ColumnName = "is_published", IsNullable = false, ColumnDescription = "是否已发布")]
    public bool IsPublished { get; set; }

    /// <summary>
    /// 发布时间
    /// </summary>
    [SugarColumn(ColumnName = "published_at", IsNullable = true, ColumnDescription = "发布时间")]
    public DateTimeOffset? PublishedAt { get; set; }

    /// <summary>
    /// 发布人
    /// </summary>
    [SugarColumn(ColumnName = "publisher_id", IsNullable = true, ColumnDescription = "发布人")]
    public Guid? PublisherId { get; set; }

    /// <summary>
    /// 发布时解析出的接收人数
    /// </summary>
    [SugarColumn(ColumnName = "recipient_count", IsNullable = false, ColumnDescription = "发布时解析出的接收人数")]
    public int RecipientCount { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "remark", Length = 500, IsNullable = true, ColumnDescription = "备注")]
    public string? Remark { get; set; }
}

/// <summary>
/// 投递渠道
/// </summary>
/// <remarks>
/// 按位组合存在通知上；站内信必有
/// </remarks>
[Flags]
public enum MessageChannels
{
    /// <summary>
    /// 不投递
    /// </summary>
    None = 0,

    /// <summary>
    /// 站内信（收件箱与实时推送）
    /// </summary>
    InApp = 1,

    /// <summary>
    /// 邮件
    /// </summary>
    Email = 2,

    /// <summary>
    /// 短信
    /// </summary>
    Sms = 4,

    /// <summary>
    /// 机器人（钉钉、飞书、企业微信的群机器人）
    /// </summary>
    /// <remarks>
    /// 一条通知发到本侧每个服务商的缺省机器人
    /// </remarks>
    Bot = 8,
}

/// <summary>
/// 通知类型
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// 系统公告
    /// </summary>
    System = 1,

    /// <summary>
    /// 安全通知
    /// </summary>
    Security = 2,

    /// <summary>
    /// 业务通知
    /// </summary>
    Business = 3,

    /// <summary>
    /// 待办通知
    /// </summary>
    Todo = 4,

    /// <summary>
    /// 紧急通知
    /// </summary>
    Emergency = 5,
}

/// <summary>
/// 优先级
/// </summary>
public enum NotificationPriority
{
    /// <summary>
    /// 低
    /// </summary>
    Low = 1,

    /// <summary>
    /// 普通
    /// </summary>
    Normal = 2,

    /// <summary>
    /// 高
    /// </summary>
    High = 3,

    /// <summary>
    /// 紧急
    /// </summary>
    Urgent = 4,
}

/// <summary>
/// 正文格式
/// </summary>
/// <remarks>
/// HTML 正文由前端按白名单清洗后渲染
/// </remarks>
public enum NotificationContentFormat
{
    /// <summary>
    /// 纯文本
    /// </summary>
    Text = 1,

    /// <summary>
    /// Markdown
    /// </summary>
    Markdown = 2,

    /// <summary>
    /// HTML
    /// </summary>
    Html = 3,
}

/// <summary>
/// 投递目标
/// </summary>
public enum NotificationTargetType
{
    /// <summary>
    /// 当前一侧的全部正常账号
    /// </summary>
    All = 0,

    /// <summary>
    /// 持有指定角色的用户
    /// </summary>
    Role = 1,

    /// <summary>
    /// 指定部门及其下级部门的成员（只在租户一侧）
    /// </summary>
    Department = 2,

    /// <summary>
    /// 指定用户
    /// </summary>
    User = 3,
}
