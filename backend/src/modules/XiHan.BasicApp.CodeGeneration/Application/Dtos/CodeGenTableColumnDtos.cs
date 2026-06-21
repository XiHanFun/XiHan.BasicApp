#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CodeGenTableColumnDtos
// Guid:c0de9e00-0403-4a00-9000-000000000403
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
    public string? DictType { get; set; }
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
    public string? DictType { get; set; }
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
