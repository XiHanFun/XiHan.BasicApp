// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;

namespace XiHan.BasicApp.Plugins;

/// <summary>
/// 插件模块的权限码
/// </summary>
/// <remarks>
/// 只在平台一侧：插件进程全平台共用，租户只经套餐与角色使用插件贡献的功能，不管理插件本身
/// 读之外的都进操作日志
/// </remarks>
public static class PluginsPermissions
{
    /// <summary>
    /// 查看插件
    /// </summary>
    public const string PluginsRead = "plugins.plugins.read";

    /// <summary>
    /// 核对与安装插件包
    /// </summary>
    public const string PluginsInstall = "plugins.plugins.install";

    /// <summary>
    /// 批准插件申请的授予
    /// </summary>
    public const string PluginsGrant = "plugins.plugins.grant";

    /// <summary>
    /// 卸载插件
    /// </summary>
    public const string PluginsDelete = "plugins.plugins.delete";

    /// <summary>
    /// 全部权限描述
    /// </summary>
    public static IReadOnlyList<PermissionDescriptor> All { get; } =
    [
        new(PluginsRead, "Read plugins", PermissionScope.Platform, description: "View installed plugins, their contributions, approved grants and versions."),
        new(PluginsInstall, "Install plugins", PermissionScope.Platform, audited: true, description: "Inspect uploaded plugin packages and install them with the approved grants."),
        new(PluginsGrant, "Grant plugins", PermissionScope.Platform, audited: true, description: "Change the grants approved for an installed plugin."),
        new(PluginsDelete, "Uninstall plugins", PermissionScope.Platform, audited: true, description: "Uninstall plugins together with their versions and grants."),
    ];
}
