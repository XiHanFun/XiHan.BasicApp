// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

namespace XiHan.BasicApp.CodeGeneration.Tests;

/// <summary>
/// 菜单/权限四个二阶产物生成器的产物结构测试。
/// </summary>
/// <remarks>
/// 二阶产物是"待并入源码"的代码片段，不是运行时写库。它们之间存在硬引用关系：
/// 生成的 AppService 引用 <c>{Class}PermissionCodes</c>、种子骨架引用 <c>{Class}PermissionDefinitions</c>、
/// PageRegistry 片段同时引用权限码常量。任何一份产物的类名/路径/权限码拼法漂移，
/// 落地方复制过去就编译不过，或编译过了但权限码永远命不中。
/// 这里逐份钉住文件名、输出目录、模板编码、写入策略与关键内容。
/// </remarks>
public sealed class CodeGenArtifactGeneratorTests
{
    /// <summary>
    /// 权限码常量类与落地 README 必须成对产出，且都落在统一的二阶产物目录下。
    /// </summary>
    [Fact]
    public void MenuPermissionBuild_ShouldProducePermissionCodesAndReadme()
    {
        var context = CodeGenerationTestHelper.CreateContext();

        var artifacts = MenuPermissionArtifactGenerator.Build(context, []);

        Assert.Equal(2, artifacts.Count);
        Assert.Equal("SysProductPermissionCodes.cs", artifacts[0].FileName, StringComparer.Ordinal);
        Assert.Equal(
            CodeGenerationTestHelper.OutputFolder + "/SysProductPermissionCodes.cs",
            artifacts[0].RelativePath,
            StringComparer.Ordinal);
        Assert.Equal("README.md", artifacts[1].FileName, StringComparer.Ordinal);
        Assert.Equal(CodeGenerationTestHelper.OutputFolder + "/README.md", artifacts[1].RelativePath, StringComparer.Ordinal);
    }

    /// <summary>
    /// 二阶产物统一打同一个模板编码，且默认写入策略是"总是覆盖"（纯推导、无手写内容）。
    /// </summary>
    [Fact]
    public void MenuPermissionBuild_ShouldTagArtifactsAsAlwaysOverwrite()
    {
        var artifacts = MenuPermissionArtifactGenerator.Build(CodeGenerationTestHelper.CreateContext(), []);

        Assert.All(artifacts, artifact =>
        {
            Assert.Equal(CodeGenerationTestHelper.ArtifactTemplateCode, artifact.TemplateCode, StringComparer.Ordinal);
            Assert.Equal(ArtifactWriteMode.AlwaysOverwrite, artifact.WriteMode);
        });
    }

    /// <summary>
    /// 上下文为空必须直接拒绝。
    /// </summary>
    [Fact]
    public void MenuPermissionBuild_NullContextShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => MenuPermissionArtifactGenerator.Build(null!, []));
    }

    /// <summary>
    /// 冲突权限码集合为空引用必须直接拒绝（空集合与 null 是两种语义）。
    /// </summary>
    [Fact]
    public void MenuPermissionBuild_NullCollidingCodesShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(
            () => MenuPermissionArtifactGenerator.Build(CodeGenerationTestHelper.CreateContext(), null!));
    }

    /// <summary>
    /// 权限码常量类必须落在 {命名空间}.Domain.Permissions，并声明资源常量。
    /// </summary>
    [Fact]
    public void PermissionCodes_ShouldDeclareNamespaceClassAndResource()
    {
        var context = CodeGenerationTestHelper.CreateContext();

        var content = MenuPermissionArtifactGenerator.Build(context, [])[0].Content;

        Assert.Contains("namespace XiHan.BasicApp.Catalog.Domain.Permissions;", content, StringComparison.Ordinal);
        Assert.Contains("public static class SysProductPermissionCodes", content, StringComparison.Ordinal);
        Assert.Contains("public const string Resource = \"sys_product\";", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 权限码常量随已启用操作裁剪：未启用的动作不得出现常量。
    /// </summary>
    [Fact]
    public void PermissionCodes_ShouldOnlyContainEffectiveActions()
    {
        var context = CodeGenerationTestHelper.CreateContext(enabledActions: ["create"]);

        var content = MenuPermissionArtifactGenerator.Build(context, [])[0].Content;

        Assert.Contains("public const string Read = \"sys_product:read\";", content, StringComparison.Ordinal);
        Assert.Contains("public const string Create = \"sys_product:create\";", content, StringComparison.Ordinal);
        Assert.DoesNotContain("public const string Update", content, StringComparison.Ordinal);
        Assert.DoesNotContain("public const string Delete", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 一个写操作都没启用时，读取基线常量仍必须存在——否则生成的查询接口引用不到权限码。
    /// </summary>
    [Fact]
    public void PermissionCodes_ShouldAlwaysKeepReadBaseline()
    {
        var context = CodeGenerationTestHelper.CreateContext(enabledActions: []);

        var content = MenuPermissionArtifactGenerator.Build(context, [])[0].Content;

        Assert.Contains("public const string Read = \"sys_product:read\";", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 权限码一律两段式 {资源}:{操作}，资源段取表名。
    /// </summary>
    /// <param name="action">已启用动作</param>
    /// <param name="constantName">期望的常量名</param>
    [Theory]
    [InlineData("create", "Create")]
    [InlineData("update", "Update")]
    [InlineData("delete", "Delete")]
    public void PermissionCodes_ShouldUseTwoSegmentCodeFormat(string action, string constantName)
    {
        var context = CodeGenerationTestHelper.CreateContext(enabledActions: [action]);

        var content = MenuPermissionArtifactGenerator.Build(context, [])[0].Content;

        Assert.Contains($"public const string {constantName} = \"sys_product:{action}\";", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 无冲突时 README 不得出现冲突告警块。
    /// </summary>
    [Fact]
    public void Readme_WithoutCollisionShouldNotWarn()
    {
        var content = MenuPermissionArtifactGenerator.Build(CodeGenerationTestHelper.CreateContext(), [])[1].Content;

        Assert.DoesNotContain("权限码冲突", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 有冲突时 README 顶部必须逐条列出撞码的权限码。
    /// </summary>
    [Fact]
    public void Readme_WithCollisionShouldListEveryCollidingCode()
    {
        var content = MenuPermissionArtifactGenerator
            .Build(CodeGenerationTestHelper.CreateContext(), ["sys_product:read", "sys_product:create"])[1]
            .Content;

        Assert.Contains("权限码冲突", content, StringComparison.Ordinal);
        Assert.Contains("`sys_product:read`", content, StringComparison.Ordinal);
        Assert.Contains("`sys_product:create`", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// README 的产物清单必须覆盖同批产出的全部五个文件，落地方照单复制才不会漏。
    /// </summary>
    [Fact]
    public void Readme_ShouldListAllCompanionArtifacts()
    {
        var content = MenuPermissionArtifactGenerator.Build(CodeGenerationTestHelper.CreateContext(), [])[1].Content;

        Assert.Contains("`SysProductPermissionCodes.cs`", content, StringComparison.Ordinal);
        Assert.Contains("`SysProductPermissionDefinitions.cs`", content, StringComparison.Ordinal);
        Assert.Contains("`SysProductPermissionSeeder.cs`", content, StringComparison.Ordinal);
        Assert.Contains("`SysProductMenuSeeder.cs`", content, StringComparison.Ordinal);
        Assert.Contains("`SysProductPageRegistry.snippet.txt`", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// README 的菜单规格行必须与 PageRegistry 片段/菜单种子的推导一致。
    /// </summary>
    [Fact]
    public void Readme_ShouldDescribeMenuSpecificationConsistently()
    {
        var content = MenuPermissionArtifactGenerator.Build(CodeGenerationTestHelper.CreateContext(), [])[1].Content;

        Assert.Contains("MenuCode=`sys_product`", content, StringComparison.Ordinal);
        Assert.Contains("Path=`/catalog/sys-product`", content, StringComparison.Ordinal);
        Assert.Contains("Component=`catalog/sys-product/index`", content, StringComparison.Ordinal);
        Assert.Contains("RouteName=`CatalogSysProduct`", content, StringComparison.Ordinal);
        Assert.Contains("I18nKey=`menu.sys_product`", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 未配置父菜单时 README 说明为顶级菜单。
    /// </summary>
    [Fact]
    public void Readme_WithoutParentMenuShouldSayTopLevel()
    {
        var content = MenuPermissionArtifactGenerator.Build(CodeGenerationTestHelper.CreateContext(), [])[1].Content;

        Assert.Contains("顶级菜单", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 配置了父菜单时 README 必须把 ParentMenuId 带出来。
    /// </summary>
    [Fact]
    public void Readme_WithParentMenuShouldEchoParentMenuId()
    {
        var context = CodeGenerationTestHelper.CreateContext();
        context.Options["ParentMenuId"] = "801";

        var content = MenuPermissionArtifactGenerator.Build(context, [])[1].Content;

        Assert.Contains("ParentMenuId=`801`", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 权限定义类的文件名、目录与写入策略必须稳定。
    /// </summary>
    [Fact]
    public void PermissionDefinitions_ShouldBeOverwritableArtifactInSharedFolder()
    {
        var artifact = CodeGenerationTestHelper.BuildPermissionDefinitions(CodeGenerationTestHelper.CreateContext());

        Assert.Equal("SysProductPermissionDefinitions.cs", artifact.FileName, StringComparer.Ordinal);
        Assert.Equal(
            CodeGenerationTestHelper.OutputFolder + "/SysProductPermissionDefinitions.cs",
            artifact.RelativePath,
            StringComparer.Ordinal);
        Assert.Equal(CodeGenerationTestHelper.ArtifactTemplateCode, artifact.TemplateCode, StringComparer.Ordinal);
        Assert.Equal(ArtifactWriteMode.AlwaysOverwrite, artifact.WriteMode);
    }

    /// <summary>
    /// 权限定义类必须自包含四个登记常量：资源、模块、资源名、资源 API 路径。
    /// </summary>
    [Fact]
    public void PermissionDefinitions_ShouldDeclareResourceRegistrationConstants()
    {
        var content = CodeGenerationTestHelper.BuildPermissionDefinitions(CodeGenerationTestHelper.CreateContext()).Content;

        Assert.Contains("public const string Resource = \"sys_product\";", content, StringComparison.Ordinal);
        Assert.Contains("public const string Module = \"catalog\";", content, StringComparison.Ordinal);
        Assert.Contains("public const string ResourceName = \"产品\";", content, StringComparison.Ordinal);
        Assert.Contains("public const string ResourcePath = \"/api/catalog/sys-product\";", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 权限项按生效动作逐条产出，并带上与操作字典一致的审计标记。
    /// </summary>
    [Fact]
    public void PermissionDefinitions_ShouldEmitOneItemPerEffectiveActionWithAuditFlag()
    {
        var context = CodeGenerationTestHelper.CreateContext(enabledActions: ["delete"]);

        var content = CodeGenerationTestHelper.BuildPermissionDefinitions(context).Content;

        Assert.Contains("new(\"read\", \"产品-查看\", \"查看产品\", false),", content, StringComparison.Ordinal);
        Assert.Contains("new(\"delete\", \"产品-删除\", \"删除产品\", true),", content, StringComparison.Ordinal);
        Assert.DoesNotContain("new(\"create\"", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 权限定义类必须同时产出配套的权限项记录类型，保证文件自身可编译。
    /// </summary>
    [Fact]
    public void PermissionDefinitions_ShouldEmitCompanionItemRecord()
    {
        var content = CodeGenerationTestHelper.BuildPermissionDefinitions(CodeGenerationTestHelper.CreateContext()).Content;

        Assert.Contains(
            "public sealed record SysProductPermissionItem(string Action, string Name, string Description, bool IsRequireAudit);",
            content,
            StringComparison.Ordinal);
        Assert.Contains("IReadOnlyList<SysProductPermissionItem> Items", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// PageRegistry 片段的文件名与写入策略必须稳定（参考片段、纯推导，总是覆盖）。
    /// </summary>
    [Fact]
    public void PageRegistrySnippet_ShouldBeOverwritableTextArtifact()
    {
        var artifact = CodeGenerationTestHelper.BuildPageRegistrySnippet(CodeGenerationTestHelper.CreateContext());

        Assert.Equal("SysProductPageRegistry.snippet.txt", artifact.FileName, StringComparer.Ordinal);
        Assert.Equal(
            CodeGenerationTestHelper.OutputFolder + "/SysProductPageRegistry.snippet.txt",
            artifact.RelativePath,
            StringComparer.Ordinal);
        Assert.Equal(ArtifactWriteMode.AlwaysOverwrite, artifact.WriteMode);
    }

    /// <summary>
    /// 页面条目的页面码 / 路径 / 组件 / 路由名 / 权限码必须与共享推导一致。
    /// </summary>
    [Fact]
    public void PageRegistrySnippet_ShouldEmitPageDescriptorWithDerivedIdentity()
    {
        var content = CodeGenerationTestHelper.BuildPageRegistrySnippet(CodeGenerationTestHelper.CreateContext()).Content;

        Assert.Contains(
            "new(\"catalog.sys-product\", \"产品\", \"menu.sys_product\", MenuType.Menu, \"/catalog/sys-product\", \"CatalogSysProduct\",",
            content,
            StringComparison.Ordinal);
        Assert.Contains("\"catalog/sys-product/index\"", content, StringComparison.Ordinal);
        Assert.Contains("SysProductPermissionCodes.Read", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 按钮条目只取写操作，且按 Buttons 定义顺序从 1 递增编号。
    /// </summary>
    /// <remarks>查询/详情走列表页的读取权限，没有独立按钮，必须被跳过。</remarks>
    [Fact]
    public void PageRegistrySnippet_ShouldEmitWriteButtonsOnlyWithSequentialSort()
    {
        var content = CodeGenerationTestHelper.BuildPageRegistrySnippet(CodeGenerationTestHelper.CreateContext()).Content;

        Assert.Contains(
            "new(\"catalog.sys-product.create\", \"新增\", \"catalog.sys-product\", SysProductPermissionCodes.Create, 1),",
            content,
            StringComparison.Ordinal);
        Assert.Contains(
            "new(\"catalog.sys-product.update\", \"编辑\", \"catalog.sys-product\", SysProductPermissionCodes.Update, 2),",
            content,
            StringComparison.Ordinal);
        Assert.Contains(
            "new(\"catalog.sys-product.delete\", \"删除\", \"catalog.sys-product\", SysProductPermissionCodes.Delete, 3),",
            content,
            StringComparison.Ordinal);
        Assert.DoesNotContain("catalog.sys-product.query", content, StringComparison.Ordinal);
        Assert.DoesNotContain("catalog.sys-product.detail", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 未启用的写操作不得产出按钮条目，且编号从剩下的第一个按钮重新从 1 开始。
    /// </summary>
    [Fact]
    public void PageRegistrySnippet_DisabledActionShouldRemoveItsButtonAndRenumber()
    {
        var context = CodeGenerationTestHelper.CreateContext(enabledActions: ["delete"]);

        var content = CodeGenerationTestHelper.BuildPageRegistrySnippet(context).Content;

        Assert.DoesNotContain("catalog.sys-product.create", content, StringComparison.Ordinal);
        Assert.DoesNotContain("catalog.sys-product.update", content, StringComparison.Ordinal);
        Assert.Contains(
            "new(\"catalog.sys-product.delete\", \"删除\", \"catalog.sys-product\", SysProductPermissionCodes.Delete, 1),",
            content,
            StringComparison.Ordinal);
    }

    /// <summary>
    /// 未配置父菜单时片段注明顶级菜单，配置后必须回显 ParentMenuId。
    /// </summary>
    [Fact]
    public void PageRegistrySnippet_ParentCodeNoteShouldFollowParentMenuId()
    {
        var withoutParent = CodeGenerationTestHelper.BuildPageRegistrySnippet(CodeGenerationTestHelper.CreateContext()).Content;

        var context = CodeGenerationTestHelper.CreateContext();
        context.Options["ParentMenuId"] = "801";
        var withParent = CodeGenerationTestHelper.BuildPageRegistrySnippet(context).Content;

        Assert.Contains("顶级菜单", withoutParent, StringComparison.Ordinal);
        Assert.Contains("ParentMenuId=801", withParent, StringComparison.Ordinal);
    }

    /// <summary>
    /// 上下文为空时三个 internal 生成器都必须直接拒绝。
    /// </summary>
    [Fact]
    public void InternalGenerators_NullContextShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => CodeGenerationTestHelper.BuildPermissionDefinitions(null!));
        Assert.Throws<ArgumentNullException>(() => CodeGenerationTestHelper.BuildPageRegistrySnippet(null!));
        Assert.Throws<ArgumentNullException>(() => CodeGenerationTestHelper.BuildSeeders(null!));
    }

    /// <summary>
    /// 种子骨架产出权限种子与菜单种子两个文件，且都是"仅首次创建"。
    /// </summary>
    /// <remarks>
    /// 骨架里的 Order 是占位、需人工确认；标成总是覆盖会把落地方确认过的 Order 反复冲掉。
    /// </remarks>
    [Fact]
    public void Seeders_ShouldProduceTwoWriteOnceSkeletons()
    {
        var artifacts = CodeGenerationTestHelper.BuildSeeders(CodeGenerationTestHelper.CreateContext());

        Assert.Equal(2, artifacts.Count);
        Assert.Equal("SysProductPermissionSeeder.cs", artifacts[0].FileName, StringComparer.Ordinal);
        Assert.Equal("SysProductMenuSeeder.cs", artifacts[1].FileName, StringComparer.Ordinal);
        Assert.All(artifacts, artifact => Assert.Equal(ArtifactWriteMode.WriteOnce, artifact.WriteMode));
        Assert.All(artifacts, artifact => Assert.Equal(
            CodeGenerationTestHelper.ArtifactTemplateCode,
            artifact.TemplateCode,
            StringComparer.Ordinal));
    }

    /// <summary>
    /// 种子骨架的全部占位符必须被替换干净，不能把 %TOKEN% 泄漏到产物里。
    /// </summary>
    [Fact]
    public void Seeders_ShouldLeaveNoUnreplacedPlaceholder()
    {
        var artifacts = CodeGenerationTestHelper.BuildSeeders(CodeGenerationTestHelper.CreateContext());

        Assert.All(artifacts, artifact => Assert.DoesNotContain("%", artifact.Content, StringComparison.Ordinal));
    }

    /// <summary>
    /// 权限种子骨架必须落在 {命名空间}.Infrastructure.Seeders 并消费同批的权限定义类。
    /// </summary>
    [Fact]
    public void PermissionSeederSkeleton_ShouldConsumePermissionDefinitions()
    {
        var content = CodeGenerationTestHelper.BuildSeeders(CodeGenerationTestHelper.CreateContext())[0].Content;

        Assert.Contains("namespace XiHan.BasicApp.Catalog.Infrastructure.Seeders;", content, StringComparison.Ordinal);
        Assert.Contains("public sealed class SysProductPermissionSeeder : DataSeederBase", content, StringComparison.Ordinal);
        Assert.Contains("SysProductPermissionDefinitions.Items", content, StringComparison.Ordinal);
        Assert.Contains("public override int Order => 200;", content, StringComparison.Ordinal);
        Assert.Contains("[Catalog]产品权限种子数据", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 菜单种子骨架的 Order 必须大于权限种子，菜单建立时才解析得到 read 权限。
    /// </summary>
    [Fact]
    public void MenuSeederSkeleton_ShouldRunAfterPermissionSeeder()
    {
        var content = CodeGenerationTestHelper.BuildSeeders(CodeGenerationTestHelper.CreateContext())[1].Content;

        Assert.Contains("public override int Order => 201;", content, StringComparison.Ordinal);
        Assert.Contains("p.PermissionCode == \"sys_product:read\"", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 菜单种子骨架写入的菜单规格必须与 README / PageRegistry 片段完全一致。
    /// </summary>
    [Fact]
    public void MenuSeederSkeleton_ShouldWriteConsistentMenuSpecification()
    {
        var content = CodeGenerationTestHelper.BuildSeeders(CodeGenerationTestHelper.CreateContext())[1].Content;

        Assert.Contains("MenuCode = \"sys_product\"", content, StringComparison.Ordinal);
        Assert.Contains("Path = \"/catalog/sys-product\"", content, StringComparison.Ordinal);
        Assert.Contains("Component = \"catalog/sys-product/index\"", content, StringComparison.Ordinal);
        Assert.Contains("RouteName = \"CatalogSysProduct\"", content, StringComparison.Ordinal);
        Assert.Contains("I18nKey = \"menu.sys_product\"", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 菜单种子骨架必须先按 MenuCode 判存在再插入，保证可重复执行（幂等）。
    /// </summary>
    [Fact]
    public void MenuSeederSkeleton_ShouldBeIdempotent()
    {
        var content = CodeGenerationTestHelper.BuildSeeders(CodeGenerationTestHelper.CreateContext())[1].Content;

        Assert.Contains("AnyAsync(m => m.MenuCode == \"sys_product\")", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 命名空间与模块名都缺失时，四份产物必须共用同一个去重后的命名空间，彼此才 using 得到。
    /// </summary>
    [Fact]
    public void AllGenerators_ShouldShareTheSameResolvedNamespace()
    {
        var context = CodeGenerationTestHelper.CreateContext(namespaceValue: null, moduleName: null);

        var codes = MenuPermissionArtifactGenerator.Build(context, [])[0].Content;
        var definitions = CodeGenerationTestHelper.BuildPermissionDefinitions(context).Content;
        var seeder = CodeGenerationTestHelper.BuildSeeders(context)[0].Content;

        Assert.Contains("namespace SysProductGenerated.Domain.Permissions;", codes, StringComparison.Ordinal);
        Assert.Contains("namespace SysProductGenerated.Domain.Permissions;", definitions, StringComparison.Ordinal);
        Assert.Contains("namespace SysProductGenerated.Infrastructure.Seeders;", seeder, StringComparison.Ordinal);
    }
}
