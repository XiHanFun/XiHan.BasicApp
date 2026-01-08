#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysNotification
// Guid:ac28152c-d6e9-4396-addb-b479254bad34
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 5:45:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities.Base;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Domain.Aggregates;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统通知实体
/// </summary>
[SugarTable("Sys_Notification", "系统通知表")]
[SugarIndex("IX_SysNotification_UserId", nameof(UserId), OrderByType.Asc)]
[SugarIndex("IX_SysNotification_NotificationType", nameof(NotificationType), OrderByType.Asc)]
[SugarIndex("IX_SysNotification_NotificationStatus", nameof(NotificationStatus), OrderByType.Asc)]
[SugarIndex("IX_SysNotification_SendTime", nameof(SendTime), OrderByType.Desc)]
[SugarIndex("IX_SysNotification_TenantId", nameof(TenantId), OrderByType.Asc)]
public partial class SysNotification : AuditedAggregateRoot<long>
{
    /// <summary>
    /// 租户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "租户ID", IsNullable = true)]
    public virtual long? TenantId { get; set; }

    /// <summary>
    /// 接收用户ID（为空表示全体用户）
    /// </summary>
    [SugarColumn(ColumnDescription = "接收用户ID", IsNullable = true)]
    public virtual long? UserId { get; set; }

    /// <summary>
    /// 发送用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "发送用户ID", IsNullable = true)]
    public virtual long? SenderId { get; set; }

    /// <summary>
    /// 通知类型
    /// </summary>
    [SugarColumn(ColumnDescription = "通知类型")]
    public virtual NotificationType NotificationType { get; set; } = NotificationType.System;

    /// <summary>
    /// 通知标题
    /// </summary>
    [SugarColumn(ColumnDescription = "通知标题", Length = 200, IsNullable = false)]
    public virtual string Title { get; set; } = string.Empty;

    /// <summary>
    /// 通知内容
    /// </summary>
    [SugarColumn(ColumnDescription = "通知内容", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? Content { get; set; }

    /// <summary>
    /// 通知图标
    /// </summary>
    [SugarColumn(ColumnDescription = "通知图标", Length = 100, IsNullable = true)]
    public virtual string? Icon { get; set; }

    /// <summary>
    /// 跳转链接
    /// </summary>
    [SugarColumn(ColumnDescription = "跳转链接", Length = 500, IsNullable = true)]
    public virtual string? Link { get; set; }

    /// <summary>
    /// 业务类型
    /// </summary>
    [SugarColumn(ColumnDescription = "业务类型", Length = 50, IsNullable = true)]
    public virtual string? BusinessType { get; set; }

    /// <summary>
    /// 业务ID
    /// </summary>
    [SugarColumn(ColumnDescription = "业务ID", IsNullable = true)]
    public virtual long? BusinessId { get; set; }

    /// <summary>
    /// 通知状态
    /// </summary>
    [SugarColumn(ColumnDescription = "通知状态")]
    public virtual NotificationStatus NotificationStatus { get; set; } = NotificationStatus.Unread;

    /// <summary>
    /// 阅读时间
    /// </summary>
    [SugarColumn(ColumnDescription = "阅读时间", IsNullable = true)]
    public virtual DateTimeOffset? ReadTime { get; set; }

    /// <summary>
    /// 发送时间
    /// </summary>
    [SugarColumn(ColumnDescription = "发送时间")]
    public virtual DateTimeOffset SendTime { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// 过期时间
    /// </summary>
    [SugarColumn(ColumnDescription = "过期时间", IsNullable = true)]
    public virtual DateTimeOffset? ExpireTime { get; set; }

    /// <summary>
    /// 是否全员通知
    /// </summary>
    [SugarColumn(ColumnDescription = "是否全员通知")]
    public virtual bool IsGlobal { get; set; } = false;

    /// <summary>
    /// 是否需要确认
    /// </summary>
    [SugarColumn(ColumnDescription = "是否需要确认")]
    public virtual bool NeedConfirm { get; set; } = false;

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnDescription = "状态")]
    public virtual YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
