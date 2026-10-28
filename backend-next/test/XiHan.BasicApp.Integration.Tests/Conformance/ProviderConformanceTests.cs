// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.MultiTenancy.Abstractions.Tenants;
using XiHan.Framework.MultiTenancy.Abstractions.Sharding;
using XiHan.Framework.MultiTenancy.Conformance;
using XiHan.Framework.Security.Abstractions.Permissions;
using XiHan.Framework.Security.Conformance;

namespace XiHan.BasicApp.Integration.Tests.Conformance;

/// <summary>
/// 一致性套件的宿主：每个用例一个空库（SQLite 临时文件，或 PostgreSQL 临时库——<see cref="PostgresDatabase.Variable"/> 没设即跳过）上的完整 BasicApp 宿主，
/// 数据经 <see cref="BasicAppTestServer.SeedAsync"/>（框架的 <c>TenantRecordStore</c> / <c>PermissionGrantRecordStore</c>）写入；用例结束先停宿主再删库。
/// 方案 N1 退出条件：BasicApp 装配出的租户存储、分片解析与授予存储通过框架的 <c>MultiTenancy.Conformance</c> 与 <c>Security.Conformance</c>。
/// </summary>
/// <param name="postgres">是否用 PostgreSQL。</param>
internal sealed class ConformanceHosts(bool postgres) : IAsyncDisposable
{
    private readonly List<IAsyncDisposable> _owned = [];
    private readonly List<SqliteDatabase> _files = [];

    /// <summary>
    /// 起一个宿主。
    /// </summary>
    public async Task<BasicAppTestServer> StartAsync()
    {
        if (!postgres)
        {
            var file = new SqliteDatabase();
            _files.Add(file);
            var sqlite = await BasicAppTestServer.StartAsync(file.ConnectionString);
            _owned.Add(sqlite);
            return sqlite;
        }

        var database = await PostgresDatabase.CreateAsync(PostgresDatabase.RequireServer(), "xihan_basicapp_conformance");
        _owned.Add(database);
        var server = await BasicAppTestServer.StartAsync(database.ConnectionString, DatabaseProviders.PostgreSql);
        _owned.Add(server);
        return server;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        for (var i = _owned.Count - 1; i >= 0; i--)
        {
            await _owned[i].DisposeAsync();
        }

        _owned.Clear();
        foreach (var file in _files)
        {
            file.Dispose();
        }

        _files.Clear();
    }
}

/// <summary>
/// 授予存储（框架 <c>Security.Database</c> 装在 BasicApp 的 SqlSugar 持久化上，SQLite）。
/// </summary>
public sealed class SqlitePermissionGrantStoreConformanceTests : PermissionGrantStoreConformance, IAsyncDisposable
{
    private readonly ConformanceHosts _hosts = new(postgres: false);

    /// <inheritdoc />
    protected override async ValueTask<IPermissionGrantStore> CreateStoreAsync(IReadOnlyList<PermissionGrant> grants)
    {
        var server = await _hosts.StartAsync();
        await server.SeedAsync([], grants);
        return server.Services.GetRequiredService<IPermissionGrantStore>();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _hosts.DisposeAsync();
}

/// <summary>
/// 授予存储（PostgreSQL）。
/// </summary>
public sealed class PostgresPermissionGrantStoreConformanceTests : PermissionGrantStoreConformance, IAsyncDisposable
{
    private readonly ConformanceHosts _hosts = new(postgres: true);

    /// <inheritdoc />
    protected override async ValueTask<IPermissionGrantStore> CreateStoreAsync(IReadOnlyList<PermissionGrant> grants)
    {
        var server = await _hosts.StartAsync();
        await server.SeedAsync([], grants);
        return server.Services.GetRequiredService<IPermissionGrantStore>();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _hosts.DisposeAsync();
}

/// <summary>
/// 租户目录（框架 <c>MultiTenancy.Database</c> 装在 BasicApp 的持久化上，SQLite）。
/// </summary>
public sealed class SqliteTenantStoreConformanceTests : TenantStoreConformance, IAsyncDisposable
{
    private readonly ConformanceHosts _hosts = new(postgres: false);

    /// <inheritdoc />
    protected override async ValueTask<ITenantStore> CreateStoreAsync(IReadOnlyList<TenantInfo> tenants)
    {
        var server = await _hosts.StartAsync();
        await server.SeedAsync(tenants, []);
        return server.Services.GetRequiredService<ITenantStore>();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _hosts.DisposeAsync();
}

/// <summary>
/// 租户目录（PostgreSQL）。
/// </summary>
public sealed class PostgresTenantStoreConformanceTests : TenantStoreConformance, IAsyncDisposable
{
    private readonly ConformanceHosts _hosts = new(postgres: true);

    /// <inheritdoc />
    protected override async ValueTask<ITenantStore> CreateStoreAsync(IReadOnlyList<TenantInfo> tenants)
    {
        var server = await _hosts.StartAsync();
        await server.SeedAsync(tenants, []);
        return server.Services.GetRequiredService<ITenantStore>();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _hosts.DisposeAsync();
}

/// <summary>
/// 分片解析（架在租户目录上，SQLite）。
/// </summary>
public sealed class SqliteTenantShardResolverConformanceTests : TenantShardResolverConformance, IAsyncDisposable
{
    private readonly ConformanceHosts _hosts = new(postgres: false);

    /// <inheritdoc />
    protected override async ValueTask<ITenantShardResolver> CreateResolverAsync(IReadOnlyList<TenantInfo> tenants)
    {
        var server = await _hosts.StartAsync();
        await server.SeedAsync(tenants, []);
        return server.Services.GetRequiredService<ITenantShardResolver>();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _hosts.DisposeAsync();
}

/// <summary>
/// 分片解析（PostgreSQL）。
/// </summary>
public sealed class PostgresTenantShardResolverConformanceTests : TenantShardResolverConformance, IAsyncDisposable
{
    private readonly ConformanceHosts _hosts = new(postgres: true);

    /// <inheritdoc />
    protected override async ValueTask<ITenantShardResolver> CreateResolverAsync(IReadOnlyList<TenantInfo> tenants)
    {
        var server = await _hosts.StartAsync();
        await server.SeedAsync(tenants, []);
        return server.Services.GetRequiredService<ITenantShardResolver>();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _hosts.DisposeAsync();
}
