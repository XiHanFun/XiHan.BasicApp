// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;

namespace XiHan.BasicApp.Files;

/// <summary>
/// 文件包提供的能力。
/// </summary>
public static class FilesCapabilities
{
    /// <summary>
    /// 文件能力：存储配置、文件与副本。
    /// </summary>
    public const string Files = AppInfo.CapabilityPrefix + "files";

    /// <summary>
    /// 能力版本。
    /// </summary>
    public const int Version = 1;
}
