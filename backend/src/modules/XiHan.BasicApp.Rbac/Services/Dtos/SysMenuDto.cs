#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysMenuDto
// Guid:c3d4e5f6-a7b8-9012-3456-7890cdef0123
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/8 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Services.Dtos;

/// <summary>
/// 系统菜单创建 DTO
/// </summary>
public class SysMenuCreateDto : RbacCreationDtoBase
{
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
    public bool IsExternal { get; set; } = false;

    /// <summary>
    /// 外链地址
    /// </summary>
    public string? ExternalUrl { get; set; }

    /// <summary>
    /// 是否缓存
    /// </summary>
    public bool IsCache { get; set; } = false;

    /// <summary>
    /// 是否显示在菜单中
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// 是否固定在标签页
    /// </summary>
    public bool IsAffix { get; set; } = false;

    /// <summary>
    /// 菜单元数据
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

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
/// 系统菜单更新 DTO
/// </summary>
public class SysMenuUpdateDto : RbacUpdateDtoBase
{
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
    public bool IsExternal { get; set; } = false;

    /// <summary>
    /// 外链地址
    /// </summary>
    public string? ExternalUrl { get; set; }

    /// <summary>
    /// 是否缓存
    /// </summary>
    public bool IsCache { get; set; } = false;

    /// <summary>
    /// 是否显示在菜单中
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// 是否固定在标签页
    /// </summary>
    public bool IsAffix { get; set; } = false;

    /// <summary>
    /// 菜单元数据
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

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
/// 系统菜单查询 DTO
/// </summary>
public class SysMenuGetDto : RbacFullAuditedDtoBase
{
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
    public bool IsExternal { get; set; } = false;

    /// <summary>
    /// 外链地址
    /// </summary>
    public string? ExternalUrl { get; set; }

    /// <summary>
    /// 是否缓存
    /// </summary>
    public bool IsCache { get; set; } = false;

    /// <summary>
    /// 是否显示在菜单中
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// 是否固定在标签页
    /// </summary>
    public bool IsAffix { get; set; } = false;

    /// <summary>
    /// 菜单元数据
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; } = 0;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
