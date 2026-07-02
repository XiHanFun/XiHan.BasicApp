#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysTelegramMessage
// Guid:f2d8a5b3-7e46-4c19-a0b8-3d6c9e1f4a57
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/03 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.Framework.Domain.Entities.Abstracts;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统 Telegram 出站消息审计实体
/// 框架 TelegramNotifier 每次出站调用（成功/失败）的审计记录
/// </summary>
/// <remarks>
/// 分表策略：
/// - 按月分表；查询/清理必带时间范围
///
/// 关联：
/// - BotConfigId → SysTelegramBot（冗余 BotName 便于快速过滤，机器人删除后记录仍可读）
///
/// 写入：
/// - 由 <c>SaasTelegramMessageAuditStore</c>（Singleton + Scope 直插）在每次发送后追加一条
/// - 审计失败吞异常仅记日志，不阻断发送主流程
/// - 字段与框架 <c>TelegramMessageAuditRecord</c> 全集一一映射
///
/// 查询：
/// - 机器人出站流水：IX_BoNa + ORDER BY CreatedTime DESC
/// - 会话消息追踪：IX_ChId
/// - 租户审计概览：IX_TeId_CrTi
///
/// 删除：
/// - 不支持业务删除；按保留策略归档
///
/// 场景：
/// - 出站消息审计追溯（谁在何时向哪个会话发了什么）
/// - 发送失败排查（错误码/错误信息/耗时）
/// </remarks>
[SugarTable(TableName = "Sys_Telegram_Message_{year}{month}{day}", TableDescription = "系统Telegram出站消息审计表"), SplitTable(SplitType.Month)]
[SugarIndex("IX_{split_table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{split_table}_BoNa", nameof(BotName), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_ChId", nameof(ChatId), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_Su", nameof(Success), OrderByType.Asc)]
public partial class SysTelegramMessage : BasicAppCreationEntity, ISplitTableEntity
{
    /// <summary>
    /// 机器人名称
    /// </summary>
    [SugarColumn(ColumnName = "Bot_Name", ColumnDescription = "机器人名称", Length = 100, IsNullable = false)]
    public virtual string BotName { get; set; } = string.Empty;

    /// <summary>
    /// 机器人配置ID（数据库来源可用，0 表示无）
    /// </summary>
    [SugarColumn(ColumnName = "Bot_Config_Id", ColumnDescription = "机器人配置ID")]
    public virtual long BotConfigId { get; set; }

    /// <summary>
    /// 目标会话ID
    /// </summary>
    [SugarColumn(ColumnName = "Chat_Id", ColumnDescription = "目标会话ID")]
    public virtual long ChatId { get; set; }

    /// <summary>
    /// Bot API 方法名（如 sendMessage / sendPhoto / editMessageText）
    /// </summary>
    [SugarColumn(ColumnName = "Api_Method", ColumnDescription = "API方法名", Length = 50, IsNullable = false)]
    public virtual string ApiMethod { get; set; } = string.Empty;

    /// <summary>
    /// 消息类型（text / photo / document 等）
    /// </summary>
    [SugarColumn(ColumnName = "Message_Type", ColumnDescription = "消息类型", Length = 50, IsNullable = false)]
    public virtual string MessageType { get; set; } = string.Empty;

    /// <summary>
    /// 消息内容（文本或图片/文件说明）
    /// </summary>
    [SugarColumn(ColumnName = "Content", ColumnDescription = "消息内容", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? Content { get; set; }

    /// <summary>
    /// 解析模式（None / Markdown / MarkdownV2 / Html）
    /// </summary>
    [SugarColumn(ColumnName = "Parse_Mode", ColumnDescription = "解析模式", Length = 20, IsNullable = true)]
    public virtual string? ParseMode { get; set; }

    /// <summary>
    /// Telegram 返回的消息ID（失败时为空）
    /// </summary>
    [SugarColumn(ColumnName = "Telegram_Message_Id", ColumnDescription = "Telegram消息ID", IsNullable = true)]
    public virtual int? TelegramMessageId { get; set; }

    /// <summary>
    /// 是否发送成功
    /// </summary>
    [SugarColumn(ColumnName = "Success", ColumnDescription = "是否发送成功")]
    public virtual bool Success { get; set; }

    /// <summary>
    /// 错误码（Bot API 错误时有值）
    /// </summary>
    [SugarColumn(ColumnName = "Error_Code", ColumnDescription = "错误码", IsNullable = true)]
    public virtual int? ErrorCode { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    [SugarColumn(ColumnName = "Error_Message", ColumnDescription = "错误信息", Length = 1000, IsNullable = true)]
    public virtual string? ErrorMessage { get; set; }

    /// <summary>
    /// 耗时（毫秒）
    /// </summary>
    [SugarColumn(ColumnName = "Elapsed_Ms", ColumnDescription = "耗时（毫秒）")]
    public virtual long ElapsedMs { get; set; }

    /// <summary>
    /// 发送时间
    /// </summary>
    [SugarColumn(ColumnName = "Send_Time", ColumnDescription = "发送时间")]
    public virtual DateTimeOffset SendTime { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(ColumnName = "Created_Time", IsNullable = false, ColumnDescription = "创建时间")]
    [SplitField]
    public override DateTimeOffset CreatedTime { get; set; }
}
