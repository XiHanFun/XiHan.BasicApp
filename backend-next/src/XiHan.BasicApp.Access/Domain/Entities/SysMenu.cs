// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Access.Domain.Entities;

/// <summary>
/// 菜单（平台库目录实体，两侧共用一棵树，按权限裁剪）。
/// </summary>
/// <remarks>
/// 页面节点由各包的页面登记派生：<see cref="Route"/>、<see cref="Component"/>、<see cref="TitleKey"/>、<see cref="Permission"/> 归登记，
/// 种子每次对齐；展示字段（标题覆盖、图标、排序、所属目录、可见、缓存、固定标签）运营可改，改过即 <see cref="IsCustomized"/>，种子不再覆盖，可经 <c>menus.reset</c> 恢复登记的值。
/// 目录与外链节点由运营新建（编码 <c>custom.&lt;名&gt;</c>），种子不碰。
/// </remarks>
public sealed class SysMenu : AppAuditedEntity
{
    /// <summary>
    /// 节点编码，唯一。
    /// </summary>
    /// <remarks>
    /// 页面节点是页面码（<c>&lt;包&gt;.&lt;页面&gt;</c>），运营节点是 <c>custom.&lt;名&gt;</c>。
    /// </remarks>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 种类。
    /// </summary>
    public MenuKind Kind { get; set; } = MenuKind.Page;

    /// <summary>
    /// 所属包。
    /// </summary>
    /// <remarks>
    /// 运营节点是 <c>custom</c>。
    /// </remarks>
    public string Package { get; set; } = string.Empty;

    /// <summary>
    /// 上级节点。
    /// </summary>
    /// <remarks>
    /// 顶层为空。子页面挂在登记的父页面下；顶层页面与外链可挂到目录下。
    /// </remarks>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// 路由路径（以 <c>/</c> 开头的 kebab 段）。
    /// </summary>
    /// <remarks>
    /// 目录与外链为空。
    /// </remarks>
    public string Route { get; set; } = string.Empty;

    /// <summary>
    /// 组件路径（路由去前导斜杠 + <c>/index</c>）。
    /// </summary>
    /// <remarks>
    /// 目录与外链为空。
    /// </remarks>
    public string Component { get; set; } = string.Empty;

    /// <summary>
    /// 标题文案键（页面登记给的）。
    /// </summary>
    /// <remarks>
    /// 运营节点为空。
    /// </remarks>
    public string TitleKey { get; set; } = string.Empty;

    /// <summary>
    /// 标题原文。
    /// </summary>
    /// <remarks>
    /// 运营节点必填；页面节点为空即用 <see cref="TitleKey"/> 的文案，非空即覆盖。
    /// </remarks>
    public string? Title { get; set; }

    /// <summary>
    /// 图标。
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 排序号。
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 所需权限码。
    /// </summary>
    /// <remarks>
    /// 为空即登录可见。页面节点取登记的，运营节点可选。
    /// </remarks>
    public string? Permission { get; set; }

    /// <summary>
    /// 外链地址（http / https 绝对地址）。
    /// </summary>
    /// <remarks>
    /// 只有外链节点有。
    /// </remarks>
    public string? ExternalUrl { get; set; }

    /// <summary>
    /// 是否在导航里显示（隐藏的页面仍可直接访问）。
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// 页面标签是否参与缓存（KeepAlive）。
    /// </summary>
    public bool KeepAlive { get; set; }

    /// <summary>
    /// 是否固定在标签栏（不能关闭）。
    /// </summary>
    public bool IsAffix { get; set; }

    /// <summary>
    /// 页面节点的展示是否被运营改过。
    /// </summary>
    /// <remarks>
    /// 改过即种子不再覆盖展示字段。运营节点恒为 <see langword="false"/>。
    /// </remarks>
    public bool IsCustomized { get; set; }
}
