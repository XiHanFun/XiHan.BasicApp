// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;
using XiHan.BasicApp.AI.Domain.Enums;
using XiHan.BasicApp.AI.Domain.Permissions;
using XiHan.BasicApp.Saas.Application.Pages;
using XiHan.BasicApp.Saas.Domain.Entities;
using AiPageRegistry = XiHan.BasicApp.AI.Application.Pages.PageRegistry;

namespace XiHan.BasicApp.AI.Tests;

/// <summary>
/// AI 模块对外契约目录的结构约束测试：枚举取值、权限码命名与唯一性、页面登记表的自洽性。
/// </summary>
/// <remarks>
/// 这三类常量是"前后端 + 种子数据 + 鉴权"三方共用的事实源：
/// 枚举值变动会让历史数据语义漂移，权限码写错会静默 403，页面码/路由重复会让菜单种子建出错乱的树。
/// 全部断言均带列名式失败消息，违规项一次列全。
/// </remarks>
public sealed class AiContractCatalogTests
{
    /// <summary>
    /// 权限码常量类家族（四类资源各一份）。
    /// </summary>
    private static readonly Type[] AllPermissionCodeTypes =
    [
        typeof(AiPermissionCodes),
        typeof(AiAssistantPermissionCodes),
        typeof(AiPromptPermissionCodes),
        typeof(KnowledgePermissionCodes)
    ];

    /// <summary>
    /// 权限码常量类家族（供 [Theory] 逐类检查）。
    /// </summary>
    public static TheoryData<Type> PermissionCodeTypes
    {
        get
        {
            var data = new TheoryData<Type>();
            foreach (var type in AllPermissionCodeTypes)
            {
                data.Add(type);
            }

            return data;
        }
    }

    /// <summary>
    /// 来源类型的数值一经落库不可变更，改了会让历史行的来源语义整体漂移。
    /// </summary>
    [Fact]
    public void KnowledgeSourceType_ValuesShouldBeStable()
    {
        Assert.Equal(0, (int)KnowledgeSourceType.PasteText);
        Assert.Equal(1, (int)KnowledgeSourceType.UploadFile);
        Assert.Equal(
            new[] { KnowledgeSourceType.PasteText, KnowledgeSourceType.UploadFile },
            Enum.GetValues<KnowledgeSourceType>());
    }

    /// <summary>
    /// 索引状态的数值一经落库不可变更，改了会让历史行的索引状态整体漂移。
    /// </summary>
    [Fact]
    public void KnowledgeIndexStatus_ValuesShouldBeStable()
    {
        Assert.Equal(0, (int)KnowledgeIndexStatus.Pending);
        Assert.Equal(1, (int)KnowledgeIndexStatus.Indexed);
        Assert.Equal(2, (int)KnowledgeIndexStatus.Failed);
        Assert.Equal(
            new[] { KnowledgeIndexStatus.Pending, KnowledgeIndexStatus.Indexed, KnowledgeIndexStatus.Failed },
            Enum.GetValues<KnowledgeIndexStatus>());
    }

    /// <summary>
    /// 两个枚举的 0 值必须分别是"粘贴文本"与"待索引"：实体默认值与数据库列默认值都靠它。
    /// </summary>
    [Fact]
    public void KnowledgeEnums_DefaultValueShouldBeTheSafeInitialState()
    {
        Assert.Equal(KnowledgeSourceType.PasteText, default(KnowledgeSourceType));
        Assert.Equal(KnowledgeIndexStatus.Pending, default(KnowledgeIndexStatus));
    }

    /// <summary>
    /// 每个枚举成员都必须带 <see cref="DescriptionAttribute"/>，前端字典与导出列靠它取中文名。
    /// </summary>
    /// <param name="enumTypeName">被检查的枚举类型全名。</param>
    [Theory]
    [InlineData("KnowledgeSourceType")]
    [InlineData("KnowledgeIndexStatus")]
    public void KnowledgeEnums_EveryMemberShouldCarryDescription(string enumTypeName)
    {
        var enumType = string.Equals(enumTypeName, "KnowledgeSourceType", StringComparison.Ordinal)
            ? typeof(KnowledgeSourceType)
            : typeof(KnowledgeIndexStatus);
        var missing = enumType
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => string.IsNullOrWhiteSpace(field.GetCustomAttribute<DescriptionAttribute>()?.Description))
            .Select(field => field.Name)
            .ToList();

        Assert.True(missing.Count == 0, $"{enumType.Name} 缺少 Description 的成员：{string.Join("、", missing)}。");
    }

    /// <summary>
    /// 权限码常量必须严格是「资源码:操作码」拼接，任何一处写歪都会在鉴权时静默 403。
    /// </summary>
    /// <param name="codesType">被检查的权限码常量类。</param>
    [Theory]
    [MemberData(nameof(PermissionCodeTypes))]
    public void PermissionCodes_EveryCodeShouldBeResourcePrefixed(Type codesType)
    {
        var resource = (string)codesType.GetField("Resource", BindingFlags.Public | BindingFlags.Static)!.GetRawConstantValue()!;
        var violations = PermissionCodeConstants(codesType)
            .Where(item => !string.Equals(item.Value, $"{resource}:{item.Name.ToLowerInvariant()}", StringComparison.Ordinal))
            .Select(item => $"{item.Name}={item.Value}（应为 {resource}:{item.Name.ToLowerInvariant()}）")
            .ToList();

        Assert.True(violations.Count == 0, $"{codesType.Name} 权限码与「资源:操作」约定不符：{string.Join("；", violations)}。");
    }

    /// <summary>
    /// 权限码必须全小写、以下划线分词，且只含一个冒号——种子数据按这个形状拆解资源与操作。
    /// </summary>
    /// <param name="codesType">被检查的权限码常量类。</param>
    [Theory]
    [MemberData(nameof(PermissionCodeTypes))]
    public void PermissionCodes_ShouldFollowLowerSnakeNamingConvention(Type codesType)
    {
        var pattern = new Regex("^[a-z][a-z0-9_]*:[a-z]+$", RegexOptions.CultureInvariant);
        var violations = PermissionCodeConstants(codesType)
            .Where(item => !pattern.IsMatch(item.Value))
            .Select(item => $"{item.Name}={item.Value}")
            .ToList();

        Assert.True(violations.Count == 0, $"{codesType.Name} 权限码命名不合规：{string.Join("；", violations)}。");
    }

    /// <summary>
    /// 资源码必须全小写、以下划线分词，且四类资源互不相同（资源码撞车会让两块功能共用一套权限）。
    /// </summary>
    [Fact]
    public void PermissionCodes_ResourceCodesShouldBeDistinctAndLowerSnake()
    {
        var pattern = new Regex("^[a-z][a-z0-9_]*$", RegexOptions.CultureInvariant);
        var resources = AllPermissionCodeTypes
            .Select(type => (Type: type, Resource: (string)type.GetField("Resource", BindingFlags.Public | BindingFlags.Static)!.GetRawConstantValue()!))
            .ToList();
        var badFormat = resources.Where(item => !pattern.IsMatch(item.Resource)).Select(item => $"{item.Type.Name}={item.Resource}").ToList();
        var duplicated = resources
            .GroupBy(item => item.Resource, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key}（{string.Join("、", group.Select(item => item.Type.Name))}）")
            .ToList();

        Assert.True(badFormat.Count == 0, $"资源码命名不合规：{string.Join("；", badFormat)}。");
        Assert.True(duplicated.Count == 0, $"资源码重复：{string.Join("；", duplicated)}。");
    }

    /// <summary>
    /// 四类权限码合并后必须两两不重复，重复会让一个权限点被两处种子重复插入而撞唯一约束。
    /// </summary>
    [Fact]
    public void PermissionCodes_AllCodesAcrossModulesShouldBeUnique()
    {
        var all = AllPermissionCodeTypes
            .SelectMany(type => PermissionCodeConstants(type).Select(item => (Owner: type.Name, item.Value)))
            .ToList();
        var duplicated = all
            .GroupBy(item => item.Value, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key}（{string.Join("、", group.Select(item => item.Owner))}）")
            .ToList();

        Assert.True(duplicated.Count == 0, $"权限码跨类重复：{string.Join("；", duplicated)}。");
    }

    /// <summary>
    /// AI 模块编码固定为 <c>ai</c>：四类资源的权限点都以它作为 ModuleCode 落库，改了会让模块筛选失效。
    /// </summary>
    [Fact]
    public void AiPermissionCodes_ModuleShouldBeStable()
    {
        Assert.Equal("ai", AiPermissionCodes.Module, StringComparer.Ordinal);
        Assert.Equal("ai", AiPermissionCodes.Resource, StringComparer.Ordinal);
    }

    /// <summary>
    /// provider 与知识库必须各带一个 execute 权限（测试连接 / 检索问答），助手与提示词库则只有四个 CRUD 权限。
    /// </summary>
    [Fact]
    public void PermissionCodes_ExecuteShouldExistOnlyWhereThereIsAnExecutableAction()
    {
        Assert.Equal("ai:execute", AiPermissionCodes.Execute, StringComparer.Ordinal);
        Assert.Equal("knowledge_base:execute", KnowledgePermissionCodes.Execute, StringComparer.Ordinal);
        Assert.Null(typeof(AiAssistantPermissionCodes).GetField("Execute", BindingFlags.Public | BindingFlags.Static));
        Assert.Null(typeof(AiPromptPermissionCodes).GetField("Execute", BindingFlags.Public | BindingFlags.Static));
    }

    /// <summary>
    /// 页面登记表不得为空，且父目录必须排在全部子项之前——种子按顺序解析 ParentId，顺序错了子菜单会挂空。
    /// </summary>
    [Fact]
    public void PageRegistry_ParentDirectoryShouldPrecedeItsChildren()
    {
        var pages = AiPageRegistry.All;

        Assert.NotEmpty(pages);
        Assert.Same(AiPageRegistry.AiAppDirectory, pages[0]);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var orphans = new List<string>();
        foreach (var page in pages)
        {
            if (page.ParentCode is not null && !seen.Contains(page.ParentCode))
            {
                orphans.Add($"{page.Code}→{page.ParentCode}");
            }

            _ = seen.Add(page.Code);
        }

        Assert.True(orphans.Count == 0, $"以下页面的父目录未提前登记：{string.Join("、", orphans)}。");
    }

    /// <summary>
    /// 页面码、路由名与路由路径必须各自唯一，任何一项撞车都会让菜单树或前端路由静默覆盖。
    /// </summary>
    [Fact]
    public void PageRegistry_CodeRouteNameAndPathShouldBeUnique()
    {
        AssertUnique(AiPageRegistry.All.Select(page => page.Code), "页面码");
        AssertUnique(AiPageRegistry.All.Select(page => page.RouteName), "路由名");
        AssertUnique(AiPageRegistry.All.Select(page => page.Path), "路由路径");
        AssertUnique(AiPageRegistry.All.Select(page => page.Sort.ToString(System.Globalization.CultureInfo.InvariantCulture)), "排序值");
    }

    /// <summary>
    /// 国际化键必须严格是 <c>menu.{页面码}</c>（点与连字符替换为下划线），否则前端菜单会退化成显示原始 key。
    /// </summary>
    [Fact]
    public void PageRegistry_I18nKeyShouldFollowMenuNamingConvention()
    {
        var violations = AiPageRegistry.All
            .Where(page => !string.Equals(
                page.I18nKey,
                "menu." + page.Code.Replace('.', '_').Replace('-', '_'),
                StringComparison.Ordinal))
            .Select(page => $"{page.Code}={page.I18nKey}")
            .ToList();

        Assert.True(violations.Count == 0, $"以下页面的国际化键不合规：{string.Join("；", violations)}。");
    }

    /// <summary>
    /// 目录节点不得绑定组件与权限；菜单节点必须同时给出组件与权限码，否则会出现"点得开但没权限"的空白页。
    /// </summary>
    [Fact]
    public void PageRegistry_DirectoryAndMenuNodesShouldCarryTheirRequiredFields()
    {
        var directoryViolations = AiPageRegistry.All
            .Where(page => page.MenuType == MenuType.Directory)
            .Where(page => page.Component is not null || page.PermissionCode is not null)
            .Select(page => page.Code)
            .ToList();
        var menuViolations = AiPageRegistry.All
            .Where(page => page.MenuType == MenuType.Menu)
            .Where(page => string.IsNullOrWhiteSpace(page.Component) || string.IsNullOrWhiteSpace(page.PermissionCode))
            .Select(page => page.Code)
            .ToList();

        Assert.True(directoryViolations.Count == 0, $"目录节点不应绑定组件或权限：{string.Join("、", directoryViolations)}。");
        Assert.True(menuViolations.Count == 0, $"菜单节点缺少组件或权限码：{string.Join("、", menuViolations)}。");
    }

    /// <summary>
    /// 每个菜单绑定的权限码都必须是本模块四类常量里真实存在的查看权限，写错即静默 403。
    /// </summary>
    [Fact]
    public void PageRegistry_MenuPermissionCodesShouldExistInPermissionConstants()
    {
        var known = AllPermissionCodeTypes
            .SelectMany(type => PermissionCodeConstants(type).Select(item => item.Value))
            .ToHashSet(StringComparer.Ordinal);
        var unknown = AiPageRegistry.All
            .Where(page => page.PermissionCode is not null && !known.Contains(page.PermissionCode))
            .Select(page => $"{page.Code}={page.PermissionCode}")
            .ToList();

        Assert.True(unknown.Count == 0, $"以下菜单绑定了不存在的权限码：{string.Join("；", unknown)}。");
        Assert.Equal(
            new[] { AiAssistantPermissionCodes.Read, AiPromptPermissionCodes.Read, KnowledgePermissionCodes.Read, AiPermissionCodes.Read },
            AiPageRegistry.All.Where(page => page.PermissionCode is not null).Select(page => page.PermissionCode!).ToList(),
            StringComparer.Ordinal);
    }

    /// <summary>
    /// 每个页面都必须有非空图标与非空标题，缺一个前端菜单就会出现空白行。
    /// </summary>
    [Fact]
    public void PageRegistry_EveryPageShouldCarryTitleAndIcon()
    {
        var violations = AiPageRegistry.All
            .Where(page => string.IsNullOrWhiteSpace(page.Title) || string.IsNullOrWhiteSpace(page.Icon))
            .Select(page => page.Code)
            .ToList();

        Assert.True(violations.Count == 0, $"以下页面缺少标题或图标：{string.Join("、", violations)}。");
    }

    /// <summary>
    /// AI 应用目录必须是根节点（无父）且目录码与常量一致，子页面必须全部挂在它下面。
    /// </summary>
    [Fact]
    public void PageRegistry_AllMenusShouldHangUnderAiAppDirectory()
    {
        Assert.Equal("ai_app", AiPageRegistry.AiAppDirectoryCode, StringComparer.Ordinal);
        Assert.Equal(AiPageRegistry.AiAppDirectoryCode, AiPageRegistry.AiAppDirectory.Code, StringComparer.Ordinal);
        Assert.Null(AiPageRegistry.AiAppDirectory.ParentCode);
        Assert.Equal(MenuType.Directory, AiPageRegistry.AiAppDirectory.MenuType);

        var misplaced = AiPageRegistry.All
            .Where(page => page.MenuType == MenuType.Menu)
            .Where(page => !string.Equals(page.ParentCode, AiPageRegistry.AiAppDirectoryCode, StringComparison.Ordinal))
            .Select(page => $"{page.Code}→{page.ParentCode}")
            .ToList();

        Assert.True(misplaced.Count == 0, $"以下菜单未挂在 AI 应用目录下：{string.Join("、", misplaced)}。");
    }

    /// <summary>
    /// 本模块当前不登记任何按钮级权限：按钮表非 null 且为空，新增按钮时必须连带补权限种子。
    /// </summary>
    [Fact]
    public void PageRegistry_ButtonsShouldBeEmptyUntilButtonLevelPermissionsExist()
    {
        Assert.NotNull(AiPageRegistry.Buttons);
        Assert.Empty(AiPageRegistry.Buttons);
    }

    /// <summary>
    /// 页面登记表必须是只读快照：两次读取拿到同一实例，任何调用方都改不动这份事实源。
    /// </summary>
    [Fact]
    public void PageRegistry_AllShouldBeAStableReadOnlySnapshot()
    {
        Assert.Same(AiPageRegistry.All, AiPageRegistry.All);
        Assert.Same(AiPageRegistry.AiAppDirectory, AiPageRegistry.AiAppDirectory);
        Assert.IsAssignableFrom<IReadOnlyList<PageDescriptor>>(AiPageRegistry.All);
    }

    /// <summary>
    /// 断言一组取值互不重复，失败时把重复项逐个列出。
    /// </summary>
    /// <param name="values">待检查取值。</param>
    /// <param name="label">失败消息里的字段名。</param>
    private static void AssertUnique(IEnumerable<string> values, string label)
    {
        var duplicated = values
            .GroupBy(value => value, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        Assert.True(duplicated.Count == 0, $"{label}重复：{string.Join("、", duplicated)}。");
    }

    /// <summary>
    /// 取权限码常量类里的全部「权限码」常量（值含冒号的那些，排除模块码/资源码）。
    /// </summary>
    /// <param name="codesType">权限码常量类。</param>
    /// <returns>常量名与取值。</returns>
    private static IReadOnlyList<(string Name, string Value)> PermissionCodeConstants(Type codesType)
    {
        return [.. codesType
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (field.Name, Value: (string)field.GetRawConstantValue()!))
            .Where(item => item.Value.Contains(':', StringComparison.Ordinal))];
    }
}
