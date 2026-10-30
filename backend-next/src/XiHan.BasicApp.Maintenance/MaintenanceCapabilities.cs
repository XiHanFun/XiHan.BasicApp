// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;

namespace XiHan.BasicApp.Maintenance;

/// <summary>
/// 运维包提供的能力。
/// </summary>
public static class MaintenanceCapabilities
{
    /// <summary>
    /// 运维管理面。
    /// </summary>
    public const string Maintenance = AppInfo.CapabilityPrefix + "maintenance";

    /// <summary>
    /// 能力版本。
    /// </summary>
    public const int Version = 1;
}
