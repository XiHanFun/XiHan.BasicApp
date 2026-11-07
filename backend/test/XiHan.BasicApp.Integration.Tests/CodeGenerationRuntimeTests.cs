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
/// 验证代码生成的零代码运行时
/// </summary>
/// <remarks>
/// 只读已配置且启用的表配置，按列配置给表结构、按表名分页读数据，主库的表只看调用方这一侧未删的行；
/// 按表配置的数据源读（外部库的表不套租户与软删），数据行按属性名给键，停用即冲突，库里读不出即不可用
/// </remarks>
public sealed class CodeGenerationRuntimeTests : IDisposable
{
    private const string Admin = "codegen-admin";
    private const string Viewer = "codegen-viewer";

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
        await server.SeedAsync([], [
            .. CodeGenerationPermissions.All.Select(static p => new PermissionGrant(GrantSubject.ForRole(Admin), p.Code)),
            new PermissionGrant(GrantSubject.ForRole(Viewer), CodeGenerationPermissions.TablesRead),
        ]);
        return server;
    }

    private static Task<(HttpResponseMessage Response, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, object? body, string token)
        => server.SendAsync(BasicAppTestServer.Request(method, path, body is null ? null : body as string ?? JsonSerializer.Serialize(body), token));

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

    private static async Task<string> ImportAsync(BasicAppTestServer server, string token, string tableName, string? dataSourceId = null)
    {
        var (imported, result) = await SendAsync(server, HttpMethod.Post, "/api/codegen-tables:import", new { tableNames = new[] { tableName }, dataSourceId }, token);
        Assert.True(imported.StatusCode == HttpStatusCode.OK, result.ToString());
        var (_, list) = await SendAsync(server, HttpMethod.Post, "/api/codegen-tables:list", new { page = new { index = 1, size = 10 }, keyword = tableName }, token);
        return list.GetProperty("items")[0].GetProperty("id").GetString()!;
    }

    private static Task<(HttpResponseMessage Response, JsonElement Body)> ListAsync(BasicAppTestServer server, string token, string id, int index = 1, int size = 20)
        => SendAsync(server, HttpMethod.Post, "/api/codegen-runtime:list", new { id, page = new { index, size } }, token);

    [Fact]
    public async Task MainDatabase_ReadsOnlyTheCallersSide_KeyedByProperty()
    {
        Execute(
            _database.ConnectionString,
            "CREATE TABLE demo_item (id TEXT PRIMARY KEY NOT NULL, item_name VARCHAR(100) NOT NULL, qty INTEGER NOT NULL, price REAL NULL, tenant_id VARCHAR(64) NOT NULL, is_deleted INTEGER NOT NULL)",
            "INSERT INTO demo_item VALUES ('a', '甲', 1, 1.5, 'platform', 0), ('b', '乙', 2, NULL, 'platform', 0), ('c', '丙', 3, NULL, 'platform', 0), ('d', '丁', 4, NULL, 'platform', 1), ('e', '戊', 5, NULL, 't1', 0)");
        await using var server = await StartAsync();
        var admin = TestTokens.Issue("root", null, Admin);
        var id = await ImportAsync(server, admin, "demo_item");

        // 表结构：列名照原样，数据行里的键是属性名的 camelCase
        var (described, schema) = await SendAsync(server, HttpMethod.Get, $"/api/codegen-runtime/{id}:get-schema", null, admin);
        Assert.True(described.StatusCode == HttpStatusCode.OK, schema.ToString());
        Assert.Equal("demo_item", schema.GetProperty("tableName").GetString());
        var columns = schema.GetProperty("columns").EnumerateArray().ToDictionary(static c => c.GetProperty("columnName").GetString()!, static c => c.GetProperty("propertyName").GetString());
        Assert.Equal("itemName", columns["item_name"]);
        Assert.Equal("tenantId", columns["tenant_id"]);

        // 数据：只看平台这一侧未删的行，分页
        var (read, page) = await ListAsync(server, admin, id, size: 2);
        Assert.True(read.StatusCode == HttpStatusCode.OK, page.ToString());
        Assert.Equal(3, page.GetProperty("total").GetInt64());
        Assert.Equal(2, page.GetProperty("items").GetArrayLength());
        var (_, all) = await ListAsync(server, admin, id);
        var rows = all.GetProperty("items").EnumerateArray().ToDictionary(static r => r.GetProperty("id").GetString()!);
        Assert.Equal(["a", "b", "c"], rows.Keys.Order());
        Assert.Equal("甲", rows["a"].GetProperty("itemName").GetString());
        Assert.Equal(1, rows["a"].GetProperty("qty").GetInt32());
        Assert.Equal(1.5, rows["a"].GetProperty("price").GetDouble());
        Assert.Equal(JsonValueKind.Null, rows["b"].GetProperty("price").ValueKind);
        Assert.False(rows["a"].TryGetProperty("item_name", out _));

        // 每页最多 500 条；没有运行时的读权限即拒绝
        var (tooLarge, _) = await ListAsync(server, admin, id, size: 501);
        Assert.Equal(HttpStatusCode.BadRequest, tooLarge.StatusCode);
        var viewer = TestTokens.Issue("viewer", null, Viewer);
        var (forbidden, _) = await ListAsync(server, viewer, id);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        var (forbiddenSchema, _) = await SendAsync(server, HttpMethod.Get, $"/api/codegen-runtime/{id}:get-schema", null, viewer);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenSchema.StatusCode);
    }

    [Fact]
    public async Task DisabledOrUnreadableTables_AreRejected()
    {
        Execute(_database.ConnectionString, "CREATE TABLE demo_note (id TEXT PRIMARY KEY NOT NULL, title VARCHAR(50) NOT NULL)");
        await using var server = await StartAsync();
        var admin = TestTokens.Issue("root", null, Admin);
        var id = await ImportAsync(server, admin, "demo_note");

        // 表在库里没了：读不出即不可用，带上表名
        Execute(_database.ConnectionString, "DROP TABLE demo_note");
        var (unreadable, problem) = await ListAsync(server, admin, id);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, unreadable.StatusCode);
        Assert.Equal(CodeGenerationErrors.RuntimeReadFailed, problem.GetProperty("code").GetString());

        // 停用的表配置：表结构与数据都拒绝
        var (_, detail) = await SendAsync(server, HttpMethod.Get, $"/api/codegen-tables/{id}", null, admin);
        var stamp = detail.GetProperty("table").GetProperty("concurrencyStamp").GetString();
        var (disabled, disabledBody) = await SendAsync(server, HttpMethod.Post, $"/api/codegen-tables/{id}:set-status", new { concurrencyStamp = stamp, status = "Disabled" }, admin);
        Assert.True(disabled.StatusCode == HttpStatusCode.OK, disabledBody.ToString());
        var (conflict, conflictBody) = await ListAsync(server, admin, id);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal(CodeGenerationErrors.TableDisabled, conflictBody.GetProperty("code").GetString());
        var (schemaConflict, _) = await SendAsync(server, HttpMethod.Get, $"/api/codegen-runtime/{id}:get-schema", null, admin);
        Assert.Equal(HttpStatusCode.Conflict, schemaConflict.StatusCode);

        // 没有的表配置
        var (missing, _) = await ListAsync(server, admin, Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task ExternalSource_IsReadAsIs()
    {
        // 外部库的表不是 BasicApp 的表：租户与软删的列照原样读，不套条件
        Execute(
            _external.ConnectionString,
            "CREATE TABLE ext_order (order_no VARCHAR(20) PRIMARY KEY NOT NULL, tenant_id VARCHAR(64) NULL, is_deleted INTEGER NOT NULL)",
            "INSERT INTO ext_order VALUES ('N1', 'other', 1), ('N2', NULL, 0)");
        await using var server = await StartAsync();
        var admin = TestTokens.Issue("root", null, Admin);
        var (created, source) = await SendAsync(server, HttpMethod.Post, "/api/codegen-datasources", new { sourceName = "ext", databaseType = "Sqlite", host = "localhost", port = 0, databaseName = _external.Path, userName = "none" }, admin);
        Assert.True(created.StatusCode == HttpStatusCode.Created, source.ToString());
        var id = await ImportAsync(server, admin, "ext_order", source.GetProperty("id").GetString());

        var (read, page) = await ListAsync(server, admin, id);
        Assert.True(read.StatusCode == HttpStatusCode.OK, page.ToString());
        Assert.Equal(2, page.GetProperty("total").GetInt64());
        var rows = page.GetProperty("items").EnumerateArray().ToDictionary(static r => r.GetProperty("orderNo").GetString()!);
        Assert.Equal("other", rows["N1"].GetProperty("tenantId").GetString());
        Assert.Equal(1, rows["N1"].GetProperty("isDeleted").GetInt32());
    }
}
