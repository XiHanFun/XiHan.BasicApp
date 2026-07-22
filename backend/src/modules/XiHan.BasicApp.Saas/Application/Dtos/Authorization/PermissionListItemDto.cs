// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 权限列表项 DTO
/// </summary>
public sealed class PermissionListItemDto : BasicAppDto
{
    /// <summary>
    /// 权限类型
    /// </summary>
    public PermissionType PermissionType { get; set; }

    /// <summary>
    /// 资源主键
    /// </summary>
    public long? ResourceId { get; set; }

    /// <summary>
    /// 资源编码
    /// </summary>
    public string? ResourceCode { get; set; }

    /// <summary>
    /// 资源名称
    /// </summary>
    public string? ResourceName { get; set; }

    /// <summary>
    /// 操作主键
    /// </summary>
    public long? OperationId { get; set; }

    /// <summary>
    /// 操作编码
    /// </summary>
    public string? OperationCode { get; set; }

    /// <summary>
    /// 操作名称
    /// </summary>
    public string? OperationName { get; set; }

    /// <summary>
    /// 模块编码
    /// </summary>
    public string? ModuleCode { get; set; }

    /// <summary>
    /// 权限编码
    /// </summary>
    public string PermissionCode { get; set; } = string.Empty;

    /// <summary>
    /// 权限名称
    /// </summary>
    public string PermissionName { get; set; } = string.Empty;

    /// <summary>
    /// 功能分组码（资源段，权限分配 UI 据此归组）
    /// </summary>
    public string GroupCode { get; set; } = string.Empty;

    /// <summary>
    /// 功能分组显示名
    /// </summary>
    public string GroupName { get; set; } = string.Empty;

    /// <summary>
    /// 权限描述
    /// </summary>
    public string? PermissionDescription { get; set; }

    /// <summary>
    /// 是否需要审计
    /// </summary>
    public bool IsRequireAudit { get; set; }

    /// <summary>
    /// 是否全局权限
    /// </summary>
    public bool IsGlobal { get; set; }

    /// <summary>
    /// 优先级
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus Status { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTimeOffset? ModifiedTime { get; set; }
}
