#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionDto
// Guid:687bb75c-9125-41d5-be9c-79d29fa2c8bb
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:42:53
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Rbac.Domain.Enums;

namespace XiHan.BasicApp.Rbac.Application.Dtos;

/// <summary>
/// 权限 DTO
/// </summary>
public class PermissionDto : BasicAppDto
{
    /// <summary>
    /// 资源ID
    /// </summary>
    public long ResourceId { get; set; }

    /// <summary>
    /// 操作ID
    /// </summary>
    public long OperationId { get; set; }

    /// <summary>
    /// 权限代码
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
    /// 是否需要审核
    /// </summary>
    public bool IsRequireAudit { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }
}

/// <summary>
/// 创建权限 DTO
/// </summary>
public class PermissionCreateDto : BasicAppCDto
{
    /// <summary>
    /// 资源ID
    /// </summary>
    [Range(1, long.MaxValue, ErrorMessage = "资源 ID 无效")]
    public long ResourceId { get; set; }

    /// <summary>
    /// 操作ID
    /// </summary>
    [Range(1, long.MaxValue, ErrorMessage = "操作 ID 无效")]
    public long OperationId { get; set; }

    /// <summary>
    /// 权限代码
    /// </summary>
    [Required(ErrorMessage = "权限代码不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "权限代码长度必须在 1～200 之间")]
    public string PermissionCode { get; set; } = string.Empty;

    /// <summary>
    /// 权限名称
    /// </summary>
    [Required(ErrorMessage = "权限名称不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "权限名称长度必须在 1～200 之间")]
    public string PermissionName { get; set; } = string.Empty;

    /// <summary>
    /// 权限描述
    /// </summary>
    [StringLength(500, ErrorMessage = "权限描述长度不能超过 500")]
    public string? PermissionDescription { get; set; }

    /// <summary>
    /// 是否需要审计
    /// </summary>
    public bool IsRequireAudit { get; set; }

    /// <summary>
    /// 优先级
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [StringLength(500, ErrorMessage = "备注长度不能超过 500")]
    public string? Remark { get; set; }
}

/// <summary>
/// 更新权限 DTO
/// </summary>
public class PermissionUpdateDto : BasicAppUDto
{
    /// <summary>
    /// 资源ID
    /// </summary>
    [Range(1, long.MaxValue, ErrorMessage = "资源 ID 无效")]
    public long ResourceId { get; set; }

    /// <summary>
    /// 操作ID
    /// </summary>
    [Range(1, long.MaxValue, ErrorMessage = "操作 ID 无效")]
    public long OperationId { get; set; }

    /// <summary>
    /// 权限代码
    /// </summary>
    [Required(ErrorMessage = "权限代码不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "权限代码长度必须在 1～200 之间")]
    public string PermissionCode { get; set; } = string.Empty;

    /// <summary>
    /// 权限名称
    /// </summary>
    [Required(ErrorMessage = "权限名称不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "权限名称长度必须在 1～200 之间")]
    public string PermissionName { get; set; } = string.Empty;

    /// <summary>
    /// 权限描述
    /// </summary>
    [StringLength(500, ErrorMessage = "权限描述长度不能超过 500")]
    public string? PermissionDescription { get; set; }

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
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [StringLength(500, ErrorMessage = "备注长度不能超过 500")]
    public string? Remark { get; set; }
}
