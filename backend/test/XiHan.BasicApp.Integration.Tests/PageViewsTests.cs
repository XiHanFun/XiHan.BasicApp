// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts.Pages;
using XiHan.BasicApp.Contracts.Workflow;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Runtime.Persistence;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 后端登记的页面与审批单据都有前端视图，前端的视图都有登记
/// </summary>
/// <remarks>
/// 组件路径按前端 <c>packages/router/views.ts</c> 的规则映射：壳层页面查它的 <c>coreViews</c> 表，其余是 <c>src/modules/&lt;包&gt;/views/&lt;路径&gt;.vue</c>
/// 登记的组件找不到视图时菜单点开即报错；前端多出来的视图没有菜单、也不是审批单据，没有入口能到
/// 宿主缺省打开全部可选模块，取到的就是全部登记
/// </remarks>
public sealed partial class PageViewsTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task RegisteredPagesAndApprovalForms_MatchTheFrontendViews_BothWays()
    {
        var frontend = Path.Combine(RepositoryRoot(), "frontend");
        var coreViews = CoreViews(frontend);
        var moduleViews = ModuleViews(frontend);

        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, DatabaseProviders.Sqlite);
        var pages = server.Services.GetServices<PageCatalogSection>().SelectMany(static s => Flatten(s.Pages)).Select(static p => (p.Code, p.Component)).ToList();
        await using var scope = server.Services.CreateAsyncScope();
        var forms = scope.ServiceProvider.GetServices<IWorkflowBusinessHandler>().Select(static h => (h.Descriptor.Code, Component: h.Descriptor.FormComponent)).ToList();
        Assert.True(pages.Count >= 50, $"Expected the pages of every package, found {pages.Count}.");
        Assert.NotEmpty(forms);

        var problems = new List<string>();
        foreach (var (code, component) in pages.Concat(forms))
        {
            if (!coreViews.ContainsKey(component) && !moduleViews.Contains(component))
            {
                problems.Add($"{code}: component '{component}' has no view at src/modules/<package>/views/<path>.vue nor in the coreViews of packages/router/views.ts.");
            }
        }

        problems.AddRange(coreViews.Where(static v => !File.Exists(v.Value)).Select(static v => $"Core view '{v.Key}' points at a missing file '{v.Value}'."));
        var registered = pages.Concat(forms).Select(static p => p.Component).ToHashSet(StringComparer.Ordinal);
        problems.AddRange(moduleViews.Where(v => !registered.Contains(v)).Order(StringComparer.Ordinal).Select(static v => $"View '{v}' is neither a registered page nor an approval form, so nothing leads to it."));
        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    private static IEnumerable<PageDescriptor> Flatten(IEnumerable<PageDescriptor> pages)
        => pages.SelectMany(static p => Flatten(p.Children).Prepend(p));

    /// <summary>
    /// 壳层页面：组件路径 → 视图文件
    /// </summary>
    /// <remarks>
    /// 读 <c>packages/router/views.ts</c> 里 <c>coreViews</c> 的每一项，<c>~</c> 是 <c>packages</c>
    /// </remarks>
    private static Dictionary<string, string> CoreViews(string frontend)
    {
        var source = File.ReadAllText(Path.Combine(frontend, "packages", "router", "views.ts"));
        var start = source.IndexOf("const coreViews", StringComparison.Ordinal);
        Assert.True(start >= 0, "packages/router/views.ts no longer declares coreViews; update this test with the new mapping.");
        var end = source.IndexOf("\n}", start, StringComparison.Ordinal);
        return CoreViewEntry().Matches(source[start..end]).ToDictionary(
            static m => m.Groups["component"].Value,
            m => Path.Combine(frontend, "packages", m.Groups["file"].Value.Replace('/', Path.DirectorySeparatorChar)),
            StringComparer.Ordinal);
    }

    /// <summary>
    /// 能力包的视图：<c>src/modules/&lt;包&gt;/views/**/index.vue</c> 的组件路径
    /// </summary>
    private static HashSet<string> ModuleViews(string frontend)
    {
        var views = new HashSet<string>(StringComparer.Ordinal);
        foreach (var module in Directory.EnumerateDirectories(Path.Combine(frontend, "src", "modules")))
        {
            var root = Path.Combine(module, "views");
            if (!Directory.Exists(root))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(root, "index.vue", SearchOption.AllDirectories))
            {
                views.Add($"{Path.GetFileName(module)}/{Path.GetRelativePath(root, file)[..^".vue".Length].Replace(Path.DirectorySeparatorChar, '/')}");
            }
        }

        return views;
    }

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "backend", "XiHan.BasicApp.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("The repository root (with backend/XiHan.BasicApp.slnx) was not found above the test output.");
    }

    [GeneratedRegex(@"'(?<component>[a-z0-9/-]+)'\s*:\s*\(\)\s*=>\s*import\('~/(?<file>[^']+)'\)")]
    private static partial Regex CoreViewEntry();
}
