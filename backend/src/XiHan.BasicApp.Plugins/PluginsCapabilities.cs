// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;

namespace XiHan.BasicApp.Plugins;

/// <summary>
/// 插件模块提供的能力
/// </summary>
public static class PluginsCapabilities
{
    /// <summary>
    /// 插件管理能力：插件包的核对、授权、安装与卸载
    /// </summary>
    public const string Plugins = AppInfo.CapabilityPrefix + "plugins";

    /// <summary>
    /// 能力版本
    /// </summary>
    public const int Version = 1;
}
