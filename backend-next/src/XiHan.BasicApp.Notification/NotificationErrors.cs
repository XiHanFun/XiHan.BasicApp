// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Notification;

/// <summary>
/// 通知包的错误码。
/// </summary>
/// <remarks>
/// 格式：<c>NOTIFICATION_&lt;KIND&gt;_&lt;NNN&gt;</c>。
/// </remarks>
public static class NotificationErrors
{
    /// <summary>
    /// 通知不存在（或不在当前一侧）。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string NotificationNotFound = "NOTIFICATION_NOTFOUND_001";

    /// <summary>
    /// 收件箱里没有这一条（或不是本人的）。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string InboxItemNotFound = "NOTIFICATION_NOTFOUND_002";

    /// <summary>
    /// 通知已经发布，不能再改或重复发布。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string AlreadyPublished = "NOTIFICATION_CONFLICT_001";

    /// <summary>
    /// 通知还没发布（提醒与接收人只对已发布的）。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string NotPublished = "NOTIFICATION_CONFLICT_002";

    /// <summary>
    /// 目标为角色、部门或用户时至少要指定一个。
    /// </summary>
    /// <remarks>
    /// 参数 <c>targetType</c>。
    /// </remarks>
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
    /// Telegram 平台设置的 JSON 写错了。
    /// </summary>
    /// <remarks>
    /// 参数 <c>key</c>、<c>detail</c>。
    /// </remarks>
    public const string TelegramSettingsInvalid = "NOTIFICATION_VALIDATION_015";

    /// <summary>
    /// Telegram 机器人不存在。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string TelegramBotNotFound = "NOTIFICATION_NOTFOUND_009";

    /// <summary>
    /// 已有这个名称的 Telegram 机器人。
    /// </summary>
    /// <remarks>
    /// 参数 <c>name</c>。
    /// </remarks>
    public const string TelegramBotNameTaken = "NOTIFICATION_CONFLICT_008";

    /// <summary>
    /// 令牌须形如「数字:字符串」（BotFather 给的）。
    /// </summary>
    public const string TelegramTokenInvalid = "NOTIFICATION_VALIDATION_012";

    /// <summary>
    /// ID 列表里有不是整数的项。
    /// </summary>
    /// <remarks>
    /// 参数 <c>field</c>、<c>item</c>。
    /// </remarks>
    public const string TelegramIdsInvalid = "NOTIFICATION_VALIDATION_013";

    /// <summary>
    /// 命令名只能是小写字母、数字与下划线，最长 32。
    /// </summary>
    /// <remarks>
    /// 参数 <c>item</c>。
    /// </remarks>
    public const string TelegramCommandsInvalid = "NOTIFICATION_VALIDATION_014";

    /// <summary>
    /// 机器人配置不存在。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string BotConfigNotFound = "NOTIFICATION_NOTFOUND_008";

    /// <summary>
    /// Webhook 地址须是所选服务商的官方 https 地址。
    /// </summary>
    /// <remarks>
    /// 参数 <c>url</c>。
    /// </remarks>
    public const string BotWebhookInvalid = "NOTIFICATION_VALIDATION_011";

    /// <summary>
    /// 消息模板不存在（或不在当前一侧）。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string TemplateNotFound = "NOTIFICATION_NOTFOUND_003";

    /// <summary>
    /// 邮件配置不存在。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string EmailConfigNotFound = "NOTIFICATION_NOTFOUND_004";

    /// <summary>
    /// 短信配置不存在。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string SmsConfigNotFound = "NOTIFICATION_NOTFOUND_005";

    /// <summary>
    /// 邮件发送记录不存在。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string EmailRecordNotFound = "NOTIFICATION_NOTFOUND_006";

    /// <summary>
    /// 短信发送记录不存在。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string SmsRecordNotFound = "NOTIFICATION_NOTFOUND_007";

    /// <summary>
    /// 同一侧、同一渠道里已有这个编码的模板。
    /// </summary>
    /// <remarks>
    /// 参数 <c>code</c>。
    /// </remarks>
    public const string TemplateCodeTaken = "NOTIFICATION_CONFLICT_003";

    /// <summary>
    /// 本侧已有这个编码的配置。
    /// </summary>
    /// <remarks>
    /// 参数 <c>code</c>。
    /// </remarks>
    public const string ConfigCodeTaken = "NOTIFICATION_CONFLICT_004";

    /// <summary>
    /// 缺省配置须启用，不能停用或删除。
    /// </summary>
    /// <remarks>
    /// 停用的配置不能设为缺省；参数 <c>code</c>。
    /// </remarks>
    public const string DefaultConfigLocked = "NOTIFICATION_CONFLICT_005";

    /// <summary>
    /// 记录正在排队或发送中。
    /// </summary>
    public const string RecordBusy = "NOTIFICATION_CONFLICT_006";

    /// <summary>
    /// 只有排队中的记录能取消。
    /// </summary>
    public const string RecordNotPending = "NOTIFICATION_CONFLICT_007";

    /// <summary>
    /// 模板语法错误或渲染失败。
    /// </summary>
    /// <remarks>
    /// 参数 <c>detail</c>。
    /// </remarks>
    public const string TemplateInvalid = "NOTIFICATION_VALIDATION_007";

    /// <summary>
    /// 邮件地址格式不对。
    /// </summary>
    /// <remarks>
    /// 参数 <c>address</c>。
    /// </remarks>
    public const string EmailAddressInvalid = "NOTIFICATION_VALIDATION_008";

    /// <summary>
    /// 模板映射须是字符串到字符串的 JSON 对象。
    /// </summary>
    public const string TemplateMapInvalid = "NOTIFICATION_VALIDATION_009";

    /// <summary>
    /// 投递渠道须包含站内信。
    /// </summary>
    public const string ChannelsInvalid = "NOTIFICATION_VALIDATION_010";

    /// <summary>
    /// 当前一侧与平台都没有启用的这个模板。
    /// </summary>
    /// <remarks>
    /// 参数 <c>code</c>。
    /// </remarks>
    public const string TemplateMissing = "NOTIFICATION_UNAVAILABLE_002";

    /// <summary>
    /// 当前一侧与平台都没有启用的缺省配置。
    /// </summary>
    /// <remarks>
    /// 参数 <c>kind</c>。
    /// </remarks>
    public const string ChannelUnavailable = "NOTIFICATION_UNAVAILABLE_003";

    /// <summary>
    /// 不支持的筛选或排序。
    /// </summary>
    public const string QueryUnsupported = "NOTIFICATION_VALIDATION_005";

    /// <summary>
    /// 标题不能只有空白。
    /// </summary>
    public const string TitleBlank = "NOTIFICATION_VALIDATION_006";

    /// <summary>
    /// 建好即发布需要发布权限。
    /// </summary>
    /// <remarks>
    /// 权限码 <c>notification.notifications.publish</c>。
    /// </remarks>
    public const string PublishDenied = "NOTIFICATION_DENIED_001";

    /// <summary>
    /// 平台的模板只能在平台维护。
    /// </summary>
    /// <remarks>
    /// 租户要不同的内容就建同编码的模板覆盖。参数 <c>id</c>。
    /// </remarks>
    public const string TemplateOtherSide = "NOTIFICATION_DENIED_002";

    /// <summary>
    /// 没有装解析这类目标的包。
    /// </summary>
    /// <remarks>
    /// 参数 <c>targetType</c>。
    /// </remarks>
    public const string RecipientSourceMissing = "NOTIFICATION_UNAVAILABLE_001";
}
