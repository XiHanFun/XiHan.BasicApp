// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Plugins.Infrastructure;

/// <summary>
/// 已加载插件的一个页面
/// </summary>
/// <remarks>
/// 取自旁挂清单的 <c>pages</c>，视图描述是包里 <c>view.file</c> 的原文，加载时读入
/// </remarks>
/// <param name="Code">页面码</param>
/// <param name="Route">路由</param>
/// <param name="TitleKey">标题的前端文案键</param>
/// <param name="Icon">图标</param>
/// <param name="Order">同目录内的次序</param>
/// <param name="Permission">打开页面要的权限码；为空即登录可见</param>
/// <param name="ViewKind">视图种类</param>
/// <param name="ViewJson">视图描述的 JSON 原文</param>
public sealed record LoadedPluginPage(
    string Code,
    string Route,
    string TitleKey,
    string? Icon,
    int Order,
    string? Permission,
    string ViewKind,
    string ViewJson)
{
    /// <summary>
    /// 前端组件路径
    /// </summary>
    /// <remarks>
    /// 与菜单节点的组件路径同一规则：路由去掉开头的斜杠再加 <c>/index</c>
    /// </remarks>
    public string Component => Route[1..] + "/index";
}
