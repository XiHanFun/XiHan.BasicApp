#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CodeGenTableCommandModels
// Guid:c0de9e00-0602-4a00-9000-000000000d03
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
/// 代码生成表配置更新命令
/// </summary>
public sealed record CodeGenTableUpdateCommand(
    long BasicId,
    string TableName,
    string? TableComment,
    string ClassName,
    string? Namespace,
    string? ModuleName,
    string? BusinessName,
    string? FunctionName,
    string? Author,
    TemplateType TemplateType,
    GenType GenType,
    GenerationScope GenerationScope,
    string? EnabledActions,
    string? GenPath,
    long? ParentMenuId,
    string? PrimaryKeyColumn,
    string? TreeParentColumn,
    string? TreeNameColumn,
    long? MasterTableId,
    string? MasterForeignKey,
    DatabaseType DatabaseType,
    long? DataSourceId,
    string? Options,
    EnableStatus Status,
    string? Remark);

/// <summary>
/// 代码生成表配置状态变更命令
/// </summary>
public sealed record CodeGenTableStatusChangeCommand(long BasicId, EnableStatus Status, string? Remark);

/// <summary>
/// 代码生成表配置命令结果（包装实体）
/// </summary>
public sealed record CodeGenTableCommandResult(SysCodeGenTable Table);
