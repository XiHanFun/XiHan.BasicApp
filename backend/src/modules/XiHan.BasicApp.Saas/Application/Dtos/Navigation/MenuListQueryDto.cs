// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 菜单列表查询 DTO（不分页，返回全部匹配项）
/// </summary>
public sealed class MenuListQueryDto
{
    /// <summary>
    /// 关键字（菜单编码、名称、路由、组件、标题、备注）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 父级菜单主键
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 权限主键
    /// </summary>
    public long? PermissionId { get; set; }

    /// <summary>
    /// 菜单类型
    /// </summary>
    public MenuType? MenuType { get; set; }

    /// <summary>
    /// 是否外链
    /// </summary>
    public bool? IsExternal { get; set; }

    /// <summary>
    /// 是否显示
    /// </summary>
    public bool? IsVisible { get; set; }

    /// <summary>
    /// 是否全局菜单
    /// </summary>
    public bool? IsGlobal { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus? Status { get; set; }
}
