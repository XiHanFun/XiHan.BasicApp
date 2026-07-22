// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 权限定义创建 DTO
/// </summary>
public sealed class PermissionCreateDto
{
    /// <summary>
    /// 权限类型
    /// </summary>
    public PermissionType PermissionType { get; set; } = PermissionType.ResourceBased;

    /// <summary>
    /// 资源主键
    /// </summary>
    public long? ResourceId { get; set; }

    /// <summary>
    /// 操作主键
    /// </summary>
    public long? OperationId { get; set; }

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
    /// 权限描述
    /// </summary>
    public string? PermissionDescription { get; set; }

    /// <summary>
    /// 权限标签
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 是否需要审计
    /// </summary>
    public bool IsRequireAudit { get; set; }

    /// <summary>
    /// 优先级
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus Status { get; set; } = EnableStatus.Enabled;

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
