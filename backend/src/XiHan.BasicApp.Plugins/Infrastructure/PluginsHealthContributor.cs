// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Plugins.Infrastructure;

/// <summary>
/// 插件加载的健康项
/// </summary>
/// <remarks>
/// 有启用的插件在本节点加载失败即降级，数据带失败的插件数与清单 ID；加载成功的插件由框架插件宿主各报一项 <c>plugins.&lt;清单 ID&gt;</c>
/// </remarks>
/// <param name="reconciler">插件对齐服务</param>
public sealed class PluginsHealthContributor(PluginReconciler reconciler) : IHealthContributor
{
    /// <summary>
    /// 健康项名
    /// </summary>
    public const string Name = "basicapp.plugins";

    /// <inheritdoc />
    public IReadOnlyList<ContributorStatus> Check()
    {
        var failures = reconciler.Failures.Values.OrderBy(static f => f.ManifestId, StringComparer.Ordinal).ToList();
        var data = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["loaded"] = reconciler.Loaded.Count,
            ["failed"] = failures.Count,
            ["plugins"] = failures.Count == 0 ? null : string.Join(",", failures.Select(static f => f.ManifestId)),
        };
        return failures.Count == 0
            ? [new ContributorStatus(Name, LifecycleState.Ready) { Data = data }]
            : [new ContributorStatus(Name, LifecycleState.Degraded)
            {
                Description = $"{failures.Count} enabled plugin(s) failed to load on this node.",
                Error = failures[0].Error,
                Data = data,
            }];
    }
}
