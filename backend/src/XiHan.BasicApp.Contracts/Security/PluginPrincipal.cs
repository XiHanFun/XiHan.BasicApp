// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Kernel.Security;

namespace XiHan.BasicApp.Contracts.Security;

/// <summary>
/// 插件主体
/// </summary>
/// <remarks>
/// 插件回调宿主操作时以插件主体执行：主体 ID 是插件的清单 ID，认证方案是 <see cref="Scheme"/>（与框架插件宿主的 <c>PluginScheme</c> 相同），没有声明
/// </remarks>
public static class PluginPrincipal
{
    /// <summary>
    /// 插件主体的认证方案
    /// </summary>
    public const string Scheme = "Plugin";

    /// <summary>
    /// 是否插件主体
    /// </summary>
    /// <param name="principal">主体</param>
    public static bool Is(Principal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        return string.Equals(principal.AuthenticationScheme, Scheme, StringComparison.Ordinal);
    }
}
