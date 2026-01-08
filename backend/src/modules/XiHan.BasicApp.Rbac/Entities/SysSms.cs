#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysSms
// Guid:cc28152c-d6e9-4396-addb-b479254bad46
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 6:05:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities.Base;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Domain.Aggregates;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统短信实体
/// </summary>
[SugarTable("Sys_Sms", "系统短信表")]
[SugarIndex("IX_SysSms_ToPhone", nameof(ToPhone), OrderByType.Asc)]
[SugarIndex("IX_SysSms_SmsStatus", nameof(SmsStatus), OrderByType.Asc)]
[SugarIndex("IX_SysSms_SmsType", nameof(SmsType), OrderByType.Asc)]
[SugarIndex("IX_SysSms_SendTime", nameof(SendTime), OrderByType.Desc)]
[SugarIndex("IX_SysSms_TenantId", nameof(TenantId), OrderByType.Asc)]
public partial class SysSms : AuditedAggregateRoot<long>
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
    public virtual long? SenderId { get; set; }

    /// <summary>
    /// 接收用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "接收用户ID", IsNullable = true)]
    public virtual long? ReceiverId { get; set; }

    /// <summary>
    /// 短信类型
    /// </summary>
    [SugarColumn(ColumnDescription = "短信类型")]
    public virtual SmsType SmsType { get; set; } = SmsType.Notification;

    /// <summary>
    /// 接收手机号（多个用逗号分隔）
    /// </summary>
    [SugarColumn(ColumnDescription = "接收手机号", Length = 500, IsNullable = false)]
    public virtual string ToPhone { get; set; } = string.Empty;

    /// <summary>
    /// 短信内容
    /// </summary>
    [SugarColumn(ColumnDescription = "短信内容", Length = 1000, IsNullable = false)]
    public virtual string Content { get; set; } = string.Empty;

    /// <summary>
    /// 模板ID
    /// </summary>
    [SugarColumn(ColumnDescription = "模板ID", Length = 50, IsNullable = true)]
    public virtual string? TemplateId { get; set; }

    /// <summary>
    /// 模板参数（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDescription = "模板参数", Length = 1000, IsNullable = true)]
    public virtual string? TemplateParams { get; set; }

    /// <summary>
    /// 短信服务商
    /// </summary>
    [SugarColumn(ColumnDescription = "短信服务商", Length = 50, IsNullable = true)]
    public virtual string? Provider { get; set; }

    /// <summary>
    /// 短信状态
    /// </summary>
    [SugarColumn(ColumnDescription = "短信状态")]
    public virtual SmsStatus SmsStatus { get; set; } = SmsStatus.Pending;

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
    /// 服务商返回ID
    /// </summary>
    [SugarColumn(ColumnDescription = "服务商返回ID", Length = 100, IsNullable = true)]
    public virtual string? ProviderMessageId { get; set; }

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
    /// 费用（分）
    /// </summary>
    [SugarColumn(ColumnDescription = "费用（分）", IsNullable = true)]
    public virtual int? Cost { get; set; }

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
