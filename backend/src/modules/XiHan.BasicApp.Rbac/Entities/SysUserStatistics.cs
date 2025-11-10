#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserStatistics
// Guid:dc28152c-d6e9-4396-addb-b479254bad47
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 6:10:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 用户统计实体
/// </summary>
[SugarTable("sys_user_statistics", "用户统计表")]
[SugarIndex("IX_SysUserStatistics_UserId", nameof(UserId), OrderByType.Asc)]
[SugarIndex("IX_SysUserStatistics_StatisticsDate", nameof(StatisticsDate), OrderByType.Desc)]
[SugarIndex("IX_SysUserStatistics_Period", nameof(Period), OrderByType.Asc)]
[SugarIndex("IX_SysUserStatistics_TenantId", nameof(TenantId), OrderByType.Asc)]
public partial class SysUserStatistics : RbacFullAuditedEntity<RbacIdType>
{
    /// <summary>
    /// 租户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "租户ID", IsNullable = true)]
    public virtual RbacIdType? TenantId { get; set; }

    /// <summary>
    /// 用户ID（为空表示全体用户统计）
    /// </summary>
    [SugarColumn(ColumnDescription = "用户ID", IsNullable = true)]
    public virtual RbacIdType? UserId { get; set; }

    /// <summary>
    /// 统计日期
    /// </summary>
    [SugarColumn(ColumnDescription = "统计日期")]
    public virtual DateOnly StatisticsDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    /// <summary>
    /// 统计时间范围
    /// </summary>
    [SugarColumn(ColumnDescription = "统计时间范围")]
    public virtual StatisticsPeriod Period { get; set; } = StatisticsPeriod.Today;

    /// <summary>
    /// 登录次数
    /// </summary>
    [SugarColumn(ColumnDescription = "登录次数")]
    public virtual int LoginCount { get; set; } = 0;

    /// <summary>
    /// 访问次数
    /// </summary>
    [SugarColumn(ColumnDescription = "访问次数")]
    public virtual int AccessCount { get; set; } = 0;

    /// <summary>
    /// 在线时长（秒）
    /// </summary>
    [SugarColumn(ColumnDescription = "在线时长（秒）")]
    public virtual long OnlineTime { get; set; } = 0;

    /// <summary>
    /// 操作次数
    /// </summary>
    [SugarColumn(ColumnDescription = "操作次数")]
    public virtual int OperationCount { get; set; } = 0;

    /// <summary>
    /// 文件上传次数
    /// </summary>
    [SugarColumn(ColumnDescription = "文件上传次数")]
    public virtual int FileUploadCount { get; set; } = 0;

    /// <summary>
    /// 文件下载次数
    /// </summary>
    [SugarColumn(ColumnDescription = "文件下载次数")]
    public virtual int FileDownloadCount { get; set; } = 0;

    /// <summary>
    /// 发送邮件次数
    /// </summary>
    [SugarColumn(ColumnDescription = "发送邮件次数")]
    public virtual int EmailSentCount { get; set; } = 0;

    /// <summary>
    /// 发送短信次数
    /// </summary>
    [SugarColumn(ColumnDescription = "发送短信次数")]
    public virtual int SmsSentCount { get; set; } = 0;

    /// <summary>
    /// 发送通知次数
    /// </summary>
    [SugarColumn(ColumnDescription = "发送通知次数")]
    public virtual int NotificationSentCount { get; set; } = 0;

    /// <summary>
    /// 接收通知次数
    /// </summary>
    [SugarColumn(ColumnDescription = "接收通知次数")]
    public virtual int NotificationReceivedCount { get; set; } = 0;

    /// <summary>
    /// API调用次数
    /// </summary>
    [SugarColumn(ColumnDescription = "API调用次数")]
    public virtual int ApiCallCount { get; set; } = 0;

    /// <summary>
    /// 错误操作次数
    /// </summary>
    [SugarColumn(ColumnDescription = "错误操作次数")]
    public virtual int ErrorOperationCount { get; set; } = 0;

    /// <summary>
    /// 最后登录时间
    /// </summary>
    [SugarColumn(ColumnDescription = "最后登录时间", IsNullable = true)]
    public virtual DateTimeOffset? LastLoginTime { get; set; }

    /// <summary>
    /// 最后访问时间
    /// </summary>
    [SugarColumn(ColumnDescription = "最后访问时间", IsNullable = true)]
    public virtual DateTimeOffset? LastAccessTime { get; set; }

    /// <summary>
    /// 最后操作时间
    /// </summary>
    [SugarColumn(ColumnDescription = "最后操作时间", IsNullable = true)]
    public virtual DateTimeOffset? LastOperationTime { get; set; }

    /// <summary>
    /// 扩展数据（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDescription = "扩展数据", ColumnDataType = "text", IsNullable = true)]
    public virtual string? ExtendData { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
