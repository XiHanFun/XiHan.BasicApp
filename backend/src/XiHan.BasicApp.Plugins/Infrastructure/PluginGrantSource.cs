// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Plugins.Infrastructure;

/// <summary>
/// 插件主体的回调授予
/// </summary>
/// <remarks>
/// 回调以插件主体执行（<see cref="PluginPrincipal"/>）：平台批准的回调权限码就是它持有的宿主权限码，命中后照常经作用侧与套餐过滤器收窄
/// 插件没有加载即没有授予；授予不落在框架的授予存储里，卸载即失效
/// </remarks>
/// <param name="catalog">已加载插件的运行期贡献</param>
public sealed class PluginGrantSource(PluginRuntimeCatalog catalog) : IPermissionGrantSource
{
    /// <inheritdoc />
    public ValueTask<Result<IReadOnlyCollection<string>>> GetGrantsAsync(SecurityContext security, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(security);
        IReadOnlyCollection<string> grants = PluginPrincipal.Is(security.Principal) && security.Principal.Id is { } manifestId && catalog.Find(manifestId).TryGetValue(out var plugin) ? plugin.Callbacks : [];
        return ValueTask.FromResult<Result<IReadOnlyCollection<string>>>(Result.Success(grants));
    }
}
