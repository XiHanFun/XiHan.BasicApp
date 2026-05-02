#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysCodeGenTemplate
// Guid:a1b2c3d4-e5f6-7890-abcd-ef1234567012
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/23 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.CodeGeneration.Domain.Entities;

/// <summary>
/// 系统代码生成模板实体
/// 代码生成引擎使用的模板文件元信息（Razor/Liquid 等），承载模板分组、目标文件名规则、输出路径
/// </summary>
/// <remarks>
/// 关联：
/// - 无 FK；模板分组（TemplateGroup）用于批量应用
///
/// 写入：
/// - TemplateCode 全局唯一（UX_TeCo）
/// - 模板正文（Content）建议独立 BigString 字段存储
/// - 模板变更建议经审核流程（影响所有生成结果）
///
/// 查询：
/// - 生成时按 TemplateGroup 批量加载：IX_TeGr
/// - 模板管理后台列表：IX_TeId_St
///
/// 删除：
/// - 仅软删；删除前须确认无正在使用的生成任务
///
/// 状态：
/// - Status: Yes/No
///
/// 场景：
/// - 全栈代码生成（Entity/DTO/Service/Controller/Vue Page）
/// - 多风格模板（CRUD 简化版 / 完整版 / 只读展示版）
/// - 模板版本化管理
/// </remarks>
[SugarTable("SysCodeGenTemplate", "系统代码生成模板表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeCo", nameof(TemplateCode), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_TeGr", nameof(TemplateGroup), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
public partial class SysCodeGenTemplate : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 模板编码
    /// </summary>
    [SugarColumn(ColumnDescription = "模板编码", Length = 100, IsNullable = false)]
    public virtual string TemplateCode { get; set; } = string.Empty;

    /// <summary>
    /// 模板名称
    /// </summary>
    [SugarColumn(ColumnDescription = "模板名称", Length = 200, IsNullable = false)]
    public virtual string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// 模板描述
    /// </summary>
    [SugarColumn(ColumnDescription = "模板描述", Length = 500, IsNullable = true)]
    public virtual string? TemplateDescription { get; set; }

    /// <summary>
    /// 模板分组
    /// </summary>
    [SugarColumn(ColumnDescription = "模板分组", Length = 100, IsNullable = true)]
    public virtual string? TemplateGroup { get; set; }

    /// <summary>
    /// 模板类型
    /// </summary>
    [SugarColumn(ColumnDescription = "模板类型")]
    public virtual TemplateType TemplateType { get; set; } = TemplateType.Single;

    /// <summary>
    /// 模板引擎
    /// </summary>
    [SugarColumn(ColumnDescription = "模板引擎")]
    public virtual TemplateEngine TemplateEngine { get; set; } = TemplateEngine.Razor;

    /// <summary>
    /// 模板内容
    /// </summary>
    [SugarColumn(ColumnDescription = "模板内容", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? TemplateContent { get; set; }

    /// <summary>
    /// 模板路径
    /// </summary>
    [SugarColumn(ColumnDescription = "模板路径", Length = 500, IsNullable = true)]
    public virtual string? TemplatePath { get; set; }

    /// <summary>
    /// 生成文件名表达式
    /// </summary>
    [SugarColumn(ColumnDescription = "生成文件名表达式", Length = 500, IsNullable = true)]
    public virtual string? FileNameExpression { get; set; }

    /// <summary>
    /// 生成文件路径表达式
    /// </summary>
    [SugarColumn(ColumnDescription = "生成文件路径表达式", Length = 500, IsNullable = true)]
    public virtual string? FilePathExpression { get; set; }

    /// <summary>
    /// 文件扩展名
    /// </summary>
    [SugarColumn(ColumnDescription = "文件扩展名", Length = 20, IsNullable = true)]
    public virtual string? FileExtension { get; set; }

    /// <summary>
    /// 是否内置
    /// </summary>
    [SugarColumn(ColumnDescription = "是否内置")]
    public virtual bool IsBuiltIn { get; set; } = false;

    /// <summary>
    /// 是否启用
    /// </summary>
    [SugarColumn(ColumnDescription = "是否启用")]
    public virtual bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 排序
    /// </summary>
    [SugarColumn(ColumnDescription = "排序")]
    public virtual int Sort { get; set; } = 0;

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
