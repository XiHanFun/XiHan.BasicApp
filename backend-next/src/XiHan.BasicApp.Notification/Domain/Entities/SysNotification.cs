// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Notification.Domain.Entities;

/// <summary>
/// 通知公告（租户库实体，平台一侧租户为 <c>platform</c>）
/// </summary>
/// <remarks>
/// 4.x 在平台库。先存草稿，发布时按目标解析出接收人、写进各自的收件箱并实时推送；发布后不能再改
/// </remarks>
[SugarIndex("ix_{table}_tenant_id_created_at", nameof(TenantId), OrderByType.Asc, nameof(CreatedAt), OrderByType.Asc)]
public sealed class SysNotification : TenantScopedAuditedEntity
{
    /// <summary>
    /// 标题
    /// </summary>
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 正文
    /// </summary>
    [LongText]
    public string? Content { get; set; }

    /// <summary>
    /// 正文格式
    /// </summary>
    public NotificationContentFormat ContentFormat { get; set; } = NotificationContentFormat.Markdown;

    /// <summary>
    /// 类型
    /// </summary>
    public NotificationType NotificationType { get; set; } = NotificationType.System;

    /// <summary>
    /// 优先级
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    /// <summary>
    /// 图标（Iconify 名）
    /// </summary>
    [StringLength(100)]
    public string? Icon { get; set; }

    /// <summary>
    /// 点开后跳转的链接（站内路由或外部地址）
    /// </summary>
    [StringLength(500)]
    public string? Link { get; set; }

    /// <summary>
    /// 关联的业务类型（如 <c>workflow.task</c>）
    /// </summary>
    [StringLength(100)]
    public string? BusinessType { get; set; }

    /// <summary>
    /// 关联的业务 ID
    /// </summary>
    [StringLength(100)]
    public string? BusinessId { get; set; }

    /// <summary>
    /// 投递渠道（按位组合，站内信必有）
    /// </summary>
    public MessageChannels DeliveryChannels { get; set; } = MessageChannels.InApp;

    /// <summary>
    /// 投递目标
    /// </summary>
    public NotificationTargetType TargetType { get; set; } = NotificationTargetType.All;

    /// <summary>
    /// 目标 ID（角色、部门或用户，JSON 数组）
    /// </summary>
    /// <remarks>
    /// <see cref="NotificationTargetType.All"/> 为空
    /// </remarks>
    [LongText]
    public string? TargetIds { get; set; }

    /// <summary>
    /// 需要收件人确认
    /// </summary>
    public bool NeedConfirm { get; set; }

    /// <summary>
    /// 强制阅读：未读前进入页面都被拦下
    /// </summary>
    public bool IsMandatory { get; set; }

    /// <summary>
    /// 未读时在页面顶部显示横幅
    /// </summary>
    public bool IsBanner { get; set; }

    /// <summary>
    /// 登录后弹窗（每人只弹一次）
    /// </summary>
    public bool IsPopup { get; set; }

    /// <summary>
    /// 收件箱里从何时起可见
    /// </summary>
    /// <remarks>
    /// 为空即发布即可见
    /// </remarks>
    public DateTimeOffset? StartsAt { get; set; }

    /// <summary>
    /// 到期时间（不含）
    /// </summary>
    /// <remarks>
    /// 之后收件箱里不再显示。为空即长期有效
    /// </remarks>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// 是否已发布
    /// </summary>
    public bool IsPublished { get; set; }

    /// <summary>
    /// 发布时间
    /// </summary>
    public DateTimeOffset? PublishedAt { get; set; }

    /// <summary>
    /// 发布人
    /// </summary>
    public Guid? PublisherId { get; set; }

    /// <summary>
    /// 发布时解析出的接收人数
    /// </summary>
    public int RecipientCount { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [StringLength(500)]
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
/// <remarks>
/// 4.x 同
/// </remarks>
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
/// <remarks>
/// 4.x 同
/// </remarks>
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
/// 4.x 同。HTML 正文由前端按白名单清洗后渲染
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
/// <remarks>
/// 4.x 同
/// </remarks>
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
