#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysMenuPageRequestDto
// Guid:d4e5f6a7-b8c9-0123-4567-890abcdef124
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/04 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Rbac.Application.Services.Menus.Dtos;

/// <summary>
/// 菜单分页查询DTO
/// </summary>
public class SysMenuPageRequestDto : PageRequestDtoBase
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 关联资源ID
    /// </summary>
    public long? ResourceId { get; set; }

    /// <summary>
    /// 父级菜单ID
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 菜单名称
    /// </summary>
    public string? MenuName { get; set; }

    /// <summary>
    /// 菜单编码
    /// </summary>
    public string? MenuCode { get; set; }

    /// <summary>
    /// 菜单类型
    /// </summary>
    public MenuType? MenuType { get; set; }

    /// <summary>
    /// 路由地址
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// 是否外链
    /// </summary>
    public bool? IsExternal { get; set; }

    /// <summary>
    /// 是否显示在菜单中
    /// </summary>
    public bool? IsVisible { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo? Status { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
