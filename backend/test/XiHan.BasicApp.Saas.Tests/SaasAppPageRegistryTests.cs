// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Pages;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 页面登记表结构约束测试。
/// </summary>
/// <remarks>
/// <see cref="PageRegistry"/> 是菜单的单一事实源：菜单种子按登记顺序解析 ParentId、
/// 前端按 Path/Component/RouteName 装配动态路由。这里把那些"写错了不会报错、只会在运行期
/// 表现为菜单丢失/路由撞名/权限失控"的约定固化成会红的断言。
/// </remarks>
public sealed class SaasAppPageRegistryTests
{
    /// <summary>
    /// 登记表不能为空，否则后面所有结构断言都会退化成空跑。
    /// </summary>
    [Fact]
    public void Registry_ShouldNotBeEmpty()
    {
        Assert.NotEmpty(PageRegistry.All);
        Assert.NotEmpty(PageRegistry.Buttons);
    }

    /// <summary>
    /// 页面码必须全局唯一：重复码会让种子在同一位置反复写入/覆盖同一条菜单。
    /// </summary>
    [Fact]
    public void PageCodes_ShouldBeUnique()
    {
        var duplicated = PageRegistry.All
            .GroupBy(page => page.Code, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key}（{group.Count()} 次）")
            .ToList();

        Assert.True(duplicated.Count == 0, $"页面码重复：{string.Join(", ", duplicated)}");
    }

    /// <summary>
    /// 路由路径必须唯一：两条菜单指向同一路径会导致前端动态路由互相覆盖。
    /// </summary>
    [Fact]
    public void PagePaths_ShouldBeUnique()
    {
        var duplicated = PageRegistry.All
            .GroupBy(page => page.Path, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key} ← {string.Join(", ", group.Select(page => page.Code))}")
            .ToList();

        Assert.True(duplicated.Count == 0, $"路由路径重复：{string.Join(" | ", duplicated)}");
    }

    /// <summary>
    /// 路由名称必须唯一：前端按名去重，撞名会导致其中一条路由被静默跳过。
    /// </summary>
    [Fact]
    public void RouteNames_ShouldBeUnique()
    {
        var duplicated = PageRegistry.All
            .GroupBy(page => page.RouteName, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key} ← {string.Join(", ", group.Select(page => page.Code))}")
            .ToList();

        Assert.True(duplicated.Count == 0, $"路由名称重复：{string.Join(" | ", duplicated)}");
    }

    /// <summary>
    /// 路由路径必须以斜杠开头（前端按绝对路径注册）。
    /// </summary>
    [Fact]
    public void PagePaths_ShouldStartWithSlash()
    {
        var offenders = PageRegistry.All
            .Where(page => !page.Path.StartsWith('/'))
            .Select(page => $"{page.Code}={page.Path}")
            .ToList();

        Assert.True(offenders.Count == 0, $"以下页面的路由路径未以 / 开头：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// 父页面码必须存在，且必须排在子项之前——种子依登记顺序解析 ParentId，父在后就解析不到。
    /// </summary>
    [Fact]
    public void ParentCodes_ShouldExistAndBeDeclaredBeforeChildren()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var offenders = new List<string>();

        foreach (var page in PageRegistry.All)
        {
            if (page.ParentCode is not null && !seen.Contains(page.ParentCode))
            {
                offenders.Add($"{page.Code} 的父项 {page.ParentCode} 不存在或排在它之后");
            }

            seen.Add(page.Code);
        }

        Assert.True(offenders.Count == 0, $"父子登记顺序违约：{string.Join(" | ", offenders)}");
    }

    /// <summary>
    /// 页面不能以自身为父项（自引用会让种子解析出无限层级）。
    /// </summary>
    [Fact]
    public void Pages_ShouldNotReferenceThemselvesAsParent()
    {
        var offenders = PageRegistry.All
            .Where(page => string.Equals(page.Code, page.ParentCode, StringComparison.Ordinal))
            .Select(page => page.Code)
            .ToList();

        Assert.True(offenders.Count == 0, $"以下页面把自己登记成了父项：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// 目录项是纯容器：不落组件、不挂权限码，并且必须给出重定向目标（否则点目录进空白页）。
    /// </summary>
    [Fact]
    public void Directories_ShouldHaveNoComponentNoPermissionAndARedirect()
    {
        var offenders = PageRegistry.All
            .Where(page => page.MenuType == MenuType.Directory)
            .Where(page => page.Component is not null || page.PermissionCode is not null || string.IsNullOrWhiteSpace(page.Redirect))
            .Select(page => $"{page.Code}(Component={page.Component ?? "null"}, Permission={page.PermissionCode ?? "null"}, Redirect={page.Redirect ?? "null"})")
            .ToList();

        Assert.True(offenders.Count == 0, $"目录项必须无组件、无权限码且有重定向：{string.Join(" | ", offenders)}");
    }

    /// <summary>
    /// 目录项的重定向目标必须是登记表里真实存在的路径，否则跳转到 404。
    /// </summary>
    [Fact]
    public void DirectoryRedirects_ShouldPointToRegisteredPaths()
    {
        var paths = PageRegistry.All.Select(page => page.Path).ToHashSet(StringComparer.Ordinal);

        var offenders = PageRegistry.All
            .Where(page => page.MenuType == MenuType.Directory && page.Redirect is not null)
            .Where(page => !paths.Contains(page.Redirect!))
            .Select(page => $"{page.Code} → {page.Redirect}")
            .ToList();

        Assert.True(offenders.Count == 0, $"目录重定向指向了未登记的路径：{string.Join(" | ", offenders)}");
    }

    /// <summary>
    /// 非外链菜单必须有组件路径，否则前端解析不出可渲染的视图。
    /// </summary>
    [Fact]
    public void InternalMenus_ShouldDeclareComponent()
    {
        var offenders = PageRegistry.All
            .Where(page => page.MenuType == MenuType.Menu && !page.IsExternal)
            .Where(page => string.IsNullOrWhiteSpace(page.Component))
            .Select(page => page.Code)
            .ToList();

        Assert.True(offenders.Count == 0, $"以下内部菜单缺少组件路径：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// 外链菜单必须给出外链地址且不落组件；反之非外链菜单不得携带外链地址。
    /// </summary>
    [Fact]
    public void ExternalMenus_ShouldCarryUrlAndNoComponent()
    {
        var missingUrl = PageRegistry.All
            .Where(page => page.IsExternal && string.IsNullOrWhiteSpace(page.ExternalUrl))
            .Select(page => page.Code)
            .ToList();
        Assert.True(missingUrl.Count == 0, $"以下外链菜单缺少外链地址：{string.Join(", ", missingUrl)}");

        var strayUrl = PageRegistry.All
            .Where(page => !page.IsExternal && !string.IsNullOrWhiteSpace(page.ExternalUrl))
            .Select(page => page.Code)
            .ToList();
        Assert.True(strayUrl.Count == 0, $"以下非外链菜单却带了外链地址：{string.Join(", ", strayUrl)}");

        var externalWithComponent = PageRegistry.All
            .Where(page => page.IsExternal && page.Component is not null)
            .Select(page => page.Code)
            .ToList();
        Assert.True(externalWithComponent.Count == 0, $"外链菜单不应落组件：{string.Join(", ", externalWithComponent)}");
    }

    /// <summary>
    /// 外链地址必须是 http/https 绝对地址，避免被当成站内相对路径打开。
    /// </summary>
    [Fact]
    public void ExternalUrls_ShouldBeAbsoluteHttpUrls()
    {
        var offenders = PageRegistry.All
            .Where(page => page.IsExternal)
            .Where(page => !Uri.TryCreate(page.ExternalUrl, UriKind.Absolute, out var uri)
                           || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            .Select(page => $"{page.Code}={page.ExternalUrl}")
            .ToList();

        Assert.True(offenders.Count == 0, $"外链地址必须是 http(s) 绝对地址：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// 组件路径约定：等于 Path 去前导斜杠再追加 /index。
    /// </summary>
    /// <remarks>
    /// 唯一豁免是 <c>_core/</c> 开头的核心页——它们由前端 dynamic.ts 的 coreComponentMap 解析，
    /// 不需要在 src/views 下落盘，因此不受路径映射约束。除此之外一旦分叉，前端就会去加载一个不存在的视图。
    /// </remarks>
    [Fact]
    public void Components_ShouldMirrorPathExceptCorePages()
    {
        var offenders = PageRegistry.All
            .Where(page => page.Component is not null)
            .Where(page => !page.Component!.StartsWith("_core/", StringComparison.Ordinal))
            .Where(page => !string.Equals(page.Component, page.Path.TrimStart('/') + "/index", StringComparison.Ordinal))
            .Select(page => $"{page.Code}: Path={page.Path} 但 Component={page.Component}")
            .ToList();

        Assert.True(offenders.Count == 0, $"组件路径与路由路径不一致：{string.Join(" | ", offenders)}");
    }

    /// <summary>
    /// 组件路径一律相对 src/views，不得以斜杠开头，也不得带文件扩展名。
    /// </summary>
    [Fact]
    public void Components_ShouldBeExtensionlessRelativePaths()
    {
        var offenders = PageRegistry.All
            .Where(page => page.Component is not null)
            .Where(page => page.Component!.StartsWith('/') || page.Component.EndsWith(".vue", StringComparison.Ordinal))
            .Select(page => $"{page.Code}={page.Component}")
            .ToList();

        Assert.True(offenders.Count == 0, $"组件路径必须是无扩展名的相对路径：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// 国际化键必须落在 menu.* 命名空间下，并与前端 locales 的 menu.ts 对齐。
    /// </summary>
    [Fact]
    public void I18nKeys_ShouldLiveUnderMenuNamespace()
    {
        var offenders = PageRegistry.All
            .Where(page => page.I18nKey is not null)
            .Where(page => !page.I18nKey!.StartsWith("menu.", StringComparison.Ordinal))
            .Select(page => $"{page.Code}={page.I18nKey}")
            .ToList();

        Assert.True(offenders.Count == 0, $"国际化键必须以 menu. 开头：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// 国际化键的默认命名规则：menu.{页面码中的 . 与 - 替换为 _}。
    /// </summary>
    /// <remarks>
    /// 个人中心刻意复用顶栏共用的 <c>menu.profile</c>，是登记表注释里写明的唯一例外；
    /// 除它之外任何偏离都意味着前端 locales 里会缺一条对应文案。
    /// </remarks>
    [Fact]
    public void I18nKeys_ShouldBeDerivedFromPageCode()
    {
        var offenders = PageRegistry.All
            .Where(page => page.I18nKey is not null)
            .Where(page => !string.Equals(page.Code, "workbench.profile", StringComparison.Ordinal))
            .Where(page => !string.Equals(
                page.I18nKey,
                "menu." + page.Code.Replace('.', '_').Replace('-', '_'),
                StringComparison.Ordinal))
            .Select(page => $"{page.Code} 期望 menu.{page.Code.Replace('.', '_').Replace('-', '_')} 实际 {page.I18nKey}")
            .ToList();

        Assert.True(offenders.Count == 0, $"国际化键命名偏离约定：{string.Join(" | ", offenders)}");
    }

    /// <summary>
    /// 国际化键必须唯一（复用 menu.profile 的个人中心除外），否则两个菜单会显示同一条文案。
    /// </summary>
    [Fact]
    public void I18nKeys_ShouldBeUniqueExceptSharedProfileKey()
    {
        var duplicated = PageRegistry.All
            .Where(page => page.I18nKey is not null)
            .GroupBy(page => page.I18nKey!, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key} ← {string.Join(", ", group.Select(page => page.Code))}")
            .ToList();

        Assert.True(duplicated.Count == 0, $"国际化键重复：{string.Join(" | ", duplicated)}");
    }

    /// <summary>
    /// 所有页面都必须有标题与图标（种子直接落库，空值会在菜单树里显示成空白行）。
    /// </summary>
    [Fact]
    public void Pages_ShouldHaveTitleAndIcon()
    {
        var offenders = PageRegistry.All
            .Where(page => string.IsNullOrWhiteSpace(page.Title) || string.IsNullOrWhiteSpace(page.Icon))
            .Select(page => page.Code)
            .ToList();

        Assert.True(offenders.Count == 0, $"以下页面缺少标题或图标：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// 登记表不产出按钮类型节点——按钮统一走 <see cref="PageRegistry.Buttons"/>。
    /// </summary>
    [Fact]
    public void Pages_ShouldNotDeclareButtonMenuType()
    {
        var offenders = PageRegistry.All
            .Where(page => page.MenuType == MenuType.Button)
            .Select(page => page.Code)
            .ToList();

        Assert.True(offenders.Count == 0, $"按钮必须登记在 Buttons 中，不能出现在页面表：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// 页面权限码必须是会被种子播下去的权限，杜绝"菜单挂了个库里不存在的权限码 → 永远无人可见"。
    /// </summary>
    /// <remarks>
    /// 基准取 <see cref="SaasPermissionDefinitions.All"/>（种子落库的扁平清单）而非
    /// <see cref="SaasPermissionCodes.All"/>：后者是手写枚举，目前漏登了 <c>saas:log-trace:read</c>，
    /// 而决定"权限在库里存不存在"的是前者。
    /// </remarks>
    [Fact]
    public void PagePermissionCodes_ShouldBeSeededPermissions()
    {
        var seeded = SaasPermissionDefinitions.All
            .Select(definition => definition.PermissionCode)
            .ToHashSet(StringComparer.Ordinal);

        var offenders = PageRegistry.All
            .Where(page => page.PermissionCode is not null)
            .Where(page => !seeded.Contains(page.PermissionCode!))
            .Select(page => $"{page.Code}={page.PermissionCode}")
            .ToList();

        Assert.True(offenders.Count == 0, $"页面引用了不会被播种的权限码：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// 回归锚点：<see cref="SaasPermissionCodes.All"/> 自称"全部权限码"，但当前漏登了链路追踪读权限。
    /// </summary>
    /// <remarks>
    /// 这条锁定的是**当前真实行为**而非期望行为——目前 src 内无人消费 <c>All</c>，故只是清单不完整；
    /// 一旦有人补上这条常量（或把 <c>All</c> 改成由 <see cref="SaasPermissionDefinitions"/> 派生），
    /// 本用例会红，提醒把这段说明一并删掉。
    /// </remarks>
    [Fact]
    public void SaasPermissionCodesAll_CurrentlyOmitsLogTraceRead()
    {
        var seeded = SaasPermissionDefinitions.All
            .Select(definition => definition.PermissionCode)
            .ToHashSet(StringComparer.Ordinal);
        var enumerated = SaasPermissionCodes.All.ToHashSet(StringComparer.Ordinal);

        var missing = seeded.Except(enumerated, StringComparer.Ordinal).OrderBy(code => code, StringComparer.Ordinal).ToList();

        Assert.Equal([SaasPermissionCodes.LogTrace.Read], missing);
    }

    /// <summary>
    /// 按钮码必须唯一。
    /// </summary>
    [Fact]
    public void ButtonCodes_ShouldBeUnique()
    {
        var duplicated = PageRegistry.Buttons
            .GroupBy(button => button.Code, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key}（{group.Count()} 次）")
            .ToList();

        Assert.True(duplicated.Count == 0, $"按钮码重复：{string.Join(", ", duplicated)}");
    }

    /// <summary>
    /// 按钮码不得与页面码撞车——两者最终落在同一张菜单表里。
    /// </summary>
    [Fact]
    public void ButtonCodes_ShouldNotCollideWithPageCodes()
    {
        var pageCodes = PageRegistry.All.Select(page => page.Code).ToHashSet(StringComparer.Ordinal);

        var offenders = PageRegistry.Buttons
            .Where(button => pageCodes.Contains(button.Code))
            .Select(button => button.Code)
            .ToList();

        Assert.True(offenders.Count == 0, $"按钮码与页面码冲突：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// 按钮的所属页面必须真实存在，且必须是 Menu 类型（目录下不挂按钮）。
    /// </summary>
    [Fact]
    public void ButtonParents_ShouldBeRegisteredMenuPages()
    {
        var menuPages = PageRegistry.All
            .Where(page => page.MenuType == MenuType.Menu)
            .Select(page => page.Code)
            .ToHashSet(StringComparer.Ordinal);

        var offenders = PageRegistry.Buttons
            .Where(button => !menuPages.Contains(button.ParentCode))
            .Select(button => $"{button.Code} → {button.ParentCode}")
            .ToList();

        Assert.True(offenders.Count == 0, $"按钮挂到了不存在或非菜单类型的页面上：{string.Join(" | ", offenders)}");
    }

    /// <summary>
    /// 按钮码约定为 <c>{页面码}.{动作}</c>，从码本身即可反查所属页面。
    /// </summary>
    [Fact]
    public void ButtonCodes_ShouldBePrefixedByParentCode()
    {
        var offenders = PageRegistry.Buttons
            .Where(button => !button.Code.StartsWith(button.ParentCode + ".", StringComparison.Ordinal))
            .Select(button => $"{button.Code}（父项 {button.ParentCode}）")
            .ToList();

        Assert.True(offenders.Count == 0, $"按钮码未以所属页面码为前缀：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// 每个按钮都必须挂权限码，且必须是已声明常量——按钮是写操作入口，无码即无门。
    /// </summary>
    [Fact]
    public void ButtonPermissionCodes_ShouldBeDeclaredAndNonEmpty()
    {
        var seeded = SaasPermissionDefinitions.All
            .Select(definition => definition.PermissionCode)
            .ToHashSet(StringComparer.Ordinal);

        var offenders = PageRegistry.Buttons
            .Where(button => string.IsNullOrWhiteSpace(button.PermissionCode) || !seeded.Contains(button.PermissionCode))
            .Select(button => $"{button.Code}={button.PermissionCode}")
            .ToList();

        Assert.True(offenders.Count == 0, $"按钮权限码缺失或未声明：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// 同一页面内的按钮排序值不得重复，否则菜单里按钮顺序不稳定。
    /// </summary>
    [Fact]
    public void ButtonSorts_ShouldBeUniqueWithinTheSamePage()
    {
        var offenders = PageRegistry.Buttons
            .GroupBy(button => button.ParentCode, StringComparer.Ordinal)
            .SelectMany(page => page
                .GroupBy(button => button.Sort)
                .Where(sortGroup => sortGroup.Count() > 1)
                .Select(sortGroup => $"{page.Key} 的排序 {sortGroup.Key} ← {string.Join(", ", sortGroup.Select(button => button.Code))}"))
            .ToList();

        Assert.True(offenders.Count == 0, $"同页面按钮排序值重复：{string.Join(" | ", offenders)}");
    }

    /// <summary>
    /// 按钮标题不得为空。
    /// </summary>
    [Fact]
    public void Buttons_ShouldHaveTitle()
    {
        var offenders = PageRegistry.Buttons
            .Where(button => string.IsNullOrWhiteSpace(button.Title))
            .Select(button => button.Code)
            .ToList();

        Assert.True(offenders.Count == 0, $"以下按钮缺少标题：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// 审批目录码常量必须真的对应登记表里那条目录，常量与数据不能各说各话。
    /// </summary>
    [Fact]
    public void ApprovalDirectoryCode_ShouldMatchARegisteredDirectory()
    {
        var directory = PageRegistry.All.SingleOrDefault(page =>
            string.Equals(page.Code, PageRegistry.ApprovalDirectoryCode, StringComparison.Ordinal));

        Assert.NotNull(directory);
        Assert.Equal(MenuType.Directory, directory!.MenuType);
    }

    /// <summary>
    /// 描述符是值语义的 record：字段相同即相等，供种子做幂等比对。
    /// </summary>
    [Fact]
    public void Descriptors_ShouldUseValueEquality()
    {
        var left = new ButtonDescriptor("a.b", "新增", "a", "saas:a:create", 1);
        var right = new ButtonDescriptor("a.b", "新增", "a", "saas:a:create", 1);

        Assert.Equal(left, right);
        Assert.NotEqual(left, right with { Sort = 2 });
    }

    /// <summary>
    /// 页面描述符的可选参数默认值：默认开启组件缓存、不固定标签页、非外链。
    /// </summary>
    [Fact]
    public void PageDescriptor_OptionalDefaults_ShouldKeepCacheOnAndExternalOff()
    {
        var page = new PageDescriptor(
            "demo", "演示", "menu.demo", MenuType.Menu, "/demo", "Demo", "demo/index", null, null, "lucide:box", 1);

        Assert.True(page.IsCache);
        Assert.False(page.IsAffix);
        Assert.False(page.IsExternal);
        Assert.Null(page.Redirect);
        Assert.Null(page.ExternalUrl);
    }
}
