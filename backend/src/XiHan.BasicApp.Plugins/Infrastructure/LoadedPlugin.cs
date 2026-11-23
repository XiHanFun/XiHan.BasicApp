// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;

namespace XiHan.BasicApp.Plugins.Infrastructure;

/// <summary>
/// 一个已加载插件交给宿主的运行期贡献
/// </summary>
/// <param name="ManifestId">清单 ID（也是插件主体的 ID）</param>
/// <param name="Name">短名（权限码与页面码的首段）</param>
/// <param name="Version">加载的版本</param>
/// <param name="Permissions">插件贡献的权限描述（作用侧与审计取自旁挂清单）</param>
/// <param name="Pages">插件的页面，带视图描述</param>
/// <param name="Callbacks">平台批准的回调权限码：插件主体持有的宿主权限码</param>
/// <param name="UnrestrictedData">平台批准了 <c>data-scope:all</c>：插件主体不受数据范围限制</param>
public sealed record LoadedPlugin(
    string ManifestId,
    string Name,
    string Version,
    IReadOnlyList<PermissionDescriptor> Permissions,
    IReadOnlyList<LoadedPluginPage> Pages,
    IReadOnlyList<string> Callbacks,
    bool UnrestrictedData);
