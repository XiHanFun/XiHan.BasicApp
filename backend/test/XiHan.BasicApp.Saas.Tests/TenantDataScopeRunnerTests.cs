// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq.Expressions;
using Moq;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 逐作用域 / 逐库执行：平台先行；库隔离租户的数据在它自己的库里，只有建好库的才进得去
/// </summary>
public sealed class TenantDataScopeRunnerTests
{
    private readonly TestCurrentTenant _currentTenant = new();

    private readonly TenantDataScopeRunner _runner;

    /// <summary>
    /// 租户目录：字段隔离 2 个，库隔离已建库 1 个、未建库 1 个
    /// </summary>
    public TenantDataScopeRunnerTests()
    {
        List<SysTenant> tenants =
        [
            Tenant(11, TenantIsolationMode.Field, TenantConfigStatus.Configured),
            Tenant(12, TenantIsolationMode.Field, TenantConfigStatus.Configured),
            Tenant(21, TenantIsolationMode.Database, TenantConfigStatus.Configured),
            Tenant(22, TenantIsolationMode.Database, TenantConfigStatus.Pending),
        ];

        var tenantRepository = new Mock<ITenantRepository>();
        _ = tenantRepository
            .Setup(repo => repo.GetListAsync(It.IsAny<Expression<Func<SysTenant, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<SysTenant, bool>> predicate, CancellationToken _) =>
            {
                // 租户目录只在平台作用域读
                Assert.Null(_currentTenant.Id);
                return [.. tenants.Where(predicate.Compile())];
            });

        _runner = new TenantDataScopeRunner(tenantRepository.Object, _currentTenant);
    }

    [Fact]
    public async Task RunAsync_平台与每个数据可达的租户各执行一次()
    {
        var visited = new List<(long? Scope, long? Current)>();

        await _runner.RunAsync(scope =>
        {
            visited.Add((scope, _currentTenant.Id));
            return Task.CompletedTask;
        }, TestContext.Current.CancellationToken);

        Assert.Equal([(null, null), (11, 11), (12, 12), (21, 21)], visited);
        Assert.Null(_currentTenant.Id);
    }

    [Fact]
    public async Task RunPerDatabaseAsync_平台库一次_再逐个已建库的库隔离租户()
    {
        var visited = new List<(long? Scope, long? Current)>();

        await _runner.RunPerDatabaseAsync(scope =>
        {
            visited.Add((scope, _currentTenant.Id));
            return Task.CompletedTask;
        }, TestContext.Current.CancellationToken);

        // 字段隔离租户与平台同库，平台库那一次就覆盖了它们；未建库的没有库可进
        Assert.Equal([(null, null), (21, 21)], visited);
        Assert.Null(_currentTenant.Id);
    }

    [Fact]
    public async Task AnyPerDatabaseAsync_平台库命中即停()
    {
        var probed = new List<long?>();

        var hit = await _runner.AnyPerDatabaseAsync(() =>
        {
            probed.Add(_currentTenant.Id);
            return Task.FromResult(true);
        }, TestContext.Current.CancellationToken);

        Assert.True(hit);
        Assert.Equal([null], probed);
    }

    [Fact]
    public async Task AnyPerDatabaseAsync_只在库隔离租户的库里命中也算()
    {
        var probed = new List<long?>();

        var hit = await _runner.AnyPerDatabaseAsync(() =>
        {
            probed.Add(_currentTenant.Id);
            return Task.FromResult(_currentTenant.Id == 21);
        }, TestContext.Current.CancellationToken);

        Assert.True(hit);
        Assert.Equal([null, 21], probed);
        Assert.Null(_currentTenant.Id);
    }

    [Fact]
    public async Task AnyPerDatabaseAsync_所有库都没有返回否()
    {
        var hit = await _runner.AnyPerDatabaseAsync(() => Task.FromResult(false), TestContext.Current.CancellationToken);

        Assert.False(hit);
    }

    private static SysTenant Tenant(long id, TenantIsolationMode isolationMode, TenantConfigStatus configStatus)
    {
        var tenant = new SysTenant
        {
            TenantCode = $"t{id}",
            TenantName = $"租户{id}",
            IsolationMode = isolationMode,
            ConfigStatus = configStatus
        };
        SaasTestHelper.SetBasicId(tenant, id);
        return tenant;
    }
}
