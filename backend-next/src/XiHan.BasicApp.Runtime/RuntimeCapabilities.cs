// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;

namespace XiHan.BasicApp.Runtime;

/// <summary>
/// 运行时基座提供的包间能力 ID；各能力包在清单里以 <c>basicapp.runtime@1</c> 声明依赖。
/// </summary>
public static class RuntimeCapabilities
{
    /// <summary>
    /// 运行时基座能力：操作人填充、租户作用域查询、SqlSugar 回调。
    /// </summary>
    public const string Runtime = AppInfo.CapabilityPrefix + "runtime";

    /// <summary>
    /// 能力版本。
    /// </summary>
    public const int Version = AppInfo.CapabilityVersion;
}
