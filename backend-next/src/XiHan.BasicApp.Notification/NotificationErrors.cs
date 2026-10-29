// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Notification;

/// <summary>
/// 通知包的错误码：<c>NOTIFICATION_&lt;KIND&gt;_&lt;NNN&gt;</c>。
/// </summary>
public static class NotificationErrors
{
    /// <summary>
    /// 通知不存在（或不在当前一侧）；参数 <c>id</c>。
    /// </summary>
    public const string NotificationNotFound = "NOTIFICATION_NOTFOUND_001";

    /// <summary>
    /// 收件箱里没有这一条（或不是本人的）；参数 <c>id</c>。
    /// </summary>
    public const string InboxItemNotFound = "NOTIFICATION_NOTFOUND_002";

    /// <summary>
    /// 通知已经发布，不能再改或重复发布；参数 <c>id</c>。
    /// </summary>
    public const string AlreadyPublished = "NOTIFICATION_CONFLICT_001";

    /// <summary>
    /// 通知还没发布（提醒与接收人只对已发布的）；参数 <c>id</c>。
    /// </summary>
    public const string NotPublished = "NOTIFICATION_CONFLICT_002";

    /// <summary>
    /// 目标为角色、部门或用户时至少要指定一个；参数 <c>targetType</c>。
    /// </summary>
    public const string TargetsRequired = "NOTIFICATION_VALIDATION_001";

    /// <summary>
    /// 平台一侧没有部门，不能按部门投递。
    /// </summary>
    public const string DepartmentNeedsTenant = "NOTIFICATION_VALIDATION_002";

    /// <summary>
    /// 到期时间须晚于可见起始时间。
    /// </summary>
    public const string WindowInvalid = "NOTIFICATION_VALIDATION_003";

    /// <summary>
    /// 按目标解析不出任何正常的接收人，不能发布。
    /// </summary>
    public const string NoRecipients = "NOTIFICATION_VALIDATION_004";

    /// <summary>
    /// 不支持的筛选或排序。
    /// </summary>
    public const string QueryUnsupported = "NOTIFICATION_VALIDATION_005";

    /// <summary>
    /// 标题不能只有空白。
    /// </summary>
    public const string TitleBlank = "NOTIFICATION_VALIDATION_006";

    /// <summary>
    /// 建好即发布需要发布权限（<c>notification.notifications.publish</c>）。
    /// </summary>
    public const string PublishDenied = "NOTIFICATION_DENIED_001";

    /// <summary>
    /// 没有装解析这类目标的包；参数 <c>targetType</c>。
    /// </summary>
    public const string RecipientSourceMissing = "NOTIFICATION_UNAVAILABLE_001";
}
