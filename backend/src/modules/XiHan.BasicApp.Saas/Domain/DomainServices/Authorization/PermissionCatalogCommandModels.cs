// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 权限定义创建命令
/// </summary>
public sealed record PermissionCreateCommand(
    PermissionType PermissionType,
    long? ResourceId,
    long? OperationId,
    string? ModuleCode,
    string PermissionCode,
    string PermissionName,
    string? PermissionDescription,
    string? Tags,
    bool IsRequireAudit,
    int Priority,
    EnableStatus Status,
    int Sort,
    string? Remark);

/// <summary>
/// 权限定义更新命令
/// </summary>
public sealed record PermissionUpdateCommand(
    long BasicId,
    string PermissionName,
    string? PermissionDescription,
    string? Tags,
    bool IsRequireAudit,
    int Priority,
    int Sort,
    string? Remark);

/// <summary>
/// 权限定义状态命令
/// </summary>
public sealed record PermissionStatusCommand(long BasicId, EnableStatus Status, string? Remark);

/// <summary>
/// 资源定义创建命令
/// </summary>
public sealed record ResourceCreateCommand(
    string ResourceCode,
    string ResourceName,
    ResourceType ResourceType,
    string? ResourcePath,
    string? Description,
    string? Metadata,
    ResourceAccessLevel AccessLevel,
    EnableStatus Status,
    int Sort,
    string? Remark);

/// <summary>
/// 资源定义更新命令
/// </summary>
public sealed record ResourceUpdateCommand(
    long BasicId,
    string ResourceName,
    ResourceType ResourceType,
    string? ResourcePath,
    string? Description,
    string? Metadata,
    ResourceAccessLevel AccessLevel,
    int Sort,
    string? Remark);

/// <summary>
/// 资源定义状态命令
/// </summary>
public sealed record ResourceStatusCommand(long BasicId, EnableStatus Status, string? Remark);

/// <summary>
/// 操作定义创建命令
/// </summary>
public sealed record OperationCreateCommand(
    string OperationCode,
    string OperationName,
    OperationTypeCode OperationTypeCode,
    OperationCategory Category,
    HttpMethodType? HttpMethod,
    string? Description,
    string? Icon,
    string? Color,
    bool IsDangerous,
    bool IsRequireAudit,
    EnableStatus Status,
    int Sort,
    string? Remark);

/// <summary>
/// 操作定义更新命令
/// </summary>
public sealed record OperationUpdateCommand(
    long BasicId,
    string OperationName,
    OperationTypeCode OperationTypeCode,
    OperationCategory Category,
    HttpMethodType? HttpMethod,
    string? Description,
    string? Icon,
    string? Color,
    bool IsDangerous,
    bool IsRequireAudit,
    int Sort,
    string? Remark);

/// <summary>
/// 操作定义状态命令
/// </summary>
public sealed record OperationStatusCommand(long BasicId, EnableStatus Status, string? Remark);

/// <summary>
/// 权限定义命令结果
/// </summary>
public sealed record PermissionCatalogCommandResult(long PermissionId);

/// <summary>
/// 资源定义命令结果
/// </summary>
public sealed record ResourceCatalogCommandResult(long ResourceId);

/// <summary>
/// 操作定义命令结果
/// </summary>
public sealed record OperationCatalogCommandResult(long OperationId);
