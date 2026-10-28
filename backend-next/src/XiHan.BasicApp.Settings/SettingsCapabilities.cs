// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;

namespace XiHan.BasicApp.Settings;

/// <summary>
/// 设置包提供的能力。
/// </summary>
public static class SettingsCapabilities
{
    /// <summary>
    /// 设置能力：系统参数、字典、业务编号。
    /// </summary>
    public const string Settings = AppInfo.CapabilityPrefix + "settings";

    /// <summary>
    /// 能力版本。
    /// </summary>
    public const int Version = 1;
}
