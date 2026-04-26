#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysEmail
// Guid:bc28152c-d6e9-4396-addb-b479254bad45
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Entities.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统邮件实体
/// 邮件发送记录聚合根：承载邮件内容、收件人、发送状态及重试信息
/// </summary>
/// <remarks>
/// 关联：
/// - 无业务 FK；BusinessId（若有）指向业务主单据
///
/// 写入：
/// - 初次入队 EmailStatus=Pending；调度器按队列发送
/// - 发送成功 → Sent + SendTime 落地；失败 → Failed + 记录重试次数
/// - 敏感正文（如密码重置链接）应设置合理 ExpiresAt
///
/// 查询：
/// - 待发队列：WHERE EmailStatus=Pending ORDER BY CreatedTime
/// - 租户邮件列表：IX_TeId_EmSt_SeTi（组合最优：租户+状态+时间倒序）
/// - 按收件人反查：IX_ToEm
///
/// 删除：
/// - 仅软删；过期邮件可批量归档
///
/// 状态：
/// - EmailStatus: Pending/Sending/Sent/Failed/Cancelled
///
/// 场景：
/// - 用户注册验证、密码重置、订单通知、系统预警
/// - 支持重试：失败后按指数退避策略重发
/// </remarks>
[SugarTable("SysEmail", "系统邮件表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("IX_{table}_ToEm", nameof(ToEmail), OrderByType.Asc)]
[SugarIndex("IX_{table}_EmSt", nameof(EmailStatus), OrderByType.Asc)]
[SugarIndex("IX_{table}_EmTy", nameof(EmailType), OrderByType.Asc)]
[SugarIndex("IX_{table}_SeTi", nameof(SendTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_TeId_EmSt_SeTi", nameof(TenantId), OrderByType.Asc, nameof(EmailStatus), OrderByType.Asc, nameof(SendTime), OrderByType.Desc)]
public partial class SysEmail : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 发送用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "发送用户ID", IsNullable = true)]
    public virtual long? SendUserId { get; set; }

    /// <summary>
    /// 接收用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "接收用户ID", IsNullable = true)]
    public virtual long? ReceiveUserId { get; set; }

    /// <summary>
    /// 邮件类型
    /// </summary>
    [SugarColumn(ColumnDescription = "邮件类型")]
    public virtual EmailType EmailType { get; set; } = EmailType.System;

    /// <summary>
    /// 发件人邮箱
    /// </summary>
    [SugarColumn(ColumnDescription = "发件人邮箱", Length = 100, IsNullable = false)]
    public virtual string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// 发件人姓名
    /// </summary>
    [SugarColumn(ColumnDescription = "发件人姓名", Length = 100, IsNullable = true)]
    public virtual string? FromName { get; set; }

    /// <summary>
    /// 收件人邮箱（多个用逗号分隔）
    /// </summary>
    [SugarColumn(ColumnDescription = "收件人邮箱", Length = 1000, IsNullable = false)]
    public virtual string ToEmail { get; set; } = string.Empty;

    /// <summary>
    /// 抄送邮箱（多个用逗号分隔）
    /// </summary>
    [SugarColumn(ColumnDescription = "抄送邮箱", Length = 1000, IsNullable = true)]
    public virtual string? CcEmail { get; set; }

    /// <summary>
    /// 密送邮箱（多个用逗号分隔）
    /// </summary>
    [SugarColumn(ColumnDescription = "密送邮箱", Length = 1000, IsNullable = true)]
    public virtual string? BccEmail { get; set; }

    /// <summary>
    /// 邮件主题
    /// </summary>
    [SugarColumn(ColumnDescription = "邮件主题", Length = 200, IsNullable = false)]
    public virtual string Subject { get; set; } = string.Empty;

    /// <summary>
    /// 邮件内容
    /// </summary>
    [SugarColumn(ColumnDescription = "邮件内容", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? Content { get; set; }

    /// <summary>
    /// 是否HTML格式
    /// </summary>
    [SugarColumn(ColumnDescription = "是否HTML格式")]
    public virtual bool IsHtml { get; set; } = true;

    /// <summary>
    /// 附件路径（多个用逗号分隔）
    /// </summary>
    [SugarColumn(ColumnDescription = "附件路径", Length = 2000, IsNullable = true)]
    public virtual string? Attachments { get; set; }

    /// <summary>
    /// 模板ID
    /// </summary>
    [SugarColumn(ColumnDescription = "模板ID", IsNullable = true)]
    public virtual long? TemplateId { get; set; }

    /// <summary>
    /// 模板参数（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDescription = "模板参数", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? TemplateParams { get; set; }

    /// <summary>
    /// 邮件状态
    /// </summary>
    [SugarColumn(ColumnDescription = "邮件状态")]
    public virtual EmailStatus EmailStatus { get; set; } = EmailStatus.Pending;

    /// <summary>
    /// 预定发送时间
    /// </summary>
    [SugarColumn(ColumnDescription = "预定发送时间", IsNullable = true)]
    public virtual DateTimeOffset? ScheduledTime { get; set; }

    /// <summary>
    /// 实际发送时间
    /// </summary>
    [SugarColumn(ColumnDescription = "实际发送时间", IsNullable = true)]
    public virtual DateTimeOffset? SendTime { get; set; }

    /// <summary>
    /// 重试次数
    /// </summary>
    [SugarColumn(ColumnDescription = "重试次数")]
    public virtual int RetryCount { get; set; } = 0;

    /// <summary>
    /// 最大重试次数
    /// </summary>
    [SugarColumn(ColumnDescription = "最大重试次数")]
    public virtual int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// 错误信息
    /// </summary>
    [SugarColumn(ColumnDescription = "错误信息", Length = 1000, IsNullable = true)]
    public virtual string? ErrorMessage { get; set; }

    /// <summary>
    /// 业务类型
    /// </summary>
    [SugarColumn(ColumnDescription = "业务类型", Length = 50, IsNullable = true)]
    public virtual string? BusinessType { get; set; }

    /// <summary>
    /// 业务ID
    /// </summary>
    [SugarColumn(ColumnDescription = "业务ID", IsNullable = true)]
    public virtual long? BusinessId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
