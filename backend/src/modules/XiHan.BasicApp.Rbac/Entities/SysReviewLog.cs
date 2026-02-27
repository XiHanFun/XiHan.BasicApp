#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysReviewLog
// Guid:b528152c-d6e9-4396-addb-b479254bad61
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/01/08 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统审查日志实体
/// </summary>
[SugarTable("Sys_Review_Log_{year}{month}{day}", "系统审查日志表"), SplitTable(SplitType.Month)]
[SugarIndex("IX_SysReviewLog_ReId", nameof(ReviewId), OrderByType.Asc)]
[SugarIndex("IX_SysReviewLog_ReUsId", nameof(ReviewUserId), OrderByType.Asc)]
[SugarIndex("IX_SysReviewLog_ReRe", nameof(ReviewResult), OrderByType.Asc)]
[SugarIndex("IX_SysReviewLog_ReTi", nameof(ReviewTime), OrderByType.Desc)]
public partial class SysReviewLog : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 审查ID
    /// </summary>
    [SugarColumn(ColumnDescription = "审查ID")]
    public virtual long ReviewId { get; set; }

    /// <summary>
    /// 审查编码
    /// </summary>
    [SugarColumn(ColumnDescription = "审查编码", Length = 100, IsNullable = true)]
    public virtual string? ReviewCode { get; set; }

    /// <summary>
    /// 审查标题
    /// </summary>
    [SugarColumn(ColumnDescription = "审查标题", Length = 200, IsNullable = true)]
    public virtual string? ReviewTitle { get; set; }

    /// <summary>
    /// 审查类型
    /// </summary>
    [SugarColumn(ColumnDescription = "审查类型", Length = 50, IsNullable = true)]
    public virtual string? ReviewType { get; set; }

    /// <summary>
    /// 审查级别
    /// </summary>
    [SugarColumn(ColumnDescription = "审查级别")]
    public virtual int ReviewLevel { get; set; } = 1;

    /// <summary>
    /// 审查人ID
    /// </summary>
    [SugarColumn(ColumnDescription = "审查人ID", IsNullable = true)]
    public virtual long? ReviewUserId { get; set; }

    /// <summary>
    /// 审查人名称
    /// </summary>
    [SugarColumn(ColumnDescription = "审查人名称", Length = 50, IsNullable = true)]
    public virtual string? ReviewUserName { get; set; }

    /// <summary>
    /// 审查人部门
    /// </summary>
    [SugarColumn(ColumnDescription = "审查人部门", Length = 100, IsNullable = true)]
    public virtual string? ReviewUserDepartment { get; set; }

    /// <summary>
    /// 原审查状态
    /// </summary>
    [SugarColumn(ColumnDescription = "原审查状态")]
    public virtual AuditStatus OriginalStatus { get; set; }

    /// <summary>
    /// 新审查状态
    /// </summary>
    [SugarColumn(ColumnDescription = "新审查状态")]
    public virtual AuditStatus NewStatus { get; set; }

    /// <summary>
    /// 审查结果
    /// </summary>
    [SugarColumn(ColumnDescription = "审查结果")]
    public virtual AuditResult ReviewResult { get; set; }

    /// <summary>
    /// 审查意见
    /// </summary>
    [SugarColumn(ColumnDescription = "审查意见", Length = 1000, IsNullable = true)]
    public virtual string? ReviewComment { get; set; }

    /// <summary>
    /// 审查动作
    /// </summary>
    [SugarColumn(ColumnDescription = "审查动作", Length = 50, IsNullable = true)]
    public virtual string? ReviewAction { get; set; }

    /// <summary>
    /// 审查前数据（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDescription = "审查前数据", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? BeforeData { get; set; }

    /// <summary>
    /// 审查后数据（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDescription = "审查后数据", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? AfterData { get; set; }

    /// <summary>
    /// 数据变更内容（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDescription = "数据变更内容", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? ChangeContent { get; set; }

    /// <summary>
    /// 附件信息（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDescription = "附件信息", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? Attachments { get; set; }

    /// <summary>
    /// 审查IP
    /// </summary>
    [SugarColumn(ColumnDescription = "审查IP", Length = 50, IsNullable = true)]
    public virtual string? ReviewIp { get; set; }

    /// <summary>
    /// 审查地址
    /// </summary>
    [SugarColumn(ColumnDescription = "审查地址", Length = 200, IsNullable = true)]
    public virtual string? ReviewLocation { get; set; }

    /// <summary>
    /// 浏览器类型
    /// </summary>
    [SugarColumn(ColumnDescription = "浏览器类型", Length = 100, IsNullable = true)]
    public virtual string? Browser { get; set; }

    /// <summary>
    /// 操作系统
    /// </summary>
    [SugarColumn(ColumnDescription = "操作系统", Length = 100, IsNullable = true)]
    public virtual string? Os { get; set; }

    /// <summary>
    /// 设备类型
    /// </summary>
    [SugarColumn(ColumnDescription = "设备类型", Length = 50, IsNullable = true)]
    public virtual string? Device { get; set; }

    /// <summary>
    /// 审查时间
    /// </summary>
    [SugarColumn(ColumnDescription = "审查时间")]
    public virtual DateTimeOffset ReviewTime { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// 审查耗时（毫秒）
    /// </summary>
    [SugarColumn(ColumnDescription = "审查耗时（毫秒）")]
    public virtual long ReviewDuration { get; set; } = 0;

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

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(IsNullable = false, ColumnDescription = "创建时间")]
    [SplitField]
    public override DateTimeOffset CreatedTime { get; set; }
}
