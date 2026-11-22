// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using XiHan.BasicApp.Contracts.Security;

namespace XiHan.BasicApp.Plugins.Domain.Packages;

/// <summary>
/// 旁挂清单 <c>basicapp.plugin.json</c>
/// </summary>
/// <remarks>
/// 框架清单不许加字段，应用层要的信息（短名、权限码的作用侧、页面、回调）写在这里；不认得的成员按不合格式拒绝，拼错的字段不会被悄悄忽略
/// </remarks>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PluginDescriptor
{
    /// <summary>
    /// 旁挂清单的接口大版本，当前为 <c>1</c>
    /// </summary>
    public string PluginApi { get; set; } = string.Empty;

    /// <summary>
    /// 短名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 发布者
    /// </summary>
    public string? Publisher { get; set; }

    /// <summary>
    /// 包里带的运行时标识（进程外插件，如 <c>linux-x64</c>、<c>win-x64</c>）
    /// </summary>
    public IReadOnlyList<string> Runtimes { get; set; } = [];

    /// <summary>
    /// 插件定义的权限码
    /// </summary>
    /// <remarks>
    /// 与框架清单 <c>contributes.permissions</c> 是同一组码，这里补上作用侧与是否审计
    /// </remarks>
    public IReadOnlyList<PluginPermissionDescriptor> Permissions { get; set; } = [];

    /// <summary>
    /// 插件贡献的页面
    /// </summary>
    public IReadOnlyList<PluginPageDescriptor> Pages { get; set; } = [];

    /// <summary>
    /// 插件主体回调宿主操作要持有的宿主权限码
    /// </summary>
    public IReadOnlyList<string> Callbacks { get; set; } = [];

    /// <summary>
    /// 扩展点（还不支持，写了即拒绝）
    /// </summary>
    public IReadOnlyList<JsonElement> Extends { get; set; } = [];
}

/// <summary>
/// 插件定义的一个权限码
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PluginPermissionDescriptor
{
    /// <summary>
    /// 权限码 <c>&lt;短名&gt;.&lt;资源&gt;.&lt;动作&gt;</c>
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 作用侧
    /// </summary>
    public PermissionScope Scope { get; set; } = PermissionScope.Tenant;

    /// <summary>
    /// 该权限保护的操作是否进审计
    /// </summary>
    public bool Audited { get; set; }
}

/// <summary>
/// 插件贡献的一个页面
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PluginPageDescriptor
{
    /// <summary>
    /// 页面码 <c>&lt;短名&gt;.&lt;页&gt;</c>
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 路由：<c>/plugins/</c> 加上页面码按点换成斜杠
    /// </summary>
    public string Route { get; set; } = string.Empty;

    /// <summary>
    /// 标题文案键（插件前端文案里的键）
    /// </summary>
    public string TitleKey { get; set; } = string.Empty;

    /// <summary>
    /// 图标名
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 同级排序
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 进入页面所需的权限码
    /// </summary>
    /// <remarks>
    /// 本插件的码或宿主已登记的码；为空只要求登录
    /// </remarks>
    public string? Permission { get; set; }

    /// <summary>
    /// 页面的视图
    /// </summary>
    public PluginViewDescriptor View { get; set; } = new();
}

/// <summary>
/// 页面的视图
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PluginViewDescriptor
{
    /// <summary>
    /// 视图种类：当前只有声明式页面 <c>schema</c>
    /// </summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>
    /// 页面描述在包里的路径，如 <c>pages/customers.json</c>
    /// </summary>
    public string File { get; set; } = string.Empty;
}
