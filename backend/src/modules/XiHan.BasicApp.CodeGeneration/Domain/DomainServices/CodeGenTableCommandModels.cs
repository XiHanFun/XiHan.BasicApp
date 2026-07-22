// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
