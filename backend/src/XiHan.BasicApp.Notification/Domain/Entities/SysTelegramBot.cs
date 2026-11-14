// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Notification.Domain.Entities;

/// <summary>
/// Telegram 机器人（平台库实体，平台托管）
/// </summary>
/// <remarks>
/// 平台托管、不分侧：机器人的 Webhook 与长轮询都由平台进程托管，令牌与会话是平台级的，所以没有 <c>OwnerTenantId</c>，租户看不到
/// 名称是路由键：Webhook 地址按名称区分机器人，所以名称唯一（部分唯一索引，删除后可重用）；运行时按名称不分大小写查找、重名只留第一个，而唯一索引区分大小写
/// 令牌就是机器人的全部权限：加密存储、响应里不回显，修改时不填即保留
/// 管理员、群组白名单、命令白名单与广播群存逗号分隔的规整文本：只随机器人整体读写，托管时规整成集合，不另建关联表；广播群收平台一侧按机器人渠道发出的通知
/// 改动不即时推送：托管循环每个刷新周期（参数 <c>notification.telegram</c>）重读启用的机器人，对照运行中的启动、重启或撤下，所以改完要等一个周期才生效
/// 软删除不检查引用：发件箱里还在排队的广播到发送时找不到机器人即失败
/// </remarks>
[SugarTable(TableName = "sys_telegram_bot", TableDescription = "Telegram 机器人（平台库实体，平台托管）")]
[ActiveUniqueIndex(nameof(Name), Name = "ux_sys_telegram_bot_name")]
public sealed class SysTelegramBot : AppAuditedEntity
{
    /// <summary>
    /// 名称（唯一）
    /// </summary>
    /// <remarks>
    /// Webhook 路由按它区分机器人
    /// </remarks>
    [SugarColumn(ColumnName = "name", Length = 64, IsNullable = false, ColumnDescription = "名称（唯一）")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 机器人令牌（密文）
    /// </summary>
    [SugarColumn(ColumnName = "token", Length = 1024, IsNullable = false, ColumnDescription = "机器人令牌（密文）")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// 管理员的 Telegram 用户 ID（逗号分隔）
    /// </summary>
    [SugarColumn(ColumnName = "admin_users", Length = 500, IsNullable = true, ColumnDescription = "管理员的 Telegram 用户 ID（逗号分隔）")]
    public string? AdminUsers { get; set; }

    /// <summary>
    /// 允许的群组与频道 chat ID（逗号分隔）
    /// </summary>
    /// <remarks>
    /// 为空即拒收所有群组与频道消息，只有 <c>/start</c> <c>/help</c> <c>/myid</c> 不受限，私聊不受影响
    /// </remarks>
    [SugarColumn(ColumnName = "allowed_group_chat_ids", Length = 500, IsNullable = true, ColumnDescription = "允许的群组与频道 chat ID（逗号分隔）")]
    public string? AllowedGroupChatIds { get; set; }

    /// <summary>
    /// 允许的命令（逗号分隔，不带斜杠）
    /// </summary>
    /// <remarks>
    /// 为空即不限
    /// </remarks>
    [SugarColumn(ColumnName = "allowed_commands", Length = 500, IsNullable = true, ColumnDescription = "允许的命令（逗号分隔，不带斜杠）")]
    public string? AllowedCommands { get; set; }

    /// <summary>
    /// 是否对认不出的消息兜底回复
    /// </summary>
    [SugarColumn(ColumnName = "enable_fallback_reply", IsNullable = false, ColumnDescription = "是否对认不出的消息兜底回复")]
    public bool EnableFallbackReply { get; set; }

    /// <summary>
    /// 广播群的 chat ID（逗号分隔）
    /// </summary>
    /// <remarks>
    /// 平台一侧按机器人渠道发的通知发到这些会话
    /// </remarks>
    [SugarColumn(ColumnName = "broadcast_chat_ids", Length = 500, IsNullable = true, ColumnDescription = "广播群的 chat ID（逗号分隔）")]
    public string? BroadcastChatIds { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [SugarColumn(ColumnName = "is_enabled", IsNullable = false, ColumnDescription = "是否启用")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 排序
    /// </summary>
    [SugarColumn(ColumnName = "order", IsNullable = false, ColumnDescription = "排序")]
    public int Order { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "remark", Length = 500, IsNullable = true, ColumnDescription = "备注")]
    public string? Remark { get; set; }
}
