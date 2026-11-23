// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Plugins.Infrastructure;

/// <summary>
/// 一个插件在本节点上加载失败
/// </summary>
/// <param name="PluginId">插件 ID</param>
/// <param name="ManifestId">清单 ID</param>
/// <param name="Version">要加载的版本号</param>
/// <param name="Error">失败</param>
public sealed record PluginFailure(Guid PluginId, string ManifestId, string Version, Error Error);
