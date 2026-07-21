#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CodeGenTableDtos
// Guid:c0de9e00-0402-4a00-9000-000000000402
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
/// 代码生成表配置更新 DTO（表头信息维护；列配置走列接口）
/// </summary>
public sealed class CodeGenTableUpdateDto : BasicAppUDto
{
    public string TableName { get; set; } = string.Empty;
    public string? TableComment { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string? Namespace { get; set; }
    public string? ModuleName { get; set; }
    public string? BusinessName { get; set; }
    public string? FunctionName { get; set; }
    public string? Author { get; set; }
    public TemplateType TemplateType { get; set; } = TemplateType.Single;
    public GenType GenType { get; set; } = GenType.Zip;
    public string? GenPath { get; set; }
    public long? ParentMenuId { get; set; }
    public string? PrimaryKeyColumn { get; set; }
    public string? TreeParentColumn { get; set; }
    public string? TreeNameColumn { get; set; }
    public long? MasterTableId { get; set; }
    public string? MasterForeignKey { get; set; }
    public DatabaseType DatabaseType { get; set; } = DatabaseType.MySql;
    /// <summary>数据源标识（对应 SysCodeGenDataSource；为空表示主库）</summary>
    public long? DataSourceId { get; set; }
    public string? Options { get; set; }
    public EnableStatus Status { get; set; } = EnableStatus.Enabled;
    public string? Remark { get; set; }
}

/// <summary>
/// 代码生成表配置状态更新 DTO
/// </summary>
public sealed class CodeGenTableStatusUpdateDto : BasicAppDto
{
    public EnableStatus Status { get; set; } = EnableStatus.Enabled;
    public string? Remark { get; set; }
}

/// <summary>
/// 代码生成表配置分页查询 DTO
/// </summary>
public sealed class CodeGenTablePageQueryDto : BasicAppPRDto
{
    public string? Keyword { get; set; }
    public string? ModuleName { get; set; }
    public TemplateType? TemplateType { get; set; }
    public GenStatus? GenStatus { get; set; }
    public EnableStatus? Status { get; set; }
}

/// <summary>
/// 代码生成表配置列表项 DTO
/// </summary>
public class CodeGenTableListItemDto : BasicAppDto
{
    public string TableName { get; set; } = string.Empty;
    public string? TableComment { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string? ModuleName { get; set; }
    public string? BusinessName { get; set; }
    public string? FunctionName { get; set; }
    public TemplateType TemplateType { get; set; }
    public GenStatus GenStatus { get; set; }
    public DateTimeOffset? LastGenTime { get; set; }
    public EnableStatus Status { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset? ModifiedTime { get; set; }
}

/// <summary>
/// 代码生成表配置详情 DTO
/// </summary>
public sealed class CodeGenTableDetailDto : CodeGenTableListItemDto
{
    public string? Namespace { get; set; }
    public string? Author { get; set; }
    public GenType GenType { get; set; }
    public string? GenPath { get; set; }
    public long? ParentMenuId { get; set; }
    public string? PrimaryKeyColumn { get; set; }
    public string? TreeParentColumn { get; set; }
    public string? TreeNameColumn { get; set; }
    public long? MasterTableId { get; set; }
    public string? MasterForeignKey { get; set; }
    public DatabaseType DatabaseType { get; set; }
    /// <summary>数据源标识（对应 SysCodeGenDataSource；为空表示主库）</summary>
    public long? DataSourceId { get; set; }
    public string? Options { get; set; }
    public string? Remark { get; set; }
    public long? CreatedId { get; set; }
    public string? CreatedBy { get; set; }
    public long? ModifiedId { get; set; }
    public string? ModifiedBy { get; set; }
    public IReadOnlyList<CodeGenTableColumnListItemDto> Columns { get; set; } = [];
}
