// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using XiHan.BasicApp.CodeGeneration;
using XiHan.BasicApp.CodeGeneration.Seeds;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证代码生成的模板维护与预览
/// </summary>
/// <remarks>
/// 照 4.x：内置模板随程序版本种下、不能改不能删、可以启停，自有模板可增删改；预览只渲染不写历史
/// 6.0 的产物：新建包出包级文件与 10 个文案文件（后端 JSON、前端 TS 各一套），加入已有包只出资源的文件，两种目标都出登记片段；按勾选的操作一份模板一个文件
/// 前端照 4.x 按生成模式出页面与 schema（生成部分、手写部分），取数走生成的契约客户端，文案按键写、键在包的 locales 里
/// </remarks>
public sealed class CodeGenerationPreviewTests : IDisposable
{
    private const string Admin = "codegen-admin";

    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private async Task<BasicAppTestServer> StartAsync()
    {
        var server = await BasicAppTestServer.StartAsync(_database.ConnectionString);
        await server.SeedDefaultsAsync();
        await server.SeedAsync([], [.. CodeGenerationPermissions.All.Select(static p => new PermissionGrant(GrantSubject.ForRole(Admin), p.Code))]);
        await server.RunSeedersAsync(["codegeneration.templates"]);
        return server;
    }

    private void Execute(params string[] statements)
    {
        using var connection = new SqliteConnection(_database.ConnectionString);
        connection.Open();
        foreach (var statement in statements)
        {
            using var command = connection.CreateCommand();
            command.CommandText = statement;
            command.ExecuteNonQuery();
        }
    }

    private static Task<(HttpResponseMessage Response, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, object? body, string token)
        => server.SendAsync(BasicAppTestServer.Request(method, path, body is null ? null : body as string ?? JsonSerializer.Serialize(body), token));

    private static async Task<JsonElement> DetailOfAsync(BasicAppTestServer server, string token, string tableName)
    {
        var (_, list) = await SendAsync(server, HttpMethod.Post, "/api/codegen-tables:list", new { page = new { index = 1, size = 50 }, keyword = tableName }, token);
        var id = list.GetProperty("items").EnumerateArray().Single(t => t.GetProperty("tableName").GetString() == tableName).GetProperty("id").GetString();
        var (_, detail) = await SendAsync(server, HttpMethod.Get, $"/api/codegen-tables/{id}", null, token);
        return detail;
    }

    private static JsonElement Column(JsonElement detail, string name)
        => detail.GetProperty("columns").EnumerateArray().Single(c => c.GetProperty("columnName").GetString() == name);

    // 保存列配置要交回整列（与前端一样），按读到的列配置填齐，再改要改的几项
    private static Dictionary<string, object?> InputOf(JsonElement column, params (string Key, object? Value)[] changes)
    {
        string[] fields =
        [
            "id", "concurrencyStamp", "cSharpType", "cSharpProperty", "tsType", "isRequired", "isUnique", "isList", "isInsert", "isEdit", "isQuery",
            "queryType", "htmlType", "dictSelectorType", "dictCode", "enumTypeName", "constValues", "relationTableId", "relationLabelColumn",
            "defaultValue", "minValue", "maxValue", "regexPattern", "validationMessage", "order", "status",
        ];
        var input = fields.ToDictionary(static f => f, f => (object?)column.GetProperty(f).Clone());
        foreach (var (key, value) in changes)
        {
            input[key] = value;
        }

        return input;
    }

    // 表配置的修改：按读到的值整体交回，再改要改的几项
    private static async Task UpdateTableAsync(BasicAppTestServer server, string token, JsonElement detail, params (string Key, object? Value)[] changes)
    {
        var table = detail.GetProperty("table");
        string[] fields =
        [
            "concurrencyStamp", "tableName", "className", "templateType", "deliveryType", "scope", "target", "databaseType", "status", "tableComment",
            "namespace", "moduleName", "businessName", "functionName", "author", "enabledActions", "primaryKeyColumn", "treeParentColumn", "treeNameColumn",
            "masterTableId", "masterForeignKey", "dataSourceId", "options", "remark",
        ];
        var body = fields.ToDictionary(static f => f, f => (object?)table.GetProperty(f).Clone());
        foreach (var (key, value) in changes)
        {
            body[key] = value;
        }

        var (updated, updatedBody) = await SendAsync(server, HttpMethod.Put, $"/api/codegen-tables/{table.GetProperty("id").GetString()}", body, token);
        Assert.True(updated.StatusCode == HttpStatusCode.OK, updatedBody.ToString());
    }

    private static async Task SaveColumnsAsync(BasicAppTestServer server, string token, JsonElement detail, params Dictionary<string, object?>[] columns)
    {
        var (saved, savedBody) = await SendAsync(server, HttpMethod.Post, "/api/codegen-table-columns:save", new { tableId = detail.GetProperty("table").GetProperty("id").GetString(), columns }, token);
        Assert.True(saved.StatusCode == HttpStatusCode.OK, savedBody.ToString());
    }

    // 生成的代码里句末带「。」的注释行（C# 的 // 与 ///、TS 的 // 与 /* */ 各行、Vue 的 <!-- -->），产物路径:行号
    private static List<string> StoppedComments(IEnumerable<KeyValuePair<string, string>> artifacts)
        => [.. artifacts
            .Where(static a => a.Key.EndsWith(".cs", StringComparison.Ordinal) || a.Key.EndsWith(".ts", StringComparison.Ordinal) || a.Key.EndsWith(".vue", StringComparison.Ordinal))
            .SelectMany(static a => a.Value.ReplaceLineEndings("\n").Split('\n').Select((line, index) => (a.Key, Line: line.TrimStart(), Index: index + 1)))
            .Where(static l => (l.Line.StartsWith("//", StringComparison.Ordinal) || l.Line.StartsWith('*') || l.Line.StartsWith("/*", StringComparison.Ordinal) || l.Line.StartsWith("<!--", StringComparison.Ordinal)) && Regex.IsMatch(l.Line, @"。(?:\s*(?:</[A-Za-z][\w:.-]*>|\*/|-->))*\s*$"))
            .Select(static l => $"{l.Key}:{l.Index} {l.Line}")];

    private static async Task<Dictionary<string, string>> PreviewAsync(BasicAppTestServer server, string token, JsonElement detail)
    {
        var (preview, body) = await SendAsync(server, HttpMethod.Get, $"/api/codegen-tables/{detail.GetProperty("table").GetProperty("id").GetString()}:preview", null, token);
        Assert.True(preview.StatusCode == HttpStatusCode.OK, body.ToString());
        return body.GetProperty("files").EnumerateArray().ToDictionary(static f => f.GetProperty("path").GetString()!, static f => f.GetProperty("content").GetString()!);
    }

    // 一张商品表（唯一的编码、关联树的分类）与一张分类树表，导入并配好：新建包 Shop
    private async Task<(JsonElement Product, JsonElement Category)> PrepareShopAsync(BasicAppTestServer server, string admin, string target)
    {
        Execute(
            "CREATE TABLE biz_category (id TEXT PRIMARY KEY NOT NULL, parent_id TEXT NULL, name VARCHAR(50) NOT NULL, status INTEGER NOT NULL, tenant_id VARCHAR(64) NOT NULL, created_at DATETIME NOT NULL, is_deleted INTEGER NOT NULL)",
            "CREATE TABLE biz_product (id TEXT PRIMARY KEY NOT NULL, product_code VARCHAR(30) NOT NULL, product_name VARCHAR(100) NOT NULL, price DECIMAL(18,2) NULL, on_sale BOOLEAN NOT NULL, category_id TEXT NULL, remark TEXT NULL, tenant_id VARCHAR(64) NOT NULL, created_at DATETIME NOT NULL, is_deleted INTEGER NOT NULL)");
        var (imported, _) = await SendAsync(server, HttpMethod.Post, "/api/codegen-tables:import", """{"tableNames":["biz_category","biz_product"]}""", admin);
        Assert.Equal(HttpStatusCode.OK, imported.StatusCode);

        var category = await DetailOfAsync(server, admin, "biz_category");
        await UpdateTableAsync(server, admin, category, ("className", "Category"), ("namespace", "XiHan.BasicApp.Shop"), ("moduleName", "Shop"), ("target", target), ("templateType", "Tree"), ("treeParentColumn", "parent_id"), ("treeNameColumn", "name"), ("businessName", ""), ("tableComment", "商品分类。"), ("enabledActions", "create,update,delete,status"));
        category = await DetailOfAsync(server, admin, "biz_category");
        await SaveColumnsAsync(server, admin, category, InputOf(Column(category, "parent_id"), ("cSharpType", "Guid?")));
        category = await DetailOfAsync(server, admin, "biz_category");

        var product = await DetailOfAsync(server, admin, "biz_product");
        await UpdateTableAsync(server, admin, product, ("className", "Product"), ("namespace", "XiHan.BasicApp.Shop"), ("moduleName", "Shop"), ("target", target), ("businessName", "商品。"), ("enabledActions", "create,update,delete,export,import,print"));
        product = await DetailOfAsync(server, admin, "biz_product");
        await SaveColumnsAsync(
            server,
            admin,
            product,
            InputOf(Column(product, "product_code"), ("isUnique", true), ("isRequired", true), ("isQuery", true), ("queryType", "Like")),
            InputOf(Column(product, "category_id"), ("cSharpType", "Guid?"), ("dictSelectorType", "TreeSelector"), ("relationTableId", category.GetProperty("table").GetProperty("id").GetString()), ("isQuery", true), ("queryType", "Equal")));
        return (await DetailOfAsync(server, admin, "biz_product"), category);
    }

    [Fact]
    public async Task BuiltInTemplates_AreSeeded_ReadOnly_AndCustomOnesCanBeMaintained()
    {
        await using var server = await StartAsync();
        var admin = TestTokens.Issue("root", null, Admin);
        var (_, list) = await SendAsync(server, HttpMethod.Post, "/api/codegen-templates:list", new { page = new { index = 1, size = 100 } }, admin);
        var items = list.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(BuiltInTemplates.All.Count, items.Count);
        Assert.All(items, static t => Assert.True(t.GetProperty("isBuiltIn").GetBoolean()));
        var entity = items.Single(static t => t.GetProperty("templateCode").GetString() == "backend.entity");
        var entityId = entity.GetProperty("id").GetString();

        // 内置模板：详情带正文；不能改、不能删；可以启停
        var (_, detail) = await SendAsync(server, HttpMethod.Get, $"/api/codegen-templates/{entityId}", null, admin);
        Assert.Contains("TenantScopedAuditedEntity", detail.GetProperty("templateContent").GetString(), StringComparison.Ordinal);
        var builtInUpdate = new { concurrencyStamp = entity.GetProperty("concurrencyStamp").GetString(), templateName = "x", templateType = "Universal", templateEngine = "Scriban", writeMode = "AlwaysOverwrite", templateContent = "x" };
        var (edited, editedBody) = await SendAsync(server, HttpMethod.Put, $"/api/codegen-templates/{entityId}", builtInUpdate, admin);
        Assert.Equal((HttpStatusCode.Conflict, CodeGenerationErrors.TemplateBuiltIn), (edited.StatusCode, editedBody.GetProperty("code").GetString()));
        var (disabled, disabledBody) = await SendAsync(server, HttpMethod.Post, $"/api/codegen-templates/{entityId}:set-status", new { concurrencyStamp = entity.GetProperty("concurrencyStamp").GetString(), status = "Disabled" }, admin);
        Assert.Equal((HttpStatusCode.OK, "Disabled"), (disabled.StatusCode, disabledBody.GetProperty("template").GetProperty("status").GetString()));

        // 语法检查只解析不渲染
        var (_, broken) = await SendAsync(server, HttpMethod.Post, "/api/codegen-templates:validate", new { templateContent = "{{ if ClassName }}" }, admin);
        Assert.False(broken.GetProperty("valid").GetBoolean());
        Assert.NotEmpty(broken.GetProperty("errors").EnumerateArray());
        var (_, fine) = await SendAsync(server, HttpMethod.Post, "/api/codegen-templates:validate", new { templateContent = "{{ ClassName }}" }, admin);
        Assert.True(fine.GetProperty("valid").GetBoolean());

        // 自有模板：编码唯一、不含空白，可改可删；批量删除时内置模板逐项拒绝
        var custom = new { templateCode = "custom.readme", templateName = "说明", templateType = "Universal", templateEngine = "Scriban", writeMode = "WriteOnce", templateContent = "# {{ ClassName }}", templateGroup = "backend-crud", fileNameExpression = "README.{{ ClassName }}.md" };
        var (created, createdBody) = await SendAsync(server, HttpMethod.Post, "/api/codegen-templates", custom, admin);
        Assert.True(created.IsSuccessStatusCode, createdBody.ToString());
        var customId = createdBody.GetProperty("template").GetProperty("id").GetString();
        var (taken, takenBody) = await SendAsync(server, HttpMethod.Post, "/api/codegen-templates", custom, admin);
        Assert.Equal((HttpStatusCode.Conflict, CodeGenerationErrors.TemplateCodeTaken), (taken.StatusCode, takenBody.GetProperty("code").GetString()));
        var (spaced, _) = await SendAsync(server, HttpMethod.Post, "/api/codegen-templates", custom with { templateCode = "has space" }, admin);
        Assert.Equal(HttpStatusCode.BadRequest, spaced.StatusCode);
        var customUpdate = new { concurrencyStamp = createdBody.GetProperty("template").GetProperty("concurrencyStamp").GetString(), templateName = "说明文件", templateType = "Universal", templateEngine = "Scriban", writeMode = "WriteOnce", templateContent = "# {{ DisplayName }}" };
        var (customEdited, customEditedBody) = await SendAsync(server, HttpMethod.Put, $"/api/codegen-templates/{customId}", customUpdate, admin);
        Assert.Equal((HttpStatusCode.OK, "说明文件"), (customEdited.StatusCode, customEditedBody.GetProperty("template").GetProperty("templateName").GetString()));
        var (deleted, deletedBody) = await SendAsync(server, HttpMethod.Delete, $"/api/codegen-templates?ids={customId}&ids={entityId}", null, admin);
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        Assert.Equal([customId], deletedBody.GetProperty("succeeded").EnumerateArray().Select(static i => i.GetString()));
        Assert.Equal(CodeGenerationErrors.TemplateBuiltIn, deletedBody.GetProperty("failed")[0].GetProperty("code").GetString());
    }

    [Fact]
    public async Task Preview_NewPackage_RendersThePackage_TheResource_AndTheLocales()
    {
        await using var server = await StartAsync();
        var admin = TestTokens.Issue("root", null, Admin);
        var (product, category) = await PrepareShopAsync(server, admin, "NewPackage");
        var files = await PreviewAsync(server, admin, product);
        const string Package = "backend/src/XiHan.BasicApp.Shop/";

        // 实体：表名与类名按约定对不上即写 [SugarTable]，唯一列是部分唯一索引，关联列不焊外键
        var entity = files[Package + "Domain/Entities/Product.Generated.cs"];
        Assert.Contains("public sealed partial class Product : TenantScopedAuditedEntity", entity, StringComparison.Ordinal);
        Assert.Contains("[SugarTable(\"biz_product\")]", entity, StringComparison.Ordinal);
        Assert.Contains("[ActiveUniqueIndex(nameof(TenantId), nameof(ProductCode))]", entity, StringComparison.Ordinal);
        Assert.Contains("[StringLength(30)]", entity, StringComparison.Ordinal);
        Assert.Contains("public Guid? CategoryId { get; set; }", entity, StringComparison.Ordinal);
        Assert.True(files.ContainsKey(Package + "Domain/Entities/Product.cs"));

        // 按勾选出操作：没勾状态切换就没有启停，不是树表就没有树；关联树出选项操作
        string[] operations = ["ListProducts", "GetProduct", "CreateProduct", "UpdateProduct", "DeleteProducts", "ProductOptionOperations", "ProductContracts.Generated", "ProductContracts", "ProductPrintDataSource.Generated"];
        Assert.All(operations, name => Assert.True(files.ContainsKey($"{Package}Operations/Products/{name}.cs"), name));
        Assert.False(files.ContainsKey(Package + "Operations/Products/SetProductStatus.cs"));
        Assert.False(files.ContainsKey(Package + "Operations/Products/GetProductTree.cs"));
        Assert.Contains("\"products.category-id-options\"", files[Package + "Operations/Products/ProductOptionOperations.cs"], StringComparison.Ordinal);
        Assert.Contains("Permission = ShopPermissions.ProductsCreate", files[Package + "Operations/Products/CreateProduct.cs"], StringComparison.Ordinal);
        Assert.Contains("ShopErrors.ProductProductCodeTaken", files[Package + "Operations/Products/CreateProduct.cs"], StringComparison.Ordinal);

        // 查询白名单：模糊查询进关键字与 contains，关联列等值与多选
        var queries = files[Package + "Queries/ProductQueries.Generated.cs"];
        Assert.Contains("(FieldProductCode, \"contains\")", queries, StringComparison.Ordinal);
        Assert.Contains("(FieldCategoryId, \"in\")", queries, StringComparison.Ordinal);
        Assert.Contains("e.ProductCode.Contains(keyword)", queries, StringComparison.Ordinal);
        Assert.True(files.ContainsKey(Package + "Queries/ProductQueries.cs"));

        // 包级文件只在新建包时出；文案 10 种区域，固定用语按区域、业务名原样
        string[] packageFiles = ["XiHan.BasicApp.Shop.csproj", "ShopFeature.cs", "ShopCapabilities.cs", "ShopPermissions.cs", "ShopPages.cs", "ShopErrors.cs", "ShopMessages.cs", "ShopJsonContext.cs", "README.md"];
        Assert.All(packageFiles, name => Assert.True(files.ContainsKey(Package + name), name));
        Assert.Contains("\"SHOP_CONFLICT_001\"", files[Package + "ShopErrors.cs"], StringComparison.Ordinal);
        Assert.Contains("new(\"shop.products\", \"/shop/products\", \"shop.products.title\"", files[Package + "ShopPages.cs"], StringComparison.Ordinal);
        Assert.Contains("AddPrintDataSource(ProductPrintDataSource.Definition)", files[Package + "ShopFeature.cs"], StringComparison.Ordinal);
        var locales = files.Keys.Where(static k => k.StartsWith(Package + "Localization/", StringComparison.Ordinal)).ToList();
        Assert.Equal(10, locales.Count);
        using (var zh = JsonDocument.Parse(files[Package + "Localization/zh-CN.json"]))
        {
            Assert.Equal("新建商品", zh.RootElement.GetProperty("products.create.title").GetString());
            Assert.Equal("Shop", zh.RootElement.GetProperty("openapi.groups.shop.name").GetString());
        }

        using (var en = JsonDocument.Parse(files[Package + "Localization/en-US.json"]))
        {
            Assert.Equal("Create 商品", en.RootElement.GetProperty("products.create.title").GetString());
            // 列没有注释时按属性名（照 4.x）
            Assert.Equal("ProductCode '{value}' is already taken.", en.RootElement.GetProperty("shop.product.product_code_taken").GetString());
        }

        var registration = files["_registration/products.md"];
        Assert.Contains("## 新建包 `XiHan.BasicApp.Shop`", registration, StringComparison.Ordinal);
        Assert.Contains("[\"Shop\"] = 30,", registration, StringComparison.Ordinal);

        // 树表：树操作与上级的校验，删除时有下级逐项拒绝
        var tree = await PreviewAsync(server, admin, category);
        Assert.Contains("\"categories.tree\"", tree[Package + "Operations/Categories/GetCategoryTree.cs"], StringComparison.Ordinal);
        Assert.Contains("ParentAllowedAsync", tree[Package + "Operations/Categories/UpdateCategory.cs"], StringComparison.Ordinal);
        Assert.Contains("ShopErrors.CategoryHasChildren", tree[Package + "Operations/Categories/DeleteCategories.cs"], StringComparison.Ordinal);

        // 前端：单表页与 schema（生成部分每次覆盖、手写部分只首次）、包的启动钩子与 10 种区域的文案
        const string View = "frontend/src/modules/shop/views/products/";
        var schema = files[View + "products.schema.generated.ts"];
        Assert.Contains("export type ProductRow = ResponseOf<'products.list'>['items'][number]", schema, StringComparison.Ordinal);
        Assert.Contains("await productsApi.categoryIdOptions({})", schema, StringComparison.Ordinal);
        Assert.Contains("export: { businessType: 'products.list', buildQuery: buildPageQuery }", schema, StringComparison.Ordinal);
        Assert.Contains("title: t('shop.products.fields.product_code')", schema, StringComparison.Ordinal);

        // 产物照前端 lint 的换行规则写：含调用的对象一个属性一行、含调用的数组一项一行（完整的门是退出验证里的 pnpm check，这里守住写法没退回单行）
        Assert.Contains("    {\n      key: 'productCode',\n      title: t('shop.products.fields.product_code'),\n      dataType: 'string',\n", schema.ReplaceLineEndings("\n"), StringComparison.Ordinal);
        Assert.Contains("  return baseFields(t)", files[View + "products.schema.ts"], StringComparison.Ordinal);
        var page = files[View + "index.vue"];
        Assert.Contains("    errors.productCode = [\n      t('shop.products.required_input', {\n        label: t('shop.products.fields.product_code'),\n      }),\n    ]\n  }\n", page.ReplaceLineEndings("\n"), StringComparison.Ordinal);
        Assert.Contains("<XTreeSelect v-model:value=\"form.categoryId\"", page, StringComparison.Ordinal);
        Assert.Contains("await productsApi.create(toCreateRequest(form))", page, StringComparison.Ordinal);
        Assert.Contains("registerModuleLocales", files["frontend/src/modules/shop/setup.ts"], StringComparison.Ordinal);
        var frontendLocales = files.Keys.Where(static k => k.StartsWith("frontend/src/modules/shop/locales/", StringComparison.Ordinal)).ToList();
        Assert.Equal(10, frontendLocales.Count);
        Assert.Contains("      title: '商品',", files["frontend/src/modules/shop/locales/zh-CN.ts"], StringComparison.Ordinal);
        Assert.Contains("      create_title: '新增商品',", files["frontend/src/modules/shop/locales/zh-CN.ts"], StringComparison.Ordinal);
        Assert.Contains("      create_title: 'New 商品',", files["frontend/src/modules/shop/locales/en-US.ts"], StringComparison.Ordinal);

        // 树表：树页按树操作取数、上级候选取同一棵树，生成的启停枚举出下拉与 10 种区域的选项文案
        var treeSchema = tree["frontend/src/modules/shop/views/categories/categories.schema.generated.ts"];
        Assert.Contains("toTreeRows((await categoriesApi.tree({})).items)", treeSchema, StringComparison.Ordinal);
        Assert.Contains("treeColumn: true", treeSchema, StringComparison.Ordinal);
        Assert.Contains("value: 'Disabled' as const", treeSchema, StringComparison.Ordinal);
        Assert.Contains(":options=\"parentOptions\"", tree["frontend/src/modules/shop/views/categories/index.vue"], StringComparison.Ordinal);
        Assert.Contains(":options=\"statusSelectOptions\"", tree["frontend/src/modules/shop/views/categories/index.vue"], StringComparison.Ordinal);
        Assert.Contains("          disabled: '停用',", tree["frontend/src/modules/shop/locales/zh-CN.ts"], StringComparison.Ordinal);

        // 整数状态列没选枚举：生成器在实体里出 <类>Status（Active 0、Disabled 1），状态列与启停请求都用它
        var categoryEntity = tree[Package + "Domain/Entities/Category.Generated.cs"];
        Assert.Contains("public enum CategoryStatus", categoryEntity, StringComparison.Ordinal);
        Assert.Contains("public XiHan.BasicApp.Shop.Domain.Entities.CategoryStatus Status { get; set; }", categoryEntity, StringComparison.Ordinal);
        Assert.EndsWith("}\n", categoryEntity.ReplaceLineEndings("\n"), StringComparison.Ordinal);
        Assert.Contains("XiHan.BasicApp.Shop.Domain.Entities.CategoryStatus Status)", tree[Package + "Operations/Categories/CategoryContracts.Generated.cs"], StringComparison.Ordinal);
        Assert.True(tree.ContainsKey(Package + "Operations/Categories/SetCategoryStatus.cs"));
        Assert.DoesNotContain("public enum", files[Package + "Domain/Entities/Product.Generated.cs"], StringComparison.Ordinal);

        // 表注释与业务名句末带「。」（6.0 实体的注释来自文档注释，导入时业务名取表注释）：当名字用、拼进句子时去掉，不出「。。」
        Assert.Contains("title: '商品分类',", tree["frontend/src/modules/shop/locales/zh-CN.ts"], StringComparison.Ordinal);
        Assert.DoesNotContain(tree, static f => f.Value.Contains("。。", StringComparison.Ordinal));
        Assert.DoesNotContain(files, static f => f.Value.Contains("。。", StringComparison.Ordinal));

        // 生成的代码照 6.0 的写法：注释句末不加「。」
        var stopped = StoppedComments(tree.Concat(files));
        Assert.True(stopped.Count == 0, string.Join('\n', stopped));
    }

    [Fact]
    public async Task Preview_ExistingPackage_EmitsOnlyTheResource_AndTheRegistrationLines()
    {
        await using var server = await StartAsync();
        var admin = TestTokens.Issue("root", null, Admin);
        var (product, _) = await PrepareShopAsync(server, admin, "ExistingPackage");
        var files = await PreviewAsync(server, admin, product);

        Assert.DoesNotContain(files.Keys, static k => k.EndsWith("ShopFeature.cs", StringComparison.Ordinal) || k.Contains("/Localization/", StringComparison.Ordinal) || k.EndsWith(".csproj", StringComparison.Ordinal));
        Assert.True(files.ContainsKey("backend/src/XiHan.BasicApp.Shop/Operations/Products/CreateProduct.cs"));
        var registration = files["_registration/products.md"];
        Assert.Contains("## 加入已有包 `XiHan.BasicApp.Shop`", registration, StringComparison.Ordinal);
        Assert.Contains("public const string ProductsRead = \"shop.products.read\";", registration, StringComparison.Ordinal);
        Assert.Contains("public const string ProductNotFound = \"SHOP_NOTFOUND_NNN\";", registration, StringComparison.Ordinal);
        Assert.Contains("[JsonSerializable(typeof(CreateProductRequest))]", registration, StringComparison.Ordinal);
        Assert.Contains("context.Services.AddScoped<ProductQueries>();", registration, StringComparison.Ordinal);
        Assert.Contains("### `Localization/ru-RU.json`", registration, StringComparison.Ordinal);

        // 前端：只出资源的页面与 schema，包的启动钩子与文案文件不出，文案按区域写进登记片段
        Assert.True(files.ContainsKey("frontend/src/modules/shop/views/products/index.vue"));
        Assert.DoesNotContain(files.Keys, static k => k.StartsWith("frontend/src/modules/shop/locales/", StringComparison.Ordinal) || k == "frontend/src/modules/shop/setup.ts");
        Assert.Contains("### `frontend-next/src/modules/shop/locales/zh-CN.ts` 的 `shop` 对象里", registration, StringComparison.Ordinal);
        Assert.Contains("    title: '商品',", registration, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Preview_RejectsIncompleteConfiguration_AndBrokenTemplates()
    {
        await using var server = await StartAsync();
        var admin = TestTokens.Issue("root", null, Admin);
        Execute("CREATE TABLE demo_node (id TEXT PRIMARY KEY NOT NULL, parent_id TEXT NULL, name VARCHAR(50) NOT NULL, state INTEGER NOT NULL)");
        await SendAsync(server, HttpMethod.Post, "/api/codegen-tables:import", """{"tableNames":["demo_node"]}""", admin);
        var node = await DetailOfAsync(server, admin, "demo_node");

        // 树表的父级列在 SQLite 里是 TEXT（推成 string），要 Guid
        await UpdateTableAsync(server, admin, node, ("namespace", "XiHan.BasicApp.Demo"), ("moduleName", "Demo"), ("templateType", "Tree"), ("treeParentColumn", "parent_id"), ("treeNameColumn", "name"));
        node = await DetailOfAsync(server, admin, "demo_node");
        var id = node.GetProperty("table").GetProperty("id").GetString();
        var (tree, treeBody) = await SendAsync(server, HttpMethod.Get, $"/api/codegen-tables/{id}:preview", null, admin);
        Assert.Equal((HttpStatusCode.BadRequest, CodeGenerationErrors.GenerationInvalid), (tree.StatusCode, treeBody.GetProperty("code").GetString()));

        // 勾了状态切换却没有启停枚举的列
        await UpdateTableAsync(server, admin, node, ("templateType", "Single"), ("enabledActions", "create,update,status"));
        var (status, statusBody) = await SendAsync(server, HttpMethod.Get, $"/api/codegen-tables/{id}:preview", null, admin);
        Assert.Equal((HttpStatusCode.BadRequest, CodeGenerationErrors.GenerationInvalid), (status.StatusCode, statusBody.GetProperty("code").GetString()));

        // 选了枚举却解析不到（4.x 出禁用的占位选项，6.0 生成前拦下）
        node = await DetailOfAsync(server, admin, "demo_node");
        await UpdateTableAsync(server, admin, node, ("enabledActions", "create,update"));
        node = await DetailOfAsync(server, admin, "demo_node");
        var state = Column(node, "state");
        await SaveColumnsAsync(server, admin, node, InputOf(state, ("dictSelectorType", "EnumSelector"), ("enumTypeName", "NoSuchEnum")));
        var (unresolved, unresolvedBody) = await SendAsync(server, HttpMethod.Get, $"/api/codegen-tables/{id}:preview", null, admin);
        Assert.Equal((HttpStatusCode.BadRequest, CodeGenerationErrors.GenerationInvalid), (unresolved.StatusCode, unresolvedBody.GetProperty("code").GetString()));
        Assert.Contains("NoSuchEnum", unresolvedBody.GetProperty("detail").GetString(), StringComparison.Ordinal);
        node = await DetailOfAsync(server, admin, "demo_node");
        await SaveColumnsAsync(server, admin, node, InputOf(Column(node, "state"), ("dictSelectorType", null), ("enumTypeName", null)));

        // 指定的模板：运行时出错与产物越出根目录都报 VALIDATION_008，带模板编码
        var looping = new { templateCode = "custom.loop", templateName = "循环", templateType = "Universal", templateEngine = "Scriban", writeMode = "WriteOnce", templateContent = "{{ for i in 1..100000 }}x{{ end }}", templateGroup = "backend-crud" };
        await SendAsync(server, HttpMethod.Post, "/api/codegen-templates", looping, admin);
        var (loop, loopBody) = await SendAsync(server, HttpMethod.Get, $"/api/codegen-tables/{id}:preview?templateCodes=custom.loop", null, admin);
        Assert.Equal((HttpStatusCode.BadRequest, CodeGenerationErrors.TemplateRenderFailed), (loop.StatusCode, loopBody.GetProperty("code").GetString()));
        Assert.Contains("custom.loop", loopBody.GetProperty("detail").GetString(), StringComparison.Ordinal);
        var escaping = new { templateCode = "custom.escape", templateName = "越界", templateType = "Universal", templateEngine = "Scriban", writeMode = "WriteOnce", templateContent = "x", templateGroup = "backend-crud", fileNameExpression = "../../outside.txt" };
        await SendAsync(server, HttpMethod.Post, "/api/codegen-templates", escaping, admin);
        var (escape, escapeBody) = await SendAsync(server, HttpMethod.Get, $"/api/codegen-tables/{id}:preview?templateCodes=custom.escape", null, admin);
        Assert.Equal((HttpStatusCode.BadRequest, CodeGenerationErrors.TemplateRenderFailed), (escape.StatusCode, escapeBody.GetProperty("code").GetString()));

        // 读权限即可预览；平台以外的主体没有代码生成
        var (tenant, _) = await SendAsync(server, HttpMethod.Get, $"/api/codegen-tables/{id}:preview", null, TestTokens.Issue("alice", "acme", "admin"));
        Assert.Equal(HttpStatusCode.Forbidden, tenant.StatusCode);
    }
}
