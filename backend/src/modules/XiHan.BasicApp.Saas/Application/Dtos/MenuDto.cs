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
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 菜单 DTO
/// </summary>
public class MenuDto : BasicAppDto
{
    /// <summary>
    /// 权限编码（菜单可见性所需的权限）
    /// </summary>
    public string? PermissionCode { get; set; }

    /// <summary>
    /// 父级菜单ID
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
    /// 路由地址
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// 组件路径
    /// </summary>
    public string? Component { get; set; }

    /// <summary>
    /// 路由名称
    /// </summary>
    public string? RouteName { get; set; }

    /// <summary>
    /// 重定向地址
    /// </summary>
    public string? Redirect { get; set; }

    /// <summary>
    /// 菜单图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 菜单标题
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 是否外链
    /// </summary>
    public bool IsExternal { get; set; }

    /// <summary>
    /// 外链地址
    /// </summary>
    public string? ExternalUrl { get; set; }

    /// <summary>
    /// 是否缓存
    /// </summary>
    public bool IsCache { get; set; }

    /// <summary>
    /// 是否可见
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// 是否固定标签
    /// </summary>
    public bool IsAffix { get; set; }

    /// <summary>
    /// 标签内容
    /// </summary>
    public string? Badge { get; set; }

    /// <summary>
    /// 标签类型
    /// </summary>
    public string? BadgeType { get; set; }

    /// <summary>
    /// 是否仅显示标签圆点
    /// </summary>
    public bool BadgeDot { get; set; }

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
    public string? Remark { get; set; }

    /// <summary>
    /// 子菜单
    /// </summary>
    public List<MenuDto> Children { get; set; } = [];
}

/// <summary>
/// 创建菜单 DTO
/// </summary>
public class MenuCreateDto : BasicAppCDto
{
    /// <summary>
    /// 权限编码（菜单可见性所需的权限）
    /// </summary>
    [StringLength(200, ErrorMessage = "权限编码长度不能超过 200")]
    public string? PermissionCode { get; set; }

    /// <summary>
    /// 父级菜单ID
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
    /// 路由地址
    /// </summary>
    [StringLength(200, ErrorMessage = "路径长度不能超过 200")]
    public string? Path { get; set; }

    /// <summary>
    /// 组件路径
    /// </summary>
    [StringLength(200, ErrorMessage = "组件路径长度不能超过 200")]
    public string? Component { get; set; }

    /// <summary>
    /// 路由名称
    /// </summary>
    [StringLength(100, ErrorMessage = "路由名称长度不能超过 100")]
    public string? RouteName { get; set; }

    /// <summary>
    /// 重定向地址
    /// </summary>
    [StringLength(200, ErrorMessage = "重定向地址长度不能超过 200")]
    public string? Redirect { get; set; }

    /// <summary>
    /// 菜单图标
    /// </summary>
    [StringLength(100, ErrorMessage = "图标长度不能超过 100")]
    public string? Icon { get; set; }

    /// <summary>
    /// 菜单标题
    /// </summary>
    [StringLength(100, ErrorMessage = "标题长度不能超过 100")]
    public string? Title { get; set; }

    /// <summary>
    /// 是否外链
    /// </summary>
    public bool IsExternal { get; set; }

    /// <summary>
    /// 外链地址
    /// </summary>
    [StringLength(500, ErrorMessage = "外链地址长度不能超过 500")]
    public string? ExternalUrl { get; set; }

    /// <summary>
    /// 是否缓存
    /// </summary>
    public bool IsCache { get; set; }

    /// <summary>
    /// 是否可见
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// 是否固定标签
    /// </summary>
    public bool IsAffix { get; set; }

    /// <summary>
    /// 标签内容
    /// </summary>
    [StringLength(50, ErrorMessage = "标签内容长度不能超过 50")]
    public string? Badge { get; set; }

    /// <summary>
    /// 标签类型
    /// </summary>
    [StringLength(20, ErrorMessage = "标签类型长度不能超过 20")]
    public string? BadgeType { get; set; }

    /// <summary>
    /// 是否仅显示标签圆点
    /// </summary>
    public bool BadgeDot { get; set; }

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
    /// 权限编码（菜单可见性所需的权限）
    /// </summary>
    [StringLength(200, ErrorMessage = "权限编码长度不能超过 200")]
    public string? PermissionCode { get; set; }

    /// <summary>
    /// 父级菜单ID
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
    /// 路由地址
    /// </summary>
    [StringLength(200, ErrorMessage = "路径长度不能超过 200")]
    public string? Path { get; set; }

    /// <summary>
    /// 组件路径
    /// </summary>
    [StringLength(200, ErrorMessage = "组件路径长度不能超过 200")]
    public string? Component { get; set; }

    /// <summary>
    /// 路由名称
    /// </summary>
    [StringLength(100, ErrorMessage = "路由名称长度不能超过 100")]
    public string? RouteName { get; set; }

    /// <summary>
    /// 重定向地址
    /// </summary>
    [StringLength(200, ErrorMessage = "重定向地址长度不能超过 200")]
    public string? Redirect { get; set; }

    /// <summary>
    /// 菜单图标
    /// </summary>
    [StringLength(100, ErrorMessage = "图标长度不能超过 100")]
    public string? Icon { get; set; }

    /// <summary>
    /// 菜单标题
    /// </summary>
    [StringLength(100, ErrorMessage = "标题长度不能超过 100")]
    public string? Title { get; set; }

    /// <summary>
    /// 是否外链
    /// </summary>
    public bool IsExternal { get; set; }

    /// <summary>
    /// 外链地址
    /// </summary>
    [StringLength(500, ErrorMessage = "外链地址长度不能超过 500")]
    public string? ExternalUrl { get; set; }

    /// <summary>
    /// 是否缓存
    /// </summary>
    public bool IsCache { get; set; }

    /// <summary>
    /// 是否可见
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// 是否固定标签
    /// </summary>
    public bool IsAffix { get; set; }

    /// <summary>
    /// 标签内容
    /// </summary>
    [StringLength(50, ErrorMessage = "标签内容长度不能超过 50")]
    public string? Badge { get; set; }

    /// <summary>
    /// 标签类型
    /// </summary>
    [StringLength(20, ErrorMessage = "标签类型长度不能超过 20")]
    public string? BadgeType { get; set; }

    /// <summary>
    /// 是否仅显示标签圆点
    /// </summary>
    public bool BadgeDot { get; set; }

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
