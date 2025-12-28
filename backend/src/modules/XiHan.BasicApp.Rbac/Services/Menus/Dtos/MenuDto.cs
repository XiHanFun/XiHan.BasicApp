#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MenuDto
// Guid:5a2b3c4d-5e6f-7890-abcd-ef1234567894
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 4:20:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Services.Base.Dtos;

namespace XiHan.BasicApp.Rbac.Services.Menus.Dtos;

/// <summary>
/// 菜单 DTO
/// </summary>
public class MenuDto : RbacFullAuditedDtoBase
{
    /// <summary>
    /// 父级菜单ID
    /// </summary>
    public XiHanBasicAppIdType? ParentId { get; set; }

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
    /// 菜单图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 权限标识
    /// </summary>
    public string? Permission { get; set; }

    /// <summary>
    /// 是否外链
    /// </summary>
    public bool IsExternal { get; set; }

    /// <summary>
    /// 是否缓存
    /// </summary>
    public bool IsCache { get; set; }

    /// <summary>
    /// 是否显示
    /// </summary>
    public bool IsVisible { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 菜单树 DTO
/// </summary>
public class MenuTreeDto : MenuDto
{
    /// <summary>
    /// 子菜单列表
    /// </summary>
    public List<MenuTreeDto> Children { get; set; } = [];
}

/// <summary>
/// 创建菜单 DTO
/// </summary>
public class CreateMenuDto : RbacCreationDtoBase
{
    /// <summary>
    /// 父级菜单ID
    /// </summary>
    public XiHanBasicAppIdType? ParentId { get; set; }

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
    /// 菜单图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 权限标识
    /// </summary>
    public string? Permission { get; set; }

    /// <summary>
    /// 是否外链
    /// </summary>
    public bool IsExternal { get; set; } = false;

    /// <summary>
    /// 是否缓存
    /// </summary>
    public bool IsCache { get; set; } = false;

    /// <summary>
    /// 是否显示
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; } = 0;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 更新菜单 DTO
/// </summary>
public class UpdateMenuDto : RbacUpdateDtoBase
{
    /// <summary>
    /// 父级菜单ID
    /// </summary>
    public XiHanBasicAppIdType? ParentId { get; set; }

    /// <summary>
    /// 菜单名称
    /// </summary>
    public string? MenuName { get; set; }

    /// <summary>
    /// 菜单类型
    /// </summary>
    public MenuType? MenuType { get; set; }

    /// <summary>
    /// 路由地址
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// 组件路径
    /// </summary>
    public string? Component { get; set; }

    /// <summary>
    /// 菜单图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 权限标识
    /// </summary>
    public string? Permission { get; set; }

    /// <summary>
    /// 是否外链
    /// </summary>
    public bool? IsExternal { get; set; }

    /// <summary>
    /// 是否缓存
    /// </summary>
    public bool? IsCache { get; set; }

    /// <summary>
    /// 是否显示
    /// </summary>
    public bool? IsVisible { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo? Status { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int? Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
