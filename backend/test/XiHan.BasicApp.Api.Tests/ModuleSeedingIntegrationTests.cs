// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SqlSugar;
using XiHan.BasicApp.AI.Extensions;
using XiHan.BasicApp.Chat.Extensions;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Extensions;
using XiHan.BasicApp.Printing.Extensions;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Extensions;
using XiHan.BasicApp.Saas.Infrastructure.Seeders;
using XiHan.BasicApp.Workflow.Extensions;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Extensions;
using XiHan.Framework.Data.SqlSugar.Seeders;
using XiHan.Framework.Domain.Entities.Abstracts;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Security.Password;

namespace XiHan.BasicApp.Api.Tests;

/// <summary>
/// 全部模块的种子按各自的注册方式装进容器、按 Order 在空库上真跑一遍
/// </summary>
/// <remarks>
/// 跨模块的依赖只有真跑才看得出来：模块菜单挂在 SaaS 的目录下、模块权限要进企业版白名单、演示角色要拿到模块权限。
/// 临时 SQLite 内存库，插入走框架同一套主键与租户注入；不连真实数据库。
/// </remarks>
public sealed class ModuleSeedingIntegrationTests : IDisposable
{
    private static readonly Type[] Tables =
    [
        typeof(SysOperation), typeof(SysResource), typeof(SysPermission), typeof(SysMenu),
        typeof(SysRole), typeof(SysRolePermission), typeof(SysRoleHierarchy), typeof(SysRoleDataScope),
        typeof(SysUser), typeof(SysUserSecurity), typeof(SysUserRole), typeof(SysUserPermission), typeof(SysUserDepartment),
        typeof(SysTenant), typeof(SysTenantUser), typeof(SysTenantEdition), typeof(SysTenantEditionPermission),
        typeof(SysDepartment), typeof(SysDepartmentHierarchy), typeof(SysPosition),
        typeof(SysConfig), typeof(SysStorageConfig), typeof(SysMessageTemplate), typeof(SysOAuthApp), typeof(SysTask),
        typeof(SysNotification), typeof(SysDict), typeof(SysDictItem), typeof(SysCodeGenTemplate),
    ];

    private readonly string _connectionString = $"Data Source=xihan-modules-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
    private readonly SqliteConnection _keepAlive;
    private readonly SqlSugarScope _db;
    private readonly ServiceProvider _services;

    /// <summary>
    /// 构造函数：建临时库，把六个模块的种子按模块自己的注册方法装进容器
    /// </summary>
    public ModuleSeedingIntegrationTests()
    {
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();

        var tenant = new ProbeCurrentTenant();
        _db = new SqlSugarScope(
            new ConnectionConfig { DbType = DbType.Sqlite, ConnectionString = _connectionString, IsAutoCloseConnection = true, InitKeyType = InitKeyType.Attribute },
            db =>
            {
                db.QueryFilter.AddTableFilter<ISoftDelete>(entity => !entity.IsDeleted);
                db.Aop.DataExecuting = (_, entityInfo) =>
                {
                    if (entityInfo.OperationType == DataFilterType.InsertByObject)
                    {
                        entityInfo.TrySetSnowflakeId(SnowFlakeSingle.Instance.NextId());
                        entityInfo.ToCreated(EntityAuditContext.From(null, tenant.Id));
                    }
                };
            });
        _db.CodeFirst.InitTables(Tables);

        var resolver = new Mock<ISqlSugarClientResolver>();
        resolver.Setup(value => value.GetCurrentClient()).Returns(_db);
        resolver.Setup(value => value.GetClientForEntity(It.IsAny<Type>())).Returns(_db);
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(value => value.HashPassword(It.IsAny<string>())).Returns((string password) => $"hash:{password}");
        var protector = new Mock<IConfigValueSecretProtector>();
        protector.Setup(value => value.Protect(It.IsAny<string?>())).Returns((string? plaintext) => $"enc:{plaintext}");

        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { [DemoDataSeederBase.EnableDemoDataKey] = "true" })
                .Build())
            .AddSingleton<ICurrentTenant>(tenant)
            .AddSingleton(resolver.Object)
            .AddSingleton(hasher.Object)
            .AddSingleton(protector.Object);
        services.AddSaasDataSeeders().AddSaasDemoDataSeeders();
        services.AddCodeGenerationDataSeeders();
        services.AddAIDataSeeders();
        services.AddWorkflowDataSeeders();
        services.AddChatDataSeeders();
        services.AddPrintingDataSeeders();
        _services = services.BuildServiceProvider();
    }

    /// <summary>
    /// 全部种子按 Order 跑完不报错；模块的权限、菜单、参数、任务、模板都落库。
    /// </summary>
    [Fact]
    public async Task AllModuleSeeders_ShouldSeedACleanDatabase()
    {
        await SeedAsync();

        foreach (var module in new[] { "code_gen", "ai", "workflow", "chat", "print-template" })
        {
            Assert.True(await _db.Queryable<SysPermission>().AnyAsync(permission => permission.ModuleCode == module), $"模块 {module} 的权限没有落库");
        }

        Assert.All(await _db.Queryable<SysPermission>().ToListAsync(), permission => Assert.StartsWith("[\"", permission.Tags, StringComparison.Ordinal));
        Assert.True(await _db.Queryable<SysCodeGenTemplate>().AnyAsync());
        Assert.True(await _db.Queryable<SysConfig>().AnyAsync(config => config.ConfigKey == "chat.policy"));
        Assert.True(await _db.Queryable<SysTask>().AnyAsync(task => task.TaskCode == "chat-retention-cleanup"));
        Assert.Equal(0, await _db.Queryable<SysMenu>().CountAsync(menu => menu.ParentId == null && menu.MenuType == MenuType.Button));
    }

    /// <summary>
    /// 企业版白名单与演示租户的管理员角色都拿到各模块租户能生效的权限，平台侧的一条都不进。
    /// </summary>
    [Fact]
    public async Task EnterpriseEditionAndTenantAdmin_ShouldIncludeModulePermissions()
    {
        await SeedAsync();

        var permissions = await _db.Queryable<SysPermission>().ToListAsync();
        var tenantEffective = permissions.Where(permission => permission.Side.IsTenantEffective()).Select(permission => permission.BasicId).ToHashSet();
        var enterprise = await _db.Queryable<SysTenantEdition>().SingleAsync(edition => edition.EditionCode == SaasEditionSeeder.Codes.Enterprise);
        var whitelist = await _db.Queryable<SysTenantEditionPermission>().Where(binding => binding.EditionId == enterprise.BasicId).Select(binding => binding.PermissionId).ToListAsync();
        Assert.Equal(tenantEffective.Order(), whitelist.Order());
        Assert.Contains(permissions, permission => permission.PermissionCode == "workflow:read" && tenantEffective.Contains(permission.BasicId));
        Assert.DoesNotContain(permissions, permission => permission.ModuleCode == "code_gen" && tenantEffective.Contains(permission.BasicId));

        var demoTenant = await _db.Queryable<SysTenant>().SingleAsync(tenant => tenant.TenantCode == "demo-enterprise");
        var tenantAdmin = await _db.Queryable<SysRole>().SingleAsync(role => role.TenantId == demoTenant.BasicId && role.RoleCode == "tenant_admin");
        var granted = await _db.Queryable<SysRolePermission>().Where(binding => binding.RoleId == tenantAdmin.BasicId).Select(binding => binding.PermissionId).ToListAsync();
        Assert.Equal(tenantEffective.Order(), granted.Order());

        // 演示的直授禁止：装了聊天模块就写上
        var direct = await _db.Queryable<SysUser>().SingleAsync(user => user.Email == "direct@enterprise.demo");
        var chatSend = permissions.Single(permission => permission.PermissionCode == "chat:send");
        Assert.True(await _db.Queryable<SysUserPermission>().AnyAsync(item => item.UserId == direct.BasicId && item.PermissionId == chatSend.BasicId && item.PermissionAction == PermissionAction.Deny));
    }

    /// <summary>
    /// 再跑一遍一行都不多。
    /// </summary>
    [Fact]
    public async Task AllModuleSeeders_Twice_ShouldNotAddRows()
    {
        await SeedAsync();
        var first = await CountAllAsync();

        await SeedAsync();

        Assert.Equal(first, await CountAllAsync());
    }

    /// <summary>
    /// 关掉连接，内存库随之消失
    /// </summary>
    public void Dispose()
    {
        _services.Dispose();
        _db.Dispose();
        _keepAlive.Dispose();
    }

    private async Task SeedAsync()
    {
        using var scope = _services.CreateScope();
        foreach (var seeder in scope.ServiceProvider.GetServices<IDataSeeder>().OrderBy(seeder => seeder.Order))
        {
            await seeder.SeedAsync();
        }
    }

    private async Task<Dictionary<string, int>> CountAllAsync()
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var table in Tables)
        {
            counts[table.Name] = await _db.Ado.GetIntAsync($"SELECT COUNT(1) FROM {_db.EntityMaintenance.GetTableName(table)}");
        }

        return counts;
    }

    /// <summary>
    /// 可切换的当前租户
    /// </summary>
    private sealed class ProbeCurrentTenant : ICurrentTenant
    {
        public bool IsAvailable => Id is > 0;

        public long? Id { get; private set; }

        public string? Name { get; private set; }

        public IDisposable Change(long? id, string? name = null)
        {
            var (previousId, previousName) = (Id, Name);
            (Id, Name) = (id, name);
            return new Restore(() => (Id, Name) = (previousId, previousName));
        }

        private sealed class Restore(Action restore) : IDisposable
        {
            public void Dispose()
            {
                restore();
            }
        }
    }
}
