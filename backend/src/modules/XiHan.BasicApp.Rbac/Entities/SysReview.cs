#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysReview
// Guid:a528152c-d6e9-4396-addb-b479254bad60
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/8 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统审查实体
/// </summary>
[SugarTable("Sys_Review", "系统审查表")]
[SugarIndex("IX_SysReview_ReviewCode", nameof(ReviewCode), OrderByType.Asc, true)]
[SugarIndex("IX_SysReview_ReviewType", nameof(ReviewType), OrderByType.Asc)]
[SugarIndex("IX_SysReview_ReviewStatus", nameof(ReviewStatus), OrderByType.Asc)]
[SugarIndex("IX_SysReview_TenantId", nameof(TenantId), OrderByType.Asc)]
public partial class SysReview : RbacAggregateRoot<long>
{
    /// <summary>
    /// 租户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "租户ID", IsNullable = true)]
    public virtual long? TenantId { get; set; }

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
    /// 优先级（1-5，数字越小优先级越高）
    /// </summary>
    [SugarColumn(ColumnDescription = "优先级")]
    public virtual int Priority { get; set; } = 3;

    /// <summary>
    /// 提交人ID
    /// </summary>
    [SugarColumn(ColumnDescription = "提交人ID", IsNullable = true)]
    public virtual long? SubmitUserId { get; set; }

    /// <summary>
    /// 提交人名称
    /// </summary>
    [SugarColumn(ColumnDescription = "提交人名称", Length = 50, IsNullable = true)]
    public virtual string? SubmitUserName { get; set; }

    /// <summary>
    /// 提交时间
    /// </summary>
    [SugarColumn(ColumnDescription = "提交时间")]
    public virtual DateTimeOffset SubmitTime { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// 当前审查人ID
    /// </summary>
    [SugarColumn(ColumnDescription = "当前审查人ID", IsNullable = true)]
    public virtual long? CurrentReviewUserId { get; set; }

    /// <summary>
    /// 当前审查人名称
    /// </summary>
    [SugarColumn(ColumnDescription = "当前审查人名称", Length = 50, IsNullable = true)]
    public virtual string? CurrentReviewUserName { get; set; }

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
    /// 审查意见
    /// </summary>
    [SugarColumn(ColumnDescription = "审查意见", Length = 1000, IsNullable = true)]
    public virtual string? ReviewComment { get; set; }

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
    /// 审查耗时（秒）
    /// </summary>
    [SugarColumn(ColumnDescription = "审查耗时（秒）")]
    public virtual long ReviewDuration { get; set; } = 0;

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
    public virtual YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
