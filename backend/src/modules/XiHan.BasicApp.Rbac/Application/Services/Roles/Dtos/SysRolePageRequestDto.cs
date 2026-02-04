#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysRolePageRequestDto
// Guid:b2c3d4e5-f6a7-8901-2345-67890abcdef2
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/04 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Rbac.Application.Services.Roles.Dtos;

/// <summary>
/// 角色分页查询DTO
/// </summary>
public class SysRolePageRequestDto : PageRequestDtoBase
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 角色编码
    /// </summary>
    public string? RoleCode { get; set; }

    /// <summary>
    /// 角色名称
    /// </summary>
    public string? RoleName { get; set; }

    /// <summary>
    /// 角色描述
    /// </summary>
    public string? RoleDescription { get; set; }

    /// <summary>
    /// 角色类型
    /// </summary>
    public RoleType? RoleType { get; set; }

    /// <summary>
    /// 数据权限范围
    /// </summary>
    public DataPermissionScope? DataScope { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo? Status { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
