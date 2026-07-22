// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Core.Entities;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统短信实体
/// 短信发送记录聚合根：承载短信模板、收件号码、发送状态及运营商回执
/// </summary>
/// <remarks>
/// 关联：
/// - 无业务 FK；BusinessId（若有）指向业务主单据
///
/// 写入：
/// - 初次入队 SmsStatus=Pending；调度器批量提交至运营商网关
/// - 发送成功 → Sent；失败 → Failed（保留 ErrorMessage 便于排查）
/// - 验证码类短信应设短 ExpirationTime（一般 5 分钟），防止盗用
///
/// 查询：
/// - 待发队列：WHERE SmsStatus=Pending ORDER BY CreatedTime
/// - 租户最近发送：IX_TeId_SmSt_SeTi
/// - 按手机号反查：IX_ToPh
///
/// 删除：
/// - 仅软删；可定期归档到冷存储降低成本
///
/// 状态：
/// - SmsStatus: Pending/Sending/Sent/Failed/Expired
///
/// 场景：
/// - 验证码、通知类短信、营销短信（需遵守运营商合规）
/// </remarks>
[SugarTable(TableName = "Sys_Sms", TableDescription = "系统短信表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("IX_{table}_ToPh", nameof(ToPhone), OrderByType.Asc)]
[SugarIndex("IX_{table}_SmSt", nameof(SmsStatus), OrderByType.Asc)]
[SugarIndex("IX_{table}_SmTy", nameof(SmsType), OrderByType.Asc)]
[SugarIndex("IX_{table}_SeTi", nameof(SendTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_TeId_SmSt_SeTi", nameof(TenantId), OrderByType.Asc, nameof(SmsStatus), OrderByType.Asc, nameof(SendTime), OrderByType.Desc)]
public partial class SysSms : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 发送用户ID
    /// </summary>
    [SugarColumn(ColumnName = "Sender_Id", ColumnDescription = "发送用户ID", IsNullable = true)]
    public virtual long? SenderId { get; set; }

    /// <summary>
    /// 接收用户ID
    /// </summary>
    [SugarColumn(ColumnName = "Receiver_Id", ColumnDescription = "接收用户ID", IsNullable = true)]
    public virtual long? ReceiverId { get; set; }

    /// <summary>
    /// 短信类型
    /// </summary>
    [SugarColumn(ColumnName = "Sms_Type", ColumnDescription = "短信类型")]
    public virtual SmsType SmsType { get; set; } = SmsType.Notification;

    /// <summary>
    /// 接收手机号（多个用逗号分隔）
    /// </summary>
    [SugarColumn(ColumnName = "To_Phone", ColumnDescription = "接收手机号", Length = 500, IsNullable = false)]
    public virtual string ToPhone { get; set; } = string.Empty;

    /// <summary>
    /// 短信内容
    /// </summary>
    [SugarColumn(ColumnName = "Content", ColumnDescription = "短信内容", Length = 1000, IsNullable = false)]
    public virtual string Content { get; set; } = string.Empty;

    /// <summary>
    /// 模板编码（关联 SysMessageTemplate.TemplateCode，发送时按编码查找模板渲染）
    /// </summary>
    [SugarColumn(ColumnName = "Template_Code", ColumnDescription = "模板编码", Length = 100, IsNullable = true)]
    public virtual string? TemplateCode { get; set; }

    /// <summary>
    /// 模板参数（JSON格式）
    /// </summary>
    [SugarColumn(ColumnName = "Template_Params", ColumnDescription = "模板参数", Length = 1000, IsNullable = true)]
    public virtual string? TemplateParams { get; set; }

    /// <summary>
    /// 短信服务商
    /// </summary>
    [SugarColumn(ColumnName = "Provider", ColumnDescription = "短信服务商", Length = 50, IsNullable = true)]
    public virtual string? Provider { get; set; }

    /// <summary>
    /// 短信状态
    /// </summary>
    [SugarColumn(ColumnName = "Sms_Status", ColumnDescription = "短信状态")]
    public virtual SmsStatus SmsStatus { get; set; } = SmsStatus.Pending;

    /// <summary>
    /// 预定发送时间
    /// </summary>
    [SugarColumn(ColumnName = "Scheduled_Time", ColumnDescription = "预定发送时间", IsNullable = true)]
    public virtual DateTimeOffset? ScheduledTime { get; set; }

    /// <summary>
    /// 实际发送时间
    /// </summary>
    [SugarColumn(ColumnName = "Send_Time", ColumnDescription = "实际发送时间", IsNullable = true)]
    public virtual DateTimeOffset? SendTime { get; set; }

    /// <summary>
    /// 服务商返回ID
    /// </summary>
    [SugarColumn(ColumnName = "Provider_Message_Id", ColumnDescription = "服务商返回ID", Length = 100, IsNullable = true)]
    public virtual string? ProviderMessageId { get; set; }

    /// <summary>
    /// 重试次数
    /// </summary>
    [SugarColumn(ColumnName = "Retry_Count", ColumnDescription = "重试次数")]
    public virtual int RetryCount { get; set; } = 0;

    /// <summary>
    /// 最大重试次数
    /// </summary>
    [SugarColumn(ColumnName = "Max_Retry_Count", ColumnDescription = "最大重试次数")]
    public virtual int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// 错误信息
    /// </summary>
    [SugarColumn(ColumnName = "Error_Message", ColumnDescription = "错误信息", Length = 1000, IsNullable = true)]
    public virtual string? ErrorMessage { get; set; }

    /// <summary>
    /// 费用（分）
    /// </summary>
    [SugarColumn(ColumnName = "Cost", ColumnDescription = "费用（分）", IsNullable = true)]
    public virtual int? Cost { get; set; }

    /// <summary>
    /// 业务类型
    /// </summary>
    [SugarColumn(ColumnName = "Business_Type", ColumnDescription = "业务类型", Length = 50, IsNullable = true)]
    public virtual string? BusinessType { get; set; }

    /// <summary>
    /// 业务ID
    /// </summary>
    [SugarColumn(ColumnName = "Business_Id", ColumnDescription = "业务ID", IsNullable = true)]
    public virtual long? BusinessId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "Remark", ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
