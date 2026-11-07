// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.CodeGeneration.Domain.Entities;

/// <summary>
/// 代码生成的列配置
/// </summary>
/// <remarks>
/// 平台库实体、只在平台一侧。同一张表的列名唯一（4.x 只在服务里保证，6.0 加部分唯一索引，方案 C2）；
/// 导入与同步结构时按库结构推断类型、控件与查询方式，人工改过的字段在同步时保留（照 4.x）
/// </remarks>
[ActiveUniqueIndex(nameof(TableId), nameof(ColumnName))]
public sealed class SysCodeGenTableColumn : AppAuditedEntity
{
    /// <summary>
    /// 表配置 ID
    /// </summary>
    public Guid TableId { get; set; }

    /// <summary>
    /// 库里的列名
    /// </summary>
    [StringLength(200)]
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// 列注释
    /// </summary>
    [StringLength(500)]
    public string? ColumnComment { get; set; }

    /// <summary>
    /// 库里的列类型名
    /// </summary>
    [StringLength(100)]
    public string? ColumnType { get; set; }

    /// <summary>
    /// C# 类型
    /// </summary>
    [StringLength(100)]
    public string? CSharpType { get; set; }

    /// <summary>
    /// C# 属性名
    /// </summary>
    [StringLength(200)]
    public string? CSharpProperty { get; set; }

    /// <summary>
    /// TypeScript 类型
    /// </summary>
    [StringLength(100)]
    public string? TsType { get; set; }

    /// <summary>
    /// 长度
    /// </summary>
    /// <remarks>
    /// 只有字符与二进制类型有（框架读库结构的口径）
    /// </remarks>
    public int? ColumnLength { get; set; }

    /// <summary>
    /// 小数位数
    /// </summary>
    public int? DecimalDigits { get; set; }

    /// <summary>
    /// 是否主键
    /// </summary>
    public bool IsPrimaryKey { get; set; }

    /// <summary>
    /// 是否自增
    /// </summary>
    public bool IsIdentity { get; set; }

    /// <summary>
    /// 是否可空
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// 是否必填
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// 是否唯一
    /// </summary>
    public bool IsUnique { get; set; }

    /// <summary>
    /// 是否在列表里显示
    /// </summary>
    public bool IsList { get; set; } = true;

    /// <summary>
    /// 是否进新建表单
    /// </summary>
    public bool IsInsert { get; set; } = true;

    /// <summary>
    /// 是否进编辑表单
    /// </summary>
    public bool IsEdit { get; set; } = true;

    /// <summary>
    /// 是否可查询
    /// </summary>
    public bool IsQuery { get; set; }

    /// <summary>
    /// 查询方式
    /// </summary>
    public CodeGenQueryType QueryType { get; set; } = CodeGenQueryType.Equal;

    /// <summary>
    /// 表单控件
    /// </summary>
    public CodeGenHtmlType HtmlType { get; set; } = CodeGenHtmlType.Input;

    /// <summary>
    /// 选项来源
    /// </summary>
    public CodeGenDictSelectorType? DictSelectorType { get; set; }

    /// <summary>
    /// 字典码
    /// </summary>
    [StringLength(200)]
    public string? DictCode { get; set; }

    /// <summary>
    /// 枚举类型全名
    /// </summary>
    [StringLength(500)]
    public string? EnumTypeName { get; set; }

    /// <summary>
    /// 常量选项（JSON）
    /// </summary>
    [LongText]
    public string? ConstValues { get; set; }

    /// <summary>
    /// 关联表的表配置 ID
    /// </summary>
    public Guid? RelationTableId { get; set; }

    /// <summary>
    /// 关联表的显示列名
    /// </summary>
    [StringLength(200)]
    public string? RelationLabelColumn { get; set; }

    /// <summary>
    /// 缺省值
    /// </summary>
    [StringLength(500)]
    public string? DefaultValue { get; set; }

    /// <summary>
    /// 最小值
    /// </summary>
    public decimal? MinValue { get; set; }

    /// <summary>
    /// 最大值
    /// </summary>
    public decimal? MaxValue { get; set; }

    /// <summary>
    /// 校验用的正则
    /// </summary>
    [StringLength(500)]
    public string? RegexPattern { get; set; }

    /// <summary>
    /// 校验不过时的提示
    /// </summary>
    [StringLength(500)]
    public string? ValidationMessage { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public CodeGenStatus Status { get; set; } = CodeGenStatus.Active;

    /// <summary>
    /// 人工改过的字段
    /// </summary>
    /// <remarks>
    /// 属性名的 JSON 数组：同步结构时不覆盖这些字段（照 4.x）
    /// </remarks>
    [StringLength(500)]
    public string? UserModifiedFields { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [StringLength(500)]
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
