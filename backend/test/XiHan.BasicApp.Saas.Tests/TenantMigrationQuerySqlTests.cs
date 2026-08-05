// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 升级通道读取库隔离租户的查询：跑在真实 SqlSugar 引擎上，覆盖纯逻辑测试看不见的结果物化环节。
/// </summary>
/// <remarks>
/// 起因是一次线上启动崩溃：该查询原本把结果投影成只有带参构造函数的类型，
/// 而 SqlSugar 物化时用 IL 调用目标类型的无参构造函数，取不到构造器即抛
/// <c>ArgumentNullException (Parameter 'con')</c>。SaasUpgradeTenantProvider 投影的
/// BasicTenantInfo 同样只有带参构造函数，故这条真引擎用例继续把该物化路径钉住。
/// </remarks>
public sealed class TenantMigrationQuerySqlTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"xihan-tenant-migration-{Guid.NewGuid():N}.db");
    private readonly SqlSugarClient _client;

    /// <summary>
    /// 建库建表。
    /// </summary>
    public TenantMigrationQuerySqlTests()
    {
        _client = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = $"DataSource={_databasePath};Pooling=False",
            DbType = DbType.Sqlite,
            IsAutoCloseConnection = false
        });
        _client.CodeFirst.InitTables<SysTenant>();
    }

    /// <summary>
    /// 查询能被真实引擎物化，并且只选出库隔离且已完成初始化的租户。
    /// </summary>
    [Fact]
    public async Task DatabaseIsolatedTenantQuery_ShouldMaterializeAndFilter()
    {
        InsertTenant("库隔离已配置", TenantIsolationMode.Database, TenantConfigStatus.Configured);
        InsertTenant("库隔离配置中", TenantIsolationMode.Database, TenantConfigStatus.Configuring);
        InsertTenant("字段隔离", TenantIsolationMode.Field, TenantConfigStatus.Configured);

        var tenants = await _client.Queryable<SysTenant>()
            .Where(tenant => !tenant.IsDeleted)
            .Where(tenant => tenant.IsolationMode == TenantIsolationMode.Database)
            .Where(tenant => tenant.ConfigStatus == TenantConfigStatus.Configured)
            .ToListAsync();

        var targets = tenants
            .Select(tenant => new BasicTenantInfo(tenant.BasicId, tenant.TenantName))
            .ToList();

        Assert.Single(targets);
        Assert.Equal("库隔离已配置", targets[0].Name);
    }

    /// <summary>
    /// 无匹配租户时返回空集合而不是抛异常：物化路径在零行时同样要走得通。
    /// </summary>
    [Fact]
    public async Task DatabaseIsolatedTenantQuery_ShouldReturnEmpty_WhenNoneMatch()
    {
        InsertTenant("字段隔离", TenantIsolationMode.Field, TenantConfigStatus.Configured);

        var tenants = await _client.Queryable<SysTenant>()
            .Where(tenant => tenant.IsolationMode == TenantIsolationMode.Database)
            .ToListAsync();

        Assert.Empty(tenants);
    }

    /// <summary>
    /// 释放连接并清理临时库文件。
    /// </summary>
    public void Dispose()
    {
        _client.Dispose();
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }

    private void InsertTenant(string name, TenantIsolationMode isolationMode, TenantConfigStatus configStatus)
    {
        _ = _client.Insertable(new SysTenant
        {
            TenantCode = Guid.NewGuid().ToString("N")[..8],
            TenantName = name,
            IsolationMode = isolationMode,
            ConfigStatus = configStatus
        }).ExecuteReturnSnowflakeId();
    }
}
