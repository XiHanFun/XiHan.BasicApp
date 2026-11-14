// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Notification.Domain.Entities;

/// <summary>
/// 邮件发送记录，也是邮件的发件箱（平台库实体，随所属侧）
/// </summary>
/// <remarks>
/// 发送记录兼作发件箱：业务在自己的工作单元里写一条排队中的记录、随业务一起提交，业务回滚就不发，提交了就一定会被发送循环取到；同一行再记尝试次数、结果与实际用的配置，队列和发送记录不必分两张表，排查看一行就够
/// 落平台库、按 <c>OwnerTenantId</c> 分侧（平台为 <c>platform</c>）：一个后台发送循环就能跨所有侧取排队记录，各侧只看得到自己的记录
/// 主题与正文写入时已按模板渲染好，发出去的就是记下的这份，模板之后再改不影响已排队的与历史记录；配置却到发送时才取（本侧启用的缺省，租户没有取平台的），换配置对排队中与重试中的立即生效，<see cref="ConfigId"/> 回写这次实际用的那条
/// 领取用条件更新把排队中改成发送中，多实例并行也只有一个领到；进程中断留在发送中的记录在启动时改回排队中，代价是发出后、回写前中断的那一条会再发一次
/// 失败按次数退避重试，最多次数在写入时定到行上，之后改配置不影响已排队的
/// 验证码（<c>identity.verification</c>）的正文就是账号的钥匙：发送结束即清空、响应里不返回、也不能重发；重试期间还要用，所以不是一入队就不存
/// 发送循环频繁改状态，所以不进差异日志；软删除，没有清理作业，记录一直保留
/// </remarks>
[SugarTable(TableName = "sys_email", TableDescription = "邮件发送记录，也是邮件的发件箱（平台库实体，随所属侧）")]
[SugarIndex("ix_sys_email_status_created_at", nameof(Status), OrderByType.Asc, nameof(CreatedAt), OrderByType.Asc)]
[SugarIndex("ix_sys_email_owner_tenant_id_created_at", nameof(OwnerTenantId), OrderByType.Asc, nameof(CreatedAt), OrderByType.Asc)]
[DiffLogIgnored]
public sealed class SysEmail : AppAuditedEntity, IOwnedByTenant, IMessageRecord
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
    /// 接收人（不是本应用用户的地址为空）
    /// </summary>
    [SugarColumn(ColumnName = "receiver_id", IsNullable = true, ColumnDescription = "接收人（不是本应用用户的地址为空）")]
    public Guid? ReceiverId { get; set; }

    /// <summary>
    /// 收件地址
    /// </summary>
    [SugarColumn(ColumnName = "to_email", Length = 256, IsNullable = false, ColumnDescription = "收件地址")]
    public string ToEmail { get; set; } = string.Empty;

    /// <summary>
    /// 主题
    /// </summary>
    [SugarColumn(ColumnName = "subject", Length = 512, IsNullable = false, ColumnDescription = "主题")]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// 正文
    /// </summary>
    [SugarColumn(ColumnName = "content", IsNullable = true, ColumnDescription = "正文")]
    [LongText]
    public string? Content { get; set; }

    /// <summary>
    /// 正文是 HTML
    /// </summary>
    [SugarColumn(ColumnName = "is_html", IsNullable = false, ColumnDescription = "正文是 HTML")]
    public bool IsHtml { get; set; }

    /// <summary>
    /// 用的模板编码
    /// </summary>
    [SugarColumn(ColumnName = "template_code", Length = 64, IsNullable = true, ColumnDescription = "用的模板编码")]
    public string? TemplateCode { get; set; }

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
    /// 下次可以尝试的时间（失败后按退避推迟）
    /// </summary>
    /// <remarks>
    /// 为空即立即
    /// </remarks>
    [SugarColumn(ColumnName = "next_attempt_at", IsNullable = true, ColumnDescription = "下次可以尝试的时间（失败后按退避推迟）")]
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
    /// 发出时用的邮件配置
    /// </summary>
    [SugarColumn(ColumnName = "config_id", IsNullable = true, ColumnDescription = "发出时用的邮件配置")]
    public Guid? ConfigId { get; set; }

    /// <summary>
    /// 关联的业务类型（如 <c>notification</c>、<c>identity.verification</c>）
    /// </summary>
    [SugarColumn(ColumnName = "business_type", Length = 100, IsNullable = true, ColumnDescription = "关联的业务类型（如 notification、identity.verification）")]
    public string? BusinessType { get; set; }

    /// <summary>
    /// 关联的业务 ID
    /// </summary>
    [SugarColumn(ColumnName = "business_id", Length = 100, IsNullable = true, ColumnDescription = "关联的业务 ID")]
    public string? BusinessId { get; set; }
}
