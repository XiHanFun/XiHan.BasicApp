#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CodeGenTableApplicationMapper
// Guid:c0de9e00-0602-4a00-9000-000000000d05
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
/// 代码生成表配置应用层映射器
/// </summary>
public static class CodeGenTableApplicationMapper
{
    /// <summary>
    /// 映射表配置更新命令
    /// </summary>
    public static CodeGenTableUpdateCommand ToUpdateCommand(CodeGenTableUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new CodeGenTableUpdateCommand(
            input.BasicId,
            input.TableName,
            input.TableComment,
            input.ClassName,
            input.Namespace,
            input.ModuleName,
            input.BusinessName,
            input.FunctionName,
            input.Author,
            input.TemplateType,
            input.GenType,
            input.GenPath,
            input.ParentMenuId,
            input.PrimaryKeyColumn,
            input.TreeParentColumn,
            input.TreeNameColumn,
            input.MasterTableId,
            input.MasterForeignKey,
            input.DatabaseType,
            input.DbConnectionName,
            input.Options,
            input.Status,
            input.Remark);
    }

    /// <summary>
    /// 映射表配置状态变更命令
    /// </summary>
    public static CodeGenTableStatusChangeCommand ToStatusCommand(CodeGenTableStatusUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new CodeGenTableStatusChangeCommand(input.BasicId, input.Status, input.Remark);
    }

    /// <summary>
    /// 映射表配置列表项
    /// </summary>
    public static CodeGenTableListItemDto ToListItemDto(SysCodeGenTable table)
    {
        ArgumentNullException.ThrowIfNull(table);

        return new CodeGenTableListItemDto
        {
            BasicId = table.BasicId,
            TableName = table.TableName,
            TableComment = table.TableComment,
            ClassName = table.ClassName,
            ModuleName = table.ModuleName,
            BusinessName = table.BusinessName,
            FunctionName = table.FunctionName,
            TemplateType = table.TemplateType,
            GenStatus = table.GenStatus,
            LastGenTime = table.LastGenTime,
            Status = table.Status,
            CreatedTime = table.CreatedTime,
            ModifiedTime = table.ModifiedTime
        };
    }

    /// <summary>
    /// 映射表配置详情（不含列；列由查询服务单独填充）
    /// </summary>
    public static CodeGenTableDetailDto ToDetailDto(SysCodeGenTable table)
    {
        ArgumentNullException.ThrowIfNull(table);

        var item = ToListItemDto(table);
        return new CodeGenTableDetailDto
        {
            BasicId = item.BasicId,
            TableName = item.TableName,
            TableComment = item.TableComment,
            ClassName = item.ClassName,
            ModuleName = item.ModuleName,
            BusinessName = item.BusinessName,
            FunctionName = item.FunctionName,
            TemplateType = item.TemplateType,
            GenStatus = item.GenStatus,
            LastGenTime = item.LastGenTime,
            Status = item.Status,
            CreatedTime = item.CreatedTime,
            ModifiedTime = item.ModifiedTime,
            Namespace = table.Namespace,
            Author = table.Author,
            GenType = table.GenType,
            GenPath = table.GenPath,
            ParentMenuId = table.ParentMenuId,
            PrimaryKeyColumn = table.PrimaryKeyColumn,
            TreeParentColumn = table.TreeParentColumn,
            TreeNameColumn = table.TreeNameColumn,
            MasterTableId = table.MasterTableId,
            MasterForeignKey = table.MasterForeignKey,
            DatabaseType = table.DatabaseType,
            DbConnectionName = table.DbConnectionName,
            Options = table.Options,
            Remark = table.Remark,
            CreatedId = table.CreatedId,
            CreatedBy = table.CreatedBy,
            ModifiedId = table.ModifiedId,
            ModifiedBy = table.ModifiedBy
        };
    }

    /// <summary>
    /// 映射表详情（含列配置）
    /// </summary>
    public static CodeGenTableDetailDto ToDetailDto(SysCodeGenTable table, IReadOnlyList<SysCodeGenTableColumn> columns)
    {
        ArgumentNullException.ThrowIfNull(columns);

        var detail = ToDetailDto(table);
        detail.Columns = columns.Select(ToColumnListItemDto).ToList();
        return detail;
    }

    /// <summary>
    /// 映射列配置列表项（本切片自带，不依赖列切片 Mapper）
    /// </summary>
    public static CodeGenTableColumnListItemDto ToColumnListItemDto(SysCodeGenTableColumn column)
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
            DictType = column.DictType,
            Sort = column.Sort,
            Status = column.Status
        };
    }
}
