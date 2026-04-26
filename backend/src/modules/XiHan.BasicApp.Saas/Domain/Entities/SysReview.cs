#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysReview
// Guid:a528152c-d6e9-4396-addb-b479254bad60
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/01/08 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Entities.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统审查实体
/// 通用业务审批/审查流聚合根：承载审查单主信息、当前节点、结果；每步审批动作由 SysReviewLog 记录
/// </summary>
/// <remarks>
/// 职责边界：
/// - 本表承载"业务审批决策过程"（谁提交了什么、谁审批了、结果如何）
/// - 与 SysAuditLog（数据库实体变更审计）职责分离：SysAuditLog 记录数据变更事实，本表记录业务审批流程
/// - 敏感操作（如权限变更、角色分配）可关联审查单，实现"先审批后执行"
///
/// 关联：
/// - SubmitUserId / CurrentReviewUserId → SysUser
/// - EntityType + EntityId → 被审查的业务实体
/// - 反向：SysReviewLog.ReviewId（审查动作历史）
///
/// 写入：
/// - TenantId + ReviewCode 租户内唯一（UX_TeId_ReCo）
/// - 提交时 ReviewStatus=Pending + SubmitUserId + SubmitTime
/// - 每次审批流转同时追加 SysReviewLog + 更新 CurrentReviewUserId / ReviewStatus
/// - 支持多级审批：服务层按流程配置推进 CurrentReviewUserId
///
/// 查询：
/// - 我的待办：WHERE CurrentReviewUserId=当前用户 AND ReviewStatus=Pending（IX_CuReUsId）
/// - 我的提交：IX_SuUsId
/// - 按状态分组：IX_TeId_ReSt
///
/// 删除：
/// - 仅软删；已完结记录不建议删除（影响可追溯性）
///
/// 状态：
/// - ReviewStatus: Pending/InProgress/Approved/Rejected/Withdrawn
/// - ReviewType: 区分业务类型（权限申请/角色变更/数据导出审批/内容审核等）
///
/// 场景：
/// - 权限申请审批：用户申请角色/权限 → 主管审批 → 自动授权
/// - 敏感操作审批：批量删除/数据导出 → 需二次确认
/// - 多级审批、会签、驳回重提
/// </remarks>
[SugarTable("SysReview", "系统审查表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_ReCo", nameof(TenantId), OrderByType.Asc, nameof(ReviewCode), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_ReTy", nameof(ReviewType), OrderByType.Asc)]
[SugarIndex("IX_{table}_SuUsId", nameof(SubmitUserId), OrderByType.Asc)]
[SugarIndex("IX_{table}_CuReUsId", nameof(CurrentReviewUserId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_ReSt", nameof(TenantId), OrderByType.Asc, nameof(ReviewStatus), OrderByType.Asc)]
public partial class SysReview : BasicAppAggregateRoot
{
    /// <summary>
    /// 审查编码
    /// </summary>
    [SugarColumn(ColumnDescription = "审查编码", Length = 100, IsNullable = false)]
    public virtual string ReviewCode { get; set; } = string.Empty;

    /// <summary>
    /// 审查标题
    /// </summary>
    [SugarColumn(ColumnDescription = "审查标题", Length = 200, IsNullable = false)]
    public virtual string ReviewTitle { get; set; } = string.Empty;

    /// <summary>
    /// 审查类型
    /// </summary>
    [SugarColumn(ColumnDescription = "审查类型", Length = 50, IsNullable = false)]
    public virtual string ReviewType { get; set; } = string.Empty;

    /// <summary>
    /// 审查内容（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDescription = "审查内容", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? ReviewContent { get; set; }

    /// <summary>
    /// 审查描述
    /// </summary>
    [SugarColumn(ColumnDescription = "审查描述", Length = 1000, IsNullable = true)]
    public virtual string? ReviewDescription { get; set; }

    /// <summary>
    /// 业务实体类型
    /// </summary>
    [SugarColumn(ColumnDescription = "业务实体类型", Length = 100, IsNullable = true)]
    public virtual string? EntityType { get; set; }

    /// <summary>
    /// 业务实体ID
    /// </summary>
    [SugarColumn(ColumnDescription = "业务实体ID", Length = 100, IsNullable = true)]
    public virtual string? EntityId { get; set; }

    /// <summary>
    /// 业务数据（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDescription = "业务数据", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? BusinessData { get; set; }

    /// <summary>
    /// 审查状态
    /// </summary>
    [SugarColumn(ColumnDescription = "审查状态")]
    public virtual AuditStatus ReviewStatus { get; set; } = AuditStatus.Pending;

    /// <summary>
    /// 审查结果
    /// </summary>
    [SugarColumn(ColumnDescription = "审查结果", IsNullable = true)]
    public virtual AuditResult? ReviewResult { get; set; }

    /// <summary>
    /// 优先级（数字越大优先级越高，与 SysPermission/SysConstraintRule/SysFieldLevelSecurity 方向一致）
    /// </summary>
    [SugarColumn(ColumnDescription = "优先级")]
    public virtual int Priority { get; set; } = 0;

    /// <summary>
    /// 提交人ID
    /// </summary>
    [SugarColumn(ColumnDescription = "提交人ID", IsNullable = true)]
    public virtual long? SubmitUserId { get; set; }

    /// <summary>
    /// 提交时间
    /// </summary>
    [SugarColumn(ColumnDescription = "提交时间")]
    public virtual DateTimeOffset SubmitTime { get; set; }

    /// <summary>
    /// 当前审查人ID
    /// </summary>
    [SugarColumn(ColumnDescription = "当前审查人ID", IsNullable = true)]
    public virtual long? CurrentReviewUserId { get; set; }

    /// <summary>
    /// 审查人ID列表（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDescription = "审查人ID列表", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? ReviewUserIds { get; set; }

    /// <summary>
    /// 审查级别
    /// </summary>
    [SugarColumn(ColumnDescription = "审查级别")]
    public virtual int ReviewLevel { get; set; } = 1;

    /// <summary>
    /// 当前审查级别
    /// </summary>
    [SugarColumn(ColumnDescription = "当前审查级别")]
    public virtual int CurrentLevel { get; set; } = 1;

    /// <summary>
    /// 审查开始时间
    /// </summary>
    [SugarColumn(ColumnDescription = "审查开始时间", IsNullable = true)]
    public virtual DateTimeOffset? ReviewStartTime { get; set; }

    /// <summary>
    /// 审查结束时间
    /// </summary>
    [SugarColumn(ColumnDescription = "审查结束时间", IsNullable = true)]
    public virtual DateTimeOffset? ReviewEndTime { get; set; }

    /// <summary>
    /// 附件信息（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDescription = "附件信息", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? Attachments { get; set; }

    /// <summary>
    /// 扩展数据（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDescription = "扩展数据", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? ExtendData { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnDescription = "状态")]
    public virtual EnableStatus Status { get; set; } = EnableStatus.Enabled;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
