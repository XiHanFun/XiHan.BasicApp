// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;

namespace XiHan.BasicApp.Tenancy;

/// <summary>
/// 租户包提供的能力名。
/// </summary>
public static class TenancyCapabilities
{
    /// <summary>
    /// 租户业务：开通、启停、管理员初始化。
    /// </summary>
    public const string Tenancy = AppInfo.CapabilityPrefix + "tenancy";

    /// <summary>
    /// 能力版本。
    /// </summary>
    public const int Version = AppInfo.CapabilityVersion;
}
