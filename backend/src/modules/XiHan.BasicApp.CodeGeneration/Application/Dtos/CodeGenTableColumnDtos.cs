// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma warning disable CS1591

using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.CodeGeneration.Application.Dtos;

/// <summary>
/// 代码生成列配置列表项 DTO
/// </summary>
public class CodeGenTableColumnListItemDto : BasicAppDto
{
    public long TableId { get; set; }
    public string ColumnName { get; set; } = string.Empty;
    public string? ColumnComment { get; set; }
    public string? ColumnType { get; set; }
    public string? CSharpType { get; set; }
    public string? CSharpProperty { get; set; }
    public string? TsType { get; set; }
    public int? ColumnLength { get; set; }
    public int? DecimalDigits { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsIdentity { get; set; }
    public bool IsNullable { get; set; }
    public bool IsRequired { get; set; }
    public bool IsList { get; set; }
    public bool IsInsert { get; set; }
    public bool IsEdit { get; set; }
    public bool IsQuery { get; set; }
    public QueryType QueryType { get; set; }
    public HtmlType HtmlType { get; set; }
    public DictSelectorType? DictSelectorType { get; set; }
    public string? DictCode { get; set; }
    public string? EnumTypeName { get; set; }
    public string? ConstValues { get; set; }
    public int Sort { get; set; }
    public EnableStatus Status { get; set; }
}

/// <summary>
/// 代码生成列配置更新 DTO（单列编辑）
/// </summary>
public sealed class CodeGenTableColumnUpdateDto : BasicAppUDto
{
    public string? ColumnComment { get; set; }
    public string? CSharpType { get; set; }
    public string? CSharpProperty { get; set; }
    public string? TsType { get; set; }
    public bool IsRequired { get; set; }
    public bool IsList { get; set; }
    public bool IsInsert { get; set; }
    public bool IsEdit { get; set; }
    public bool IsQuery { get; set; }
    public QueryType QueryType { get; set; } = QueryType.Equal;
    public HtmlType HtmlType { get; set; } = HtmlType.Input;
    public DictSelectorType? DictSelectorType { get; set; }
    public string? DictCode { get; set; }
    public string? EnumTypeName { get; set; }
    public string? ConstValues { get; set; }
    public string? DefaultValue { get; set; }
    public string? RegexPattern { get; set; }
    public string? ValidationMessage { get; set; }
    public int Sort { get; set; }
    public EnableStatus Status { get; set; } = EnableStatus.Enabled;
}

/// <summary>
/// 代码生成列配置批量保存 DTO（按表整体提交列配置）
/// </summary>
public sealed class CodeGenTableColumnBatchSaveDto
{
    public long TableId { get; set; }
    public IReadOnlyList<CodeGenTableColumnUpdateDto> Columns { get; set; } = [];
}
