#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MenuDto
// Guid:95de1acc-af1d-4d76-b47c-2ea233daafc8
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:43:06
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Rbac.Domain.Enums;

namespace XiHan.BasicApp.Rbac.Application.Dtos;

/// <summary>
/// 菜单 DTO
/// </summary>
public class MenuDto : BasicAppDto
{
    /// <summary>
    /// 资源ID
    /// </summary>
    public long? ResourceId { get; set; }

    /// <summary>
    /// 父级ID
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 菜单名称
    /// </summary>
    public string MenuName { get; set; } = string.Empty;

    /// <summary>
    /// 菜单编码
    /// </summary>
    public string MenuCode { get; set; } = string.Empty;

    /// <summary>
    /// 菜单类型
    /// </summary>
    public MenuType MenuType { get; set; } = MenuType.Directory;

    /// <summary>
    /// 路径
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// 组件
    /// </summary>
    public string? Component { get; set; }

    /// <summary>
    /// 图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 是否可见
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }
}

/// <summary>
/// 创建菜单 DTO
/// </summary>
public class MenuCreateDto : BasicAppCDto
{
    /// <summary>
    /// 资源ID
    /// </summary>
    public long? ResourceId { get; set; }

    /// <summary>
    /// 父级ID
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 菜单名称
    /// </summary>
    [Required(ErrorMessage = "菜单名称不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "菜单名称长度必须在 1～100 之间")]
    public string MenuName { get; set; } = string.Empty;

    /// <summary>
    /// 菜单编码
    /// </summary>
    [Required(ErrorMessage = "菜单编码不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "菜单编码长度必须在 1～100 之间")]
    public string MenuCode { get; set; } = string.Empty;

    /// <summary>
    /// 菜单类型
    /// </summary>
    public MenuType MenuType { get; set; } = MenuType.Directory;

    /// <summary>
    /// 路径
    /// </summary>
    [StringLength(200, ErrorMessage = "路径长度不能超过 200")]
    public string? Path { get; set; }

    /// <summary>
    /// 组件
    /// </summary>
    [StringLength(200, ErrorMessage = "组件路径长度不能超过 200")]
    public string? Component { get; set; }

    /// <summary>
    /// 图标
    /// </summary>
    [StringLength(100, ErrorMessage = "图标长度不能超过 100")]
    public string? Icon { get; set; }

    /// <summary>
    /// 是否可见
    /// </summary>
    public bool IsVisible { get; set; } = true;

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
/// 更新菜单 DTO
/// </summary>
public class MenuUpdateDto : BasicAppUDto
{
    /// <summary>
    /// 资源ID
    /// </summary>
    public long? ResourceId { get; set; }

    /// <summary>
    /// 父级ID
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 菜单名称
    /// </summary>
    [Required(ErrorMessage = "菜单名称不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "菜单名称长度必须在 1～100 之间")]
    public string MenuName { get; set; } = string.Empty;

    /// <summary>
    /// 菜单编码
    /// </summary>
    [Required(ErrorMessage = "菜单编码不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "菜单编码长度必须在 1～100 之间")]
    public string MenuCode { get; set; } = string.Empty;

    /// <summary>
    /// 菜单类型
    /// </summary>
    public MenuType MenuType { get; set; } = MenuType.Directory;

    /// <summary>
    /// 路径
    /// </summary>
    [StringLength(200, ErrorMessage = "路径长度不能超过 200")]
    public string? Path { get; set; }

    /// <summary>
    /// 组件
    /// </summary>
    [StringLength(200, ErrorMessage = "组件路径长度不能超过 200")]
    public string? Component { get; set; }

    /// <summary>
    /// 图标
    /// </summary>
    [StringLength(100, ErrorMessage = "图标长度不能超过 100")]
    public string? Icon { get; set; }

    /// <summary>
    /// 是否可见
    /// </summary>
    public bool IsVisible { get; set; } = true;

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
