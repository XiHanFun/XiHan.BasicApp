// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.CodeGeneration.Domain.Entities;

/// <summary>
/// 代码生成的列配置
/// </summary>
/// <remarks>
/// 平台库实体、只在平台一侧。列配置单独成表、按 <see cref="TableId"/> 挂在表配置下：一张表的列数不定，每列的类型、控件、查询方式与选项来源都要单独调
/// 同一张表的列名唯一（部分唯一索引，方案 C2）：同步结构按列名对上库里的列，软删除后同名的列可以重新补进来
/// 结构字段与推断字段分开对待：列类型、注释、主键、自增、可空、长度与小数位数以库为准，每次同步都覆盖；C# 类型、控件、查询方式等是推断再加人工调整，改过的记进 <see cref="UserModifiedFields"/>，同步时不再覆盖
/// 选项来源只留生效的那一个字段（字典码、枚举名、常量 JSON 或关联表），保存时清掉其余的，免得前一种来源的残留值报出无关的错
/// 关联表与关联树按 <see cref="RelationTableId"/> 指向另一张表配置，生成时才解析目标并核对类型
/// 库里已删的列在同步时软删列配置；零代码运行时只逐列取列配置里有的列，库里已没有的读不出，要先同步结构
/// </remarks>
[SugarTable(TableName = "sys_code_gen_table_column", TableDescription = "代码生成的列配置")]
[ActiveUniqueIndex(nameof(TableId), nameof(ColumnName), Name = "ux_sys_code_gen_table_column_table_id_column_name")]
public sealed class SysCodeGenTableColumn : AppAuditedEntity
{
    /// <summary>
    /// 表配置 ID
    /// </summary>
    [SugarColumn(ColumnName = "table_id", IsNullable = false, ColumnDescription = "表配置 ID")]
    public Guid TableId { get; set; }

    /// <summary>
    /// 库里的列名
    /// </summary>
    [SugarColumn(ColumnName = "column_name", Length = 200, IsNullable = false, ColumnDescription = "库里的列名")]
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// 列注释
    /// </summary>
    [SugarColumn(ColumnName = "column_comment", Length = 500, IsNullable = true, ColumnDescription = "列注释")]
    public string? ColumnComment { get; set; }

    /// <summary>
    /// 库里的列类型名
    /// </summary>
    [SugarColumn(ColumnName = "column_type", Length = 100, IsNullable = true, ColumnDescription = "库里的列类型名")]
    public string? ColumnType { get; set; }

    /// <summary>
    /// C# 类型
    /// </summary>
    [SugarColumn(ColumnName = "csharp_type", Length = 100, IsNullable = true, ColumnDescription = "C# 类型")]
    public string? CSharpType { get; set; }

    /// <summary>
    /// C# 属性名
    /// </summary>
    [SugarColumn(ColumnName = "csharp_property", Length = 200, IsNullable = true, ColumnDescription = "C# 属性名")]
    public string? CSharpProperty { get; set; }

    /// <summary>
    /// TypeScript 类型
    /// </summary>
    [SugarColumn(ColumnName = "ts_type", Length = 100, IsNullable = true, ColumnDescription = "TypeScript 类型")]
    public string? TsType { get; set; }

    /// <summary>
    /// 长度
    /// </summary>
    /// <remarks>
    /// 只有字符与二进制类型有（框架读库结构的口径）
    /// </remarks>
    [SugarColumn(ColumnName = "column_length", IsNullable = true, ColumnDescription = "长度")]
    public int? ColumnLength { get; set; }

    /// <summary>
    /// 小数位数
    /// </summary>
    [SugarColumn(ColumnName = "decimal_digits", IsNullable = true, ColumnDescription = "小数位数")]
    public int? DecimalDigits { get; set; }

    /// <summary>
    /// 是否主键
    /// </summary>
    [SugarColumn(ColumnName = "is_primary_key", IsNullable = false, ColumnDescription = "是否主键")]
    public bool IsPrimaryKey { get; set; }

    /// <summary>
    /// 是否自增
    /// </summary>
    [SugarColumn(ColumnName = "is_identity", IsNullable = false, ColumnDescription = "是否自增")]
    public bool IsIdentity { get; set; }

    /// <summary>
    /// 是否可空
    /// </summary>
    [SugarColumn(ColumnName = "is_nullable", IsNullable = false, ColumnDescription = "是否可空")]
    public bool IsNullable { get; set; }

    /// <summary>
    /// 是否必填
    /// </summary>
    [SugarColumn(ColumnName = "is_required", IsNullable = false, ColumnDescription = "是否必填")]
    public bool IsRequired { get; set; }

    /// <summary>
    /// 是否唯一
    /// </summary>
    [SugarColumn(ColumnName = "is_unique", IsNullable = false, ColumnDescription = "是否唯一")]
    public bool IsUnique { get; set; }

    /// <summary>
    /// 是否在列表里显示
    /// </summary>
    [SugarColumn(ColumnName = "is_list", IsNullable = false, ColumnDescription = "是否在列表里显示")]
    public bool IsList { get; set; } = true;

    /// <summary>
    /// 是否进新建表单
    /// </summary>
    [SugarColumn(ColumnName = "is_insert", IsNullable = false, ColumnDescription = "是否进新建表单")]
    public bool IsInsert { get; set; } = true;

    /// <summary>
    /// 是否进编辑表单
    /// </summary>
    [SugarColumn(ColumnName = "is_edit", IsNullable = false, ColumnDescription = "是否进编辑表单")]
    public bool IsEdit { get; set; } = true;

    /// <summary>
    /// 是否可查询
    /// </summary>
    [SugarColumn(ColumnName = "is_query", IsNullable = false, ColumnDescription = "是否可查询")]
    public bool IsQuery { get; set; }

    /// <summary>
    /// 查询方式
    /// </summary>
    [SugarColumn(ColumnName = "query_type", IsNullable = false, ColumnDescription = "查询方式")]
    public CodeGenQueryType QueryType { get; set; } = CodeGenQueryType.Equal;

    /// <summary>
    /// 表单控件
    /// </summary>
    [SugarColumn(ColumnName = "html_type", IsNullable = false, ColumnDescription = "表单控件")]
    public CodeGenHtmlType HtmlType { get; set; } = CodeGenHtmlType.Input;

    /// <summary>
    /// 选项来源
    /// </summary>
    [SugarColumn(ColumnName = "dict_selector_type", IsNullable = true, ColumnDescription = "选项来源")]
    public CodeGenDictSelectorType? DictSelectorType { get; set; }

    /// <summary>
    /// 字典码
    /// </summary>
    [SugarColumn(ColumnName = "dict_code", Length = 200, IsNullable = true, ColumnDescription = "字典码")]
    public string? DictCode { get; set; }

    /// <summary>
    /// 枚举类型全名
    /// </summary>
    [SugarColumn(ColumnName = "enum_type_name", Length = 500, IsNullable = true, ColumnDescription = "枚举类型全名")]
    public string? EnumTypeName { get; set; }

    /// <summary>
    /// 常量选项（JSON）
    /// </summary>
    [SugarColumn(ColumnName = "const_values", IsNullable = true, ColumnDescription = "常量选项（JSON）")]
    [LongText]
    public string? ConstValues { get; set; }

    /// <summary>
    /// 关联表的表配置 ID
    /// </summary>
    [SugarColumn(ColumnName = "relation_table_id", IsNullable = true, ColumnDescription = "关联表的表配置 ID")]
    public Guid? RelationTableId { get; set; }

    /// <summary>
    /// 关联表的显示列名
    /// </summary>
    [SugarColumn(ColumnName = "relation_label_column", Length = 200, IsNullable = true, ColumnDescription = "关联表的显示列名")]
    public string? RelationLabelColumn { get; set; }

    /// <summary>
    /// 缺省值
    /// </summary>
    [SugarColumn(ColumnName = "default_value", Length = 500, IsNullable = true, ColumnDescription = "缺省值")]
    public string? DefaultValue { get; set; }

    /// <summary>
    /// 最小值
    /// </summary>
    [SugarColumn(ColumnName = "min_value", IsNullable = true, ColumnDescription = "最小值")]
    public decimal? MinValue { get; set; }

    /// <summary>
    /// 最大值
    /// </summary>
    [SugarColumn(ColumnName = "max_value", IsNullable = true, ColumnDescription = "最大值")]
    public decimal? MaxValue { get; set; }

    /// <summary>
    /// 校验用的正则
    /// </summary>
    [SugarColumn(ColumnName = "regex_pattern", Length = 500, IsNullable = true, ColumnDescription = "校验用的正则")]
    public string? RegexPattern { get; set; }

    /// <summary>
    /// 校验不过时的提示
    /// </summary>
    [SugarColumn(ColumnName = "validation_message", Length = 500, IsNullable = true, ColumnDescription = "校验不过时的提示")]
    public string? ValidationMessage { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    [SugarColumn(ColumnName = "order", IsNullable = false, ColumnDescription = "排序号")]
    public int Order { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "status", IsNullable = false, ColumnDescription = "状态")]
    public CodeGenStatus Status { get; set; } = CodeGenStatus.Active;

    /// <summary>
    /// 人工改过的字段
    /// </summary>
    /// <remarks>
    /// 属性名的 JSON 数组：同步结构时不覆盖这些字段
    /// </remarks>
    [SugarColumn(ColumnName = "user_modified_fields", Length = 500, IsNullable = true, ColumnDescription = "人工改过的字段")]
    public string? UserModifiedFields { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "remark", Length = 500, IsNullable = true, ColumnDescription = "备注")]
    public string? Remark { get; set; }
}

/// <summary>
/// 列的查询方式
/// </summary>
public enum CodeGenQueryType
{
    /// <summary>
    /// 等于
    /// </summary>
    Equal = 0,

    /// <summary>
    /// 不等于
    /// </summary>
    NotEqual = 1,

    /// <summary>
    /// 大于
    /// </summary>
    GreaterThan = 2,

    /// <summary>
    /// 大于等于
    /// </summary>
    GreaterThanOrEqual = 3,

    /// <summary>
    /// 小于
    /// </summary>
    LessThan = 4,

    /// <summary>
    /// 小于等于
    /// </summary>
    LessThanOrEqual = 5,

    /// <summary>
    /// 模糊匹配
    /// </summary>
    Like = 6,

    /// <summary>
    /// 左模糊
    /// </summary>
    LikeLeft = 7,

    /// <summary>
    /// 右模糊
    /// </summary>
    LikeRight = 8,

    /// <summary>
    /// 区间
    /// </summary>
    Between = 9,

    /// <summary>
    /// 在其中
    /// </summary>
    In = 10,

    /// <summary>
    /// 不在其中
    /// </summary>
    NotIn = 11,
}

/// <summary>
/// 列的表单控件
/// </summary>
public enum CodeGenHtmlType
{
    /// <summary>
    /// 文本框
    /// </summary>
    Input = 0,

    /// <summary>
    /// 文本域
    /// </summary>
    Textarea = 1,

    /// <summary>
    /// 下拉框
    /// </summary>
    Select = 2,

    /// <summary>
    /// 单选框
    /// </summary>
    Radio = 3,

    /// <summary>
    /// 复选框
    /// </summary>
    Checkbox = 4,

    /// <summary>
    /// 日期
    /// </summary>
    DatePicker = 5,

    /// <summary>
    /// 日期时间
    /// </summary>
    DateTimePicker = 6,

    /// <summary>
    /// 时间
    /// </summary>
    TimePicker = 7,

    /// <summary>
    /// 图片上传
    /// </summary>
    ImageUpload = 8,

    /// <summary>
    /// 文件上传
    /// </summary>
    FileUpload = 9,

    /// <summary>
    /// 富文本
    /// </summary>
    Editor = 10,

    /// <summary>
    /// 数字输入框
    /// </summary>
    InputNumber = 11,

    /// <summary>
    /// 开关
    /// </summary>
    Switch = 12,

    /// <summary>
    /// 树形选择
    /// </summary>
    TreeSelect = 13,
}

/// <summary>
/// 列的选项来源
/// </summary>
public enum CodeGenDictSelectorType
{
    /// <summary>
    /// 系统字典（字典码）
    /// </summary>
    DictSelector = 0,

    /// <summary>
    /// 枚举类型（枚举全名）
    /// </summary>
    EnumSelector = 1,

    /// <summary>
    /// 常量数组（内联 JSON）
    /// </summary>
    ConstSelector = 2,

    /// <summary>
    /// 关联表下拉（外键指向另一张表配置）
    /// </summary>
    TableSelector = 3,

    /// <summary>
    /// 关联树选择（外键指向另一张树表配置）
    /// </summary>
    TreeSelector = 4,
}
