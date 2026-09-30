// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.RegularExpressions;
using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

namespace XiHan.BasicApp.CodeGeneration.Tests;

/// <summary>
/// 内置模板渲染测试。
/// </summary>
/// <remarks>
/// 用程序集里嵌入的真实内置模板渲染，钉住几条跨模板的行为约定：
/// 报文可空性跟列本身走（非空列留空时发缺省值，而不是 null 整单 400 或落库撞 NOT NULL）；
/// 树表写接口挡住自环、挂到下级与删带子节点的父级；删除不存在的记录如实报错；
/// 主子表页面的样式只用语义令牌（生成页不在 px 登记表里）。
/// 产物能否编译、能否过前端类型检查与门禁，由探针在真实工程里验证，这里只锁住容易回退的关键片段。
/// </remarks>
public sealed partial class CodeGenBuiltInTemplateTests
{
    private readonly ScribanTemplateRenderer _renderer = new();

    /// <summary>
    /// 树表 AppService 在新增与更新时校验父级、删除时拦下带子节点的父级。
    /// </summary>
    [Fact]
    public async Task TreeAppService_ShouldGuardParentChainAndChildren()
    {
        var content = await RenderAsync("Backend/AppService.sbn", TreeContext(nullableParent: true));

        Assert.Contains("await EnsureValidParentAsync(input.ParentId, null, cancellationToken);", content, StringComparison.Ordinal);
        Assert.Contains("await EnsureValidParentAsync(input.ParentId, entity.BasicId, cancellationToken);", content, StringComparison.Ordinal);
        Assert.Contains("_repository.AnyAsync(entity => entity.ParentId == id, cancellationToken)", content, StringComparison.Ordinal);
        Assert.Contains("产品分类存在子节点，不能直接删除。", content, StringComparison.Ordinal);
        Assert.Contains("产品分类不能选择自身作为父级。", content, StringComparison.Ordinal);
        Assert.Contains("产品分类不能挂到自己的下级下面。", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 单表 AppService 不产树校验；更新与删除找不到记录时如实报错，不静默成功。
    /// 表注释只进普通字符串字面量：注释里的花括号进插值串会被当成插值而编译不过。
    /// </summary>
    [Fact]
    public async Task SingleAppService_ShouldReportMissingRecordWithoutInterpolatingComment()
    {
        var context = SingleContext();
        context.TableComment = "产品{表}";

        var content = await RenderAsync("Backend/AppService.sbn", context);

        Assert.DoesNotContain("EnsureValidParentAsync", content, StringComparison.Ordinal);
        Assert.Contains("?? throw new InvalidOperationException(\"产品{表}不存在。\");", content, StringComparison.Ordinal);
        Assert.Contains("if (!await _repository.DeleteByIdAsync(id, cancellationToken))", content, StringComparison.Ordinal);
        Assert.DoesNotContain("$\"", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 前端 DTO 类型的可空性只跟列本身走：非空列即便没勾必填也不是可选字段。
    /// </summary>
    [Fact]
    public async Task Types_ShouldFollowColumnNullabilityNotRequiredFlag()
    {
        var content = await RenderAsync("Frontend/Types.sbn", SingleContext());

        Assert.Contains("  stock: number\n", content, StringComparison.Ordinal);
        Assert.Contains("  remark?: string | null\n", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 单表页面：非空列留空发缺省值、可空文本清空发 null、必填下拉判 null、整数列拦小数。
    /// </summary>
    [Fact]
    public async Task Page_ShouldSubmitWireValuesThatTheBackendAccepts()
    {
        var content = await RenderAsync("Frontend/Page.sbn", SingleContext());

        Assert.Contains("stock: form.value.stock ?? 0,", content, StringComparison.Ordinal);
        Assert.Contains("remark: form.value.remark || null,", content, StringComparison.Ordinal);
        Assert.Contains("if (form.value.level == null) {", content, StringComparison.Ordinal);
        Assert.Contains("toast.warning('请选择级别')", content, StringComparison.Ordinal);
        Assert.Contains("if (form.value.stock != null && !Number.isInteger(form.value.stock)) {", content, StringComparison.Ordinal);
        Assert.Contains("toast.warning('库存只能填整数')", content, StringComparison.Ordinal);
        Assert.DoesNotContain(":min=\"0\"", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 主子表页面的样式只用语义令牌：生成页不在 px 登记表里，写裸 px 过不了前端门禁。
    /// </summary>
    [Fact]
    public async Task MasterDetailPage_StyleShouldUseTokensOnly()
    {
        var context = SingleContext();
        context.DetailTables =
        [
            new RelatedTableRef
            {
                TableId = 2,
                TableName = "sys_product_sku",
                TableComment = "产品规格",
                ClassName = "SysProductSku",
                ClassNameCamel = "sysProductSku",
                ClassNameKebab = "sys-product-sku",
                ModuleName = "Catalog",
                Namespace = "XiHan.BasicApp.Catalog",
                ForeignKeyColumn = "ProductId",
                ForeignKeyProperty = "ProductId",
                Columns = [Column("ProductId", "long", "string", isRequired: true), Column("SkuName", "string", "string", isRequired: true)]
            }
        ];

        var content = await RenderAsync("Frontend/Page.sbn", context);
        var style = content[content.IndexOf("<style scoped>", StringComparison.Ordinal)..];

        Assert.DoesNotMatch(BarePxRegex(), style);
        Assert.Contains("var(--xh-space-3)", style, StringComparison.Ordinal);
    }

    /// <summary>
    /// 非空父级以 0 表示根节点：不选时提交 '0'，回填时把 0 还原成「未选」。
    /// </summary>
    [Fact]
    public async Task TreePage_NotNullParentShouldUseZeroAsRoot()
    {
        var content = await RenderAsync("Frontend/TreePage.sbn", TreeContext(nullableParent: false));

        Assert.Contains("parentId: form.value.parentId || '0',", content, StringComparison.Ordinal);
        Assert.Contains("parentId: src.parentId && src.parentId !== '0' ? src.parentId : null,", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 可空父级不选即 null，回填原样。
    /// </summary>
    [Fact]
    public async Task TreePage_NullableParentShouldUseNullAsRoot()
    {
        var content = await RenderAsync("Frontend/TreePage.sbn", TreeContext(nullableParent: true));

        Assert.Contains("parentId: form.value.parentId || null,", content, StringComparison.Ordinal);
        Assert.Contains("parentId: src.parentId ?? null,", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// 从程序集嵌入资源读取内置模板
    /// </summary>
    private static string LoadTemplate(string resourceFile)
    {
        var suffix = $".Templates.{resourceFile.Replace('/', '.')}";
        var assembly = CodeGenerationTestHelper.ModuleAssembly;
        var resourceName = assembly.GetManifestResourceNames().Single(name => name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private async Task<string> RenderAsync(string resourceFile, CodeGenerationContext context)
        => (await _renderer.RenderAsync(LoadTemplate(resourceFile), context)).Replace("\r\n", "\n", StringComparison.Ordinal);

    private static ColumnSchema Column(string property, string csharpType, string tsType, HtmlType htmlType = HtmlType.Input, bool isNullable = false, bool isRequired = false)
    {
        var column = CodeGenerationTestHelper.CreateColumn(property, csharpType, tsType, htmlType: htmlType);
        column.IsNullable = isNullable;
        column.IsRequired = isRequired;
        return column;
    }

    private static CodeGenerationContext SingleContext()
    {
        var level = Column("Level", "int", "number", HtmlType.Select, isRequired: true);
        level.ColumnComment = "级别";
        level.DictSelectorType = DictSelectorType.ConstSelector;
        level.ConstValues = """[{"label":"低","value":1},{"label":"高","value":2}]""";
        var stock = Column("Stock", "int", "number", HtmlType.InputNumber);
        stock.ColumnComment = "库存";
        return CodeGenerationTestHelper.CreateContext(columns:
        [
            Column("BasicId", "long", "string"),
            Column("ProductName", "string", "string", isRequired: true),
            Column("Remark", "string?", "string", HtmlType.Textarea, isNullable: true),
            level,
            stock
        ]);
    }

    private static CodeGenerationContext TreeContext(bool nullableParent)
    {
        var parent = Column("ParentId", nullableParent ? "long?" : "long", "string", isNullable: nullableParent, isRequired: !nullableParent);
        var name = Column("CategoryName", "string", "string", isRequired: true);
        var context = CodeGenerationTestHelper.CreateContext(
            tableName: "sys_category",
            className: "SysCategory",
            templateType: TemplateType.Tree,
            columns: [Column("BasicId", "long", "string"), parent, name]);
        context.TableComment = "产品分类";
        context.TreeParentColumn = parent;
        context.TreeNameColumn = name;
        return context;
    }

    [GeneratedRegex(@"\d+px")]
    private static partial Regex BarePxRegex();
}
