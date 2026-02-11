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
using XiHan.BasicApp.Rbac.Entities.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统邮件实体
/// </summary>
[SugarTable("Sys_Email", "系统邮件表")]
[SugarIndex("IX_SysEmail_ToEmail", nameof(ToEmail), OrderByType.Asc)]
[SugarIndex("IX_SysEmail_EmailStatus", nameof(EmailStatus), OrderByType.Asc)]
[SugarIndex("IX_SysEmail_EmailType", nameof(EmailType), OrderByType.Asc)]
[SugarIndex("IX_SysEmail_SendTime", nameof(SendTime), OrderByType.Desc)]
[SugarIndex("IX_SysEmail_TenantId", nameof(TenantId), OrderByType.Asc)]
public partial class SysEmail : RbacAggregateRoot<long>
{
    /// <summary>
    /// 租户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "租户ID", IsNullable = true)]
    public virtual long? TenantId { get; set; }

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
