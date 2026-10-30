// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Auditing.Domain.Entities;
using XiHan.BasicApp.Auditing.Domain.Services;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.Framework.Auditing.Database.Trail;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证租户独立库的配置与初始化。
/// </summary>
/// <remarks>
/// 配好连接即待初始化（连接串加密存放、不进响应），初始化建库建表并登记台账，之后不能再改；独立库的租户要先初始化数据库才能初始化管理员。
/// 就绪之后落租户库的实体（部门）与该租户的审计日志都写进它自己的库，平台库里没有。
/// </remarks>
public sealed class TenantDatabaseTests : IDisposable
{
    private readonly SqliteDatabase _database = new();
    private readonly SqliteDatabase _tenantDatabase = new();

    public void Dispose()
    {
        _database.Dispose();
        _tenantDatabase.Dispose();
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, string? body, string token)
    {
        var (response, json) = await server.SendAsync(BasicAppTestServer.Request(method, path, body, token));
        return (response.StatusCode, json);
    }

    private static long Count(string connectionString, string sql)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static List<string> Tables(string connectionString, string prefix)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name LIKE $prefix";
        command.Parameters.AddWithValue("$prefix", prefix + "%");
        using var reader = command.ExecuteReader();
        var names = new List<string>();
        while (reader.Read())
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }

    [Fact]
    public async Task DedicatedDatabase_IsConfiguredInitialized_AndHoldsTheTenantData()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString);
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        var superadmin = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;

        var enterpriseId = await EditionsTests.EditionIdAsync(server, superadmin, "enterprise");
        var (created, createdBody) = await SendAsync(server, HttpMethod.Post, "/api/tenants", $$"""{"tenantId":"iso","name":"Isolated","editionId":"{{enterpriseId}}"}""", superadmin);
        Assert.Equal(HttpStatusCode.Created, created);
        var isoId = createdBody.GetProperty("id").GetString()!;
        Assert.Equal(("Field", "NotRequired"), (createdBody.GetProperty("isolationMode").GetString(), createdBody.GetProperty("databaseStatus").GetString()));

        // 共库的租户没有独立库可初始化；独立库要给连接串
        var (shared, sharedBody) = await SendAsync(server, HttpMethod.Post, $"/api/tenants/{isoId}:initialize-database", "{}", superadmin);
        Assert.Equal((HttpStatusCode.BadRequest, "TENANCY_VALIDATION_009"), (shared, sharedBody.GetProperty("code").GetString()));
        var (noConnection, noConnectionBody) = await SendAsync(server, HttpMethod.Post, $"/api/tenants/{isoId}:configure-database", """{"isolationMode":"Database"}""", superadmin);
        Assert.Equal((HttpStatusCode.BadRequest, "TENANCY_VALIDATION_008"), (noConnection, noConnectionBody.GetProperty("code").GetString()));

        // 配好连接即待初始化；响应里没有连接串，平台库里存的是密文
        var connection = JsonSerializer.Serialize(_tenantDatabase.ConnectionString);
        var (configured, configuredBody) = await SendAsync(server, HttpMethod.Post, $"/api/tenants/{isoId}:configure-database", $$"""{"isolationMode":"Database","connectionString":{{connection}},"databaseProvider":"sqlite"}""", superadmin);
        Assert.Equal(HttpStatusCode.OK, configured);
        Assert.Equal(("Database", "sqlite", "Pending"), (configuredBody.GetProperty("isolationMode").GetString(), configuredBody.GetProperty("databaseProvider").GetString(), configuredBody.GetProperty("databaseStatus").GetString()));
        Assert.False(configuredBody.TryGetProperty("connectionString", out _));
        Assert.Equal(0, Count(_database.ConnectionString, $"SELECT COUNT(*) FROM sys_tenant WHERE connection_string LIKE '%{_tenantDatabase.Path.Replace("'", "''", StringComparison.Ordinal)}%'"));

        // 独立库没就绪不能初始化管理员
        var (adminTooEarly, adminTooEarlyBody) = await SendAsync(server, HttpMethod.Post, $"/api/tenants/{isoId}:initialize-admin", """{"userName":"admin","password":"Admin@123456"}""", superadmin);
        Assert.Equal((HttpStatusCode.Conflict, "TENANCY_CONFLICT_014"), (adminTooEarly, adminTooEarlyBody.GetProperty("code").GetString()));

        // 初始化：建库、只建租户库的表、登记台账；之后不能再改、也不用再初始化
        var (initialized, initializedBody) = await SendAsync(server, HttpMethod.Post, $"/api/tenants/{isoId}:initialize-database", "{}", superadmin);
        Assert.Equal(HttpStatusCode.OK, initialized);
        Assert.Equal("Ready", initializedBody.GetProperty("databaseStatus").GetString());
        Assert.Contains("sys_department", Tables(_tenantDatabase.ConnectionString, "sys_department"));
        Assert.Empty(Tables(_tenantDatabase.ConnectionString, "sys_user_session"));
        Assert.Equal(1, Count(_tenantDatabase.ConnectionString, "SELECT COUNT(*) FROM sys_version WHERE db_version IS NOT NULL"));
        var (reconfigure, reconfigureBody) = await SendAsync(server, HttpMethod.Post, $"/api/tenants/{isoId}:configure-database", """{"isolationMode":"Field"}""", superadmin);
        Assert.Equal((HttpStatusCode.Conflict, "TENANCY_CONFLICT_013"), (reconfigure, reconfigureBody.GetProperty("code").GetString()));
        var (again, againBody) = await SendAsync(server, HttpMethod.Post, $"/api/tenants/{isoId}:initialize-database", "{}", superadmin);
        Assert.Equal((HttpStatusCode.Conflict, "TENANCY_CONFLICT_013"), (again, againBody.GetProperty("code").GetString()));

        // 初始化管理员、登录、建部门：部门落进租户自己的库，平台库里没有
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, $"/api/tenants/{isoId}:initialize-admin", """{"userName":"admin","password":"Admin@123456"}""", superadmin)).Status);
        var admin = (await AuthFlowTests.LoginAsync(server, "admin", "Admin@123456", "iso")).GetProperty("accessToken").GetString()!;
        var (department, _) = await SendAsync(server, HttpMethod.Post, "/api/departments", """{"code":"ops","name":"Operations"}""", admin);
        Assert.Equal(HttpStatusCode.Created, department);
        var (tree, treeBody) = await SendAsync(server, HttpMethod.Get, "/api/departments:tree", null, admin);
        Assert.Equal(HttpStatusCode.OK, tree);
        Assert.Equal(["ops"], treeBody.GetProperty("items").EnumerateArray().Select(static d => d.GetProperty("department").GetProperty("code").GetString()).ToList());
        Assert.Equal(1, Count(_tenantDatabase.ConnectionString, "SELECT COUNT(*) FROM sys_department WHERE tenant_id = 'iso'"));
        Assert.Equal(0, Count(_database.ConnectionString, "SELECT COUNT(*) FROM sys_department WHERE tenant_id = 'iso'"));

        // 该租户的登录日志写进它自己的库
        await server.Services.GetRequiredService<DatabaseAuditTrail>().FlushAsync(CancellationToken.None);
        await server.Services.GetRequiredService<AuditLogWriter>().FlushAsync(CancellationToken.None);
        var tenantLogs = Tables(_tenantDatabase.ConnectionString, "sys_login_log").Sum(table => Count(_tenantDatabase.ConnectionString, $"SELECT COUNT(*) FROM \"{table}\" WHERE tenant_id = 'iso'"));
        var platformLogs = Tables(_database.ConnectionString, "sys_login_log").Sum(table => Count(_database.ConnectionString, $"SELECT COUNT(*) FROM \"{table}\" WHERE tenant_id = 'iso'"));
        Assert.True(tenantLogs > 0);
        Assert.Equal(0, platformLogs);

        // 停用之后，该租户的日志照样写进它自己的库（后台写入按租户目录绑定分片，不看租户是否启用）
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, "/api/tenants:suspend", $$"""{"ids":["{{isoId}}"]}""", superadmin)).Status);
        var writer = server.Services.GetRequiredService<AuditLogWriter>();
        Assert.True(writer.Enqueue(new SysLoginLog { Id = Guid.CreateVersion7(), TenantId = "iso", OccurredAt = DateTime.UtcNow, TraceId = "suspended", Event = LoginEvent.Login }));
        await writer.FlushAsync(CancellationToken.None);
        var afterSuspend = Tables(_tenantDatabase.ConnectionString, "sys_login_log").Sum(table => Count(_tenantDatabase.ConnectionString, $"SELECT COUNT(*) FROM \"{table}\" WHERE tenant_id = 'iso'"));
        Assert.True(afterSuspend > tenantLogs);
    }
}
