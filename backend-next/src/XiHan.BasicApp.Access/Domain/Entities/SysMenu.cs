// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Access.Domain.Entities;

/// <summary>
/// 菜单（平台库目录实体）：由各包的页面登记派生（种子每次对齐），前端按 <see cref="Component"/> 映射页面；<see cref="Permission"/> 决定对谁可见。
/// </summary>
public sealed class SysMenu : AppAuditedEntity
{
    /// <summary>
    /// 页面码（<c>&lt;包&gt;.&lt;页面&gt;</c>），唯一。
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 所属包。
    /// </summary>
    public string Package { get; set; } = string.Empty;

    /// <summary>
    /// 上级菜单；顶层为空。
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// 路由路径（以 <c>/</c> 开头的 kebab 段）。
    /// </summary>
    public string Route { get; set; } = string.Empty;

    /// <summary>
    /// 组件路径（路由去前导斜杠 + <c>/index</c>）。
    /// </summary>
    public string Component { get; set; } = string.Empty;

    /// <summary>
    /// 标题文案键。
    /// </summary>
    public string TitleKey { get; set; } = string.Empty;

    /// <summary>
    /// 图标。
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 排序号。
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 所需权限码；为空即登录可见。
    /// </summary>
    public string? Permission { get; set; }

    /// <summary>
    /// 是否在菜单里显示（隐藏的页面仍可直接访问）。
    /// </summary>
    public bool IsVisible { get; set; } = true;
}
