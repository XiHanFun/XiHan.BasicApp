// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;

namespace XiHan.BasicApp.OpenPlatform;

/// <summary>
/// 开放平台包提供的能力。
/// </summary>
public static class OpenPlatformCapabilities
{
    /// <summary>
    /// 开放平台能力：签名开放接口。
    /// </summary>
    public const string OpenPlatform = AppInfo.CapabilityPrefix + "openplatform";

    /// <summary>
    /// 能力版本。
    /// </summary>
    public const int Version = 1;
}
