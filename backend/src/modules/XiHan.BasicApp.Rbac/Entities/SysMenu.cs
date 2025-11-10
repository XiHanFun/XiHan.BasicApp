#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysMenu
// Guid:5c28152c-d6e9-4396-addb-b479254bad0f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 2:40:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities.Base;

using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统菜单实体
/// </summary>
[SugarTable("sys_menu", "系统菜单表")]
[SugarIndex("IX_SysMenu_MenuCode", nameof(MenuCode), OrderByType.Asc, true)]
[SugarIndex("IX_SysMenu_ParentId", nameof(ParentId), OrderByType.Asc)]
public partial class SysMenu : RbacFullAuditedEntity<RbacIdType>
{
    /// <summary>
    /// 父级菜单ID
    /// </summary>
    [SugarColumn(ColumnDescription = "父级菜单ID", IsNullable = true)]
    public virtual RbacIdType? ParentId { get; set; }

    /// <summary>
    /// 菜单名称
    /// </summary>
    [SugarColumn(ColumnDescription = "菜单名称", Length = 100, IsNullable = false)]
    public virtual string MenuName { get; set; } = string.Empty;

    /// <summary>
    /// 菜单编码
    /// </summary>
    [SugarColumn(ColumnDescription = "菜单编码", Length = 100, IsNullable = false)]
    public virtual string MenuCode { get; set; } = string.Empty;

    /// <summary>
    /// 菜单类型
    /// </summary>
    [SugarColumn(ColumnDescription = "菜单类型")]
    public virtual MenuType MenuType { get; set; } = MenuType.Directory;

    /// <summary>
    /// 路由地址
    /// </summary>
    [SugarColumn(ColumnDescription = "路由地址", Length = 200, IsNullable = true)]
    public virtual string? Path { get; set; }

    /// <summary>
    /// 组件路径
    /// </summary>
    [SugarColumn(ColumnDescription = "组件路径", Length = 200, IsNullable = true)]
    public virtual string? Component { get; set; }

    /// <summary>
    /// 菜单图标
    /// </summary>
    [SugarColumn(ColumnDescription = "菜单图标", Length = 100, IsNullable = true)]
    public virtual string? Icon { get; set; }

    /// <summary>
    /// 权限标识
    /// </summary>
    [SugarColumn(ColumnDescription = "权限标识", Length = 100, IsNullable = true)]
    public virtual string? Permission { get; set; }

    /// <summary>
    /// 是否外链
    /// </summary>
    [SugarColumn(ColumnDescription = "是否外链")]
    public virtual bool IsExternal { get; set; } = false;

    /// <summary>
    /// 是否缓存
    /// </summary>
    [SugarColumn(ColumnDescription = "是否缓存")]
    public virtual bool IsCache { get; set; } = false;

    /// <summary>
    /// 是否显示
    /// </summary>
    [SugarColumn(ColumnDescription = "是否显示")]
    public virtual bool IsVisible { get; set; } = true;

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
