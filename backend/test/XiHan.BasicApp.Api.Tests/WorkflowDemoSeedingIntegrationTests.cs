// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SqlSugar;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Saas.Infrastructure.Seeders;
using XiHan.BasicApp.Workflow.Domain.Entities;
using XiHan.BasicApp.Workflow.Domain.Repositories;
using XiHan.BasicApp.Workflow.Extensions;
using XiHan.BasicApp.Workflow.Infrastructure.Repositories;
using XiHan.BasicApp.Workflow.Infrastructure.Seeders;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Extensions;
using XiHan.Framework.Data.SqlSugar.Seeders;
using XiHan.Framework.DistributedIds;
using XiHan.Framework.Domain.Entities.Abstracts;
using XiHan.Framework.MultiTenancy;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Timing;
using XiHan.Framework.Workflow.Abstractions.Definitions;
using XiHan.Framework.Workflow.Extensions.DependencyInjection;

namespace XiHan.BasicApp.Api.Tests;

/// <summary>
/// 工作流演示流程种子在空库上真跑：经流程定义管理器与 SqlSugar 存储写进演示租户，重复启动不重复创建
/// </summary>
/// <remarks>
/// 临时 SQLite 内存库按框架同一套口径登记软删与租户过滤（平台就是 0 号租户），租户上下文用框架的 AsyncLocal 实现，
/// 与仓储写守卫同源；种子在平台上下文里查租户的行，只有显式清掉过滤器才看得见。
/// </remarks>
public sealed class WorkflowDemoSeedingIntegrationTests : IDisposable
{
    private readonly string _connectionString = $"Data Source=xihan-workflow-demo-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
    private readonly SqliteConnection _keepAlive;
    private readonly SqlSugarScope _db;
    private readonly CurrentTenant _tenant = new(AsyncLocalCurrentTenantAccessor.Instance);

    /// <summary>
    /// 构造函数：建临时库，写入演示租户与流程要用的三个演示账号
    /// </summary>
    public WorkflowDemoSeedingIntegrationTests()
    {
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();

        var tenant = _tenant;
        _db = new SqlSugarScope(
            new ConnectionConfig { DbType = DbType.Sqlite, ConnectionString = _connectionString, IsAutoCloseConnection = true, InitKeyType = InitKeyType.Attribute },
            db =>
            {
                db.QueryFilter.AddTableFilter<ISoftDelete>(entity => !entity.IsDeleted);
                db.QueryFilter.AddTableFilter<IMultiTenantEntity>(entity => entity.TenantId == 0 || entity.TenantId == ScopeId());
                db.QueryFilter.AddTableFilter<IStrictMultiTenantEntity>(entity => entity.TenantId == ScopeId());
                db.Aop.DataExecuting = (_, entityInfo) =>
                {
                    if (entityInfo.OperationType == DataFilterType.InsertByObject)
                    {
                        entityInfo.TrySetSnowflakeId(SnowFlakeSingle.Instance.NextId());
                        entityInfo.ToCreated(EntityAuditContext.From(null, tenant.Id));
                    }
                };
            });
        _db.CodeFirst.InitTables(typeof(SysTenant), typeof(SysUser), typeof(SysWorkflowDefinition));
    }

    /// <summary>
    /// 演示开关打开、启动两次：示例企业里正好四个演示流程，都是第 1 版、已发布。
    /// </summary>
    [Fact]
    public async Task DemoSeeder_RunTwice_ShouldPublishEachDefinitionOnceInTheDemoTenant()
    {
        var demoTenantId = await SeedDemoTenantAsync();
        using var services = BuildServices("true");

        await SeedAsync(services);
        await SeedAsync(services);

        var rows = await _db.Queryable<SysWorkflowDefinition>().ClearTenantAndSoftDeleteFilter().ToListAsync();
        Assert.Equal(WorkflowDemoCatalog.Codes.Order(StringComparer.Ordinal), rows.Select(row => row.Code).Order(StringComparer.Ordinal));
        Assert.All(rows, row =>
        {
            Assert.Equal(demoTenantId, row.TenantId);
            Assert.Equal(1, row.Version);
            Assert.Equal(WorkflowDefinitionStatus.Published, row.Status);
            Assert.Equal(WorkflowDemoCatalog.Category, row.Category);
        });
    }

    /// <summary>
    /// 软删掉的演示流程不补回，其余已有的也不再建新版本。
    /// </summary>
    [Fact]
    public async Task DemoSeeder_DeletedDefinition_ShouldNotBeRecreated()
    {
        _ = await SeedDemoTenantAsync();
        using var services = BuildServices("true");
        await SeedAsync(services);
        _ = await _db.Updateable<SysWorkflowDefinition>()
            .SetColumns(row => row.IsDeleted == true)
            .Where(row => row.Code == WorkflowDemoCatalog.Leave)
            .ExecuteCommandAsync();

        await SeedAsync(services);

        var rows = await _db.Queryable<SysWorkflowDefinition>().ClearTenantAndSoftDeleteFilter().ToListAsync();
        Assert.Equal(WorkflowDemoCatalog.Codes.Count, rows.Count);
        Assert.Single(rows, row => row.IsDeleted);
    }

    /// <summary>
    /// 演示开关关着：一个流程都不写。
    /// </summary>
    [Fact]
    public async Task DemoSeeder_SwitchOff_ShouldWriteNothing()
    {
        _ = await SeedDemoTenantAsync();
        using var services = BuildServices("false");

        await SeedAsync(services);

        Assert.Equal(0, await _db.Queryable<SysWorkflowDefinition>().ClearTenantAndSoftDeleteFilter().CountAsync());
    }

    /// <summary>
    /// 关掉连接，内存库随之消失
    /// </summary>
    public void Dispose()
    {
        _db.Dispose();
        _keepAlive.Dispose();
    }

    private static long ScopeId()
    {
        return AsyncLocalCurrentTenantAccessor.Instance.Current?.TenantId ?? 0;
    }

    private async Task<long> SeedDemoTenantAsync()
    {
        var tenant = await _db.Insertable(new SysTenant { TenantCode = WorkflowDemoCatalog.TenantCode, TenantName = "示例企业" }).ExecuteReturnEntityAsync();
        using (_tenant.Change(tenant.BasicId, tenant.TenantName))
        {
            foreach (var userName in WorkflowDemoCatalog.UserNames)
            {
                _ = await _db.Insertable(new SysUser { TenantId = tenant.BasicId, UserName = userName, RealName = userName, NickName = userName }).ExecuteCommandAsync();
            }
        }

        return tenant.BasicId;
    }

    private ServiceProvider BuildServices(string demoSwitch)
    {
        var resolver = new Mock<ISqlSugarClientResolver>();
        resolver.Setup(value => value.GetCurrentClient()).Returns(_db);
        resolver.Setup(value => value.GetClientForEntity(It.IsAny<Type>())).Returns(_db);
        var ids = new Mock<IDistributedIdGenerator<long>>();
        ids.Setup(value => value.NextIdString()).Returns(() => SnowFlakeSingle.Instance.NextId().ToString(System.Globalization.CultureInfo.InvariantCulture));
        var clock = new Mock<IClock>();
        clock.SetupGet(value => value.Now).Returns(() => DateTime.UtcNow);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [DemoDataSeederBase.EnableDemoDataKey] = demoSwitch })
            .Build();
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IConfiguration>(configuration)
            .AddSingleton<ICurrentTenant>(_tenant)
            .AddSingleton(resolver.Object)
            .AddSingleton(ids.Object)
            .AddSingleton(clock.Object)
            .AddSingleton(Mock.Of<IUserDirectory>())
            .AddSingleton(Mock.Of<IRoleRepository>())
            .AddSingleton(Mock.Of<IUserRoleRepository>())
            .AddSingleton(Mock.Of<IDepartmentRepository>())
            .AddSingleton(Mock.Of<IUserDepartmentRepository>())
            .AddSingleton(Mock.Of<IPositionRepository>())
            .AddScoped<IWorkflowDefinitionRepository, WorkflowDefinitionRepository>();
        services.AddXiHanWorkflow(configuration);
        services.AddWorkflowStores();
        services.AddWorkflowAssigneeProviders();
        services.AddWorkflowDemoData();
        return services.BuildServiceProvider();
    }

    private static async Task SeedAsync(ServiceProvider services)
    {
        using var scope = services.CreateScope();
        foreach (var seeder in scope.ServiceProvider.GetServices<IDataSeeder>().OrderBy(seeder => seeder.Order))
        {
            await seeder.SeedAsync();
        }
    }
}
