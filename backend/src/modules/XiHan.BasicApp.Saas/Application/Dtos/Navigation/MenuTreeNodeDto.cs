#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MenuTreeNodeDto
// Guid:f9c1738e-f0cb-42e3-a843-3e546bf0e6d8
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 菜单树节点 DTO
/// </summary>
public sealed class MenuTreeNodeDto : BasicAppDto
{
    /// <summary>
    /// 父级菜单主键
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 权限主键
    /// </summary>
    public long? PermissionId { get; set; }

    /// <summary>
    /// 权限编码
    /// </summary>
    public string? PermissionCode { get; set; }

    /// <summary>
    /// 权限名称
    /// </summary>
    public string? PermissionName { get; set; }

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
    public MenuType MenuType { get; set; }

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
    /// 是否显示
    /// </summary>
    public bool IsVisible { get; set; }

    /// <summary>
    /// 是否固定标签
    /// </summary>
    public bool IsAffix { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus Status { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 子菜单
    /// </summary>
    public List<MenuTreeNodeDto> Children { get; set; } = [];
}
