// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Notification.Domain.Entities;

/// <summary>
/// 短信发送记录，也是短信的发件箱（平台库实体，随所属侧）
/// </summary>
/// <remarks>
/// 与 <see cref="SysEmail"/> 同一套发件箱：随业务一起提交、发送循环条件更新领取、配置到发送时才取（租户没有取平台的）、按次数退避重试、验证码发完即清空正文与参数
/// 云服务商只认它那边审核过的模板：记录里的正文是本应用模板渲染出的文字，只供查看；<see cref="TemplateParams"/> 另存调用方给的参数交给服务商，补上的品牌名只用于本地渲染，免得参数个数和服务商模板对不上
/// 记录存本应用的模板编码，到发送时才按配置的模板映射换成服务商的模板 ID：换配置或改映射不用动排队中的记录
/// 发送时回写服务商与服务商返回的消息 ID，供到服务商那边核对
/// </remarks>
[SugarTable(TableName = "sys_sms", TableDescription = "短信发送记录，也是短信的发件箱（平台库实体，随所属侧）")]
[SugarIndex("ix_sys_sms_status_created_at", nameof(Status), OrderByType.Asc, nameof(CreatedAt), OrderByType.Asc)]
[SugarIndex("ix_sys_sms_owner_tenant_id_created_at", nameof(OwnerTenantId), OrderByType.Asc, nameof(CreatedAt), OrderByType.Asc)]
[DiffLogIgnored]
public sealed class SysSms : AppAuditedEntity, IOwnedByTenant, IMessageRecord
{
    /// <inheritdoc />
    [SugarColumn(ColumnName = "owner_tenant_id", Length = 512, IsNullable = false, ColumnDescription = "属主租户 ID")]
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 发起人（系统发出的为空）
    /// </summary>
    [SugarColumn(ColumnName = "sender_id", IsNullable = true, ColumnDescription = "发起人（系统发出的为空）")]
    public Guid? SenderId { get; set; }

    /// <summary>
    /// 接收人（不是本应用用户的号码为空）
    /// </summary>
    [SugarColumn(ColumnName = "receiver_id", IsNullable = true, ColumnDescription = "接收人（不是本应用用户的号码为空）")]
    public Guid? ReceiverId { get; set; }

    /// <summary>
    /// 手机号
    /// </summary>
    [SugarColumn(ColumnName = "to_phone", Length = 32, IsNullable = false, ColumnDescription = "手机号")]
    public string ToPhone { get; set; } = string.Empty;

    /// <summary>
    /// 正文（本应用模板渲染出的文字）
    /// </summary>
    [SugarColumn(ColumnName = "content", Length = 1000, IsNullable = false, ColumnDescription = "正文（本应用模板渲染出的文字）")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 用的模板编码（按短信配置的模板映射换成服务商的模板 ID）
    /// </summary>
    [SugarColumn(ColumnName = "template_code", Length = 64, IsNullable = true, ColumnDescription = "用的模板编码（按短信配置的模板映射换成服务商的模板 ID）")]
    public string? TemplateCode { get; set; }

    /// <summary>
    /// 模板参数（JSON 对象，按写入顺序交给服务商）
    /// </summary>
    [SugarColumn(ColumnName = "template_params", IsNullable = true, ColumnDescription = "模板参数（JSON 对象，按写入顺序交给服务商）")]
    [LongText]
    public string? TemplateParams { get; set; }

    /// <summary>
    /// 发出时用的服务商
    /// </summary>
    [SugarColumn(ColumnName = "provider", IsNullable = true, ColumnDescription = "发出时用的服务商")]
    public SmsProvider? Provider { get; set; }

    /// <summary>
    /// 服务商返回的消息 ID
    /// </summary>
    [SugarColumn(ColumnName = "provider_message_id", Length = 128, IsNullable = true, ColumnDescription = "服务商返回的消息 ID")]
    public string? ProviderMessageId { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "status", IsNullable = false, ColumnDescription = "状态")]
    public MessageSendStatus Status { get; set; } = MessageSendStatus.Pending;

    /// <summary>
    /// 已尝试的次数
    /// </summary>
    [SugarColumn(ColumnName = "attempts", IsNullable = false, ColumnDescription = "已尝试的次数")]
    public int Attempts { get; set; }

    /// <summary>
    /// 最多尝试的次数
    /// </summary>
    [SugarColumn(ColumnName = "max_attempts", IsNullable = false, ColumnDescription = "最多尝试的次数")]
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// 下次可以尝试的时间
    /// </summary>
    /// <remarks>
    /// 为空即立即
    /// </remarks>
    [SugarColumn(ColumnName = "next_attempt_at", IsNullable = true, ColumnDescription = "下次可以尝试的时间")]
    public DateTimeOffset? NextAttemptAt { get; set; }

    /// <summary>
    /// 发出时间
    /// </summary>
    [SugarColumn(ColumnName = "sent_at", IsNullable = true, ColumnDescription = "发出时间")]
    public DateTimeOffset? SentAt { get; set; }

    /// <summary>
    /// 最近一次失败的原因
    /// </summary>
    [SugarColumn(ColumnName = "error_message", Length = 2000, IsNullable = true, ColumnDescription = "最近一次失败的原因")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 发出时用的短信配置
    /// </summary>
    [SugarColumn(ColumnName = "config_id", IsNullable = true, ColumnDescription = "发出时用的短信配置")]
    public Guid? ConfigId { get; set; }

    /// <summary>
    /// 关联的业务类型
    /// </summary>
    [SugarColumn(ColumnName = "business_type", Length = 100, IsNullable = true, ColumnDescription = "关联的业务类型")]
    public string? BusinessType { get; set; }

    /// <summary>
    /// 关联的业务 ID
    /// </summary>
    [SugarColumn(ColumnName = "business_id", Length = 100, IsNullable = true, ColumnDescription = "关联的业务 ID")]
    public string? BusinessId { get; set; }
}
