#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CodeGenTableColumnCommandModels
// Guid:c0de9e00-0b03-4a00-9000-000000000b03
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.CodeGeneration.Domain.DomainServices;

/// <summary>
/// 列配置单列更新命令（可变字段覆盖；BasicId 用于定位行）
/// </summary>
public sealed record CodeGenTableColumnUpdateCommand(
    long BasicId,
    string? CSharpType,
    string? CSharpProperty,
    string? TsType,
    bool IsRequired,
    bool IsList,
    bool IsInsert,
    bool IsEdit,
    bool IsQuery,
    QueryType QueryType,
    HtmlType HtmlType,
    DictSelectorType? DictSelectorType,
    string? DictCode,
    string? EnumTypeName,
    string? ConstValues,
    string? DefaultValue,
    string? RegexPattern,
    string? ValidationMessage,
    int Sort,
    EnableStatus Status);

/// <summary>
/// 列配置批量保存命令（按表整体提交各列）
/// </summary>
public sealed record CodeGenTableColumnBatchSaveCommand(
    long TableId,
    IReadOnlyList<CodeGenTableColumnUpdateCommand> Columns);

/// <summary>
/// 列配置单列命令结果（包裹实体）
/// </summary>
public sealed record CodeGenTableColumnCommandResult(SysCodeGenTableColumn Column);

/// <summary>
/// 列配置批量保存命令结果（包裹更新后的实体集合）
/// </summary>
public sealed record CodeGenTableColumnBatchSaveResult(IReadOnlyList<SysCodeGenTableColumn> Columns);
