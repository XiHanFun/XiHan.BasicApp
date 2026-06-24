#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysMessageTemplate
// Guid:7f2c9a14-58b3-4e6d-9c01-3a8f5d2e7b46
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/11 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统消息模板实体
/// 邮件 / 短信 / 站内通知的内容模板（Scriban 语法），消息发送链路按 渠道+编码 查找并渲染
/// </summary>
/// <remarks>
/// 职责边界：
/// - 模板只负责"内容如何渲染"；"发给谁/何时发"由 MessageDeliveryService 等发送链路决定
/// - 渲染引擎复用框架 XiHan.Framework.Templating（Scriban：{{ variable }} / {{ if }} / {{ for }}）
///
/// 写入：
/// - TenantId + Channel + TemplateCode 唯一（UX_TeId_Ch_TeCo）；TenantId=0 为平台全局模板（共享读，Model A）
/// - 租户可创建同编码模板覆盖全局默认（查找时租户模板优先于全局模板）
/// - 全局模板仅平台运维态可维护（与菜单/权限等全局模板同一不变量）
///
/// 查询：
/// - 发送链路按 (Channel, TemplateCode) 查找：当前租户 → 全局 兜底（IX_Ch_TeCo）
/// - 查找结果走分布式缓存（写路径失效）
///
/// 状态：
/// - Status: 启用/停用（停用模板查找时跳过，回退全局或调用方内置内容）
/// </remarks>
[SugarTable(TableName = "Sys_Message_Template", TableDescription = "系统消息模板表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_Ch_TeCo", nameof(TenantId), OrderByType.Asc, nameof(Channel), OrderByType.Asc, nameof(TemplateCode), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_Ch_TeCo", nameof(Channel), OrderByType.Asc, nameof(TemplateCode), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
public partial class SysMessageTemplate : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 模板编码（渠道内唯一标识，如 auth-email-login-code）
    /// </summary>
    [SugarColumn(ColumnName = "Template_Code", ColumnDescription = "模板编码", Length = 100, IsNullable = false)]
    public virtual string TemplateCode { get; set; } = string.Empty;

    /// <summary>
    /// 消息渠道（站内通知/邮件/短信）
    /// </summary>
    [SugarColumn(ColumnName = "Channel", ColumnDescription = "消息渠道")]
    public virtual MessageChannel Channel { get; set; } = MessageChannel.Email;

    /// <summary>
    /// 模板名称
    /// </summary>
    [SugarColumn(ColumnName = "Template_Name", ColumnDescription = "模板名称", Length = 100, IsNullable = false)]
    public virtual string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// 主题模板（邮件主题/通知标题，Scriban 语法；短信不使用）
    /// </summary>
    [SugarColumn(ColumnName = "Subject", ColumnDescription = "主题模板", Length = 200, IsNullable = true)]
    public virtual string? Subject { get; set; }

    /// <summary>
    /// 内容模板（Scriban 语法）
    /// </summary>
    [SugarColumn(ColumnName = "Content", ColumnDescription = "内容模板", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = false)]
    public virtual string Content { get; set; } = string.Empty;

    /// <summary>
    /// 内容是否 HTML（邮件渠道有效）
    /// </summary>
    [SugarColumn(ColumnName = "Is_Html", ColumnDescription = "是否HTML")]
    public virtual bool IsHtml { get; set; }

    /// <summary>
    /// 模板描述（可用变量说明等）
    /// </summary>
    [SugarColumn(ColumnName = "Description", ColumnDescription = "模板描述", Length = 500, IsNullable = true)]
    public virtual string? Description { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "Status", ColumnDescription = "状态")]
    public virtual EnableStatus Status { get; set; } = EnableStatus.Enabled;

    /// <summary>
    /// 排序
    /// </summary>
    [SugarColumn(ColumnName = "Sort", ColumnDescription = "排序")]
    public virtual int Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "Remark", ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
