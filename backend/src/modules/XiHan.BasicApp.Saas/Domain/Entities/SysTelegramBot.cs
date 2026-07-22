// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Core.Entities;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统 Telegram 机器人实体（多机器人专表）
/// 承载 Bot Token 与访问控制白名单，框架 Telegram 管理器按启用行动态拉起/重启机器人
/// </summary>
/// <remarks>
/// 写入：
/// - BotName 在租户内必须唯一（UX_TeId_BoNa），业务侧以该名称定位机器人（同时是 Webhook 路由段）
/// - Token 为敏感字段，Data Protection 加密落库，读侧永不回显
///
/// 查询：
/// - 框架 store 读全部「启用」行并解密 Token 映射为 TelegramBotConfig 列表
/// - 管理器按刷新周期轮询 diff，配置改动无需重启应用即可生效
///
/// 场景：
/// - 多个 Telegram 机器人并行运行（通知机器人/客服机器人等）
/// </remarks>
[SugarTable(TableName = "Sys_Telegram_Bot", TableDescription = "系统Telegram机器人表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_BoNa", nameof(TenantId), OrderByType.Asc, nameof(BotName), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_TeId_IsEn", nameof(TenantId), OrderByType.Asc, nameof(IsEnabled), OrderByType.Asc)]
public partial class SysTelegramBot : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 机器人名称（租户内唯一；业务侧定位标识，同时作为 Webhook 路由段）
    /// </summary>
    [SugarColumn(ColumnName = "Bot_Name", ColumnDescription = "机器人名称", Length = 100, IsNullable = false)]
    public virtual string BotName { get; set; } = string.Empty;

    /// <summary>
    /// Bot Token（敏感字段，加密落库，读侧不回显）
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(ColumnName = "Token", ColumnDescription = "BotToken", Length = 500, IsNullable = false)]
    public virtual string Token { get; set; } = string.Empty;

    /// <summary>
    /// 超级管理员 Telegram 用户 Id 列表（逗号分隔 long 串，可空）
    /// </summary>
    [SugarColumn(ColumnName = "Admin_Users", ColumnDescription = "管理员用户Id列表", Length = 500, IsNullable = true)]
    public virtual string? AdminUsers { get; set; }

    /// <summary>
    /// 允许的群组 ChatId 白名单（逗号分隔 long 串，可空）
    /// </summary>
    /// <remarks>
    /// <b>fail-closed 语义：为空表示拒收所有群组消息</b>（与直觉「空=不限制」相反）；
    /// 仅永久放行命令（/start /myid /help）不受该白名单限制，私聊消息不受此项影响。
    /// </remarks>
    [SugarColumn(ColumnName = "Allowed_Group_Chat_Ids", ColumnDescription = "群组ChatId白名单", Length = 500, IsNullable = true)]
    public virtual string? AllowedGroupChatIds { get; set; }

    /// <summary>
    /// 允许执行的命令白名单（逗号分隔，可空；为空表示不限制命令）
    /// </summary>
    [SugarColumn(ColumnName = "Allowed_Commands", ColumnDescription = "命令白名单", Length = 500, IsNullable = true)]
    public virtual string? AllowedCommands { get; set; }

    /// <summary>
    /// 是否启用兜底回复（无处理器命中普通消息时回复提示文案；与平台全局设置任一开启即生效）
    /// </summary>
    [SugarColumn(ColumnName = "Enable_Fallback_Reply", ColumnDescription = "是否启用兜底回复")]
    public virtual bool EnableFallbackReply { get; set; } = false;

    /// <summary>
    /// 是否启用
    /// </summary>
    [SugarColumn(ColumnName = "Is_Enabled", ColumnDescription = "是否启用")]
    public virtual bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 排序（数字越小越靠前）
    /// </summary>
    [SugarColumn(ColumnName = "Sort", ColumnDescription = "排序")]
    public virtual int Sort { get; set; } = 0;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "Remark", ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
