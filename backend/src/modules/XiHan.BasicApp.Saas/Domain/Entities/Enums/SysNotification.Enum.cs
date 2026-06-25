#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysNotification.Enum
// Guid:dd46a2aa-4292-4d09-b629-eead81506b16
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 通知类型枚举
/// 内容分类（与展示分级/优先级正交）：用于按类筛选、按类推送策略、图标着色
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// 系统公告（升级、停机维护）
    /// </summary>
    [Description("系统公告")]
    System = 1,

    /// <summary>
    /// 安全通知（密码策略、风险提醒）
    /// </summary>
    [Description("安全通知")]
    Security = 2,

    /// <summary>
    /// 业务通知（新功能上线、运营活动）
    /// </summary>
    [Description("业务通知")]
    Business = 3,

    /// <summary>
    /// 待办通知（审批、任务）
    /// </summary>
    [Description("待办通知")]
    Todo = 4,

    /// <summary>
    /// 紧急通知（故障、攻击、停服）
    /// </summary>
    [Description("紧急通知")]
    Emergency = 5
}

/// <summary>
/// 通知优先级枚举
/// 与类型正交：决定排序权重、紧急置顶、分级推送强度
/// </summary>
public enum NotificationPriority
{
    /// <summary>
    /// 低
    /// </summary>
    [Description("低")]
    Low = 1,

    /// <summary>
    /// 普通
    /// </summary>
    [Description("普通")]
    Normal = 2,

    /// <summary>
    /// 高
    /// </summary>
    [Description("高")]
    High = 3,

    /// <summary>
    /// 紧急（置顶 + 强提醒）
    /// </summary>
    [Description("紧急")]
    Urgent = 4
}

/// <summary>
/// 通知正文格式枚举
/// 决定前端如何渲染 Content：纯文本 / Markdown / HTML
/// </summary>
public enum NotificationContentFormat
{
    /// <summary>
    /// 纯文本
    /// </summary>
    [Description("纯文本")]
    Text = 1,

    /// <summary>
    /// Markdown（默认，配合 MdEditor）
    /// </summary>
    [Description("Markdown")]
    Markdown = 2,

    /// <summary>
    /// HTML
    /// </summary>
    [Description("HTML")]
    Html = 3
}

/// <summary>
/// 通知目标类型枚举
/// 决定通知的接收方范围：全员/角色/部门/指定用户
/// </summary>
public enum NotificationTargetType
{
    /// <summary>
    /// 全员（所有用户）
    /// </summary>
    [Description("全员")]
    All = 0,

    /// <summary>
    /// 角色（按角色推送）
    /// </summary>
    [Description("角色")]
    Role = 1,

    /// <summary>
    /// 部门（按部门推送）
    /// </summary>
    [Description("部门")]
    Department = 2,

    /// <summary>
    /// 指定用户（按用户ID推送）
    /// </summary>
    [Description("用户")]
    User = 3
}
