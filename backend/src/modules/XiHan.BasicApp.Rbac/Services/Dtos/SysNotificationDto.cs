#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysNotificationDto
// Guid:c5d6e7f8-a9b0-1234-5678-901c23456789
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/8 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Services.Dtos;

/// <summary>
/// 系统通知创建 DTO
/// </summary>
public class SysNotificationCreateDto : RbacCreationDtoBase
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 接收用户ID
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// 发送用户ID
    /// </summary>
    public long? SenderId { get; set; }

    /// <summary>
    /// 通知类型
    /// </summary>
    public NotificationType NotificationType { get; set; } = NotificationType.System;

    /// <summary>
    /// 通知标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 通知内容
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 通知图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 跳转链接
    /// </summary>
    public string? Link { get; set; }

    /// <summary>
    /// 业务类型
    /// </summary>
    public string? BusinessType { get; set; }

    /// <summary>
    /// 业务ID
    /// </summary>
    public long? BusinessId { get; set; }

    /// <summary>
    /// 发送时间
    /// </summary>
    public DateTimeOffset SendTime { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTimeOffset? ExpireTime { get; set; }

    /// <summary>
    /// 是否全员通知
    /// </summary>
    public bool IsGlobal { get; set; } = false;

    /// <summary>
    /// 是否需要确认
    /// </summary>
    public bool NeedConfirm { get; set; } = false;

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 系统通知更新 DTO
/// </summary>
public class SysNotificationUpdateDto : RbacUpdateDtoBase
{
    /// <summary>
    /// 通知状态
    /// </summary>
    public NotificationStatus NotificationStatus { get; set; }

    /// <summary>
    /// 阅读时间
    /// </summary>
    public DateTimeOffset? ReadTime { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 系统通知查询 DTO
/// </summary>
public class SysNotificationGetDto : RbacFullAuditedDtoBase
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 接收用户ID
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// 发送用户ID
    /// </summary>
    public long? SenderId { get; set; }

    /// <summary>
    /// 通知类型
    /// </summary>
    public NotificationType NotificationType { get; set; } = NotificationType.System;

    /// <summary>
    /// 通知标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 通知内容
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 通知图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 跳转链接
    /// </summary>
    public string? Link { get; set; }

    /// <summary>
    /// 业务类型
    /// </summary>
    public string? BusinessType { get; set; }

    /// <summary>
    /// 业务ID
    /// </summary>
    public long? BusinessId { get; set; }

    /// <summary>
    /// 通知状态
    /// </summary>
    public NotificationStatus NotificationStatus { get; set; } = NotificationStatus.Unread;

    /// <summary>
    /// 阅读时间
    /// </summary>
    public DateTimeOffset? ReadTime { get; set; }

    /// <summary>
    /// 发送时间
    /// </summary>
    public DateTimeOffset SendTime { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTimeOffset? ExpireTime { get; set; }

    /// <summary>
    /// 是否全员通知
    /// </summary>
    public bool IsGlobal { get; set; } = false;

    /// <summary>
    /// 是否需要确认
    /// </summary>
    public bool NeedConfirm { get; set; } = false;

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
