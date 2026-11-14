// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.CodeGeneration.Domain.Entities;

/// <summary>
/// 代码生成的表配置
/// </summary>
/// <remarks>
/// 平台库实体、只在平台一侧。由导入表结构建出，一张库表一条（表名唯一，与数据源无关），删除时连带软删它的列配置
/// 生成的页面登记在所属包的 <c>Pages.cs</c>、目录按包；生成目标是新建包或加入已有包
/// </remarks>
[SugarTable(TableName = "sys_code_gen_table", TableDescription = "代码生成的表配置")]
[ActiveUniqueIndex(nameof(TableName), Name = "ux_sys_code_gen_table_table_name")]
[SugarIndex("ix_sys_code_gen_table_class_name", nameof(ClassName), OrderByType.Asc)]
[SugarIndex("ix_sys_code_gen_table_module_name", nameof(ModuleName), OrderByType.Asc)]
public sealed class SysCodeGenTable : AppAuditedEntity
{
    /// <summary>
    /// 库里的表名
    /// </summary>
    /// <remarks>
    /// 照读库结构返回的原名（大小写混合的照原样）
    /// </remarks>
    [SugarColumn(ColumnName = "table_name", Length = 200, IsNullable = false, ColumnDescription = "库里的表名")]
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// 表注释
    /// </summary>
    [SugarColumn(ColumnName = "table_comment", Length = 500, IsNullable = true, ColumnDescription = "表注释")]
    public string? TableComment { get; set; }

    /// <summary>
    /// 实体类名
    /// </summary>
    [SugarColumn(ColumnName = "class_name", Length = 200, IsNullable = false, ColumnDescription = "实体类名")]
    public string ClassName { get; set; } = string.Empty;

    /// <summary>
    /// 包的根命名空间
    /// </summary>
    /// <remarks>
    /// 如 <c>XiHan.BasicApp.Demo</c>，即包的工程名
    /// </remarks>
    [SugarColumn(ColumnName = "namespace", Length = 500, IsNullable = true, ColumnDescription = "包的根命名空间")]
    public string? Namespace { get; set; }

    /// <summary>
    /// 包名
    /// </summary>
    /// <remarks>
    /// 如 <c>Demo</c>：权限码、页面码与前端模块目录取它的小写
    /// </remarks>
    [SugarColumn(ColumnName = "module_name", Length = 100, IsNullable = true, ColumnDescription = "包名")]
    public string? ModuleName { get; set; }

    /// <summary>
    /// 业务名
    /// </summary>
    [SugarColumn(ColumnName = "business_name", Length = 100, IsNullable = true, ColumnDescription = "业务名")]
    public string? BusinessName { get; set; }

    /// <summary>
    /// 功能名
    /// </summary>
    [SugarColumn(ColumnName = "function_name", Length = 100, IsNullable = true, ColumnDescription = "功能名")]
    public string? FunctionName { get; set; }

    /// <summary>
    /// 作者
    /// </summary>
    [SugarColumn(ColumnName = "author", Length = 100, IsNullable = true, ColumnDescription = "作者")]
    public string? Author { get; set; }

    /// <summary>
    /// 生成模式
    /// </summary>
    [SugarColumn(ColumnName = "template_type", IsNullable = false, ColumnDescription = "生成模式")]
    public CodeGenTemplateType TemplateType { get; set; } = CodeGenTemplateType.Single;

    /// <summary>
    /// 交付方式
    /// </summary>
    [SugarColumn(ColumnName = "delivery_type", IsNullable = false, ColumnDescription = "交付方式")]
    public CodeGenDeliveryType DeliveryType { get; set; } = CodeGenDeliveryType.Zip;

    /// <summary>
    /// 生成范围
    /// </summary>
    [SugarColumn(ColumnName = "scope", IsNullable = false, ColumnDescription = "生成范围")]
    public CodeGenScope Scope { get; set; } = CodeGenScope.All;

    /// <summary>
    /// 生成目标
    /// </summary>
    [SugarColumn(ColumnName = "target", IsNullable = false, ColumnDescription = "生成目标")]
    public CodeGenTarget Target { get; set; } = CodeGenTarget.NewPackage;

    /// <summary>
    /// 勾选的动作
    /// </summary>
    /// <remarks>
    /// 逗号分隔，取自 <c>create,update,delete,export,import,status,print</c>；为空即缺省的新增、编辑、删除、导出与导入（启停与打印要显式勾选）
    /// </remarks>
    [SugarColumn(ColumnName = "enabled_actions", Length = 200, IsNullable = true, ColumnDescription = "勾选的动作")]
    public string? EnabledActions { get; set; }

    /// <summary>
    /// 主键列名
    /// </summary>
    [SugarColumn(ColumnName = "primary_key_column", Length = 200, IsNullable = true, ColumnDescription = "主键列名")]
    public string? PrimaryKeyColumn { get; set; }

    /// <summary>
    /// 树表的父级列名
    /// </summary>
    [SugarColumn(ColumnName = "tree_parent_column", Length = 200, IsNullable = true, ColumnDescription = "树表的父级列名")]
    public string? TreeParentColumn { get; set; }

    /// <summary>
    /// 树表的名称列名
    /// </summary>
    [SugarColumn(ColumnName = "tree_name_column", Length = 200, IsNullable = true, ColumnDescription = "树表的名称列名")]
    public string? TreeNameColumn { get; set; }

    /// <summary>
    /// 主从模式里主表的表配置 ID
    /// </summary>
    /// <remarks>
    /// 本表是明细表时指向主表
    /// </remarks>
    [SugarColumn(ColumnName = "master_table_id", IsNullable = true, ColumnDescription = "主从模式里主表的表配置 ID")]
    public Guid? MasterTableId { get; set; }

    /// <summary>
    /// 主从模式里指向主表的外键列名
    /// </summary>
    [SugarColumn(ColumnName = "master_foreign_key", Length = 200, IsNullable = true, ColumnDescription = "主从模式里指向主表的外键列名")]
    public string? MasterForeignKey { get; set; }

    /// <summary>
    /// 数据库类型
    /// </summary>
    /// <remarks>
    /// 导入时按所在的库记下，类型映射按它选
    /// </remarks>
    [SugarColumn(ColumnName = "database_type", IsNullable = false, ColumnDescription = "数据库类型")]
    public CodeGenDatabaseType DatabaseType { get; set; }

    /// <summary>
    /// 数据源 ID
    /// </summary>
    /// <remarks>
    /// 为空即主库
    /// </remarks>
    [SugarColumn(ColumnName = "data_source_id", IsNullable = true, ColumnDescription = "数据源 ID")]
    public Guid? DataSourceId { get; set; }

    /// <summary>
    /// 最近一次生成的结果
    /// </summary>
    [SugarColumn(ColumnName = "generation_status", IsNullable = false, ColumnDescription = "最近一次生成的结果")]
    public CodeGenGenerationStatus GenerationStatus { get; set; } = CodeGenGenerationStatus.NotGenerated;

    /// <summary>
    /// 最近一次成功生成的时间（UTC）
    /// </summary>
    [SugarColumn(ColumnName = "last_generated_at", IsNullable = true, ColumnDescription = "最近一次成功生成的时间（UTC）")]
    public DateTimeOffset? LastGeneratedAt { get; set; }

    /// <summary>
    /// 扩展选项（JSON）
    /// </summary>
    [SugarColumn(ColumnName = "options", IsNullable = true, ColumnDescription = "扩展选项（JSON）")]
    [LongText]
    public string? Options { get; set; }

    /// <summary>
    /// 人工改过的字段
    /// </summary>
    /// <remarks>
    /// 属性名的 JSON 数组：同步结构时不覆盖这些字段
    /// </remarks>
    [SugarColumn(ColumnName = "user_modified_fields", Length = 1000, IsNullable = true, ColumnDescription = "人工改过的字段")]
    public string? UserModifiedFields { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "status", IsNullable = false, ColumnDescription = "状态")]
    public CodeGenStatus Status { get; set; } = CodeGenStatus.Active;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "remark", Length = 500, IsNullable = true, ColumnDescription = "备注")]
    public string? Remark { get; set; }
}

/// <summary>
/// 生成范围
/// </summary>
public enum CodeGenScope
{
    /// <summary>
    /// 前后端都生成
    /// </summary>
    All = 0,

    /// <summary>
    /// 只生成后端
    /// </summary>
    BackendOnly = 1,

    /// <summary>
    /// 只生成前端
    /// </summary>
    FrontendOnly = 2,
}

/// <summary>
/// 生成目标
/// </summary>
/// <remarks>
/// 新建包另出包级文件（工程、特性、权限、页面、文案……）；加入已有包只出资源相关文件与一份要人工合入的登记片段
/// </remarks>
public enum CodeGenTarget
{
    /// <summary>
    /// 新建包
    /// </summary>
    NewPackage = 0,

    /// <summary>
    /// 加入已有包
    /// </summary>
    ExistingPackage = 1,
}
