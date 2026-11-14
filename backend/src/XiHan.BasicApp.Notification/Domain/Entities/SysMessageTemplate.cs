// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Notification.Domain.Entities;

/// <summary>
/// 消息模板（平台库实体，随所属侧）
/// </summary>
/// <remarks>
/// 落平台库、按 <c>OwnerTenantId</c> 分侧：平台由种子写全局模板、之后由运营维护；租户读得到平台的、只改自己的，建同编码的即覆盖，删掉自己的即回落到平台的，不必每个租户复制一份
/// 发送方按渠道与编码引用而不是按主键：Identity、通知与短信配置的模板映射都只认编码，覆盖与回落都不用改调用方，所以编码与渠道建好不改
/// 同一侧、同一渠道内编码唯一，用部分唯一索引，删除后同编码可重建
/// 发送时按侧、渠道与编码读穿共享缓存，先看本侧启用的、再看平台启用的，平台模板改了只失效平台那一份；失效推到工作单元结束之后，提交前的旧值不会被回填进缓存
/// 按 Scriban 渲染，保存前做语法检查：坏模板在保存时就被拒，不等到发送时才失败
/// </remarks>
[SugarTable(TableName = "sys_message_template", TableDescription = "消息模板（平台库实体，随所属侧）")]
[ActiveUniqueIndex(nameof(OwnerTenantId), nameof(Channel), nameof(Code), Name = "ux_sys_message_template_owner_tenant_id_channel_code")]
public sealed class SysMessageTemplate : AppAuditedEntity, IOwnedByTenant
{
    /// <inheritdoc />
    [SugarColumn(ColumnName = "owner_tenant_id", Length = 512, IsNullable = false, ColumnDescription = "属主租户 ID")]
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 编码（同一侧、同一渠道内唯一，如 <c>auth-email-login-code</c>）
    /// </summary>
    [SugarColumn(ColumnName = "code", Length = 64, IsNullable = false, ColumnDescription = "编码（同一侧、同一渠道内唯一，如 auth-email-login-code）")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 渠道
    /// </summary>
    [SugarColumn(ColumnName = "channel", IsNullable = false, ColumnDescription = "渠道")]
    public TemplateChannel Channel { get; set; } = TemplateChannel.Email;

    /// <summary>
    /// 名称
    /// </summary>
    [SugarColumn(ColumnName = "name", Length = 128, IsNullable = false, ColumnDescription = "名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 主题（邮件）
    /// </summary>
    [SugarColumn(ColumnName = "subject", Length = 256, IsNullable = true, ColumnDescription = "主题（邮件）")]
    public string? Subject { get; set; }

    /// <summary>
    /// 正文
    /// </summary>
    [SugarColumn(ColumnName = "content", IsNullable = false, ColumnDescription = "正文")]
    [LongText]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 正文是 HTML（邮件）
    /// </summary>
    [SugarColumn(ColumnName = "is_html", IsNullable = false, ColumnDescription = "正文是 HTML（邮件）")]
    public bool IsHtml { get; set; }

    /// <summary>
    /// 说明（可用变量等）
    /// </summary>
    [SugarColumn(ColumnName = "description", Length = 512, IsNullable = true, ColumnDescription = "说明（可用变量等）")]
    public string? Description { get; set; }

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

/// <summary>
/// 消息模板的渠道
/// </summary>
/// <remarks>
/// 与 <see cref="MessageChannels"/> 同值
/// </remarks>
public enum TemplateChannel
{
    /// <summary>
    /// 站内通知（主题即通知标题，正文是纯文本）
    /// </summary>
    /// <remarks>
    /// 内置的发送都不用它
    /// </remarks>
    InApp = 1,

    /// <summary>
    /// 邮件（有主题，正文可以是 HTML）
    /// </summary>
    Email = 2,

    /// <summary>
    /// 短信（纯文本）
    /// </summary>
    /// <remarks>
    /// 云厂商发送要在短信配置里把模板编码映射到服务商的模板 ID
    /// </remarks>
    Sms = 4,

    /// <summary>
    /// 机器人（纯文本，不用主题）
    /// </summary>
    /// <remarks>
    /// 通知发到机器人时按 <c>notification-bot</c> 渲染
    /// </remarks>
    Bot = 8,
}
