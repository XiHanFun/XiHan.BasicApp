#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserStatistics
// Guid:dc28152c-d6e9-4396-addb-b479254bad47
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 06:10:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统用户统计实体
/// 按周期汇总的用户行为统计快照（登录次数、在线时长、操作量等），供看板/报表读取
/// </summary>
/// <remarks>
/// 关联：
/// - UserId → SysUser
///
/// 写入：
/// - TenantId + UserId + StatisticsDate + Period 唯一（UX_TeId_UsId_StDa_Pe），避免重复统计
/// - 由定时任务按周期（Day/Week/Month/Year）批量生成，非实时写入
/// - 同 Period 的当日行数据可被覆盖刷新（upsert），历史 Period 数据只读
///
/// 查询：
/// - 用户趋势图：WHERE UserId=? AND Period=? ORDER BY StatisticsDate
/// - 租户汇总看板：IX_TeId_StDa
///
/// 删除：
/// - 仅软删；可定期归档到冷存储
///
/// 场景：
/// - 管理后台用户活跃度看板
/// - 用户画像数据源
/// - 异常行为检测（比平时 10x 操作量 → 告警）
/// </remarks>
[SugarTable("SysUserStatistics", "系统用户统计表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_UsId_StDa_Pe", nameof(TenantId), OrderByType.Asc, nameof(UserId), OrderByType.Asc, nameof(StatisticsDate), OrderByType.Desc, nameof(Period), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_TeId_StDa", nameof(TenantId), OrderByType.Asc, nameof(StatisticsDate), OrderByType.Desc)]
public partial class SysUserStatistics : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 用户ID（0 表示全体用户汇总统计）
    /// </summary>
    [SugarColumn(ColumnDescription = "用户ID")]
    public virtual long UserId { get; set; }

    /// <summary>
    /// 统计日期
    /// </summary>
    [SugarColumn(ColumnDescription = "统计日期")]
    public virtual DateOnly StatisticsDate { get; set; }

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
    [SugarColumn(ColumnDescription = "扩展数据", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? ExtendData { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
