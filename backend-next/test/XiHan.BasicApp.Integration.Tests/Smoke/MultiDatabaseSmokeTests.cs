// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.MultiTenancy.Abstractions.Sharding;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Integration.Tests.Smoke;

/// <summary>
/// 验证一次租户调用同时写平台库与租户库时一起提交或回滚。
/// </summary>
/// <remarks>
/// 方案 9.1 第三项。
/// 一次租户调用同时写默认连接（平台库 <c>sys_user</c>）与 <c>shard</c> 连接（租户库 <c>sys_user_statistics</c>），失败时一起回滚。
/// 框架用例是 SQLite 双文件，这里用真实 PostgreSQL 两个库再跑一遍；需要环境变量 <c>XIHAN_TEST_POSTGRES</c>（服务器连接串，不含 Database），没有即跳过。
/// 结论：两边同一调用一起提交或一起回滚；顺序提交、非两阶段。
/// </remarks>
public sealed class MultiDatabaseSmokeTests
{
    private const string Variable = "XIHAN_TEST_POSTGRES";

    [Fact]
    public async Task TenantCall_WritesPlatformAndShardDatabases_AndRollsBackBoth()
    {
        var serverConnection = Environment.GetEnvironmentVariable(Variable);
        Assert.SkipWhen(string.IsNullOrWhiteSpace(serverConnection), $"{Variable} is not set; the two-database PostgreSQL smoke test only runs against a real server.");

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var platformDb = $"xihan_basicapp_test_platform_{suffix}";
        var tenantDb = $"xihan_basicapp_test_tenant_{suffix}";
        await using var databases = await PostgresDatabases.CreateAsync(serverConnection, platformDb, tenantDb);

        // 分片库的表结构由应用管理：用同一套约定给租户库建租户实体的表
        using (var shard = new SqlSugarClient(SqlSugarConventions.Apply(Configure(databases.Tenant))))
        {
            shard.CodeFirst.InitTables<SysUserStatistics>();
        }

        await using var server = await BasicAppTestServer.StartAsync(databases.Platform, DatabaseProviders.PostgreSql);
        await server.SeedDefaultsAsync(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [TenantShardKeys.ConnectionString] = databases.Tenant,
            [TenantShardKeys.DatabaseProvider] = DatabaseProviders.PostgreSql,
        });
        var acme = TestTokens.Issue("alice", "acme", "admin");

        // 带分片的调用里：用户落默认连接，统计落 shard 连接
        string? userConnection = null;
        string? statisticsConnection = null;
        (await server.InvokeAsync(ctx =>
        {
            var unitOfWork = ctx.Services.GetRequiredService<SqlSugarUnitOfWork>();
            userConnection = unitOfWork.ConnectionFor(typeof(SysUser)).CurrentConnectionConfig.ConnectionString;
            statisticsConnection = unitOfWork.ConnectionFor(typeof(SysUserStatistics)).CurrentConnectionConfig.ConnectionString;
            return Task.FromResult(Result.Success());
        }, acme)).ThrowIfFailure();
        Assert.Equal(databases.Platform, userConnection);
        Assert.Equal(databases.Tenant, statisticsConnection);

        // 失败的调用：先写平台库再写租户库，以失败结束，两边都不能留下行
        var userId = Guid.CreateVersion7();
        var failed = await server.InvokeAsync(async ctx =>
        {
            (await ctx.Services.GetRequiredService<IRepository<SysUser, Guid>>().AddAsync(new SysUser { Id = userId, UserName = "rollback", DisplayName = "Rollback", HomeTenantId = "acme" }, ctx.CancellationToken)).ThrowIfFailure();
            (await ctx.Services.GetRequiredService<IRepository<SysUserStatistics, Guid>>().AddAsync(new SysUserStatistics { Id = Guid.CreateVersion7(), UserId = userId }, ctx.CancellationToken)).ThrowIfFailure();
            return Error.Conflict("IDENTITY_CONFLICT_001", "identity.user.name_taken");
        }, acme);
        Assert.True(failed.IsFailure);
        Assert.Equal(0, Count<SysUser>(databases.Platform, u => u.Id == userId));
        Assert.Equal(0, Count<SysUserStatistics>(databases.Tenant, s => s.UserId == userId));

        // 成功的调用（经 HTTP 的示例操作）：平台行在平台库，统计行在租户库，平台库的统计表保持空
        var (created, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", """{"userName":"sharded","displayName":"Sharded"}""", acme));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var createdId = Guid.Parse(body.GetProperty("id").GetString()!);
        // 时间列在 PostgreSQL 上按 UTC 往返（框架 ADR-0037：Npgsql legacy 模式交出的本地时间要先还原 UTC）：新增后读回等于时钟，更新一次后创建时间不变
        Assert.InRange((DateTimeOffset.UtcNow - body.GetProperty("createdAt").GetDateTimeOffset()).Duration(), TimeSpan.Zero, TimeSpan.FromMinutes(1));
        // 创建响应里是内存值（100ns 刻度），库里是微秒；比较"更新前后不变"用两次读回的值
        var (stored, storedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/users/{createdId}", token: acme));
        Assert.Equal(HttpStatusCode.OK, stored.StatusCode);
        var createdAt = storedBody.GetProperty("createdAt").GetDateTimeOffset();
        Assert.InRange((DateTimeOffset.UtcNow - createdAt).Duration(), TimeSpan.Zero, TimeSpan.FromMinutes(1));
        (await server.InvokeAsync(async ctx =>
        {
            var repository = ctx.Services.GetRequiredService<IRepository<SysUser, Guid>>();
            Assert.True((await repository.FindAsync(createdId, ctx.CancellationToken)).TryGetValue(out var user));
            user!.DisplayName = "Sharded Again";
            return await repository.UpdateAsync(user, ctx.CancellationToken);
        }, acme)).ThrowIfFailure();
        var (fetched, fetchedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/users/{createdId}", token: acme));
        Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
        Assert.Equal("Sharded Again", fetchedBody.GetProperty("displayName").GetString());
        Assert.Equal(createdAt, fetchedBody.GetProperty("createdAt").GetDateTimeOffset());
        Assert.Equal(1, Count<SysUser>(databases.Platform, u => u.Id == createdId));
        Assert.Equal(1, Count<SysUserStatistics>(databases.Tenant, s => s.UserId == createdId));
        Assert.Equal(0, Count<SysUserStatistics>(databases.Platform, s => s.UserId == createdId));

        // 同一租户的后续调用能从两个库读回自己的行；统计行经仓储受租户过滤
        var seen = (false, false);
        (await server.InvokeAsync(async ctx =>
        {
            var user = await ctx.Services.GetRequiredService<IRepository<SysUser, Guid>>().FindAsync(createdId, ctx.CancellationToken);
            var statistics = await ctx.Services.GetRequiredService<IRepository<SysUserStatistics, Guid>>().CountAsync(XiHan.Framework.Persistence.Abstractions.Queries.EntityQuery.All<SysUserStatistics>(), ctx.CancellationToken);
            seen = (user.HasValue, statistics == 1);
            return Result.Success();
        }, acme)).ThrowIfFailure();
        Assert.Equal((true, true), seen);
    }

    private static int Count<TEntity>(string connectionString, System.Linq.Expressions.Expression<Func<TEntity, bool>> predicate)
        where TEntity : class, new()
    {
        using var client = new SqlSugarClient(SqlSugarConventions.Apply(Configure(connectionString)));
        return client.Queryable<TEntity>().Where(predicate).Count();
    }

    private static ConnectionConfig Configure(string connectionString)
    {
        var config = new ConnectionConfig();
        SqlSugarRuntime.Configure(Maybe.Some(new BindingValue("db", "connection:relational", connectionString, new Dictionary<string, string>(StringComparer.Ordinal) { ["provider"] = DatabaseProviders.PostgreSql })), config);
        return config;
    }

    private sealed class PostgresDatabases(string server, string platformName, string tenantName) : IAsyncDisposable
    {
        public string Platform { get; } = $"{server.TrimEnd(';')};Database={platformName}";

        public string Tenant { get; } = $"{server.TrimEnd(';')};Database={tenantName}";

        public static async Task<PostgresDatabases> CreateAsync(string server, string platformName, string tenantName)
        {
            using var client = Maintenance(server);
            await client.Ado.ExecuteCommandAsync($"CREATE DATABASE \"{platformName}\"");
            await client.Ado.ExecuteCommandAsync($"CREATE DATABASE \"{tenantName}\"");
            return new PostgresDatabases(server, platformName, tenantName);
        }

        public async ValueTask DisposeAsync()
        {
            using var client = Maintenance(server);
            await client.Ado.ExecuteCommandAsync($"DROP DATABASE IF EXISTS \"{platformName}\" WITH (FORCE)");
            await client.Ado.ExecuteCommandAsync($"DROP DATABASE IF EXISTS \"{tenantName}\" WITH (FORCE)");
        }

        private static SqlSugarClient Maintenance(string server)
            => new(new ConnectionConfig { DbType = DbType.PostgreSQL, ConnectionString = $"{server.TrimEnd(';')};Database=postgres", IsAutoCloseConnection = true });
    }
}
