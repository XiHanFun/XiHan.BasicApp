// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 通知实时推送载荷构造
/// </summary>
/// <remarks>
/// SignalR 不经过 MVC 的 JSON 配置，枚举不会被 JsonStringEnumConverter 转成名称，直接发实体字段会是数字。
/// 前端枚举取的是名称（ContentFormat 为 Text/Markdown/Html），拿到数字则格式判断全部落空——
/// Markdown 通知会被当纯文本渲染。故此处显式投影为名称，与 REST 接口保持同一形态。
/// 三个推送点共用本工厂，避免各写各的又出现形态分歧。
/// </remarks>
public static class NotificationRealtimePayload
{
    /// <summary>
    /// 通知类型映射为前端弹窗样式标识
    /// </summary>
    public static string ToRealtimeType(NotificationType notificationType)
    {
        return notificationType switch
        {
            NotificationType.Emergency => "Error",
            NotificationType.Security => "Warning",
            NotificationType.Business => "Success",
            _ => "Info"
        };
    }

    /// <summary>
    /// 构造推送载荷
    /// </summary>
    /// <param name="basicId">载体主键（收件箱行或公告行）</param>
    /// <param name="notificationId">通知主键</param>
    /// <param name="notificationType">通知类型</param>
    /// <param name="title">标题</param>
    /// <param name="content">正文</param>
    /// <param name="contentFormat">正文格式</param>
    /// <param name="sendTime">发送时间</param>
    /// <param name="notificationStatus">收件箱阅读状态（公告广播无此概念，传 null）</param>
    /// <returns>推送载荷</returns>
    public static object Create(
        long basicId,
        long notificationId,
        NotificationType notificationType,
        string title,
        string? content,
        NotificationContentFormat contentFormat,
        DateTimeOffset? sendTime,
        NotificationStatus? notificationStatus = null)
    {
        return new
        {
            type = ToRealtimeType(notificationType),
            title,
            content,
            contentFormat = contentFormat.ToString(),
            basicId,
            notificationId,
            notificationType = notificationType.ToString(),
            notificationStatus = notificationStatus?.ToString(),
            sendTime
        };
    }
}
