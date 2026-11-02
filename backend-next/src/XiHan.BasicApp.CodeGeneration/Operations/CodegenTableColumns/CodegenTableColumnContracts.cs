// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Services;

namespace XiHan.BasicApp.CodeGeneration.Operations.CodegenTableColumns;

/// <summary>
/// 列配置。
/// </summary>
/// <param name="Id">主键。</param>
/// <param name="TableId">表配置 ID。</param>
/// <param name="ColumnName">库里的列名。</param>
/// <param name="ColumnComment">列注释。</param>
/// <param name="ColumnType">库里的列类型名。</param>
/// <param name="CSharpType">C# 类型。</param>
/// <param name="CSharpProperty">C# 属性名。</param>
/// <param name="TsType">TypeScript 类型。</param>
/// <param name="ColumnLength">长度（只有字符与二进制类型有）。</param>
/// <param name="DecimalDigits">小数位数。</param>
/// <param name="IsPrimaryKey">是否主键。</param>
/// <param name="IsIdentity">是否自增。</param>
/// <param name="IsNullable">是否可空。</param>
/// <param name="IsBase">是否实体基类的列（主键、租户、审计、软删与并发戳）。</param>
/// <param name="IsRequired">是否必填。</param>
/// <param name="IsUnique">是否唯一。</param>
/// <param name="IsList">是否在列表里显示。</param>
/// <param name="IsInsert">是否进新建表单。</param>
/// <param name="IsEdit">是否进编辑表单。</param>
/// <param name="IsQuery">是否可查询。</param>
/// <param name="QueryType">查询方式。</param>
/// <param name="HtmlType">表单控件。</param>
/// <param name="DictSelectorType">选项来源。</param>
/// <param name="DictCode">字典码。</param>
/// <param name="EnumTypeName">枚举类型全名。</param>
/// <param name="ConstValues">常量选项（JSON）。</param>
/// <param name="RelationTableId">关联表的表配置 ID。</param>
/// <param name="RelationLabelColumn">关联表的显示列名。</param>
/// <param name="DefaultValue">缺省值。</param>
/// <param name="MinValue">最小值。</param>
/// <param name="MaxValue">最大值。</param>
/// <param name="RegexPattern">校验用的正则。</param>
/// <param name="ValidationMessage">校验不过时的提示。</param>
/// <param name="Order">排序号。</param>
/// <param name="Status">状态。</param>
/// <param name="ConcurrencyStamp">并发戳：保存时原样交回。</param>
public sealed record CodegenTableColumnResponse(
    Guid Id,
    Guid TableId,
    string ColumnName,
    string? ColumnComment,
    string? ColumnType,
    string? CSharpType,
    string? CSharpProperty,
    string? TsType,
    int? ColumnLength,
    int? DecimalDigits,
    bool IsPrimaryKey,
    bool IsIdentity,
    bool IsNullable,
    bool IsBase,
    bool IsRequired,
    bool IsUnique,
    bool IsList,
    bool IsInsert,
    bool IsEdit,
    bool IsQuery,
    CodeGenQueryType QueryType,
    CodeGenHtmlType HtmlType,
    CodeGenDictSelectorType? DictSelectorType,
    string? DictCode,
    string? EnumTypeName,
    string? ConstValues,
    Guid? RelationTableId,
    string? RelationLabelColumn,
    string? DefaultValue,
    decimal? MinValue,
    decimal? MaxValue,
    string? RegexPattern,
    string? ValidationMessage,
    int Order,
    CodeGenStatus Status,
    string ConcurrencyStamp)
{
    /// <summary>
    /// 从实体投影。
    /// </summary>
    /// <param name="column">列配置。</param>
    public static CodegenTableColumnResponse From(SysCodeGenTableColumn column)
    {
        ArgumentNullException.ThrowIfNull(column);
        return new CodegenTableColumnResponse(
            column.Id,
            column.TableId,
            column.ColumnName,
            column.ColumnComment,
            column.ColumnType,
            column.CSharpType,
            column.CSharpProperty,
            column.TsType,
            column.ColumnLength,
            column.DecimalDigits,
            column.IsPrimaryKey,
            column.IsIdentity,
            column.IsNullable,
            CodeGenInference.IsBaseColumn(column.ColumnName),
            column.IsRequired,
            column.IsUnique,
            column.IsList,
            column.IsInsert,
            column.IsEdit,
            column.IsQuery,
            column.QueryType,
            column.HtmlType,
            column.DictSelectorType,
            column.DictCode,
            column.EnumTypeName,
            column.ConstValues,
            column.RelationTableId,
            column.RelationLabelColumn,
            column.DefaultValue,
            column.MinValue,
            column.MaxValue,
            column.RegexPattern,
            column.ValidationMessage,
            column.Order,
            column.Status,
            column.ConcurrencyStamp);
    }
}

/// <summary>
/// 一张表的列配置。
/// </summary>
/// <param name="Items">列配置，按排序号。</param>
public sealed record CodegenTableColumnsResponse(IReadOnlyList<CodegenTableColumnResponse> Items);

/// <summary>
/// <c>codegen-table-columns.list</c> 的请求。
/// </summary>
/// <param name="TableId">表配置 ID。</param>
public sealed record ListCodegenTableColumnsRequest(Guid TableId);

/// <summary>
/// 保存一列的人工配置。
/// </summary>
/// <remarks>
/// 结构字段（列名、类型、主键、可空、长度）以库为准，不在这里改。选项来源只留当前来源要的那个字段（照 4.x）。
/// </remarks>
/// <param name="Id">列配置 ID。</param>
/// <param name="ConcurrencyStamp">读到的并发戳。</param>
/// <param name="CSharpType">C# 类型。</param>
/// <param name="CSharpProperty">C# 属性名。</param>
/// <param name="TsType">TypeScript 类型。</param>
/// <param name="IsRequired">是否必填。</param>
/// <param name="IsUnique">是否唯一。</param>
/// <param name="IsList">是否在列表里显示。</param>
/// <param name="IsInsert">是否进新建表单。</param>
/// <param name="IsEdit">是否进编辑表单。</param>
/// <param name="IsQuery">是否可查询。</param>
/// <param name="QueryType">查询方式。</param>
/// <param name="HtmlType">表单控件。</param>
/// <param name="DictSelectorType">选项来源。</param>
/// <param name="DictCode">字典码（系统字典时必填）。</param>
/// <param name="EnumTypeName">枚举类型全名（枚举时必填）。</param>
/// <param name="ConstValues">常量选项 JSON（常量数组时必填）。</param>
/// <param name="RelationTableId">关联表的表配置 ID（关联表或关联树时必填）。</param>
/// <param name="RelationLabelColumn">关联表的显示列名（关联表时必填，关联树缺省取它的名称列）。</param>
/// <param name="DefaultValue">缺省值。</param>
/// <param name="MinValue">最小值。</param>
/// <param name="MaxValue">最大值。</param>
/// <param name="RegexPattern">校验用的正则。</param>
/// <param name="ValidationMessage">校验不过时的提示。</param>
/// <param name="Order">排序号。</param>
/// <param name="Status">状态。</param>
public sealed record CodegenTableColumnInput(
    Guid Id,
    [Required, StringLength(64, MinimumLength = 1)] string ConcurrencyStamp,
    [StringLength(100)] string? CSharpType,
    [StringLength(200)] string? CSharpProperty,
    [StringLength(100)] string? TsType,
    bool IsRequired,
    bool IsUnique,
    bool IsList,
    bool IsInsert,
    bool IsEdit,
    bool IsQuery,
    CodeGenQueryType QueryType,
    CodeGenHtmlType HtmlType,
    CodeGenDictSelectorType? DictSelectorType,
    [StringLength(200)] string? DictCode,
    [StringLength(500)] string? EnumTypeName,
    string? ConstValues,
    Guid? RelationTableId,
    [StringLength(200)] string? RelationLabelColumn,
    [StringLength(500)] string? DefaultValue,
    decimal? MinValue,
    decimal? MaxValue,
    [StringLength(500)] string? RegexPattern,
    [StringLength(500)] string? ValidationMessage,
    [Range(0, int.MaxValue)] int Order,
    CodeGenStatus Status);

/// <summary>
/// <c>codegen-table-columns.save</c> 的请求。
/// </summary>
/// <param name="TableId">表配置 ID。</param>
/// <param name="Columns">要保存的列，都属于这张表。</param>
public sealed record SaveCodegenTableColumnsRequest(Guid TableId, [Required, MinLength(1), MaxLength(500)] IReadOnlyList<CodegenTableColumnInput> Columns);
