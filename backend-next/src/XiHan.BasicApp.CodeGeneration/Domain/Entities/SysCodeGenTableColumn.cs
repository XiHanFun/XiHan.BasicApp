// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.CodeGeneration.Domain.Entities;

/// <summary>
/// 代码生成的列配置。
/// </summary>
/// <remarks>
/// 平台库实体、只在平台一侧。同一张表的列名唯一（4.x 只在服务里保证，6.0 加部分唯一索引，方案 C2）；
/// 导入与同步结构时按库结构推断类型、控件与查询方式，人工改过的字段在同步时保留（照 4.x）。
/// </remarks>
[ActiveUniqueIndex(nameof(TableId), nameof(ColumnName))]
public sealed class SysCodeGenTableColumn : AppAuditedEntity
{
    /// <summary>
    /// 表配置 ID。
    /// </summary>
    public Guid TableId { get; set; }

    /// <summary>
    /// 库里的列名。
    /// </summary>
    [StringLength(200)]
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// 列注释。
    /// </summary>
    [StringLength(500)]
    public string? ColumnComment { get; set; }

    /// <summary>
    /// 库里的列类型名。
    /// </summary>
    [StringLength(100)]
    public string? ColumnType { get; set; }

    /// <summary>
    /// C# 类型。
    /// </summary>
    [StringLength(100)]
    public string? CSharpType { get; set; }

    /// <summary>
    /// C# 属性名。
    /// </summary>
    [StringLength(200)]
    public string? CSharpProperty { get; set; }

    /// <summary>
    /// TypeScript 类型。
    /// </summary>
    [StringLength(100)]
    public string? TsType { get; set; }

    /// <summary>
    /// 长度。
    /// </summary>
    /// <remarks>
    /// 只有字符与二进制类型有（框架读库结构的口径）。
    /// </remarks>
    public int? ColumnLength { get; set; }

    /// <summary>
    /// 小数位数。
    /// </summary>
    public int? DecimalDigits { get; set; }

    /// <summary>
    /// 是否主键。
    /// </summary>
    public bool IsPrimaryKey { get; set; }

    /// <summary>
    /// 是否自增。
    /// </summary>
    public bool IsIdentity { get; set; }

    /// <summary>
    /// 是否可空。
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// 是否必填。
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// 是否唯一。
    /// </summary>
    public bool IsUnique { get; set; }

    /// <summary>
    /// 是否在列表里显示。
    /// </summary>
    public bool IsList { get; set; } = true;

    /// <summary>
    /// 是否进新建表单。
    /// </summary>
    public bool IsInsert { get; set; } = true;

    /// <summary>
    /// 是否进编辑表单。
    /// </summary>
    public bool IsEdit { get; set; } = true;

    /// <summary>
    /// 是否可查询。
    /// </summary>
    public bool IsQuery { get; set; }

    /// <summary>
    /// 查询方式。
    /// </summary>
    public CodeGenQueryType QueryType { get; set; } = CodeGenQueryType.Equal;

    /// <summary>
    /// 表单控件。
    /// </summary>
    public CodeGenHtmlType HtmlType { get; set; } = CodeGenHtmlType.Input;

    /// <summary>
    /// 选项来源。
    /// </summary>
    public CodeGenDictSelectorType? DictSelectorType { get; set; }

    /// <summary>
    /// 字典码。
    /// </summary>
    [StringLength(200)]
    public string? DictCode { get; set; }

    /// <summary>
    /// 枚举类型全名。
    /// </summary>
    [StringLength(500)]
    public string? EnumTypeName { get; set; }

    /// <summary>
    /// 常量选项（JSON）。
    /// </summary>
    [LongText]
    public string? ConstValues { get; set; }

    /// <summary>
    /// 关联表的表配置 ID。
    /// </summary>
    public Guid? RelationTableId { get; set; }

    /// <summary>
    /// 关联表的显示列名。
    /// </summary>
    [StringLength(200)]
    public string? RelationLabelColumn { get; set; }

    /// <summary>
    /// 缺省值。
    /// </summary>
    [StringLength(500)]
    public string? DefaultValue { get; set; }

    /// <summary>
    /// 最小值。
    /// </summary>
    public decimal? MinValue { get; set; }

    /// <summary>
    /// 最大值。
    /// </summary>
    public decimal? MaxValue { get; set; }

    /// <summary>
    /// 校验用的正则。
    /// </summary>
    [StringLength(500)]
    public string? RegexPattern { get; set; }

    /// <summary>
    /// 校验不过时的提示。
    /// </summary>
    [StringLength(500)]
    public string? ValidationMessage { get; set; }

    /// <summary>
    /// 排序号。
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 状态。
    /// </summary>
    public CodeGenStatus Status { get; set; } = CodeGenStatus.Active;

    /// <summary>
    /// 人工改过的字段。
    /// </summary>
    /// <remarks>
    /// 属性名的 JSON 数组：同步结构时不覆盖这些字段（照 4.x）。
    /// </remarks>
    [StringLength(500)]
    public string? UserModifiedFields { get; set; }

    /// <summary>
    /// 备注。
    /// </summary>
    [StringLength(500)]
    public string? Remark { get; set; }
}
