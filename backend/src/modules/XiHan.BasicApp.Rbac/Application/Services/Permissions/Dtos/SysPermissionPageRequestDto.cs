#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysPermissionPageRequestDto
// Guid:c3d4e5f6-a7b8-9012-3456-7890abcdef13
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/04 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Rbac.Application.Services.Permissions.Dtos;

/// <summary>
/// 权限分页查询DTO
/// </summary>
public class SysPermissionPageRequestDto : PageRequestDtoBase
{
    /// <summary>
    /// 资源ID
    /// </summary>
    public long? ResourceId { get; set; }

    /// <summary>
    /// 操作ID
    /// </summary>
    public long? OperationId { get; set; }

    /// <summary>
    /// 权限编码
    /// </summary>
    public string? PermissionCode { get; set; }

    /// <summary>
    /// 权限名称
    /// </summary>
    public string? PermissionName { get; set; }

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
    public bool? IsRequireAudit { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo? Status { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
