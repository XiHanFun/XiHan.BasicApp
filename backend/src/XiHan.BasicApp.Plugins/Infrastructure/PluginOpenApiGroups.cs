// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Primitives;
using XiHan.Framework.Protocols.Http.OpenApi;

namespace XiHan.BasicApp.Plugins.Infrastructure;

/// <summary>
/// 已加载插件的接口文档分组
/// </summary>
/// <remarks>
/// 方案 5.3：每个加载成功的插件一组，组名是短名、归属是清单 ID，按短名排序、序号从 <see cref="FirstOrder"/> 起，排在宿主各包的分组之后；显示名与说明取插件消息目录里的 <c>openapi.groups.&lt;短名&gt;.*</c>
/// 分组跟着运行期目录变：插件加载与卸载时令牌触发，全量文档的 <c>x-tagGroups</c> 与 <c>/openapi/&lt;短名&gt;.json</c> 随之出现或撤回
/// </remarks>
/// <param name="catalog">已加载插件的运行期贡献</param>
public sealed class PluginOpenApiGroups(PluginRuntimeCatalog catalog) : IOpenApiGroupSource
{
    /// <summary>
    /// 插件分组的起始序号
    /// </summary>
    public const int FirstOrder = 1000;

    /// <inheritdoc />
    public IReadOnlyList<OpenApiGroup> Groups => [.. catalog.Loaded.Select(static (plugin, index) => new OpenApiGroup(plugin.Name, FirstOrder + index, [plugin.ManifestId]))];

    /// <inheritdoc />
    public IChangeToken GetChangeToken() => catalog.GetChangeToken();
}
