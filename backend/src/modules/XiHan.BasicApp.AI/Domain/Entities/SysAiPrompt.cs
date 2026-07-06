#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysAiPrompt
// Guid:a11c0de0-9001-4a10-9a00-00000000ai90
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/06 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.AI.Domain.Entities;

/// <summary>
/// 系统 AI 提示词模板实体（store 化提示词库；填框架 IAiPromptStore 抽象）
/// </summary>
/// <remarks>非机密，无需加密；按 PromptCode 取用，支持版本；无默认标记(按名显式选取)。</remarks>
[SugarTable(TableName = "Sys_Ai_Prompt", TableDescription = "系统 AI 提示词模板表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_PrCd", nameof(TenantId), OrderByType.Asc, nameof(PromptCode), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
public partial class SysAiPrompt : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 提示词编码（租户内唯一，上层以此取用）
    /// </summary>
    [SugarColumn(ColumnName = "Prompt_Code", ColumnDescription = "提示词编码", Length = 100, IsNullable = false)]
    public virtual string PromptCode { get; set; } = string.Empty;

    /// <summary>
    /// 提示词名称
    /// </summary>
    [SugarColumn(ColumnName = "Prompt_Name", ColumnDescription = "提示词名称", Length = 200, IsNullable = false)]
    public virtual string PromptName { get; set; } = string.Empty;

    /// <summary>
    /// 分组/分类
    /// </summary>
    [SugarColumn(ColumnName = "Category", ColumnDescription = "分类", Length = 100, IsNullable = true)]
    public virtual string? Category { get; set; }

    /// <summary>
    /// 版本（空为当前/最新）
    /// </summary>
    [SugarColumn(ColumnName = "Version", ColumnDescription = "版本", Length = 100, IsNullable = true)]
    public virtual string? Version { get; set; }

    /// <summary>
    /// 提示词正文（可含占位/Scriban 变量）
    /// </summary>
    [SugarColumn(ColumnName = "Content", ColumnDescription = "提示词正文", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = false)]
    public virtual string Content { get; set; } = string.Empty;

    /// <summary>
    /// 是否启用（禁用后不参与库解析）
    /// </summary>
    [SugarColumn(ColumnName = "Is_Enabled", ColumnDescription = "是否启用")]
    public virtual bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 排序
    /// </summary>
    [SugarColumn(ColumnName = "Sort", ColumnDescription = "排序")]
    public virtual int Sort { get; set; } = 0;

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "Status", ColumnDescription = "状态")]
    public virtual EnableStatus Status { get; set; } = EnableStatus.Enabled;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "Remark", ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
