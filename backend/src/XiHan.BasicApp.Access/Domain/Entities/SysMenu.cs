// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Access.Domain.Entities;

/// <summary>
/// 菜单（平台库目录实体，两侧共用一棵树，按权限裁剪）
/// </summary>
/// <remarks>
/// 页面节点由各包的页面登记派生：<see cref="Route"/>、<see cref="Component"/>、<see cref="TitleKey"/>、<see cref="Permission"/> 归登记，
/// 种子每次对齐；展示字段（标题覆盖、图标、排序、所属目录、可见、缓存、固定标签、启停、徽标）运营可改，改过即 <see cref="IsCustomized"/>，种子不再覆盖，可经 <c>menus.reset</c> 恢复登记的值
/// 目录与外链节点由运营新建（编码 <c>custom.&lt;名&gt;</c>），种子不碰；内置的几个（见 <see cref="BuiltInMenus"/>）由种子只首写一次，此后与运营建的一样
/// </remarks>
[SugarTable(TableName = "sys_menu", TableDescription = "菜单（平台库目录实体，两侧共用一棵树，按权限裁剪）")]
[ActiveUniqueIndex(nameof(Code), Name = "ux_sys_menu_code")]
public sealed class SysMenu : AppAuditedEntity
{
    /// <summary>
    /// 节点编码，唯一
    /// </summary>
    /// <remarks>
    /// 页面节点是页面码（<c>&lt;包&gt;.&lt;页面&gt;</c>），运营节点是 <c>custom.&lt;名&gt;</c>
    /// </remarks>
    [SugarColumn(ColumnName = "code", Length = 512, IsNullable = false, ColumnDescription = "节点编码，唯一")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 种类
    /// </summary>
    [SugarColumn(ColumnName = "kind", IsNullable = false, ColumnDescription = "种类")]
    public MenuKind Kind { get; set; } = MenuKind.Page;

    /// <summary>
    /// 所属包
    /// </summary>
    /// <remarks>
    /// 运营节点是 <c>custom</c>
    /// </remarks>
    [SugarColumn(ColumnName = "package", Length = 512, IsNullable = false, ColumnDescription = "所属包")]
    public string Package { get; set; } = string.Empty;

    /// <summary>
    /// 上级节点
    /// </summary>
    /// <remarks>
    /// 顶层为空。子页面挂在登记的父页面下；目录、顶层页面与外链可挂到目录下
    /// </remarks>
    [SugarColumn(ColumnName = "parent_id", IsNullable = true, ColumnDescription = "上级节点")]
    public Guid? ParentId { get; set; }

    /// <summary>
    /// 路由路径（以 <c>/</c> 开头的 kebab 段）
    /// </summary>
    /// <remarks>
    /// 目录与外链为空
    /// </remarks>
    [SugarColumn(ColumnName = "route", Length = 512, IsNullable = false, ColumnDescription = "路由路径（以 / 开头的 kebab 段）")]
    public string Route { get; set; } = string.Empty;

    /// <summary>
    /// 组件路径（路由去前导斜杠 + <c>/index</c>）
    /// </summary>
    /// <remarks>
    /// 目录与外链为空
    /// </remarks>
    [SugarColumn(ColumnName = "component", Length = 512, IsNullable = false, ColumnDescription = "组件路径（路由去前导斜杠 + /index）")]
    public string Component { get; set; } = string.Empty;

    /// <summary>
    /// 标题文案键（页面登记或内置节点给的）
    /// </summary>
    /// <remarks>
    /// 运营建的节点为空
    /// </remarks>
    [SugarColumn(ColumnName = "title_key", Length = 512, IsNullable = false, ColumnDescription = "标题文案键（页面登记或内置节点给的）")]
    public string TitleKey { get; set; } = string.Empty;

    /// <summary>
    /// 标题原文
    /// </summary>
    /// <remarks>
    /// 运营建的节点必填；页面节点与内置节点为空即用 <see cref="TitleKey"/> 的文案，非空即覆盖
    /// </remarks>
    [SugarColumn(ColumnName = "title", Length = 512, IsNullable = true, ColumnDescription = "标题原文")]
    public string? Title { get; set; }

    /// <summary>
    /// 图标
    /// </summary>
    [SugarColumn(ColumnName = "icon", Length = 512, IsNullable = true, ColumnDescription = "图标")]
    public string? Icon { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    [SugarColumn(ColumnName = "order", IsNullable = false, ColumnDescription = "排序号")]
    public int Order { get; set; }

    /// <summary>
    /// 所需权限码
    /// </summary>
    /// <remarks>
    /// 为空即登录可见。页面节点取登记的，运营节点可选
    /// </remarks>
    [SugarColumn(ColumnName = "permission", Length = 512, IsNullable = true, ColumnDescription = "所需权限码")]
    public string? Permission { get; set; }

    /// <summary>
    /// 外链地址（http / https 绝对地址）
    /// </summary>
    /// <remarks>
    /// 只有外链节点有
    /// </remarks>
    [SugarColumn(ColumnName = "external_url", Length = 512, IsNullable = true, ColumnDescription = "外链地址（http / https 绝对地址）")]
    public string? ExternalUrl { get; set; }

    /// <summary>
    /// 是否在导航里显示（隐藏的页面仍可直接访问）
    /// </summary>
    [SugarColumn(ColumnName = "is_visible", IsNullable = false, ColumnDescription = "是否在导航里显示（隐藏的页面仍可直接访问）")]
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// 页面标签是否参与缓存（KeepAlive）
    /// </summary>
    [SugarColumn(ColumnName = "keep_alive", IsNullable = false, ColumnDescription = "页面标签是否参与缓存（KeepAlive）")]
    public bool KeepAlive { get; set; }

    /// <summary>
    /// 是否固定在标签栏（不能关闭）
    /// </summary>
    [SugarColumn(ColumnName = "is_affix", IsNullable = false, ColumnDescription = "是否固定在标签栏（不能关闭）")]
    public bool IsAffix { get; set; }

    /// <summary>
    /// 启停状态
    /// </summary>
    /// <remarks>
    /// 停用的节点连同子树不进 <c>auth.permissions</c>，页面没有路由；菜单管理里照常列出
    /// </remarks>
    [SugarColumn(ColumnName = "status", IsNullable = false, ColumnDescription = "启停状态")]
    public MenuStatus Status { get; set; } = MenuStatus.Active;

    /// <summary>
    /// 徽标文字（显示在菜单项右侧，如 <c>New</c>、<c>3</c>）
    /// </summary>
    [SugarColumn(ColumnName = "badge", Length = 512, IsNullable = true, ColumnDescription = "徽标文字（显示在菜单项右侧，如 New、3）")]
    public string? Badge { get; set; }

    /// <summary>
    /// 徽标语气
    /// </summary>
    /// <remarks>
    /// 与组件库的 tone 同名：<c>neutral</c>、<c>brand</c>、<c>info</c>、<c>success</c>、<c>warning</c>、<c>danger</c>；为空时文字徽标取中性、圆点取提醒红
    /// </remarks>
    [SugarColumn(ColumnName = "badge_type", Length = 512, IsNullable = true, ColumnDescription = "徽标语气")]
    public string? BadgeType { get; set; }

    /// <summary>
    /// 是否显示圆点（优先于徽标文字）
    /// </summary>
    [SugarColumn(ColumnName = "badge_dot", IsNullable = false, ColumnDescription = "是否显示圆点（优先于徽标文字）")]
    public bool BadgeDot { get; set; }

    /// <summary>
    /// 页面节点的展示是否被运营改过
    /// </summary>
    /// <remarks>
    /// 改过即种子不再覆盖展示字段。运营节点恒为 <see langword="false"/>
    /// </remarks>
    [SugarColumn(ColumnName = "is_customized", IsNullable = false, ColumnDescription = "页面节点的展示是否被运营改过")]
    public bool IsCustomized { get; set; }
}

/// <summary>
/// 菜单节点的种类
/// </summary>
/// <remarks>
/// 按钮不是菜单节点（方案第 10 节第 3 项：按钮即操作，门控用操作的权限码）
/// </remarks>
public enum MenuKind
{
    /// <summary>
    /// 页面：由各包的页面登记生成（种子对齐），路由、组件与权限归登记，运营只能调展示
    /// </summary>
    Page = 0,

    /// <summary>
    /// 目录：运营建的分组节点
    /// </summary>
    /// <remarks>
    /// 可在顶层或另一个目录下，没有路由；裁剪后没有子节点即不下发。内置的帮助中心目录由种子只首写一次
    /// </remarks>
    Directory = 1,

    /// <summary>
    /// 外链：运营建的外部地址
    /// </summary>
    /// <remarks>
    /// 新标签打开，不生成路由；可在顶层或目录下。内置的三个外链由种子只首写一次
    /// </remarks>
    Link = 2,
}

/// <summary>
/// 菜单节点的启停状态
/// </summary>
/// <remarks>
/// 与「在导航里显示」是两回事：隐藏的页面仍有路由、可直接访问；停用的节点连同子树不下发，页面没有路由
/// </remarks>
public enum MenuStatus
{
    /// <summary>
    /// 启用
    /// </summary>
    Active = 0,

    /// <summary>
    /// 停用
    /// </summary>
    Disabled = 1,
}
