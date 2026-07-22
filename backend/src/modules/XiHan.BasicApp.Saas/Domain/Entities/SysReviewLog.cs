// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.Framework.Domain.Entities.Abstracts;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统审查日志实体
/// SysReview 每步审批动作的不可变记录：谁在什么时候做了什么决定，构成完整审批链路
/// </summary>
/// <remarks>
/// 职责边界：
/// - 本表记录"审批决策动作"（同意/拒绝/退回/加签/转办），是业务审批的合规证据
/// - 与 SysDiffLog（数据库实体变更审计）职责分离：数据变更由 SysDiffLog 自动捕获，本表只记录审批人的决策行为
/// - 设备/浏览器等请求上下文通过 TraceId 关联 SysAccessLog 查询，本表不冗余
///
/// 分表策略：
/// - 按月分表；查询/清理必带时间范围
///
/// 关联：
/// - ReviewId → SysReview；ReviewUserId → SysUser（执行审查动作的人）
///
/// 写入：
/// - 每次审批流转（同意/拒绝/退回/加签/转办）追加一条
/// - 与 SysReview.ReviewStatus 变更同事务写入，保证一致性
/// - ReviewComment 记录本步审批意见；ReviewResult 枚举具体动作
///
/// 查询：
/// - 审批链路还原：IX_ReId + ORDER BY ReviewTime ASC
/// - 某用户的审查历史：IX_ReUsId
/// - 审查结果统计：IX_ReRe
///
/// 删除：
/// - 禁止业务删除；审查记录是合规证据
/// </remarks>
[SugarTable(TableName = "Sys_Review_Log_{year}{month}{day}", TableDescription = "系统审查日志表"), SplitTable(SplitType.Month)]
[SugarIndex("IX_{split_table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{split_table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_ReId", nameof(ReviewId), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_ReUsId", nameof(ReviewUserId), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_ReRe", nameof(ReviewResult), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_ReTi", nameof(ReviewTime), OrderByType.Desc)]
public partial class SysReviewLog : BasicAppCreationEntity, ISplitTableEntity
{
    /// <summary>
    /// 审查ID
    /// </summary>
    [SugarColumn(ColumnName = "Review_Id", ColumnDescription = "审查ID")]
    public virtual long ReviewId { get; set; }

    /// <summary>
    /// 审查级别
    /// </summary>
    [SugarColumn(ColumnName = "Review_Level", ColumnDescription = "审查级别")]
    public virtual int ReviewLevel { get; set; } = 1;

    /// <summary>
    /// 审查人ID
    /// </summary>
    [SugarColumn(ColumnName = "Review_User_Id", ColumnDescription = "审查人ID", IsNullable = true)]
    public virtual long? ReviewUserId { get; set; }

    /// <summary>
    /// 原审查状态
    /// </summary>
    [SugarColumn(ColumnName = "Original_Status", ColumnDescription = "原审查状态")]
    public virtual AuditStatus OriginalStatus { get; set; }

    /// <summary>
    /// 新审查状态
    /// </summary>
    [SugarColumn(ColumnName = "New_Status", ColumnDescription = "新审查状态")]
    public virtual AuditStatus NewStatus { get; set; }

    /// <summary>
    /// 审查结果
    /// </summary>
    [SugarColumn(ColumnName = "Review_Result", ColumnDescription = "审查结果")]
    public virtual AuditResult ReviewResult { get; set; }

    /// <summary>
    /// 审查意见
    /// </summary>
    [SugarColumn(ColumnName = "Review_Comment", ColumnDescription = "审查意见", Length = 1000, IsNullable = true)]
    public virtual string? ReviewComment { get; set; }

    /// <summary>
    /// 审查动作（Approve/Reject/Return/Countersign/Transfer 等）
    /// </summary>
    [SugarColumn(ColumnName = "Review_Action", ColumnDescription = "审查动作", Length = 50, IsNullable = true)]
    public virtual string? ReviewAction { get; set; }

    /// <summary>
    /// 附件信息（JSON格式，本步审批附带的附件）
    /// </summary>
    [SugarColumn(ColumnName = "Attachments", ColumnDescription = "附件信息", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? Attachments { get; set; }

    /// <summary>
    /// 操作IP
    /// </summary>
    [SugarColumn(ColumnName = "Review_Ip", ColumnDescription = "操作IP", Length = 50, IsNullable = true)]
    public virtual string? ReviewIp { get; set; }

    /// <summary>
    /// 审查时间
    /// </summary>
    [SugarColumn(ColumnName = "Review_Time", ColumnDescription = "审查时间")]
    public virtual DateTimeOffset ReviewTime { get; set; }

    /// <summary>
    /// 扩展数据（JSON格式）
    /// </summary>
    [SugarColumn(ColumnName = "Extend_Data", ColumnDescription = "扩展数据", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? ExtendData { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "Remark", ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(ColumnName = "Created_Time", IsNullable = false, ColumnDescription = "创建时间")]
    [SplitField]
    public override DateTimeOffset CreatedTime { get; set; }
}
