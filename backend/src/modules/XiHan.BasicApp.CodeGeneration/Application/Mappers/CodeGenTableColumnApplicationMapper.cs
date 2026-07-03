#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CodeGenTableColumnApplicationMapper
// Guid:c0de9e00-0d03-4a00-9000-000000000d03
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.CodeGeneration.Application.Dtos;
using XiHan.BasicApp.CodeGeneration.Domain.DomainServices;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;

namespace XiHan.BasicApp.CodeGeneration.Application.Mappers;

/// <summary>
/// 代码生成列配置应用层映射器（手写静态映射，对齐 Saas 约定）
/// </summary>
public static class CodeGenTableColumnApplicationMapper
{
    /// <summary>
    /// 单列更新 DTO → 更新命令
    /// </summary>
    public static CodeGenTableColumnUpdateCommand ToUpdateCommand(CodeGenTableColumnUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new CodeGenTableColumnUpdateCommand(
            input.BasicId,
            input.CSharpType,
            input.CSharpProperty,
            input.TsType,
            input.IsRequired,
            input.IsList,
            input.IsInsert,
            input.IsEdit,
            input.IsQuery,
            input.QueryType,
            input.HtmlType,
            input.DictSelectorType,
            input.DictCode,
            input.EnumTypeName,
            input.ConstValues,
            input.DefaultValue,
            input.RegexPattern,
            input.ValidationMessage,
            input.Sort,
            input.Status);
    }

    /// <summary>
    /// 批量保存 DTO → 批量保存命令
    /// </summary>
    public static CodeGenTableColumnBatchSaveCommand ToBatchSaveCommand(CodeGenTableColumnBatchSaveDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var columns = input.Columns is null
            ? []
            : input.Columns.Select(ToUpdateCommand).ToList();

        return new CodeGenTableColumnBatchSaveCommand(input.TableId, columns);
    }

    /// <summary>
    /// 实体 → 列表项 DTO
    /// </summary>
    public static CodeGenTableColumnListItemDto ToListItemDto(SysCodeGenTableColumn column)
    {
        ArgumentNullException.ThrowIfNull(column);

        return new CodeGenTableColumnListItemDto
        {
            BasicId = column.BasicId,
            TableId = column.TableId,
            ColumnName = column.ColumnName,
            ColumnComment = column.ColumnComment,
            ColumnType = column.ColumnType,
            CSharpType = column.CSharpType,
            CSharpProperty = column.CSharpProperty,
            TsType = column.TsType,
            ColumnLength = column.ColumnLength,
            DecimalDigits = column.DecimalDigits,
            IsPrimaryKey = column.IsPrimaryKey,
            IsIdentity = column.IsIdentity,
            IsNullable = column.IsNullable,
            IsRequired = column.IsRequired,
            IsList = column.IsList,
            IsInsert = column.IsInsert,
            IsEdit = column.IsEdit,
            IsQuery = column.IsQuery,
            QueryType = column.QueryType,
            HtmlType = column.HtmlType,
            DictSelectorType = column.DictSelectorType,
            DictCode = column.DictCode,
            EnumTypeName = column.EnumTypeName,
            ConstValues = column.ConstValues,
            Sort = column.Sort,
            Status = column.Status
        };
    }
}
