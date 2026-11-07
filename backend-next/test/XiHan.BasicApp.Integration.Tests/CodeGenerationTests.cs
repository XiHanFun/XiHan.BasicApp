// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.CodeGeneration;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证代码生成模块的数据源与模块开关
/// </summary>
/// <remarks>
/// 数据源照 4.x：名称唯一、口令与连接串加密存储且不回显、留空即不改、缺省最多一个、启停与修改带并发戳；按 C2 修的缺陷：被表配置引用的拒绝删除、
/// 测试连接要修改权限、解不开的密钥报错不当明文；只在平台一侧；关掉模块宿主照常启动、页面不下发
/// </remarks>
public sealed class CodeGenerationTests : IDisposable
{
    private const string Admin = "codegen-admin";
    private const string Reader = "codegen-reader";

    private readonly SqliteDatabase _database = new();

    private readonly SqliteDatabase _external = new();

    public void Dispose()
    {
        _database.Dispose();
        _external.Dispose();
    }

    private async Task<BasicAppTestServer> StartAsync(IEnumerable<KeyValuePair<string, string?>>? settings = null)
    {
        var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, settings: settings);
        await server.SeedDefaultsAsync();
        await server.SeedAsync([], [
            .. CodeGenerationPermissions.All.Select(static p => new PermissionGrant(GrantSubject.ForRole(Admin), p.Code)),
            .. CodeGenerationPermissions.All.Where(static p => p.Code.EndsWith(".read", StringComparison.Ordinal)).Select(static p => new PermissionGrant(GrantSubject.ForRole(Reader), p.Code)),
        ]);
        return server;
    }

    private static string Json(object value) => JsonSerializer.Serialize(value);

    private static Task<(HttpResponseMessage Response, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, string? json, string token)
        => server.SendAsync(BasicAppTestServer.Request(method, path, json, token));

    private Task<(HttpResponseMessage Response, JsonElement Body)> CreateSqliteAsync(BasicAppTestServer server, string token, string name, bool isDefault = false)
        => SendAsync(server, HttpMethod.Post, "/api/codegen-datasources", Json(new { sourceName = name, databaseType = "Sqlite", host = "localhost", port = 0, databaseName = _external.Path, userName = "none", isDefault }), token);

    [Fact]
    public async Task DataSources_FollowTheRulesOf4x_AndKeepSecretsOutOfResponses()
    {
        await using var server = await StartAsync();
        var admin = TestTokens.Issue("root", null, Admin);

        // 新建：口令与连接串加密存储、响应里只说有没有显式连接串
        var (created, body) = await SendAsync(server, HttpMethod.Post, "/api/codegen-datasources", Json(new { sourceName = "erp", databaseType = "PostgreSql", host = "127.0.0.1", port = 5432, databaseName = "erp", userName = "reader", password = "p@ss", connectionString = "Host=127.0.0.1;Database=erp;Username=reader;Password=p@ss", isDefault = true }), admin);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.True(body.GetProperty("hasConnectionString").GetBoolean());
        Assert.DoesNotContain("p@ss", body.GetRawText(), StringComparison.Ordinal);
        Assert.False(body.TryGetProperty("password", out _));
        var erp = body.GetProperty("id").GetString()!;
        var stored = await server.InvokeAsync(async ctx =>
        {
            var row = (await ctx.Services.GetRequiredService<IRepository<SysCodeGenDataSource, Guid>>().FindAsync(Guid.Parse(erp), ctx.CancellationToken)).ValueOrThrow();
            Assert.NotEqual("p@ss", row.Password);
            Assert.DoesNotContain("p@ss", row.ConnectionString, StringComparison.Ordinal);
            return Result.Success();
        });
        Assert.True(stored.IsSuccess);

        // 名称唯一
        var (taken, takenBody) = await SendAsync(server, HttpMethod.Post, "/api/codegen-datasources", Json(new { sourceName = "erp", databaseType = "MySql", host = "h", port = 3306, databaseName = "d", userName = "u" }), admin);
        Assert.Equal(HttpStatusCode.Conflict, taken.StatusCode);
        Assert.Equal(CodeGenerationErrors.DataSourceNameTaken, takenBody.GetProperty("code").GetString());

        // 缺省最多一个：新的缺省让旧的取消
        var (_, local) = await CreateSqliteAsync(server, admin, "local", isDefault: true);
        var (_, erpAfter) = await SendAsync(server, HttpMethod.Get, $"/api/codegen-datasources/{erp}", null, admin);
        Assert.False(erpAfter.GetProperty("isDefault").GetBoolean());
        Assert.True(local.GetProperty("isDefault").GetBoolean());

        // 列表：数据库类型多选、关键字、缺省排序（排序号再名称）、不支持的筛选
        var (_, list) = await SendAsync(server, HttpMethod.Post, "/api/codegen-datasources:list", """{"page":{"index":1,"size":20},"filters":[{"field":"databaseType","operator":"in","value":"PostgreSql,Sqlite"}]}""", admin);
        Assert.Equal(["erp", "local"], list.GetProperty("items").EnumerateArray().Select(static i => i.GetProperty("sourceName").GetString()));
        var (_, keyword) = await SendAsync(server, HttpMethod.Post, "/api/codegen-datasources:list", """{"page":{"index":1,"size":20},"keyword":"loc"}""", admin);
        Assert.Equal(1, keyword.GetProperty("total").GetInt32());
        var (badFilter, badFilterBody) = await SendAsync(server, HttpMethod.Post, "/api/codegen-datasources:list", """{"page":{"index":1,"size":20},"filters":[{"field":"host","operator":"eq","value":"x"}]}""", admin);
        Assert.Equal(HttpStatusCode.BadRequest, badFilter.StatusCode);
        Assert.Equal(CodeGenerationErrors.QueryUnsupported, badFilterBody.GetProperty("code").GetString());
        var (_, options) = await SendAsync(server, HttpMethod.Get, "/api/codegen-datasources:options", null, admin);
        Assert.Equal(2, options.GetProperty("items").GetArrayLength());

        // 修改：口令与连接串留空即不改；过期的并发戳即冲突
        var (updated, updatedBody) = await SendAsync(server, HttpMethod.Put, $"/api/codegen-datasources/{erp}", Json(new { concurrencyStamp = erpAfter.GetProperty("concurrencyStamp").GetString(), sourceName = "erp-main", databaseType = "PostgreSql", host = "127.0.0.1", port = 5432, databaseName = "erp", userName = "reader", order = 5 }), admin);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.True(updatedBody.GetProperty("hasConnectionString").GetBoolean());
        var (stale, _) = await SendAsync(server, HttpMethod.Put, $"/api/codegen-datasources/{erp}", Json(new { concurrencyStamp = erpAfter.GetProperty("concurrencyStamp").GetString(), sourceName = "erp-main", databaseType = "PostgreSql", host = "127.0.0.1", port = 5432, databaseName = "erp", userName = "reader" }), admin);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);

        // 启停带并发戳
        var (disabled, disabledBody) = await SendAsync(server, HttpMethod.Post, $"/api/codegen-datasources/{erp}:set-status", Json(new { concurrencyStamp = updatedBody.GetProperty("concurrencyStamp").GetString(), status = "Disabled" }), admin);
        Assert.Equal(HttpStatusCode.OK, disabled.StatusCode);
        Assert.Equal("Disabled", disabledBody.GetProperty("status").GetString());

        // 删除：被表配置引用的逐项拒绝（4.x 不查引用），不存在的逐项报不存在
        var referenced = await server.InvokeAsync(async ctx => await ctx.Services.GetRequiredService<IRepository<SysCodeGenTable, Guid>>().AddAsync(new SysCodeGenTable { Id = Guid.NewGuid(), TableName = "orders", ClassName = "Order", DataSourceId = Guid.Parse(erp) }, ctx.CancellationToken));
        Assert.True(referenced.IsSuccess);
        var missing = Guid.NewGuid();
        var localId = local.GetProperty("id").GetString()!;
        var (deleted, deletedBody) = await SendAsync(server, HttpMethod.Delete, $"/api/codegen-datasources?ids={erp}&ids={localId}&ids={missing}", null, admin);
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        Assert.Equal([localId], deletedBody.GetProperty("succeeded").EnumerateArray().Select(static i => i.GetString()));
        var failures = deletedBody.GetProperty("failed").EnumerateArray().ToDictionary(static f => f.GetProperty("id").GetString()!, static f => f.GetProperty("code").GetString());
        Assert.Equal(CodeGenerationErrors.DataSourceInUse, failures[erp]);
        Assert.Equal(CodeGenerationErrors.DataSourceNotFound, failures[missing.ToString()]);

        // 软删后同名可以重建
        var (recreated, _) = await CreateSqliteAsync(server, admin, "local");
        Assert.Equal(HttpStatusCode.Created, recreated.StatusCode);
    }

    [Fact]
    public async Task TestConnection_RecordsTheResult_NeedsTheUpdatePermission_AndRejectsUnreadableSecrets()
    {
        await using var server = await StartAsync();
        var admin = TestTokens.Issue("root", null, Admin);
        var reader = TestTokens.Issue("viewer", null, Reader);
        var (_, local) = await CreateSqliteAsync(server, admin, "local");
        var id = local.GetProperty("id").GetString()!;

        // 4.x 只要读权限却写库：6.0 要修改权限
        var (denied, _) = await SendAsync(server, HttpMethod.Post, $"/api/codegen-datasources/{id}:test-connection", null, reader);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);

        var (ok, okBody) = await SendAsync(server, HttpMethod.Post, $"/api/codegen-datasources/{id}:test-connection", null, admin);
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.True(okBody.GetProperty("success").GetBoolean());
        var (_, tested) = await SendAsync(server, HttpMethod.Get, $"/api/codegen-datasources/{id}", null, admin);
        Assert.True(tested.GetProperty("lastTestResult").GetBoolean());
        Assert.NotEqual(JsonValueKind.Null, tested.GetProperty("lastTestAt").ValueKind);

        // 连不上不是失败的调用：结果为否、说明是框架给的原因，并记下
        var (_, unreachable) = await SendAsync(server, HttpMethod.Post, "/api/codegen-datasources", Json(new { sourceName = "nowhere", databaseType = "PostgreSql", host = "127.0.0.1", port = 1, databaseName = "x", userName = "x", password = "secret-value", connectionTimeout = 1 }), admin);
        var nowhere = unreachable.GetProperty("id").GetString()!;
        var (failed, failedBody) = await SendAsync(server, HttpMethod.Post, $"/api/codegen-datasources/{nowhere}:test-connection", null, admin);
        Assert.Equal(HttpStatusCode.OK, failed.StatusCode);
        Assert.False(failedBody.GetProperty("success").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(failedBody.GetProperty("message").GetString()));
        Assert.DoesNotContain("secret-value", failedBody.GetRawText(), StringComparison.Ordinal);
        var (_, recorded) = await SendAsync(server, HttpMethod.Get, $"/api/codegen-datasources/{nowhere}", null, admin);
        Assert.False(recorded.GetProperty("lastTestResult").GetBoolean());

        // 解不开的密钥报错，不当明文用（4.x 的静默兜底）
        var tampered = await server.InvokeAsync(async ctx =>
        {
            var repository = ctx.Services.GetRequiredService<IRepository<SysCodeGenDataSource, Guid>>();
            var row = (await repository.FindAsync(Guid.Parse(nowhere), ctx.CancellationToken)).ValueOrThrow();
            row.Password = "not-a-protected-value";
            return await repository.UpdateAsync(row, ctx.CancellationToken);
        });
        Assert.True(tampered.IsSuccess);
        var (unreadable, unreadableBody) = await SendAsync(server, HttpMethod.Post, $"/api/codegen-datasources/{nowhere}:test-connection", null, admin);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, unreadable.StatusCode);
        Assert.Equal(CodeGenerationErrors.SecretUnreadable, unreadableBody.GetProperty("code").GetString());
    }

    [Fact]
    public async Task DataSources_AreOnlyOnThePlatformSide()
    {
        await using var server = await StartAsync();
        // 租户管理员持 *，平台侧的权限码按作用侧收窄：在授权阶段拒绝
        var tenantAdmin = TestTokens.Issue("alice", "acme", "admin");
        var (list, listBody) = await SendAsync(server, HttpMethod.Post, "/api/codegen-datasources:list", """{"page":{"index":1,"size":20}}""", tenantAdmin);
        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
        Assert.Equal("SECURITY_DENIED_004", listBody.GetProperty("code").GetString());
    }

    [Fact]
    public async Task CodeGenerationModule_IsOnByDefault_WithItsPageAndPermissions()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString);
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        var superadmin = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        var (permissions, permissionsBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:permissions", token: superadmin));
        Assert.Equal(HttpStatusCode.OK, permissions.StatusCode);
        Assert.Contains("codegeneration.code-gen", permissionsBody.GetProperty("pages").EnumerateArray().Select(static p => p.GetString()));
        Assert.Contains(CodeGenerationPermissions.DatasourcesUpdate, permissionsBody.GetProperty("permissions").EnumerateArray().Select(static p => p.GetString()));
        var page = MenuTrees.Find(permissionsBody.GetProperty("menus"), "codegeneration.code-gen");
        Assert.Equal("/codegeneration/code-gen", page.GetProperty("route").GetString());
        Assert.Equal("codegeneration/code-gen/index", page.GetProperty("component").GetString());
    }

    [Fact]
    public async Task CodeGenerationModule_CanBeSwitchedOff()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, settings: [new("BasicApp:Modules:CodeGeneration:Enabled", "false")]);
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        var superadmin = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        var (permissions, permissionsBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:permissions", token: superadmin));
        Assert.Equal(HttpStatusCode.OK, permissions.StatusCode);
        Assert.DoesNotContain("codegeneration.code-gen", permissionsBody.GetProperty("pages").EnumerateArray().Select(static p => p.GetString()));
        Assert.DoesNotContain(permissionsBody.GetProperty("permissions").EnumerateArray().Select(static p => p.GetString()), static p => p!.StartsWith("codegeneration.", StringComparison.Ordinal));
        var (options, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/codegen-datasources:options", token: superadmin));
        Assert.Equal(HttpStatusCode.NotFound, options.StatusCode);
        var (docs, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/openapi.json"));
        Assert.Equal(HttpStatusCode.OK, docs.StatusCode);

        await Assert.ThrowsAsync<ArgumentException>(() => BasicAppTestServer.StartAsync(_database.ConnectionString, settings: [new("BasicApp:Modules:CodeGeneration:Enabled", "maybe")]));
    }
}
