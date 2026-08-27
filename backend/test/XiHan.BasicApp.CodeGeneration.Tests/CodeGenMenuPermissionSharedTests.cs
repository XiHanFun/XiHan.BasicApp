// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Domain.Generation;

namespace XiHan.BasicApp.CodeGeneration.Tests;

/// <summary>
/// 菜单/权限二阶产物共享推导工具的测试。
/// </summary>
/// <remarks>
/// 四个二阶生成器（权限码常量 / 权限定义 / PageRegistry 片段 / 种子骨架）共用这一组推导。
/// 任何一条推导漂移，四份产物就会互相对不上——权限码常量类里写的是 A，种子里播的是 B，
/// 编译能过、运行期权限却永远命不中。这里逐条钉死推导口径。
/// </remarks>
public sealed class CodeGenMenuPermissionSharedTests
{
    /// <summary>
    /// 权限码的资源段取表名（snake，全局唯一），不做任何大小写或前缀加工。
    /// </summary>
    [Fact]
    public void Resource_ShouldBeRawTableName()
    {
        var context = CodeGenerationTestHelper.CreateContext(tableName: "Sys_Product");

        Assert.Equal("Sys_Product", CodeGenerationTestHelper.InvokeShared<string>("Resource", context), StringComparer.Ordinal);
    }

    /// <summary>
    /// 展示名优先取业务名，业务名空白时回退实体类名。
    /// </summary>
    /// <param name="businessName">业务名</param>
    /// <param name="expected">期望展示名</param>
    [Theory]
    [InlineData("产品", "产品")]
    [InlineData("  产品  ", "产品")]
    [InlineData(null, "SysProduct")]
    [InlineData("", "SysProduct")]
    [InlineData("   ", "SysProduct")]
    public void Display_ShouldPreferBusinessNameThenClassName(string? businessName, string expected)
    {
        var context = CodeGenerationTestHelper.CreateContext(businessName: businessName);

        Assert.Equal(expected, CodeGenerationTestHelper.InvokeShared<string>("Display", context), StringComparer.Ordinal);
    }

    /// <summary>
    /// 命名空间优先取表配置命名空间。
    /// </summary>
    [Fact]
    public void ResolveNamespace_ShouldPreferConfiguredNamespace()
    {
        var context = CodeGenerationTestHelper.CreateContext(namespaceValue: "  XiHan.BasicApp.Catalog  ");

        Assert.Equal(
            "XiHan.BasicApp.Catalog",
            CodeGenerationTestHelper.InvokeShared<string>("ResolveNamespace", context),
            StringComparer.Ordinal);
    }

    /// <summary>
    /// 命名空间为空时回退模块段。
    /// </summary>
    [Fact]
    public void ResolveNamespace_BlankNamespaceShouldFallBackToModuleSegment()
    {
        var context = CodeGenerationTestHelper.CreateContext(namespaceValue: "  ", moduleName: "Catalog");

        Assert.Equal("Catalog", CodeGenerationTestHelper.InvokeShared<string>("ResolveNamespace", context), StringComparer.Ordinal);
    }

    /// <summary>
    /// 命名空间与模块名都为空时，回退段若与类名同名必须加后缀去重。
    /// </summary>
    /// <remarks>
    /// 同名会让产物里的 <c>namespace SysProduct.Domain.Permissions</c> 与实体类 <c>SysProduct</c>
    /// 撞成 CS0118（同一个名字既是命名空间又是类型），编译直接不过。
    /// </remarks>
    [Fact]
    public void ResolveNamespace_ShouldNotCollideWithClassName()
    {
        var context = CodeGenerationTestHelper.CreateContext(namespaceValue: null, moduleName: null, className: "SysProduct");

        Assert.Equal(
            "SysProductGenerated",
            CodeGenerationTestHelper.InvokeShared<string>("ResolveNamespace", context),
            StringComparer.Ordinal);
    }

    /// <summary>
    /// 模块名恰好等于类名时同样触发去重。
    /// </summary>
    [Fact]
    public void ResolveNamespace_ModuleNameEqualToClassNameShouldAlsoBeDeduplicated()
    {
        var context = CodeGenerationTestHelper.CreateContext(namespaceValue: null, moduleName: "SysProduct", className: "SysProduct");

        Assert.Equal(
            "SysProductGenerated",
            CodeGenerationTestHelper.InvokeShared<string>("ResolveNamespace", context),
            StringComparer.Ordinal);
    }

    /// <summary>
    /// 模块段原样保留大小写；模块名空白时回退类名。
    /// </summary>
    /// <param name="moduleName">模块名</param>
    /// <param name="expected">期望模块段</param>
    [Theory]
    [InlineData("Catalog", "Catalog")]
    [InlineData("  Catalog  ", "Catalog")]
    [InlineData(null, "SysProduct")]
    [InlineData("", "SysProduct")]
    [InlineData("   ", "SysProduct")]
    public void ModuleSegment_ShouldTrimAndFallBackToClassName(string? moduleName, string expected)
    {
        var context = CodeGenerationTestHelper.CreateContext(moduleName: moduleName);

        Assert.Equal(expected, CodeGenerationTestHelper.InvokeShared<string>("ModuleSegment", context), StringComparer.Ordinal);
    }

    /// <summary>
    /// 模块段的 Pascal / 小写两种形态用于不同落点，必须分别稳定。
    /// </summary>
    [Fact]
    public void ModulePascalAndModuleLower_ShouldDeriveFromModuleSegment()
    {
        var context = CodeGenerationTestHelper.CreateContext(moduleName: "catalog");

        Assert.Equal("Catalog", CodeGenerationTestHelper.InvokeShared<string>("ModulePascal", context), StringComparer.Ordinal);
        Assert.Equal("catalog", CodeGenerationTestHelper.InvokeShared<string>("ModuleLower", context), StringComparer.Ordinal);
    }

    /// <summary>
    /// 组件路径必须对齐生成的 Vue 页面落点 src/views/{module}/{kebab}/index.vue。
    /// </summary>
    [Fact]
    public void Component_ShouldMatchGeneratedVuePageLocation()
    {
        var context = CodeGenerationTestHelper.CreateContext(moduleName: "Catalog", className: "SysProduct");

        Assert.Equal("catalog/sys-product/index", CodeGenerationTestHelper.InvokeShared<string>("Component", context), StringComparer.Ordinal);
        Assert.Equal("sys-product", CodeGenerationTestHelper.InvokeShared<string>("Kebab", context), StringComparer.Ordinal);
    }

    /// <summary>
    /// 路由名为「模块 Pascal + 类名」。
    /// </summary>
    [Fact]
    public void RouteName_ShouldConcatModulePascalAndClassName()
    {
        var context = CodeGenerationTestHelper.CreateContext(moduleName: "catalog", className: "SysProduct");

        Assert.Equal("CatalogSysProduct", CodeGenerationTestHelper.InvokeShared<string>("RouteName", context), StringComparer.Ordinal);
    }

    /// <summary>
    /// 生效动作集：读取基线 read 恒在，且排在最前。
    /// </summary>
    [Fact]
    public void EffectiveActions_ShouldAlwaysStartWithRead()
    {
        var context = CodeGenerationTestHelper.CreateContext(enabledActions: []);

        var actions = CodeGenerationTestHelper.InvokeShared<IReadOnlyList<string>>("EffectiveActions", context);

        Assert.Equal(["read"], actions);
    }

    /// <summary>
    /// 已启用写操作按原顺序追加在 read 之后。
    /// </summary>
    [Fact]
    public void EffectiveActions_ShouldAppendEnabledWriteActionsInOrder()
    {
        var context = CodeGenerationTestHelper.CreateContext(enabledActions: ["delete", "create"]);

        var actions = CodeGenerationTestHelper.InvokeShared<IReadOnlyList<string>>("EffectiveActions", context);

        Assert.Equal(["read", "delete", "create"], actions);
    }

    /// <summary>
    /// 重复动作必须去重；显式传入 read 也不会出现两次。
    /// </summary>
    [Fact]
    public void EffectiveActions_ShouldDeduplicate()
    {
        var context = CodeGenerationTestHelper.CreateContext(enabledActions: ["read", "create", "create"]);

        var actions = CodeGenerationTestHelper.InvokeShared<IReadOnlyList<string>>("EffectiveActions", context);

        Assert.Equal(["read", "create"], actions);
    }

    /// <summary>
    /// 动作元数据必须与平台操作字典 SysOperation 对齐（标题 / 是否审计 / 是否危险）。
    /// </summary>
    /// <param name="action">动作码</param>
    /// <param name="title">期望标题</param>
    /// <param name="requireAudit">期望是否审计</param>
    /// <param name="dangerous">期望是否危险</param>
    [Theory]
    [InlineData("read", "查看", false, false)]
    [InlineData("create", "创建", true, false)]
    [InlineData("update", "更新", true, false)]
    [InlineData("delete", "删除", true, true)]
    [InlineData("export", "导出", false, false)]
    [InlineData("import", "导入", true, false)]
    public void MetaOf_ShouldMatchPlatformOperationDictionary(string action, string title, bool requireAudit, bool dangerous)
    {
        var meta = InvokeMetaOf(action);

        Assert.Equal(title, meta.Title, StringComparer.Ordinal);
        Assert.Equal(requireAudit, meta.IsRequireAudit);
        Assert.Equal(dangerous, meta.IsDangerous);
    }

    /// <summary>
    /// 未知动作回退为「标题取原值、不审计、不危险」，不得抛异常。
    /// </summary>
    [Fact]
    public void MetaOf_UnknownActionShouldFallBackSafely()
    {
        var meta = InvokeMetaOf("approve");

        Assert.Equal("approve", meta.Title, StringComparer.Ordinal);
        Assert.False(meta.IsRequireAudit);
        Assert.False(meta.IsDangerous);
    }

    /// <summary>
    /// 空白段一律归 null，非空段去两端空格。
    /// </summary>
    /// <param name="value">入参</param>
    /// <param name="expected">期望结果</param>
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("  a  ", "a")]
    public void SafeSegment_ShouldNormalizeBlankToNull(string? value, string? expected)
    {
        Assert.Equal(expected, CodeGenerationTestHelper.InvokeShared<string?>("SafeSegment", value), StringComparer.Ordinal);
    }

    /// <summary>
    /// 首字母大写只动第一个字符，其余原样。
    /// </summary>
    /// <param name="value">入参</param>
    /// <param name="expected">期望结果</param>
    [Theory]
    [InlineData("read", "Read")]
    [InlineData("Read", "Read")]
    [InlineData("r", "R")]
    [InlineData("", "")]
    [InlineData("aBC", "ABC")]
    public void Pascalize_ShouldOnlyUpperCaseFirstChar(string value, string expected)
    {
        Assert.Equal(expected, CodeGenerationTestHelper.InvokeShared<string>("Pascalize", value), StringComparer.Ordinal);
    }

    /// <summary>
    /// kebab 转换需覆盖连写大写、下划线与数字边界。
    /// </summary>
    /// <param name="value">入参</param>
    /// <param name="expected">期望结果</param>
    [Theory]
    [InlineData("SysProduct", "sys-product")]
    [InlineData("HTTPServer", "http-server")]
    [InlineData("sys_product", "sys-product")]
    [InlineData("Sys2Product", "sys2-product")]
    [InlineData("", "")]
    [InlineData("A", "a")]
    public void Kebabize_ShouldHandleAcronymAndUnderscoreBoundaries(string value, string expected)
    {
        Assert.Equal(expected, CodeGenerationTestHelper.InvokeShared<string>("Kebabize", value), StringComparer.Ordinal);
    }

    /// <summary>
    /// 共享工具的 kebab 转换必须与 <see cref="NamingConventions.Kebabize"/> 保持同一结果。
    /// </summary>
    /// <remarks>两处各写一份正则，任何一处改动都可能让菜单 Path 与前端页面目录对不上。</remarks>
    /// <param name="value">入参</param>
    [Theory]
    [InlineData("SysProduct")]
    [InlineData("HTTPServer")]
    [InlineData("sys_product_item")]
    [InlineData("XMLHttpRequest")]
    public void Kebabize_ShouldAgreeWithNamingConventions(string value)
    {
        Assert.Equal(
            NamingConventions.Kebabize(value),
            CodeGenerationTestHelper.InvokeShared<string>("Kebabize", value),
            StringComparer.Ordinal);
    }

    /// <summary>
    /// 反射调用 internal 的 <c>MetaOf</c> 并把 internal 记录读成测试可断言的三元组。
    /// </summary>
    /// <param name="action">动作码</param>
    private static (string Title, bool IsRequireAudit, bool IsDangerous) InvokeMetaOf(string action)
    {
        var meta = CodeGenerationTestHelper.InvokeInternalStatic(
            "XiHan.BasicApp.CodeGeneration.Infrastructure.Generation.MenuPermissionArtifactShared",
            "MetaOf",
            action)!;

        var type = meta.GetType();
        return (
            (string)type.GetProperty("Title")!.GetValue(meta)!,
            (bool)type.GetProperty("IsRequireAudit")!.GetValue(meta)!,
            (bool)type.GetProperty("IsDangerous")!.GetValue(meta)!);
    }
}
