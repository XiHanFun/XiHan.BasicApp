// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IO.Compression;
using System.Net;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;
using XiHan.BasicApp.CodeGeneration;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证代码生成的生成、生成历史与产物包下载。
/// </summary>
/// <remarks>
/// 照 4.x：每次生成不论成败都记一条历史，成功才回写表的生成状态；打包的产物分 <c>_generated/</c>、<c>_manual/</c> 与 <c>README.txt</c>，
/// 6.0 存成 7 天的临时文件、从历史的下载端点取；写进项目只在开发环境且打开开关时，只首次写的文件已存在即跳过，项目里有同名的手写文件即整体拒绝。
/// </remarks>
public sealed class CodeGenerationGenerateTests : IDisposable
{
    private const string Admin = "codegen-admin";

    private readonly SqliteDatabase _database = new();
    private readonly string _root = Path.Combine(Path.GetTempPath(), "xihan-codegen-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        _database.Dispose();
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private async Task<BasicAppTestServer> StartAsync(bool development = false)
    {
        var backend = Path.Combine(_root, "backend");
        var frontend = Path.Combine(_root, "frontend");
        Directory.CreateDirectory(Path.Combine(backend, "src"));
        Directory.CreateDirectory(frontend);
        await File.WriteAllTextAsync(Path.Combine(frontend, "package.json"), "{}");
        var server = await BasicAppTestServer.StartAsync(
            _database.ConnectionString,
            settings:
            [
                new("BasicApp:Files:LocalRootPath", Path.Combine(_root, "files")),
                // 开发环境的 appsettings.Development.json 把框架日志开到 Debug，用例里压回去
                new("Logging:LogLevel:XiHan", "Warning"),
                new("BasicApp:CodeGeneration:EnableGenerateToProject", "true"),
                new("BasicApp:CodeGeneration:BackendRootPath", backend),
                new("BasicApp:CodeGeneration:FrontendRootPath", frontend),
            ],
            args: development ? ["--environment", Environments.Development] : null);
        await server.SeedDefaultsAsync();
        await server.SeedAsync([], [.. CodeGenerationPermissions.All.Select(static p => new PermissionGrant(GrantSubject.ForRole(Admin), p.Code))]);
        await server.RunSeedersAsync(["codegeneration.templates", "files.storage-configs"]);
        return server;
    }

    private static Task<(HttpResponseMessage Response, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, object? body, string token)
        => server.SendAsync(BasicAppTestServer.Request(method, path, body is null ? null : body as string ?? JsonSerializer.Serialize(body), token));

    // 一张演示表导入并配成新建包 Demo 的资源 Item
    private async Task<JsonElement> PrepareAsync(BasicAppTestServer server, string token, string delivery, string target = "NewPackage")
    {
        using (var connection = new SqliteConnection(_database.ConnectionString))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE demo_item (id TEXT PRIMARY KEY NOT NULL, item_code VARCHAR(30) NOT NULL, item_name VARCHAR(100) NOT NULL, tenant_id VARCHAR(64) NOT NULL, created_at DATETIME NOT NULL, is_deleted INTEGER NOT NULL)";
            command.ExecuteNonQuery();
        }

        var (imported, _) = await SendAsync(server, HttpMethod.Post, "/api/codegen-tables:import", """{"tableNames":["demo_item"]}""", token);
        Assert.Equal(HttpStatusCode.OK, imported.StatusCode);
        var detail = await DetailAsync(server, token);
        var table = detail.GetProperty("table");
        string[] fields =
        [
            "concurrencyStamp", "tableName", "className", "templateType", "deliveryType", "scope", "target", "databaseType", "status", "tableComment",
            "namespace", "moduleName", "businessName", "functionName", "author", "enabledActions", "primaryKeyColumn", "treeParentColumn", "treeNameColumn",
            "masterTableId", "masterForeignKey", "dataSourceId", "options", "remark",
        ];
        var body = fields.ToDictionary(static f => f, f => (object?)table.GetProperty(f).Clone());
        body["className"] = "Item";
        body["namespace"] = "XiHan.BasicApp.Demo";
        body["moduleName"] = "Demo";
        body["deliveryType"] = delivery;
        body["target"] = target;
        body["enabledActions"] = "create,update,delete";
        var (updated, updatedBody) = await SendAsync(server, HttpMethod.Put, $"/api/codegen-tables/{table.GetProperty("id").GetString()}", body, token);
        Assert.True(updated.StatusCode == HttpStatusCode.OK, updatedBody.ToString());
        return (await DetailAsync(server, token)).GetProperty("table");
    }

    private static async Task<JsonElement> DetailAsync(BasicAppTestServer server, string token)
    {
        var (_, list) = await SendAsync(server, HttpMethod.Post, "/api/codegen-tables:list", new { page = new { index = 1, size = 10 }, keyword = "demo_item" }, token);
        var id = list.GetProperty("items")[0].GetProperty("id").GetString();
        var (_, detail) = await SendAsync(server, HttpMethod.Get, $"/api/codegen-tables/{id}", null, token);
        return detail;
    }

    private static async Task<JsonElement> HistoriesAsync(BasicAppTestServer server, string token)
    {
        var (listed, body) = await SendAsync(server, HttpMethod.Post, "/api/codegen-histories:list", new { page = new { index = 1, size = 10 }, keyword = "demo_item" }, token);
        Assert.True(listed.StatusCode == HttpStatusCode.OK, body.ToString());
        return body.GetProperty("items");
    }

    [Fact]
    public async Task Generate_Zip_RecordsHistory_AndThePackageDownloads()
    {
        await using var server = await StartAsync();
        var admin = TestTokens.Issue("root", null, Admin);
        var table = await PrepareAsync(server, admin, "Zip");
        var tableId = table.GetProperty("id").GetString();

        var (generated, result) = await SendAsync(server, HttpMethod.Post, $"/api/codegen-tables/{tableId}:generate", new { }, admin);
        Assert.True(generated.StatusCode == HttpStatusCode.OK, result.ToString());
        Assert.Equal("Zip", result.GetProperty("deliveryType").GetString());
        Assert.True(result.GetProperty("fileCount").GetInt32() > 10);
        Assert.Equal(JsonValueKind.String, result.GetProperty("fileId").ValueKind);

        // 历史：成功、带产物文件与用到的模板；表的生成状态回写
        var historyId = result.GetProperty("historyId").GetString();
        var (_, history) = await SendAsync(server, HttpMethod.Get, $"/api/codegen-histories/{historyId}", null, admin);
        Assert.Equal("Generated", history.GetProperty("history").GetProperty("generationStatus").GetString());
        Assert.Equal(JsonValueKind.String, history.GetProperty("history").GetProperty("packageExpiresAt").ValueKind);
        Assert.Contains(history.GetProperty("generatedFiles").EnumerateArray(), static f => f.GetString() == "backend/src/XiHan.BasicApp.Demo/Domain/Entities/Item.Generated.cs");
        Assert.Contains(history.GetProperty("usedTemplates").EnumerateArray(), static t => t.GetString() == "frontend.page");
        Assert.Equal("Item", history.GetProperty("tableSnapshot").GetProperty("className").GetString());
        Assert.Equal("root", history.GetProperty("operatorId").GetString());
        Assert.Equal("Generated", (await DetailAsync(server, admin)).GetProperty("table").GetProperty("generationStatus").GetString());
        Assert.Single((await HistoriesAsync(server, admin)).EnumerateArray());

        // 产物包：每次覆盖的进 _generated/，只首次写的进 _manual/，登记片段与说明在包根
        using var download = BasicAppTestServer.Request(HttpMethod.Get, $"/api/codegen-histories/{historyId}/package", token: admin);
        using var response = await server.Client.SendAsync(download);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/zip", response.Content.Headers.ContentType?.MediaType);
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        var entries = archive.Entries.Select(static e => e.FullName).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("_generated/backend/src/XiHan.BasicApp.Demo/Domain/Entities/Item.Generated.cs", entries);
        Assert.Contains("_manual/backend/src/XiHan.BasicApp.Demo/Domain/Entities/Item.cs", entries);
        Assert.Contains("_manual/frontend/src/modules/demo/views/items/index.vue", entries);
        Assert.Contains("_generated/frontend/src/modules/demo/views/items/items.schema.generated.ts", entries);
        Assert.Contains("_registration/items.md", entries);
        Assert.Contains("README.txt", entries);

        // 没有平台的生成历史读权限即看不到；租户一侧没有代码生成
        var (denied, _) = await SendAsync(server, HttpMethod.Get, $"/api/codegen-histories/{historyId}", null, TestTokens.Issue("alice", "acme", "admin"));
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    [Fact]
    public async Task Generate_Failure_StillRecordsHistory_AndHasNoPackage()
    {
        await using var server = await StartAsync();
        var admin = TestTokens.Issue("root", null, Admin);
        var table = await PrepareAsync(server, admin, "Zip");
        var tableId = table.GetProperty("id").GetString();

        // 只用一份不存在的模板：没有可用的模板，调用失败，历史照样记一条失败
        var (failed, failedBody) = await SendAsync(server, HttpMethod.Post, $"/api/codegen-tables/{tableId}:generate", $$"""{"id":"{{tableId}}","templateCodes":["no.such.template"]}""", admin);
        Assert.Equal((HttpStatusCode.BadRequest, CodeGenerationErrors.NoTemplates), (failed.StatusCode, failedBody.GetProperty("code").GetString()));

        var histories = await HistoriesAsync(server, admin);
        var history = Assert.Single(histories.EnumerateArray());
        Assert.Equal("Failed", history.GetProperty("generationStatus").GetString());
        Assert.Equal(JsonValueKind.Null, history.GetProperty("packageExpiresAt").ValueKind);
        var (_, detail) = await SendAsync(server, HttpMethod.Get, $"/api/codegen-histories/{history.GetProperty("id").GetString()}", null, admin);
        Assert.StartsWith(CodeGenerationErrors.NoTemplates + ": ", detail.GetProperty("errorMessage").GetString(), StringComparison.Ordinal);
        Assert.Equal("NotGenerated", (await DetailAsync(server, admin)).GetProperty("table").GetProperty("generationStatus").GetString());

        var (missing, missingBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/codegen-histories/{history.GetProperty("id").GetString()}/package", token: admin));
        Assert.Equal((HttpStatusCode.NotFound, CodeGenerationErrors.PackageNotFound), (missing.StatusCode, missingBody.GetProperty("code").GetString()));
    }

    [Fact]
    public async Task Generate_ToProject_IsRejectedOutsideDevelopment()
    {
        await using var server = await StartAsync();
        var admin = TestTokens.Issue("root", null, Admin);
        var table = await PrepareAsync(server, admin, "Project");

        var (rejected, body) = await SendAsync(server, HttpMethod.Post, $"/api/codegen-tables/{table.GetProperty("id").GetString()}:generate", new { }, admin);
        Assert.Equal((HttpStatusCode.BadRequest, CodeGenerationErrors.ProjectWriteRejected), (rejected.StatusCode, body.GetProperty("code").GetString()));
        Assert.Empty(Directory.EnumerateFiles(Path.Combine(_root, "backend", "src"), "*", SearchOption.AllDirectories));
        Assert.Equal("Failed", (await HistoriesAsync(server, admin))[0].GetProperty("generationStatus").GetString());
    }

    [Fact]
    public async Task Generate_ToProject_WritesFiles_SkipsExistingManualOnes_AndRejectsForeignFiles()
    {
        await using var server = await StartAsync(development: true);
        var admin = TestTokens.Issue("root", null, Admin);
        var table = await PrepareAsync(server, admin, "Project");
        var path = $"/api/codegen-tables/{table.GetProperty("id").GetString()}:generate";
        var package = Path.Combine(_root, "backend", "src", "XiHan.BasicApp.Demo");
        var manual = Path.Combine(package, "Domain", "Entities", "Item.cs");
        var generatedEntity = Path.Combine(package, "Domain", "Entities", "Item.Generated.cs");

        var (first, firstBody) = await SendAsync(server, HttpMethod.Post, path, new { }, admin);
        Assert.True(first.StatusCode == HttpStatusCode.OK, firstBody.ToString());
        Assert.Equal(firstBody.GetProperty("fileCount").GetInt32(), firstBody.GetProperty("writtenCount").GetInt32());
        Assert.Empty(firstBody.GetProperty("skippedPaths").EnumerateArray());
        Assert.True(File.Exists(generatedEntity));
        Assert.True(File.Exists(Path.Combine(_root, "frontend", "src", "modules", "demo", "views", "items", "index.vue")));
        Assert.True(File.Exists(Path.Combine(_root, "backend", "_registration", "items.md")));

        // 再生成：手改过的只首次写的文件跳过、保留改动，每次覆盖的照写
        await File.WriteAllTextAsync(manual, "// 手写的代码");
        await File.WriteAllTextAsync(generatedEntity, "// 会被覆盖");
        var (second, secondBody) = await SendAsync(server, HttpMethod.Post, path, new { }, admin);
        Assert.True(second.StatusCode == HttpStatusCode.OK, secondBody.ToString());
        Assert.Contains(secondBody.GetProperty("skippedPaths").EnumerateArray(), static p => p.GetString() == "src/XiHan.BasicApp.Demo/Domain/Entities/Item.cs");
        Assert.Equal("// 手写的代码", await File.ReadAllTextAsync(manual));
        Assert.Contains("public sealed partial class Item", await File.ReadAllTextAsync(generatedEntity), StringComparison.Ordinal);

        // 只首次写的文件在、生成部分却没有：是项目里原有的手写文件，整体拒绝、什么都不写
        File.Delete(generatedEntity);
        var (foreign, foreignBody) = await SendAsync(server, HttpMethod.Post, path, new { }, admin);
        Assert.Equal((HttpStatusCode.BadRequest, CodeGenerationErrors.ProjectWriteRejected), (foreign.StatusCode, foreignBody.GetProperty("code").GetString()));
        Assert.Contains("Item.Generated.cs", foreignBody.GetProperty("detail").GetString(), StringComparison.Ordinal);
        Assert.False(File.Exists(generatedEntity));

        var outcomes = (await HistoriesAsync(server, admin)).EnumerateArray().Select(static h => h.GetProperty("generationStatus").GetString());
        Assert.Equal("Failed,Generated,Generated", string.Join(',', outcomes));
    }

    [Fact]
    public async Task Generate_ToExistingPackage_NeedsThePackageInTheProject()
    {
        await using var server = await StartAsync(development: true);
        var admin = TestTokens.Issue("root", null, Admin);
        var table = await PrepareAsync(server, admin, "Project", target: "ExistingPackage");

        var (rejected, body) = await SendAsync(server, HttpMethod.Post, $"/api/codegen-tables/{table.GetProperty("id").GetString()}:generate", new { }, admin);
        Assert.Equal((HttpStatusCode.BadRequest, CodeGenerationErrors.ProjectWriteRejected), (rejected.StatusCode, body.GetProperty("code").GetString()));
        Assert.Contains("XiHan.BasicApp.Demo", body.GetProperty("detail").GetString(), StringComparison.Ordinal);
    }
}
