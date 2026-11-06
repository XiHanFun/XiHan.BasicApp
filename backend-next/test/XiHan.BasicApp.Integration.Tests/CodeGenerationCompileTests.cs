// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics;
using System.Formats.Tar;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;
using XiHan.BasicApp.CodeGeneration;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 代码生成的退出验证：生成的新包能编译、能过前端门禁
/// </summary>
/// <remarks>
/// 方案 N3 第四批的退出条件，手动触发、不进缺省 CI：设 <c>XIHAN_TEST_CODEGEN_COMPILE</c> 为框架源码的 <c>src/</c> 目录（同 <c>-p:XiHanFrameworkSrc</c>）才跑，
/// 前端要先在本检出的 <c>frontend-next</c> 装好依赖。把当前提交的 <c>backend-next</c> 与 <c>frontend-next</c> 导出到临时目录，
/// 四张演示表（树表 Catalog 带生成的启停枚举；单表 Shop 带关联树、常量下拉、系统字典、图片上传、时间、布尔、日期时间与定点数；主从 Sales / SalesItems）
/// 各配成新建包，按「生成到项目」写进去，再照生成的登记片段登记进宿主与模块门禁：宿主 Release 构建不许有警告，导出契约、生成前端契约之后前端 <c>pnpm check</c> 八项通过
/// </remarks>
public sealed class CodeGenerationCompileTests : IDisposable
{
    private const string Variable = "XIHAN_TEST_CODEGEN_COMPILE";
    private const string Admin = "codegen-admin";

    private readonly SqliteDatabase _database = new();
    private readonly string _root = Path.Combine(Path.GetTempPath(), "xihan-codegen-compile-" + Guid.NewGuid().ToString("N")[..8]);
    private readonly List<string> _links = [];

    public void Dispose()
    {
        _database.Dispose();
        // 先拆掉指向本检出依赖的链接，再删临时目录，免得删到链接的另一头
        foreach (var link in _links.Where(Directory.Exists))
        {
            Directory.Delete(link);
        }

        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public async Task NewPackages_Compile_AndPassTheFrontendGates()
    {
        var frameworkSource = Environment.GetEnvironmentVariable(Variable);
        Assert.SkipWhen(string.IsNullOrWhiteSpace(frameworkSource), $"{Variable} is not set; set it to the framework src/ directory to run the code generation exit check.");
        var repository = RepositoryRoot();
        var nodeModules = Path.Combine(repository, "frontend-next", "node_modules");
        Assert.True(Directory.Exists(nodeModules), $"Install the frontend dependencies first: {nodeModules} is missing.");
        var cancellation = TestContext.Current.CancellationToken;

        // 当前提交的两个工程导出到临时目录；前端依赖用本检出装好的
        var checkout = Path.Combine(_root, "checkout");
        Directory.CreateDirectory(checkout);
        var archive = Path.Combine(_root, "head.tar");
        await RunAsync("git", ["-C", repository, "archive", "--format=tar", "-o", archive, "HEAD", "backend-next", "frontend-next"], repository, TimeSpan.FromMinutes(5), cancellation);
        await TarFile.ExtractToDirectoryAsync(archive, checkout, overwriteFiles: false, cancellation);
        var backend = Path.Combine(checkout, "backend-next");
        var frontend = Path.Combine(checkout, "frontend-next");
        Mirror(nodeModules, Path.Combine(frontend, "node_modules"));
        foreach (var package in Directory.GetDirectories(Path.Combine(repository, "frontend-next", "packages")))
        {
            var modules = Path.Combine(package, "node_modules");
            if (Directory.Exists(modules))
            {
                Mirror(modules, Path.Combine(frontend, "packages", Path.GetFileName(package), "node_modules"));
            }
        }

        // 四个新建包按「生成到项目」写进临时检出
        await using (var server = await StartAsync(backend, frontend))
        {
            var admin = TestTokens.Issue("root", null, Admin);
            foreach (var table in await PrepareAsync(server, admin))
            {
                var (generated, result) = await SendAsync(server, HttpMethod.Post, $"/api/codegen-tables/{table}:generate", new { deliveryType = "Project" }, admin);
                Assert.True(generated.StatusCode == HttpStatusCode.OK, result.ToString());
                Assert.True(result.GetProperty("writtenCount").GetInt32() > 0, result.ToString());
                Assert.Empty(result.GetProperty("skippedPaths").EnumerateArray());
            }
        }

        // 照生成的登记片段登记：宿主的工程引用、UseFeature、OpenAPI 分组（序号按现有分组往后排），前端模块门禁的 KNOWN
        var snippets = Directory.GetFiles(Path.Combine(backend, "_registration"), "*.md").Order(StringComparer.Ordinal).ToList();
        Assert.Equal(4, snippets.Count);
        var host = Path.Combine(backend, "src", "XiHan.BasicApp.Host");
        var order = 200;
        foreach (var snippet in snippets)
        {
            var blocks = Blocks(await File.ReadAllTextAsync(snippet, cancellation));
            Insert(Path.Combine(host, "XiHan.BasicApp.Host.csproj"), "    <ProjectReference Include=\"../XiHan.BasicApp.Contracts/", "    " + Block(blocks, "xml", "<ProjectReference") + "\n");
            Insert(Path.Combine(host, "BasicAppHost.cs"), "        });\n\n        var app = builder.Build();", "            " + Block(blocks, "csharp", "UseFeature<") + "\n");
            var group = Regex.Replace(Block(blocks, "csharp", "Group("), @",\s*\d+\)", $", {order})");
            order += 10;
            Insert(Path.Combine(host, "BasicAppOpenApi.cs"), "        ],\n    };", "            " + group + "\n");
            // KNOWN 一项一行：片段给的就是带尾逗号的一整项，插成新的一行
            Insert(Path.Combine(frontend, "scripts", "check-modules.mjs"), "  'codegeneration',\n", "  " + Block(blocks, "js", "'") + "\n");
        }

        // 后端：宿主连同四个新包 Release 构建，警告即失败
        var artifacts = Path.Combine(_root, "artifacts");
        await RunAsync("dotnet", ["build", Path.Combine(host, "XiHan.BasicApp.Host.csproj"), "-c", "Release", "-warnaserror", $"-p:XiHanFrameworkSrc={Path.TrimEndingDirectorySeparator(frameworkSource!)}{Path.DirectorySeparatorChar}", "--artifacts-path", artifacts], backend, TimeSpan.FromMinutes(30), cancellation);

        // 契约：导出之后有四个资源的操作，前端按它生成契约层
        var output = Path.Combine(artifacts, "bin", "XiHan.BasicApp.Host", "release");
        await RunAsync("dotnet", [Path.Combine(output, "XiHan.BasicApp.Host.dll"), "contracts", "export", "--out", Path.Combine(frontend, "contracts")], output, TimeSpan.FromMinutes(5), cancellation, new() { ["ASPNETCORE_ENVIRONMENT"] = Environments.Development });
        var openApi = await File.ReadAllTextAsync(Path.Combine(frontend, "contracts", "openapi.json"), cancellation);
        foreach (var operation in new[] { "categories.tree", "products.list", "sales-orders.list", "sales-order-items.list" })
        {
            Assert.Contains($"\"operationId\": \"{operation}\"", openApi, StringComparison.Ordinal);
        }

        await ShellAsync("pnpm gen:contracts", frontend, TimeSpan.FromMinutes(5), cancellation);

        // 前端：类型检查、lint 与 px、令牌、文案、模块、契约、权限门禁
        await ShellAsync("pnpm check", frontend, TimeSpan.FromMinutes(20), cancellation);
    }

    private async Task<BasicAppTestServer> StartAsync(string backend, string frontend)
    {
        var server = await BasicAppTestServer.StartAsync(
            _database.ConnectionString,
            settings:
            [
                new("Logging:LogLevel:XiHan", "Warning"),
                new("BasicApp:CodeGeneration:EnableGenerateToProject", "true"),
                new("BasicApp:CodeGeneration:BackendRootPath", backend),
                new("BasicApp:CodeGeneration:FrontendRootPath", frontend),
            ],
            args: ["--environment", Environments.Development]);
        await server.SeedDefaultsAsync();
        await server.SeedAsync([], [.. CodeGenerationPermissions.All.Select(static p => new PermissionGrant(GrantSubject.ForRole(Admin), p.Code))]);
        await server.RunSeedersAsync(["codegeneration.templates"]);
        return server;
    }

    private static Task<(HttpResponseMessage Response, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, object? body, string token)
        => server.SendAsync(BasicAppTestServer.Request(method, path, body is null ? null : body as string ?? JsonSerializer.Serialize(body), token));

    // 四张演示表导入并配好，返回表配置 ID（先树表，关联树要它在）
    private async Task<IReadOnlyList<string>> PrepareAsync(BasicAppTestServer server, string token)
    {
        using (var connection = new SqliteConnection(_database.ConnectionString))
        {
            connection.Open();
            foreach (var sql in new[]
            {
                "CREATE TABLE biz_category (id TEXT PRIMARY KEY NOT NULL, parent_id TEXT NULL, name VARCHAR(50) NOT NULL, sort INTEGER NOT NULL, status INTEGER NOT NULL, tenant_id VARCHAR(64) NOT NULL)",
                "CREATE TABLE biz_product (id TEXT PRIMARY KEY NOT NULL, product_code VARCHAR(30) NOT NULL, product_name VARCHAR(100) NOT NULL, price DECIMAL(18,2) NULL, on_sale BOOLEAN NOT NULL, released_on DATETIME NULL, category_id TEXT NULL, remark TEXT NULL, cover VARCHAR(200) NULL, level INTEGER NOT NULL, unit VARCHAR(20) NULL, open_time TIME NULL, tenant_id VARCHAR(64) NOT NULL)",
                "CREATE TABLE biz_order (id TEXT PRIMARY KEY NOT NULL, order_no VARCHAR(30) NOT NULL, customer VARCHAR(100) NULL, amount DECIMAL(18,2) NOT NULL, tenant_id VARCHAR(64) NOT NULL)",
                "CREATE TABLE biz_order_item (id TEXT PRIMARY KEY NOT NULL, order_id TEXT NOT NULL, item_name VARCHAR(100) NOT NULL, quantity INTEGER NOT NULL, tenant_id VARCHAR(64) NOT NULL)",
            })
            {
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
        }

        var (imported, importBody) = await SendAsync(server, HttpMethod.Post, "/api/codegen-tables:import", """{"tableNames":["biz_category","biz_product","biz_order","biz_order_item"]}""", token);
        Assert.True(imported.StatusCode == HttpStatusCode.OK, importBody.ToString());

        var category = await DetailAsync(server, token, "biz_category");
        await UpdateAsync(server, token, category, ("className", "Category"), ("namespace", "XiHan.BasicApp.Catalog"), ("moduleName", "Catalog"), ("templateType", "Tree"), ("treeParentColumn", "parent_id"), ("treeNameColumn", "name"), ("enabledActions", "create,update,delete,export,import,status"));
        category = await DetailAsync(server, token, "biz_category");
        await SaveAsync(server, token, category, Input(Column(category, "parent_id"), ("cSharpType", "Guid?")), Input(Column(category, "name"), ("isUnique", true), ("isQuery", true), ("queryType", "Like")));

        var product = await DetailAsync(server, token, "biz_product");
        await UpdateAsync(server, token, product, ("className", "Product"), ("namespace", "XiHan.BasicApp.Shop"), ("moduleName", "Shop"), ("businessName", "商品"), ("enabledActions", "create,update,delete,export,import,print"));
        product = await DetailAsync(server, token, "biz_product");
        await SaveAsync(server, token, product,
            Input(Column(product, "product_code"), ("isUnique", true), ("isRequired", true), ("isQuery", true), ("queryType", "Like")),
            Input(Column(product, "price"), ("isQuery", true), ("queryType", "Between")),
            Input(Column(product, "released_on"), ("isQuery", true)),
            Input(Column(product, "on_sale"), ("isQuery", true), ("queryType", "Equal")),
            Input(Column(product, "cover"), ("htmlType", "ImageUpload")),
            Input(Column(product, "level"), ("dictSelectorType", "ConstSelector"), ("constValues", "[{\"label\":\"一级\",\"value\":1},{\"label\":\"二级\",\"value\":2}]"), ("htmlType", "Select"), ("isQuery", true), ("queryType", "Equal")),
            Input(Column(product, "unit"), ("dictSelectorType", "DictSelector"), ("dictCode", "sys_unit"), ("htmlType", "Select"), ("isQuery", true), ("queryType", "Equal")),
            Input(Column(product, "open_time"), ("htmlType", "TimePicker")),
            Input(Column(product, "category_id"), ("cSharpType", "Guid?"), ("dictSelectorType", "TreeSelector"), ("relationTableId", category.GetProperty("table").GetProperty("id").GetString()), ("isQuery", true), ("queryType", "In")));

        var order = await DetailAsync(server, token, "biz_order");
        await UpdateAsync(server, token, order, ("className", "SalesOrder"), ("namespace", "XiHan.BasicApp.Sales"), ("moduleName", "Sales"), ("templateType", "Single"));
        var item = await DetailAsync(server, token, "biz_order_item");
        await UpdateAsync(server, token, item, ("className", "SalesOrderItem"), ("namespace", "XiHan.BasicApp.SalesItems"), ("moduleName", "SalesItems"), ("templateType", "MasterDetail"), ("masterTableId", order.GetProperty("table").GetProperty("id").GetString()), ("masterForeignKey", "order_id"));
        item = await DetailAsync(server, token, "biz_order_item");
        await SaveAsync(server, token, item, Input(Column(item, "order_id"), ("cSharpType", "Guid")));

        return [.. new[] { category, product, order, item }.Select(static t => t.GetProperty("table").GetProperty("id").GetString()!)];
    }

    private static async Task<JsonElement> DetailAsync(BasicAppTestServer server, string token, string tableName)
    {
        var (_, list) = await SendAsync(server, HttpMethod.Post, "/api/codegen-tables:list", new { page = new { index = 1, size = 50 }, keyword = tableName }, token);
        var id = list.GetProperty("items").EnumerateArray().Single(t => t.GetProperty("tableName").GetString() == tableName).GetProperty("id").GetString();
        return (await SendAsync(server, HttpMethod.Get, $"/api/codegen-tables/{id}", null, token)).Body;
    }

    private static JsonElement Column(JsonElement detail, string name) => detail.GetProperty("columns").EnumerateArray().Single(c => c.GetProperty("columnName").GetString() == name);

    private static Dictionary<string, object?> Input(JsonElement column, params (string Key, object? Value)[] changes)
    {
        string[] fields =
        [
            "id", "concurrencyStamp", "cSharpType", "cSharpProperty", "tsType", "isRequired", "isUnique", "isList", "isInsert", "isEdit", "isQuery", "queryType", "htmlType", "dictSelectorType",
            "dictCode", "enumTypeName", "constValues", "relationTableId", "relationLabelColumn", "defaultValue", "minValue", "maxValue", "regexPattern", "validationMessage", "order", "status",
        ];
        var input = fields.ToDictionary(static f => f, f => (object?)column.GetProperty(f).Clone());
        foreach (var (key, value) in changes)
        {
            input[key] = value;
        }

        return input;
    }

    private static async Task UpdateAsync(BasicAppTestServer server, string token, JsonElement detail, params (string Key, object? Value)[] changes)
    {
        var table = detail.GetProperty("table");
        string[] fields =
        [
            "concurrencyStamp", "tableName", "className", "templateType", "deliveryType", "scope", "target", "databaseType", "status", "tableComment", "namespace", "moduleName", "businessName",
            "functionName", "author", "enabledActions", "primaryKeyColumn", "treeParentColumn", "treeNameColumn", "masterTableId", "masterForeignKey", "dataSourceId", "options", "remark",
        ];
        var body = fields.ToDictionary(static f => f, f => (object?)table.GetProperty(f).Clone());
        foreach (var (key, value) in changes)
        {
            body[key] = value;
        }

        var (updated, updatedBody) = await SendAsync(server, HttpMethod.Put, $"/api/codegen-tables/{table.GetProperty("id").GetString()}", body, token);
        Assert.True(updated.StatusCode == HttpStatusCode.OK, updatedBody.ToString());
    }

    private static async Task SaveAsync(BasicAppTestServer server, string token, JsonElement detail, params Dictionary<string, object?>[] columns)
    {
        var (saved, savedBody) = await SendAsync(server, HttpMethod.Post, "/api/codegen-table-columns:save", new { tableId = detail.GetProperty("table").GetProperty("id").GetString(), columns }, token);
        Assert.True(saved.StatusCode == HttpStatusCode.OK, savedBody.ToString());
    }

    // 登记片段里的代码块：(语言, 内容)
    private static List<(string Language, string Code)> Blocks(string markdown)
        => [.. Regex.Matches(markdown.ReplaceLineEndings("\n"), "```(\\w+)\\n(.*?)\\n```", RegexOptions.Singleline).Select(static m => (m.Groups[1].Value, m.Groups[2].Value.Trim()))];

    private static string Block(List<(string Language, string Code)> blocks, string language, string marker)
        => blocks.Single(b => b.Language == language && b.Code.Contains(marker, StringComparison.Ordinal)).Code;

    // 在锚点之前插入一段（文件的换行照原样）
    private static void Insert(string path, string anchor, string text)
    {
        var content = File.ReadAllText(path);
        var newline = content.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var normalized = content.ReplaceLineEndings("\n");
        var at = normalized.IndexOf(anchor, StringComparison.Ordinal);
        Assert.True(at >= 0, $"{path}: anchor not found: {anchor}");
        File.WriteAllText(path, (normalized[..at] + text + normalized[at..]).ReplaceLineEndings(newline));
    }

    // 依赖目录逐项链到最终目标：pnpm 的 link: 覆盖是相对路径的符号链接，整体经联接访问会按临时目录解析错位
    private void Mirror(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (var entry in new DirectoryInfo(source).EnumerateFileSystemInfos())
        {
            var destination = Path.Combine(target, entry.Name);
            var final = entry.LinkTarget is null ? entry : entry.ResolveLinkTarget(returnFinalTarget: true)!;
            if (final is FileInfo || File.Exists(final.FullName))
            {
                File.Copy(final.FullName, destination);
            }
            else if (entry.LinkTarget is null && entry.Name.StartsWith('@'))
            {
                // 作用域目录里是一个个包的链接
                Mirror(entry.FullName, destination);
            }
            else
            {
                Link(destination, final.FullName);
            }
        }
    }

    private void Link(string link, string target)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(link)!);
        if (OperatingSystem.IsWindows())
        {
            // 目录联接不要求开发者模式
            using var process = Process.Start(new ProcessStartInfo("cmd.exe", ["/d", "/c", "mklink", "/J", link, target]) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true })!;
            process.WaitForExit();
            Assert.True(process.ExitCode == 0, process.StandardError.ReadToEnd());
        }
        else
        {
            Directory.CreateSymbolicLink(link, target);
        }

        _links.Add(link);
    }

    private static Task ShellAsync(string command, string directory, TimeSpan timeout, CancellationToken cancellationToken)
        => OperatingSystem.IsWindows()
            ? RunAsync("cmd.exe", ["/d", "/c", command], directory, timeout, cancellationToken)
            : RunAsync("/bin/sh", ["-c", command], directory, timeout, cancellationToken);

    private static async Task RunAsync(string fileName, IReadOnlyList<string> arguments, string directory, TimeSpan timeout, CancellationToken cancellationToken, Dictionary<string, string>? environment = null)
    {
        var start = new ProcessStartInfo(fileName, arguments) { WorkingDirectory = directory, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var (key, value) in environment ?? [])
        {
            start.Environment[key] = value;
        }

        var output = new StringBuilder();
        using var process = new Process { StartInfo = start };
        process.OutputDataReceived += (_, e) => { lock (output) { output.AppendLine(e.Data); } };
        process.ErrorDataReceived += (_, e) => { lock (output) { output.AppendLine(e.Data); } };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limit.CancelAfter(timeout);
        try
        {
            await process.WaitForExitAsync(limit.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        string text;
        lock (output)
        {
            text = output.ToString();
        }

        Assert.True(process.ExitCode == 0, $"{fileName} {string.Join(' ', arguments)} exited with {process.ExitCode}:\n{(text.Length > 6000 ? text[^6000..] : text)}");
    }

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "backend-next", "XiHan.BasicApp.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("The repository root (with backend-next/XiHan.BasicApp.slnx) was not found above the test output.");
    }
}
