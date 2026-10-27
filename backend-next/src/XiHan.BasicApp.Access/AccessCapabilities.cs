// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;

namespace XiHan.BasicApp.Access;

/// <summary>
/// 授权包提供的能力名。
/// </summary>
public static class AccessCapabilities
{
    /// <summary>
    /// 角色、授予、权限目录与菜单。
    /// </summary>
    public const string Access = AppInfo.CapabilityPrefix + "access";

    /// <summary>
    /// 能力版本。
    /// </summary>
    public const int Version = AppInfo.CapabilityVersion;
}
