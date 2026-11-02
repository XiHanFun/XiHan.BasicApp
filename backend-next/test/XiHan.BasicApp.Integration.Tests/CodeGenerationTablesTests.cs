// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using XiHan.BasicApp.CodeGeneration;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证代码生成的表结构导入、表配置与列配置。
/// </summary>
/// <remarks>
/// 照 4.x：导入按表隔离（已导入、不存在的各记一条失败），类名去掉表名前缀、业务名取表注释，有父级列与名称列推为树表，基类的列不进表单；
/// 修改记下人工改过的字段，同步结构时保留它们、补新列、删掉库里已删的列；列配置整体保存、选项来源只留生效的那个；删除连带列配置。
/// 「沿用已有实体」按登记的实体识别（只认主库）；外部库要启用。
/// </remarks>
public sealed class CodeGenerationTablesTests : IDisposable
{
    private const string Admin = "codegen-admin";

    private readonly SqliteDatabase _database = new();

    private readonly SqliteDatabase _external = new();

    public void Dispose()
    {
        _database.Dispose();
        _external.Dispose();
    }

    private async Task<BasicAppTestServer> StartAsync()
    {
        var server = await BasicAppTestServer.StartAsync(_database.ConnectionString);
        await server.SeedDefaultsAsync();
        await server.SeedAsync([], [.. CodeGenerationPermissions.All.Select(static p => new PermissionGrant(GrantSubject.ForRole(Admin), p.Code))]);
        return server;
    }

    private static void Execute(string connectionString, params string[] statements)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        foreach (var statement in statements)
        {
            using var command = connection.CreateCommand();
            command.CommandText = statement;
            command.ExecuteNonQuery();
        }
    }

    private static Task<(HttpResponseMessage Response, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, string? json, string token)
        => server.SendAsync(BasicAppTestServer.Request(method, path, json, token));

    private static async Task<JsonElement> DetailOfAsync(BasicAppTestServer server, string token, string tableName)
    {
        var (_, list) = await SendAsync(server, HttpMethod.Post, "/api/codegen-tables:list", JsonSerializer.Serialize(new { page = new { index = 1, size = 50 }, keyword = tableName }), token);
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

    [Fact]
    public async Task Import_InfersTheConfiguration_AndIsolatesEachTable()
    {
        await using var server = await StartAsync();
        Execute(_database.ConnectionString,
            "CREATE TABLE sys_demo_category (id TEXT PRIMARY KEY NOT NULL, parent_id TEXT NULL, name VARCHAR(100) NOT NULL, sort INTEGER NOT NULL, tenant_id VARCHAR(64) NOT NULL, created_at DATETIME NOT NULL, is_deleted INTEGER NOT NULL)",
            "CREATE TABLE demo_product (id TEXT PRIMARY KEY NOT NULL, product_code VARCHAR(50) NOT NULL, price DECIMAL(18,2) NULL, on_sale BOOLEAN NOT NULL, cover_url VARCHAR(500) NULL, remark VARCHAR(500) NULL, released_on DATE NULL)");
        var admin = TestTokens.Issue("root", null, Admin);

        // 主库的表：关键字过滤、分页总数
        var (listed, tables) = await SendAsync(server, HttpMethod.Post, "/api/codegen-tables:db-tables", """{"keyword":"demo","page":{"index":1,"size":1}}""", admin);
        Assert.Equal(HttpStatusCode.OK, listed.StatusCode);
        Assert.Equal(2, tables.GetProperty("total").GetInt32());
        Assert.Single(tables.GetProperty("items").EnumerateArray());

        // 按表隔离：已导入与不存在的各记一条失败
        var (imported, result) = await SendAsync(server, HttpMethod.Post, "/api/codegen-tables:import", """{"tableNames":["sys_demo_category","demo_product","no_such_table"]}""", admin);
        Assert.Equal(HttpStatusCode.OK, imported.StatusCode);
        Assert.Equal(["sys_demo_category", "demo_product"], result.GetProperty("succeeded").EnumerateArray().Select(static n => n.GetString()));
        Assert.Equal("PERSISTENCE_NOTFOUND_003", result.GetProperty("failed")[0].GetProperty("code").GetString());
        var (_, again) = await SendAsync(server, HttpMethod.Post, "/api/codegen-tables:import", """{"tableNames":["demo_product"]}""", admin);
        Assert.Equal(CodeGenerationErrors.TableAlreadyImported, again.GetProperty("failed")[0].GetProperty("code").GetString());

        // 推断：去掉 sys_ 前缀、有父级列与名称列即树表、基类的列不进表单
        var category = await DetailOfAsync(server, admin, "sys_demo_category");
        Assert.Equal("DemoCategory", category.GetProperty("table").GetProperty("className").GetString());
        Assert.Equal("Tree", category.GetProperty("table").GetProperty("templateType").GetString());
        Assert.Equal(("parent_id", "name"), (category.GetProperty("table").GetProperty("treeParentColumn").GetString(), category.GetProperty("table").GetProperty("treeNameColumn").GetString()));
        Assert.Equal("Sqlite", category.GetProperty("table").GetProperty("databaseType").GetString());
        Assert.True(Column(category, "tenant_id").GetProperty("isBase").GetBoolean());
        Assert.False(Column(category, "tenant_id").GetProperty("isInsert").GetBoolean());
        Assert.True(Column(category, "name").GetProperty("isQuery").GetBoolean());

        // 类型与语义：定点数、布尔、日期、封面图、备注
        var product = await DetailOfAsync(server, admin, "demo_product");
        Assert.Equal("Single", product.GetProperty("table").GetProperty("templateType").GetString());
        Assert.Equal(("decimal?", "InputNumber"), (Column(product, "price").GetProperty("cSharpType").GetString(), Column(product, "price").GetProperty("htmlType").GetString()));
        Assert.Equal(("bool", "Switch"), (Column(product, "on_sale").GetProperty("cSharpType").GetString(), Column(product, "on_sale").GetProperty("htmlType").GetString()));
        Assert.Equal("DatePicker", Column(product, "released_on").GetProperty("htmlType").GetString());
        Assert.Equal("ImageUpload", Column(product, "cover_url").GetProperty("htmlType").GetString());
        Assert.Equal("Textarea", Column(product, "remark").GetProperty("htmlType").GetString());
        Assert.True(Column(product, "product_code").GetProperty("isRequired").GetBoolean());
    }

    [Fact]
    public async Task Update_TracksHandEditedFields_ThatSyncKeeps_AndSyncFollowsTheSchema()
    {
        await using var server = await StartAsync();
        Execute(_database.ConnectionString, "CREATE TABLE demo_order (id TEXT PRIMARY KEY NOT NULL, order_no VARCHAR(30) NOT NULL, amount DECIMAL(18,2) NOT NULL, legacy VARCHAR(10) NULL)");
        var admin = TestTokens.Issue("root", null, Admin);
        await SendAsync(server, HttpMethod.Post, "/api/codegen-tables:import", """{"tableNames":["demo_order"]}""", admin);
        var detail = await DetailOfAsync(server, admin, "demo_order");
        var table = detail.GetProperty("table");
        var id = table.GetProperty("id").GetString();

        // 通用模式、只勾导入都拒绝
        var body = new Dictionary<string, object?>
        {
            ["concurrencyStamp"] = table.GetProperty("concurrencyStamp").GetString(),
            ["tableName"] = "demo_order",
            ["className"] = "SalesOrder",
            ["templateType"] = "Universal",
            ["deliveryType"] = "Zip",
            ["scope"] = "All",
            ["target"] = "NewPackage",
            ["databaseType"] = "Sqlite",
            ["status"] = "Active",
            ["moduleName"] = "Sales",
            ["namespace"] = "XiHan.BasicApp.Sales",
        };
        var (universal, universalBody) = await SendAsync(server, HttpMethod.Put, $"/api/codegen-tables/{id}", JsonSerializer.Serialize(body), admin);
        Assert.Equal((HttpStatusCode.BadRequest, CodeGenerationErrors.TemplateTypeUniversal), (universal.StatusCode, universalBody.GetProperty("code").GetString()));
        body["templateType"] = "Single";
        body["enabledActions"] = "import";
        var (conflict, conflictBody) = await SendAsync(server, HttpMethod.Put, $"/api/codegen-tables/{id}", JsonSerializer.Serialize(body), admin);
        Assert.Equal((HttpStatusCode.BadRequest, CodeGenerationErrors.ActionsConflict), (conflict.StatusCode, conflictBody.GetProperty("code").GetString()));
        body["enabledActions"] = "create,update,import";
        var (updated, _) = await SendAsync(server, HttpMethod.Put, $"/api/codegen-tables/{id}", JsonSerializer.Serialize(body), admin);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

        // 列配置：人工改了金额的控件
        var amount = Column(detail, "amount");
        var save = new
        {
            tableId = id,
            columns = new[]
            {
                InputOf(amount, ("isRequired", true), ("isQuery", false), ("htmlType", "Input")),
            },
        };
        var (saved, _) = await SendAsync(server, HttpMethod.Post, "/api/codegen-table-columns:save", JsonSerializer.Serialize(save), admin);
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);

        // 库里加一列、删一列，再同步：类名与人工改过的控件保留，新列补进来、删掉的列去掉
        Execute(_database.ConnectionString, "ALTER TABLE demo_order ADD COLUMN paid_at DATETIME NULL", "ALTER TABLE demo_order DROP COLUMN legacy");
        var (synced, syncBody) = await SendAsync(server, HttpMethod.Post, $"/api/codegen-tables/{id}:sync-schema", null, admin);
        Assert.Equal(HttpStatusCode.OK, synced.StatusCode);
        Assert.Equal(["paid_at"], syncBody.GetProperty("addedColumns").EnumerateArray().Select(static c => c.GetString()));
        Assert.Equal(["legacy"], syncBody.GetProperty("removedColumns").EnumerateArray().Select(static c => c.GetString()));
        var after = await DetailOfAsync(server, admin, "demo_order");
        Assert.Equal("SalesOrder", after.GetProperty("table").GetProperty("className").GetString());
        Assert.Equal("Input", Column(after, "amount").GetProperty("htmlType").GetString());
        Assert.Equal("DateTimePicker", Column(after, "paid_at").GetProperty("htmlType").GetString());

        // 删除连带列配置；之后可以重新导入
        var (deleted, _) = await SendAsync(server, HttpMethod.Delete, $"/api/codegen-tables?ids={id}", null, admin);
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        var (columns, _) = await SendAsync(server, HttpMethod.Get, $"/api/codegen-table-columns?tableId={id}", null, admin);
        Assert.Equal(HttpStatusCode.NotFound, columns.StatusCode);
        var (reimported, reimportBody) = await SendAsync(server, HttpMethod.Post, "/api/codegen-tables:import", """{"tableNames":["demo_order"]}""", admin);
        Assert.Equal((HttpStatusCode.OK, 1), (reimported.StatusCode, reimportBody.GetProperty("succeeded").GetArrayLength()));
    }

    [Fact]
    public async Task Columns_KeepOnlyTheActiveOptionSource_AndRejectIncompleteOnes()
    {
        await using var server = await StartAsync();
        Execute(_database.ConnectionString, "CREATE TABLE demo_task (id TEXT PRIMARY KEY NOT NULL, state VARCHAR(20) NOT NULL, category VARCHAR(20) NULL)");
        var admin = TestTokens.Issue("root", null, Admin);
        await SendAsync(server, HttpMethod.Post, "/api/codegen-tables:import", """{"tableNames":["demo_task"]}""", admin);
        var detail = await DetailOfAsync(server, admin, "demo_task");
        var id = detail.GetProperty("table").GetProperty("id").GetString();
        var state = Column(detail, "state");

        Dictionary<string, object?> Input(string? selector, string? dictCode = null, string? constValues = null, string? enumTypeName = null) => InputOf(
            state,
            ("isRequired", true),
            ("isQuery", true),
            ("queryType", "Equal"),
            ("htmlType", "Select"),
            ("dictSelectorType", selector),
            ("dictCode", dictCode),
            ("enumTypeName", enumTypeName),
            ("constValues", constValues));

        var (missing, missingBody) = await SendAsync(server, HttpMethod.Post, "/api/codegen-table-columns:save", JsonSerializer.Serialize(new { tableId = id, columns = new[] { Input("DictSelector") } }), admin);
        Assert.Equal((HttpStatusCode.BadRequest, CodeGenerationErrors.SelectorInvalid), (missing.StatusCode, missingBody.GetProperty("code").GetString()));
        var (badJson, _) = await SendAsync(server, HttpMethod.Post, "/api/codegen-table-columns:save", JsonSerializer.Serialize(new { tableId = id, columns = new[] { Input("ConstSelector", constValues: "[{") } }), admin);
        Assert.Equal(HttpStatusCode.BadRequest, badJson.StatusCode);

        // 常量来源：前一种来源的残留值被清掉
        var (saved, savedBody) = await SendAsync(server, HttpMethod.Post, "/api/codegen-table-columns:save", JsonSerializer.Serialize(new { tableId = id, columns = new[] { Input("ConstSelector", dictCode: "leftover", constValues: """[{"value":"open","label":"Open"}]""") } }), admin);
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        var stored = savedBody.GetProperty("items").EnumerateArray().Single(c => c.GetProperty("columnName").GetString() == "state");
        Assert.Equal(JsonValueKind.Null, stored.GetProperty("dictCode").ValueKind);
        Assert.Equal("ConstSelector", stored.GetProperty("dictSelectorType").GetString());

        // 不属于这张表的列
        var (foreign, foreignBody) = await SendAsync(server, HttpMethod.Post, "/api/codegen-table-columns:save", JsonSerializer.Serialize(new { tableId = id, columns = new[] { InputOf(state, ("id", Guid.NewGuid()), ("concurrencyStamp", "x")) } }), admin);
        Assert.Equal((HttpStatusCode.BadRequest, CodeGenerationErrors.ColumnNotInTable), (foreign.StatusCode, foreignBody.GetProperty("code").GetString()));
    }

    [Fact]
    public async Task RegisteredEntities_AreReused_OnlyOnThePrimaryDatabase()
    {
        await using var server = await StartAsync();
        var admin = TestTokens.Issue("root", null, Admin);

        // 主库里登记过的实体：类名、命名空间、包名取实体，枚举属性推为枚举选项，目标缺省加入已有包
        var (_, result) = await SendAsync(server, HttpMethod.Post, "/api/codegen-tables:import", """{"tableNames":["sys_code_gen_template"]}""", admin);
        Assert.Equal(1, result.GetProperty("succeeded").GetArrayLength());
        var detail = await DetailOfAsync(server, admin, "sys_code_gen_template");
        var table = detail.GetProperty("table");
        Assert.Equal(("SysCodeGenTemplate", "XiHan.BasicApp.CodeGeneration", "CodeGeneration", "ExistingPackage"), (table.GetProperty("className").GetString(), table.GetProperty("namespace").GetString(), table.GetProperty("moduleName").GetString(), table.GetProperty("target").GetString()));
        var status = Column(detail, "status");
        Assert.Equal(("EnumSelector", "XiHan.BasicApp.CodeGeneration.Domain.Entities.CodeGenStatus", "CodeGenStatus"), (status.GetProperty("dictSelectorType").GetString(), status.GetProperty("enumTypeName").GetString(), status.GetProperty("cSharpType").GetString()));

        // 外部库里同名的表不是本应用的实体
        Execute(_external.ConnectionString, "CREATE TABLE sys_code_gen_template (id TEXT PRIMARY KEY NOT NULL, template_code VARCHAR(100) NOT NULL, status INTEGER NOT NULL)");
        var (_, source) = await SendAsync(server, HttpMethod.Post, "/api/codegen-datasources", JsonSerializer.Serialize(new { sourceName = "external", databaseType = "Sqlite", host = "localhost", port = 0, databaseName = _external.Path, userName = "none" }), admin);
        var sourceId = source.GetProperty("id").GetString();
        var (_, external) = await SendAsync(server, HttpMethod.Post, "/api/codegen-tables:db-tables", JsonSerializer.Serialize(new { dataSourceId = sourceId, page = new { index = 1, size = 20 } }), admin);
        Assert.Equal(["sys_code_gen_template"], external.GetProperty("items").EnumerateArray().Select(static t => t.GetProperty("name").GetString()));

        // 停用的数据源不能用
        var (_, stamp) = await SendAsync(server, HttpMethod.Get, $"/api/codegen-datasources/{sourceId}", null, admin);
        await SendAsync(server, HttpMethod.Post, $"/api/codegen-datasources/{sourceId}:set-status", JsonSerializer.Serialize(new { concurrencyStamp = stamp.GetProperty("concurrencyStamp").GetString(), status = "Disabled" }), admin);
        var (disabled, disabledBody) = await SendAsync(server, HttpMethod.Post, "/api/codegen-tables:db-tables", JsonSerializer.Serialize(new { dataSourceId = sourceId, page = new { index = 1, size = 20 } }), admin);
        Assert.Equal((HttpStatusCode.Conflict, CodeGenerationErrors.DataSourceDisabled), (disabled.StatusCode, disabledBody.GetProperty("code").GetString()));
    }
}
