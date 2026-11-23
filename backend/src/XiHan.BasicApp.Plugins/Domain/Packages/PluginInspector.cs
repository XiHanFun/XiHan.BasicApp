// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using XiHan.BasicApp.Contracts.Pages;
using XiHan.BasicApp.Contracts.Security;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Primitives;
using XiHan.Framework.Primitives.Manifests;
using PluginGrant = XiHan.Framework.Kernel.Plugins.PermissionGrant;

namespace XiHan.BasicApp.Plugins.Domain.Packages;

/// <summary>
/// 插件包的静态核对
/// </summary>
/// <remarks>
/// 方案 4.3：安装前把框架清单、旁挂清单与包里的文件对一遍，插件代码一行都不跑。依次核对旁挂清单版本、清单种类与隔离级别、管道中间件、短名、当前平台的运行时或模块、
/// 前端文案、操作的资源段、权限码、页面、回调、扩展点，最后与已登记的操作和权限码比对；第一处不过即返回它的错误
/// 通过后给出插件申请的授予：框架要的（清单 <c>permissions</c>、<c>config:</c>、<c>binding:</c>）与回调要的 <c>callback:&lt;权限码&gt;</c> 都必须批准，<c>data-scope:all</c> 可选
/// </remarks>
/// <param name="options">插件模块选项</param>
/// <param name="operations">操作登记表</param>
/// <param name="permissions">权限登记表</param>
/// <param name="permissionSections">各包登记的权限目录片段</param>
/// <param name="pageSections">各包登记的页面片段</param>
public sealed partial class PluginInspector(PluginsOptions options, OperationRegistry operations, PermissionRegistry permissions, IEnumerable<PermissionCatalogSection> permissionSections, IEnumerable<PageCatalogSection> pageSections)
{
    /// <summary>
    /// 旁挂清单当前的接口版本
    /// </summary>
    public const string PluginApi = "1";

    /// <summary>
    /// 回调授予的前缀
    /// </summary>
    public const string CallbackPrefix = "callback:";

    /// <summary>
    /// 插件主体在租户里看全部数据的授予
    /// </summary>
    public const string DataScopeAll = "data-scope:all";

    /// <summary>
    /// 回调宿主操作要的框架能力
    /// </summary>
    public const string PipelineCapability = "capability:kernel.pipeline";

    /// <summary>
    /// 声明式页面的视图种类
    /// </summary>
    public const string SchemaView = "schema";

    /// <summary>
    /// 插件页面路由的前缀
    /// </summary>
    public const string RoutePrefix = "/plugins/";

    /// <summary>
    /// 前端文案要齐的区域
    /// </summary>
    public static IReadOnlyList<string> Locales { get; } = ["en-US", "zh-CN", "zh-TW", "ja-JP", "ko-KR", "de-DE", "es-ES", "fr-FR", "pt-BR", "ru-RU"];

    /// <summary>
    /// 内置包名之外还要保留的短名
    /// </summary>
    /// <remarks>
    /// <c>installed</c> 是 <c>/plugins/</c> 下管理页的路由段，<c>custom</c> 是运营菜单节点的包段
    /// </remarks>
    private static readonly string[] _reservedNames = ["plugins", "installed", "custom", "system", "api", "embed"];

    /// <summary>
    /// 核对读出来的包
    /// </summary>
    /// <param name="package">插件包</param>
    /// <returns>核对结果；第一处不过的错误</returns>
    public Result<PluginInspection> Inspect(PluginPackage package)
    {
        ArgumentNullException.ThrowIfNull(package);
        var manifest = package.Manifest;
        var descriptor = package.Descriptor;
        if (!string.Equals(descriptor.PluginApi, PluginApi, StringComparison.Ordinal))
        {
            return Error.Validation(PluginsErrors.DescriptorInvalid, PluginsMessages.DescriptorApiUnsupported, MessageArguments.Of(("version", descriptor.PluginApi)));
        }

        if (manifest.Kind != ManifestKind.Plugin || manifest.Isolation is not { } isolation)
        {
            return Error.Validation(PluginsErrors.ManifestRejected, PluginsMessages.PackageKindInvalid);
        }

        if (!options.Isolations.Contains(isolation))
        {
            return Error.Validation(PluginsErrors.ManifestRejected, PluginsMessages.PackageIsolationNotAllowed, MessageArguments.Of(("isolation", PluginsOptions.NameOf(isolation))));
        }

        // 管道中间件能改写任何请求，Wasm 插件也先不开放
        if (manifest.Contributes.Pipeline.Count > 0)
        {
            return Error.Validation(PluginsErrors.ManifestRejected, PluginsMessages.PackagePipelineUnsupported);
        }

        var name = descriptor.Name;
        if (!NamePattern().IsMatch(name))
        {
            return Error.Validation(PluginsErrors.NameInvalid, PluginsMessages.NameInvalid, MessageArguments.Of(("name", name)));
        }

        if (ReservedNames().Contains(name))
        {
            return Error.Validation(PluginsErrors.NameInvalid, PluginsMessages.NameReserved, MessageArguments.Of(("name", name)));
        }

        var checks = new Func<Error?>[]
        {
            () => CheckEntry(package, isolation),
            () => CheckLocales(package),
            () => CheckOperations(manifest, name),
            () => CheckPermissions(manifest, descriptor, name),
            () => CheckPages(package, name),
            () => CheckCallbacks(manifest, descriptor),
            () => descriptor.Extends.Count > 0 ? Error.Validation(PluginsErrors.ContributionInvalid, PluginsMessages.ExtendsUnsupported) : null,
            () => CheckConflicts(manifest),
        };
        foreach (var check in checks)
        {
            if (check() is { } error)
            {
                return error;
            }
        }

        return new PluginInspection(package, RequiredGrantsOf(manifest, descriptor), [DataScopeAll]);
    }

    /// <summary>
    /// 插件申请、必须全部批准的授予
    /// </summary>
    /// <remarks>
    /// 框架清单的 <c>permissions</c> 与它要读的配置节、绑定（框架的 <see cref="PluginGrant"/> 按空授予算出缺的项），加上每个回调权限码的 <c>callback:</c>；去重后按序数排序
    /// </remarks>
    /// <param name="manifest">框架清单</param>
    /// <param name="descriptor">旁挂清单</param>
    public static IReadOnlyList<string> RequiredGrantsOf(Manifest manifest, PluginDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(descriptor);
        return PluginGrant.Empty.Missing(manifest)
            .Concat(PluginGrant.Empty.MissingConfigurationGrants(manifest))
            .Concat(descriptor.Callbacks.Select(static code => CallbackPrefix + code))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// 资源段：操作 ID 的第一个点之前
    /// </summary>
    private static string ResourceOf(string operationId) => operationId[..operationId.IndexOf('.', StringComparison.Ordinal)];

    /// <summary>
    /// 当前平台的可执行文件或 Wasm 模块在包里
    /// </summary>
    /// <remarks>
    /// 进程外插件的可执行文件在 <c>runtimes/&lt;运行时标识&gt;/&lt;入口&gt;</c>，Windows 上入口没有扩展名时补 <c>.exe</c>（与框架宿主的找法一致）；Wasm 模块在根上
    /// </remarks>
    private static Error? CheckEntry(PluginPackage package, PluginIsolation isolation)
    {
        var entry = package.Manifest.Entry ?? string.Empty;
        if (isolation == PluginIsolation.Wasm)
        {
            return entry.Length > 0 && package.Entries.Contains(entry)
                ? null
                : Error.Validation(PluginsErrors.PackageInvalid, PluginsMessages.PackageEntryMissing, MessageArguments.Of(("entry", entry)));
        }

        var runtime = RuntimeInformation.RuntimeIdentifier;
        var path = $"runtimes/{runtime}/{entry}";
        var found = entry.Length > 0 && (package.Entries.Contains(path) || (OperatingSystem.IsWindows() && Path.GetExtension(entry).Length == 0 && package.Entries.Contains(path + ".exe")));
        return found && package.Descriptor.Runtimes.Contains(runtime, StringComparer.Ordinal)
            ? null
            : Error.Validation(PluginsErrors.PackageInvalid, PluginsMessages.PackageRuntimeMissing, MessageArguments.Of(("runtime", runtime)));
    }

    /// <summary>
    /// 10 种区域的前端文案都在包里
    /// </summary>
    private static Error? CheckLocales(PluginPackage package)
        => Locales.FirstOrDefault(locale => !package.Entries.Contains($"locales/{locale}.json")) is { } missing
            ? Error.Validation(PluginsErrors.PackageInvalid, PluginsMessages.PackageLocaleMissing, MessageArguments.Of(("locale", missing)))
            : null;

    /// <summary>
    /// 每个操作的资源段等于短名或以 <c>&lt;短名&gt;-</c> 开头
    /// </summary>
    /// <remarks>
    /// 插件的操作因此不会撞上内置资源（内置包名都是保留名），路由也就落在插件自己的资源下
    /// </remarks>
    private static Error? CheckOperations(Manifest manifest, string name)
        => manifest.Contributes.Operations.FirstOrDefault(id => ResourceOf(id) is var resource && resource != name && !resource.StartsWith(name + "-", StringComparison.Ordinal)) is { } outside
            ? Error.Validation(PluginsErrors.ContributionInvalid, PluginsMessages.OperationOutsideName, MessageArguments.Of(("id", outside), ("name", name)))
            : null;

    /// <summary>
    /// 权限码与框架清单一致且合命名规则
    /// </summary>
    /// <remarks>
    /// 旁挂清单的码与框架清单 <c>contributes.permissions</c> 是同一组，且合 <c>&lt;短名&gt;.&lt;资源&gt;.&lt;动作&gt;</c>，资源段是本插件某个操作的资源段
    /// </remarks>
    private static Error? CheckPermissions(Manifest manifest, PluginDescriptor descriptor, string name)
    {
        var declared = descriptor.Permissions.Select(static p => p.Code).ToList();
        if (declared.Count != declared.Distinct(StringComparer.Ordinal).Count() || !declared.ToHashSet(StringComparer.Ordinal).SetEquals(manifest.Contributes.Permissions))
        {
            return Error.Validation(PluginsErrors.ContributionInvalid, PluginsMessages.CodesMismatch);
        }

        var resources = manifest.Contributes.Operations.Select(ResourceOf).ToHashSet(StringComparer.Ordinal);
        foreach (var permission in descriptor.Permissions)
        {
            var code = permission.Code;
            if (!PermissionDescriptor.IsValidCode(code) || code.Split('.') is not [var package, var resource, _] || package != name || !resources.Contains(resource) || !Enum.IsDefined(permission.Scope))
            {
                return Error.Validation(PluginsErrors.ContributionInvalid, PluginsMessages.CodeInvalid, MessageArguments.Of(("code", code)));
            }
        }

        return null;
    }

    /// <summary>
    /// 页面码 <c>&lt;短名&gt;.&lt;页&gt;</c>、路由 <c>/plugins/&lt;短名&gt;/&lt;页&gt;</c>、标题键、视图与所需权限码都合规则
    /// </summary>
    private Error? CheckPages(PluginPackage package, string name)
    {
        var pluginCodes = package.Descriptor.Permissions.Select(static p => p.Code).ToHashSet(StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var page in package.Descriptor.Pages)
        {
            var valid = PageCodePattern().IsMatch(page.Code)
                && page.Code.StartsWith(name + ".", StringComparison.Ordinal)
                && seen.Add(page.Code)
                && string.Equals(page.Route, RoutePrefix + page.Code.Replace('.', '/'), StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(page.TitleKey)
                && (page.Permission is null || pluginCodes.Contains(page.Permission) || permissions.Find(page.Permission).HasValue)
                && string.Equals(page.View.Kind, SchemaView, StringComparison.Ordinal)
                && package.Entries.Contains(page.View.File)
                && IsMenuPage(page);
            if (!valid)
            {
                return Error.Validation(PluginsErrors.ContributionInvalid, PluginsMessages.PageInvalid, MessageArguments.Of(("code", page.Code)));
            }
        }

        return null;
    }

    /// <summary>
    /// 页面能登记成菜单页面
    /// </summary>
    /// <remarks>
    /// 以菜单页面登记自己的文法为准（路由每段 kebab-case、权限码文法），装好之后对齐菜单时不会被拒
    /// </remarks>
    private static bool IsMenuPage(PluginPageDescriptor page)
    {
        try
        {
            _ = new PageDescriptor(page.Code, page.Route, page.TitleKey, page.Icon, page.Order, page.Permission);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// 回调要持有的码都是宿主已登记的，且清单申请了回调要的管道能力
    /// </summary>
    private Error? CheckCallbacks(Manifest manifest, PluginDescriptor descriptor)
    {
        if (descriptor.Callbacks.FirstOrDefault(code => !permissions.Find(code).HasValue) is { } unknown)
        {
            return Error.Validation(PluginsErrors.ContributionInvalid, PluginsMessages.CallbackUnknown, MessageArguments.Of(("code", unknown)));
        }

        return descriptor.Callbacks.Count > 0 && !manifest.Permissions.Contains(PipelineCapability, StringComparer.Ordinal)
            ? Error.Validation(PluginsErrors.ContributionInvalid, PluginsMessages.CallbackPipelineRequired)
            : null;
    }

    /// <summary>
    /// 贡献的操作与权限码都还没被别人登记
    /// </summary>
    /// <remarks>
    /// 只是预检：加载时框架按登记表整组再核对一次（重名、路由与 tool 名冲突），以加载时为准
    /// </remarks>
    private Error? CheckConflicts(Manifest manifest)
    {
        var taken = manifest.Contributes.Operations.FirstOrDefault(id => operations.Find(id).HasValue)
            ?? manifest.Contributes.Permissions.FirstOrDefault(code => permissions.Find(code).HasValue);
        return taken is null ? null : Error.Conflict(PluginsErrors.ContributionConflict, PluginsMessages.ContributionConflict, MessageArguments.Of(("item", taken)));
    }

    /// <summary>
    /// 全部保留名：内置包名与固定的几个
    /// </summary>
    private HashSet<string> ReservedNames()
    {
        var names = new HashSet<string>(_reservedNames, StringComparer.Ordinal);
        names.UnionWith(permissionSections.Select(static s => s.Package));
        names.UnionWith(pageSections.Select(static s => s.Package));
        return names;
    }

    /// <summary>
    /// 短名的文法
    /// </summary>
    [GeneratedRegex("^[a-z][a-z0-9-]{1,31}$")]
    private static partial Regex NamePattern();

    /// <summary>
    /// 页面码的文法
    /// </summary>
    [GeneratedRegex("^[a-z][a-z0-9-]*\\.[a-z][a-z0-9-]*$")]
    private static partial Regex PageCodePattern();
}

/// <summary>
/// 核对通过的插件包
/// </summary>
/// <param name="Package">插件包</param>
/// <param name="RequiredGrants">必须批准的授予</param>
/// <param name="OptionalGrants">可以另外批准的授予</param>
public sealed record PluginInspection(PluginPackage Package, IReadOnlyList<string> RequiredGrants, IReadOnlyList<string> OptionalGrants);
