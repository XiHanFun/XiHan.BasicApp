// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Plugins.Domain.Packages;
using XiHan.BasicApp.Plugins.Infrastructure;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Localization.Abstractions;
using XiHan.Framework.Primitives;
using XiHan.Framework.Protocols.Http;
using XiHan.Framework.Security.Abstractions.Permissions;
using SecurityPolicies = XiHan.BasicApp.Contracts.Security.Policies;

namespace XiHan.BasicApp.Plugins.Operations.PluginViews;

/// <summary>
/// 列出调用方能打开的插件页面
/// </summary>
/// <remarks>
/// 方案 6.1：前端运行期据此把插件页面交给插件视图加载器、按操作 ID 找到要调的路由。只看本节点加载成功的插件；页面要的权限码按调用方当前的有效权限判定（含作用侧与套餐收窄），没有可见页面的插件不列出
/// 操作路由与宿主发布的一致（显式覆盖与前缀都算上），只列发布到 HTTP 的；前端文案取调用方的区域，缺的键按 en-US 补上
/// </remarks>
/// <param name="catalog">已加载插件的运行期贡献</param>
/// <param name="registry">操作登记表</param>
/// <param name="http">HTTP 协议选项</param>
/// <param name="permissions">权限判定</param>
/// <param name="localization">本地化选项：协商调用方的区域</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Policy = SecurityPolicies.Authenticated)]
public sealed class ListPluginViews(PluginRuntimeCatalog catalog, OperationRegistry registry, HttpProtocolOptions http, IPermissionChecker permissions, LocalizationOptions localization) : IOperation<PluginViewsRequest, PluginViewsResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "plugin-views.list";

    /// <inheritdoc />
    public async ValueTask<Result<PluginViewsResponse>> HandleAsync(PluginViewsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var security = context.GetSecurity();
        var locale = localization.Resolve(context);
        var plugins = new List<PluginViewResponse>();
        foreach (var plugin in catalog.Loaded)
        {
            var pages = new List<PluginViewPageResponse>();
            foreach (var page in plugin.Pages.OrderBy(static p => p.Order).ThenBy(static p => p.Code, StringComparer.Ordinal))
            {
                if (page.Permission is { } code)
                {
                    var allowed = await permissions.HasPermissionAsync(security, code, context.CancellationToken).ConfigureAwait(false);
                    if (!allowed.TryGetValue(out var granted))
                    {
                        return allowed.Error!;
                    }

                    if (!granted)
                    {
                        continue;
                    }
                }

                pages.Add(new PluginViewPageResponse(page.Code, page.Route, page.Component, page.TitleKey, page.Icon, page.Order, page.Permission, page.ViewKind, page.ViewJson));
            }

            if (pages.Count == 0)
            {
                continue;
            }

            var operations = registry.All()
                .Where(d => string.Equals(d.OwnerId, plugin.ManifestId, StringComparison.Ordinal) && d.Contract.Expose.HasFlag(Expose.Http))
                .OrderBy(static d => d.Id, StringComparer.Ordinal)
                .Select(d =>
                {
                    var route = http.RouteFor(d);
                    return new PluginViewOperationResponse(d.Id, route.Method, route.Template, d.Contract.Permission);
                })
                .ToList();
            plugins.Add(new PluginViewResponse(plugin.Name, plugin.ManifestId, plugin.Version, pages, operations, TextsFor(plugin, locale)));
        }

        return new PluginViewsResponse(plugins);
    }

    /// <summary>
    /// 一个区域的前端文案：en-US 打底，再盖上这个区域有的键
    /// </summary>
    private static Dictionary<string, string> TextsFor(LoadedPlugin plugin, string locale)
    {
        var texts = new Dictionary<string, string>(plugin.Texts.GetValueOrDefault(PluginInspector.SourceLocale) ?? new Dictionary<string, string>(), StringComparer.Ordinal);
        if (locale != PluginInspector.SourceLocale && plugin.Texts.TryGetValue(locale, out var localized))
        {
            foreach (var (key, text) in localized)
            {
                texts[key] = text;
            }
        }

        return texts;
    }
}
