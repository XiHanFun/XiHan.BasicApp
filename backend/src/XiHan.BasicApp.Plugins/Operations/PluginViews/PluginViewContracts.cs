// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Plugins.Operations.PluginViews;

/// <summary>
/// 列出插件页面的请求
/// </summary>
/// <remarks>
/// 没有参数：按调用方的身份与本节点加载的插件给出
/// </remarks>
public sealed record PluginViewsRequest;

/// <summary>
/// 调用方能打开的插件页面
/// </summary>
/// <param name="Plugins">有可见页面的已加载插件，按短名</param>
public sealed record PluginViewsResponse(IReadOnlyList<PluginViewResponse> Plugins);

/// <summary>
/// 一个已加载插件的页面与操作路由
/// </summary>
/// <param name="Name">短名</param>
/// <param name="ManifestId">清单 ID</param>
/// <param name="Version">本节点加载的版本</param>
/// <param name="Pages">调用方能打开的页面，按次序与页面码</param>
/// <param name="Operations">插件发布到 HTTP 的操作，按操作 ID</param>
public sealed record PluginViewResponse(
    string Name,
    string ManifestId,
    string Version,
    IReadOnlyList<PluginViewPageResponse> Pages,
    IReadOnlyList<PluginViewOperationResponse> Operations);

/// <summary>
/// 插件的一个页面
/// </summary>
/// <param name="Code">页面码</param>
/// <param name="Route">路由</param>
/// <param name="Component">组件路径，与菜单节点上的一致</param>
/// <param name="TitleKey">标题的前端文案键</param>
/// <param name="Icon">图标</param>
/// <param name="Order">次序</param>
/// <param name="Permission">打开页面要的权限码；为空即登录可见</param>
/// <param name="ViewKind">视图种类</param>
/// <param name="ViewJson">视图描述的 JSON 原文</param>
public sealed record PluginViewPageResponse(
    string Code,
    string Route,
    string Component,
    string TitleKey,
    string? Icon,
    int Order,
    string? Permission,
    string ViewKind,
    string ViewJson);

/// <summary>
/// 插件一个操作的 HTTP 路由
/// </summary>
/// <param name="Id">操作 ID</param>
/// <param name="Method">HTTP 方法</param>
/// <param name="Path">路由模板（含前缀，如 <c>/api/crm-customers/{id}</c>）</param>
/// <param name="Permission">操作要的权限码；为空即没有权限要求</param>
public sealed record PluginViewOperationResponse(string Id, string Method, string Path, string? Permission);
