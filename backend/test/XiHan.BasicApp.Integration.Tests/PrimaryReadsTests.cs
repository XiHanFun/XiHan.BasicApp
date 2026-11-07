// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证决定授权的读取走主库
/// </summary>
/// <remarks>
/// 从库是另一个建好表、不复制的库，新写入主库的行在那里读不到，就是一个落后的从库
/// </remarks>
public sealed class PrimaryReadsTests
{
    [Fact]
    public async Task SessionGate_ReadsThePrimary_WhileReadOnlyScopesReadTheReplica()
    {
        var postgres = PostgresDatabase.RequireServer();
        await using var primary = await PostgresDatabase.CreateAsync(postgres, "xihan_basicapp_test_primary");
        await using var replica = await PostgresDatabase.CreateAsync(postgres, "xihan_basicapp_test_replica");
        await using (await BasicAppTestServer.StartAsync(replica.ConnectionString, DatabaseProviders.PostgreSql))
        {
        }

        await using var server = await BasicAppTestServer.StartAsync(primary.ConnectionString, DatabaseProviders.PostgreSql, [new("BasicApp:Database:Replicas:0:ConnectionString", replica.ConnectionString), new("BasicApp:Database:Replicas:0:HitRate", "10")]);
        await server.RunSeedersAsync(["identity.platform"]);

        // 两种作用域读到的不是同一个库：种子只写进了主库
        var scopes = server.Services.GetRequiredService<IServiceScopeFactory>();
        var cancellationToken = TestContext.Current.CancellationToken;
        Task<long> CountAsync(IServiceProvider services, CancellationToken ct) => services.GetRequiredService<IRepository<SysUser, Guid>>().CountAsync(EntityQuery.All<SysUser>(), ct);
        Assert.True(await StoreScope.ReadPrimaryAsync(scopes, CountAsync, cancellationToken) > 0);
        Assert.Equal(0, await StoreScope.ReadAsync(scopes, CountAsync, cancellationToken));

        // 刚登录的会话只在主库：门禁读从库的话这里就是会话无效
        var token = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        var (me, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:permissions", token: token));
        Assert.True(me.StatusCode == HttpStatusCode.OK, body.ToString());
    }
}
