#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysMenu
// Guid:5c28152c-d6e9-4396-addb-b479254bad0f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 02:40:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统菜单实体
/// 菜单是一种特殊的资源类型，用于前端界面渲染
/// 与 SysResource 是一对一关系，SysResource 提供权限控制，SysMenu 提供界面配置
/// </summary>
[SugarTable("Sys_Menu", "系统菜单表")]
[SugarIndex("IX_SysMenu_MenuCode", nameof(MenuCode), OrderByType.Asc, true)]
[SugarIndex("IX_SysMenu_ParentId", nameof(ParentId), OrderByType.Asc)]
[SugarIndex("IX_SysMenu_ResourceId", nameof(ResourceId), OrderByType.Asc)]
[SugarIndex("IX_SysMenu_TenantId", nameof(TenantId), OrderByType.Asc)]
[SugarIndex("IX_SysMenu_Status", nameof(Status), OrderByType.Asc)]
[SugarIndex("IX_SysMenu_MenuType", nameof(MenuType), OrderByType.Asc)]
[SugarIndex("IX_SysMenu_IsVisible", nameof(IsVisible), OrderByType.Asc)]
public partial class SysMenu : RbacAggregateRoot<long>
{
    /// <summary>
    /// 租户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "租户ID", IsNullable = true)]
    public virtual long? TenantId { get; set; }

    /// <summary>
    /// 关联资源ID（每个菜单对应一个资源，可为空表示纯展示菜单）
    /// </summary>
    [SugarColumn(ColumnDescription = "关联资源ID", IsNullable = true)]
    public virtual long? ResourceId { get; set; }

    /// <summary>
    /// 父级菜单ID
    /// </summary>
    [SugarColumn(ColumnDescription = "父级菜单ID", IsNullable = true)]
    public virtual long? ParentId { get; set; }

    /// <summary>
    /// 菜单名称
    /// </summary>
    [SugarColumn(ColumnDescription = "菜单名称", Length = 100, IsNullable = false)]
    public virtual string MenuName { get; set; } = string.Empty;

    /// <summary>
    /// 菜单编码（唯一标识）
    /// </summary>
    [SugarColumn(ColumnDescription = "菜单编码", Length = 100, IsNullable = false)]
    public virtual string MenuCode { get; set; } = string.Empty;

    /// <summary>
    /// 菜单类型
    /// </summary>
    [SugarColumn(ColumnDescription = "菜单类型")]
    public virtual MenuType MenuType { get; set; } = MenuType.Directory;

    /// <summary>
    /// 路由地址（前端路由路径）
    /// </summary>
    [SugarColumn(ColumnDescription = "路由地址", Length = 200, IsNullable = true)]
    public virtual string? Path { get; set; }

    /// <summary>
    /// 组件路径（前端组件文件路径）
    /// </summary>
    [SugarColumn(ColumnDescription = "组件路径", Length = 200, IsNullable = true)]
    public virtual string? Component { get; set; }

    /// <summary>
    /// 路由名称（用于 Vue Router 的 name 属性）
    /// </summary>
    [SugarColumn(ColumnDescription = "路由名称", Length = 100, IsNullable = true)]
    public virtual string? RouteName { get; set; }

    /// <summary>
    /// 重定向地址
    /// </summary>
    [SugarColumn(ColumnDescription = "重定向地址", Length = 200, IsNullable = true)]
    public virtual string? Redirect { get; set; }

    /// <summary>
    /// 菜单图标
    /// </summary>
    [SugarColumn(ColumnDescription = "菜单图标", Length = 100, IsNullable = true)]
    public virtual string? Icon { get; set; }

    /// <summary>
    /// 菜单标题（用于面包屑、标签页等）
    /// </summary>
    [SugarColumn(ColumnDescription = "菜单标题", Length = 100, IsNullable = true)]
    public virtual string? Title { get; set; }

    /// <summary>
    /// 是否外链
    /// </summary>
    [SugarColumn(ColumnDescription = "是否外链")]
    public virtual bool IsExternal { get; set; } = false;

    /// <summary>
    /// 外链地址（当 IsExternal=true 时使用）
    /// </summary>
    [SugarColumn(ColumnDescription = "外链地址", Length = 500, IsNullable = true)]
    public virtual string? ExternalUrl { get; set; }

    /// <summary>
    /// 是否缓存（Keep-Alive）
    /// </summary>
    [SugarColumn(ColumnDescription = "是否缓存")]
    public virtual bool IsCache { get; set; } = false;

    /// <summary>
    /// 是否显示在菜单中
    /// </summary>
    [SugarColumn(ColumnDescription = "是否显示")]
    public virtual bool IsVisible { get; set; } = true;

    /// <summary>
    /// 是否固定在标签页（不可关闭）
    /// </summary>
    [SugarColumn(ColumnDescription = "是否固定标签")]
    public virtual bool IsAffix { get; set; } = false;

    /// <summary>
    /// 菜单元数据（JSON格式，存储额外配置）
    /// 例如：{"badge": "new", "activeMenu": "/system/user"}
    /// </summary>
    [SugarColumn(ColumnDescription = "菜单元数据", ColumnDataType = "text", IsNullable = true)]
    public virtual string? Metadata { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnDescription = "状态")]
    public virtual YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 排序
    /// </summary>
    [SugarColumn(ColumnDescription = "排序")]
    public virtual int Sort { get; set; } = 0;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
