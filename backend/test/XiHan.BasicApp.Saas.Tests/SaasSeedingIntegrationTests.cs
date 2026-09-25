// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SqlSugar;
using XiHan.BasicApp.Saas.Application.Pages;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Infrastructure.Seeders;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Extensions;
using XiHan.Framework.Data.SqlSugar.Seeders;
using XiHan.Framework.Domain.Entities.Abstracts;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Security.Password;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 种子真实落库的集成测试：临时 SQLite 内存库，按 Order 把 SaaS 的全部种子跑一遍
/// </summary>
/// <remarks>
/// 插入走框架同一套主键与租户注入（<see cref="EntityAuditExtensions"/>）：实体预置的租户号与当前作用域不符会直接抛错，
/// 所以这里也顺带验证了「切对了租户再写」。软删用框架同样的全局过滤器。不连真实数据库。
/// </remarks>
public sealed class SaasSeedingIntegrationTests : IDisposable
{
    private static readonly Type[] Tables =
    [
        typeof(SysOperation), typeof(SysResource), typeof(SysPermission), typeof(SysMenu),
        typeof(SysRole), typeof(SysRolePermission), typeof(SysRoleHierarchy), typeof(SysRoleDataScope),
        typeof(SysUser), typeof(SysUserSecurity), typeof(SysUserRole), typeof(SysUserPermission), typeof(SysUserDepartment),
        typeof(SysTenant), typeof(SysTenantUser), typeof(SysTenantEdition), typeof(SysTenantEditionPermission),
        typeof(SysDepartment), typeof(SysDepartmentHierarchy), typeof(SysPosition),
        typeof(SysConfig), typeof(SysStorageConfig), typeof(SysMessageTemplate), typeof(SysOAuthApp), typeof(SysTask),
        typeof(SysNotification), typeof(SysDict), typeof(SysDictItem),
    ];

    private readonly string _connectionString = $"Data Source=xihan-seed-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
    private readonly TestCurrentTenant _tenant = new();
    private readonly SqlSugarScope _db;

    /// <summary>
    /// 共享内存库在最后一个连接关闭时消失：测试期间一直开着一个连接
    /// </summary>
    private readonly Microsoft.Data.Sqlite.SqliteConnection _keepAlive;

    /// <summary>
    /// 构造函数：建临时库与种子用到的表
    /// </summary>
    public SaasSeedingIntegrationTests()
    {
        _keepAlive = new Microsoft.Data.Sqlite.SqliteConnection(_connectionString);
        _keepAlive.Open();
        _db = new SqlSugarScope(
            new ConnectionConfig
            {
                DbType = DbType.Sqlite,
                ConnectionString = _connectionString,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            },
            db =>
            {
                db.QueryFilter.AddTableFilter<ISoftDelete>(entity => !entity.IsDeleted);
                db.Aop.DataExecuting = (_, entityInfo) =>
                {
                    if (entityInfo.OperationType == DataFilterType.InsertByObject)
                    {
                        entityInfo.TrySetSnowflakeId(SnowFlakeSingle.Instance.NextId());
                        entityInfo.ToCreated(EntityAuditContext.From(null, _tenant.Id));
                    }
                };
            });
        _db.CodeFirst.InitTables(Tables);
    }

    /// <summary>
    /// 演示开关关闭：只有系统运行所需的基础数据，没有任何租户与演示账号。
    /// </summary>
    [Fact]
    public async Task Baseline_WithoutDemo_ShouldSeedOnlyWhatTheSystemNeeds()
    {
        await SeedAsync(demo: false);

        var superAdmin = await _db.Queryable<SysUser>().SingleAsync(user => user.TenantId == 0);
        Assert.Equal(SaasSuperAdminSeeder.UserName, superAdmin.UserName);
        Assert.True(superAdmin.IsSystemAccount);
        var security = await _db.Queryable<SysUserSecurity>().SingleAsync(item => item.UserId == superAdmin.BasicId);
        Assert.True(security.PasswordChangeRequired);
        Assert.Equal($"hash:{SaasSuperAdminSeeder.InitialPassword}", security.Password);
        var role = await _db.Queryable<SysRole>().SingleAsync();
        Assert.Equal((SaasRoleCodes.SuperAdmin, RoleType.System), (role.RoleCode, role.RoleType));
        Assert.Equal(0, await _db.Queryable<SysRolePermission>().CountAsync());

        Assert.Equal(OperationSeeds.All.Count, await _db.Queryable<SysOperation>().CountAsync());
        Assert.Equal(SaasPermissionDefinitions.All.Count, await _db.Queryable<SysPermission>().CountAsync());
        Assert.Equal(PageRegistry.All.Count + PageRegistry.Buttons.Count, await _db.Queryable<SysMenu>().CountAsync());
        Assert.Equal(SaasEditionSeeder.Editions.Count, await _db.Queryable<SysTenantEdition>().CountAsync());
        Assert.Equal(1, await _db.Queryable<SysStorageConfig>().CountAsync());
        Assert.Equal(1, await _db.Queryable<SysOAuthApp>().CountAsync());
        Assert.Equal(2, await _db.Queryable<SysTask>().CountAsync());
        Assert.True(await _db.Queryable<SysMessageTemplate>().CountAsync() > 0);
        Assert.True(await _db.Queryable<SysConfig>().CountAsync() > 0);

        Assert.Equal(0, await _db.Queryable<SysTenant>().CountAsync());
        Assert.Equal(0, await _db.Queryable<SysDict>().CountAsync());
        Assert.Equal(0, await _db.Queryable<SysNotification>().CountAsync());
    }

    /// <summary>
    /// 套餐：企业版白名单是租户能生效的全部权限，其余三档按声明；免费版是默认套餐。
    /// </summary>
    [Fact]
    public async Task Editions_ShouldGetWhitelistsFromTheCatalog()
    {
        await SeedAsync(demo: false);

        var tenantEffective = (await _db.Queryable<SysPermission>().ToListAsync()).Count(permission => permission.Side.IsTenantEffective());
        var editions = await _db.Queryable<SysTenantEdition>().ToListAsync();
        foreach (var seed in SaasEditionSeeder.Editions)
        {
            var edition = editions.Single(item => item.EditionCode == seed.Code);
            var count = await _db.Queryable<SysTenantEditionPermission>().CountAsync(binding => binding.EditionId == edition.BasicId);
            Assert.Equal(seed.PermissionCodes?.Count ?? tenantEffective, count);
            Assert.Equal(seed.IsDefault, edition.IsDefault);
        }
    }

    /// <summary>
    /// 演示开关开启：每个演示租户连同组织、角色、账号、成员都写进去，引用都对得上。
    /// </summary>
    [Fact]
    public async Task Demo_ShouldSeedEveryScenario()
    {
        await SeedAsync(demo: true);

        var tenants = await _db.Queryable<SysTenant>().ToListAsync();
        Assert.Equal(SaasDemoScenario.Tenants.Select(tenant => tenant.Code).Order(), tenants.Select(tenant => tenant.TenantCode).Order());
        var enterprise = tenants.Single(tenant => tenant.TenantCode == "demo-enterprise");
        var declared = SaasDemoScenario.Tenants.Single(tenant => tenant.Code == "demo-enterprise");

        Assert.Equal(declared.Members!.Count, await _db.Queryable<SysTenantUser>().CountAsync(member => member.TenantId == enterprise.BasicId));
        Assert.Equal(declared.Departments!.Count, await _db.Queryable<SysDepartment>().CountAsync(department => department.TenantId == enterprise.BasicId));
        Assert.Equal(declared.Roles!.Count + 1, await _db.Queryable<SysRole>().CountAsync(role => role.TenantId == enterprise.BasicId));

        // 所有者：系统账号、持有所有者系统角色
        var owner = await _db.Queryable<SysUser>().SingleAsync(user => user.Email == "owner@enterprise.demo");
        Assert.True(owner.IsSystemAccount);
        var ownerRole = await _db.Queryable<SysRole>().SingleAsync(role => role.TenantId == enterprise.BasicId && role.RoleCode == SaasRoleCodes.TenantOwner);
        Assert.True(await _db.Queryable<SysUserRole>().AnyAsync(binding => binding.UserId == owner.BasicId && binding.RoleId == ownerRole.BasicId));
        Assert.Equal(owner.BasicId, (await _db.Queryable<SysDepartment>().SingleAsync(department => department.TenantId == enterprise.BasicId && department.DepartmentCode == "hq")).LeaderId);

        // 租户管理员：租户能生效的全部权限，一条平台侧都没有
        var tenantAdmin = await _db.Queryable<SysRole>().SingleAsync(role => role.TenantId == enterprise.BasicId && role.RoleCode == "tenant_admin");
        var adminPermissionIds = await _db.Queryable<SysRolePermission>().Where(binding => binding.RoleId == tenantAdmin.BasicId).Select(binding => binding.PermissionId).ToListAsync();
        var permissions = await _db.Queryable<SysPermission>().ToListAsync();
        Assert.Equal(permissions.Count(permission => permission.Side.IsTenantEffective()), adminPermissionIds.Count);

        // 角色继承、自定义数据范围、闭包表
        Assert.Equal(3, await _db.Queryable<SysRoleHierarchy>().CountAsync(row => row.TenantId == enterprise.BasicId));
        Assert.Equal(2, await _db.Queryable<SysRoleDataScope>().CountAsync(row => row.TenantId == enterprise.BasicId));
        Assert.True(await _db.Queryable<SysDepartmentHierarchy>().CountAsync(row => row.TenantId == enterprise.BasicId) > declared.Departments.Count);

        // 账号与成员的状态
        Assert.Equal(EnableStatus.Disabled, (await _db.Queryable<SysUser>().SingleAsync(user => user.Email == "disabled@enterprise.demo")).Status);
        var locked = await _db.Queryable<SysUser>().SingleAsync(user => user.Email == "locked@enterprise.demo");
        Assert.True((await _db.Queryable<SysUserSecurity>().SingleAsync(item => item.UserId == locked.BasicId)).IsLocked);
        var removed = await _db.Queryable<SysUser>().SingleAsync(user => user.Email == "removed@enterprise.demo");
        Assert.Equal(ValidityStatus.Invalid, (await _db.Queryable<SysTenantUser>().SingleAsync(member => member.UserId == removed.BasicId)).Status);
        Assert.False(await _db.Queryable<SysUserSecurity>().AnyAsync(item => !item.PasswordChangeRequired));

        // 跨租户：合作方账号注册在专业版，同时是企业版的外部协作者
        var partner = await _db.Queryable<SysUser>().SingleAsync(user => user.Email == "partner@pro.demo");
        var partnerTenants = await _db.Queryable<SysTenantUser>().Where(member => member.UserId == partner.BasicId).Select(member => member.TenantId).ToListAsync();
        Assert.Equal(2, partnerTenants.Count);
        Assert.NotEqual(enterprise.BasicId, partner.TenantId);

        // 支持人员：平台账号、成员类型为平台管理员
        var support = await _db.Queryable<SysUser>().SingleAsync(user => user.TenantId == 0 && user.UserName == "support");
        Assert.Equal(TenantMemberType.PlatformAdmin, (await _db.Queryable<SysTenantUser>().SingleAsync(member => member.TenantId == enterprise.BasicId && member.UserId == support.BasicId)).MemberType);

        // 通知草稿与字典
        Assert.Equal(3, await _db.Queryable<SysNotification>().CountAsync(notification => !notification.IsPublished));
        Assert.Equal(SaasDemoDictSeeder.Dicts.Count, await _db.Queryable<SysDict>().CountAsync());
        Assert.Equal(SaasDemoDictSeeder.Dicts.Sum(dict => dict.Items.Count), await _db.Queryable<SysDictItem>().CountAsync());
    }

    /// <summary>
    /// 种子每次启动都跑：再跑一遍一行都不多。
    /// </summary>
    [Fact]
    public async Task Seeding_Twice_ShouldNotAddRows()
    {
        await SeedAsync(demo: true);
        var first = await CountAllAsync();

        await SeedAsync(demo: true);

        Assert.Equal(first, await CountAllAsync());
    }

    /// <summary>
    /// 运营改过的不覆盖：参数值、套餐价格、存储默认、模板内容、任务周期、菜单排序与启停、权限启停、演示租户名称；
    /// 代码定义的元数据照常对齐：权限名称、菜单名称。
    /// </summary>
    [Fact]
    public async Task Reseeding_ShouldKeepOperatorChangesAndRealignMetadata()
    {
        await SeedAsync(demo: true);

        var config = await _db.Queryable<SysConfig>().FirstAsync(item => item.ConfigKey == "saas.log.retention-days");
        config.ConfigValue = "30";
        config.ConfigName = "被改掉的名称";
        _ = await _db.Updateable(config).ExecuteCommandAsync();
        _ = await _db.Updateable<SysTenantEdition>().SetColumns(edition => edition.Price == 1).Where(edition => edition.EditionCode == SaasEditionSeeder.Codes.Pro).ExecuteCommandAsync();
        _ = await _db.Updateable<SysStorageConfig>().SetColumns(storage => storage.IsDefault == false).Where(storage => storage.ConfigCode == SaasStorageSeeder.LocalDefaultCode).ExecuteCommandAsync();
        _ = await _db.Updateable<SysMessageTemplate>().SetColumns(template => template.Content == "运营改过").Where(template => template.BasicId > 0).ExecuteCommandAsync();
        _ = await _db.Updateable<SysTask>().SetColumns(task => task.CronExpression == "0 0 * * *").Where(task => task.BasicId > 0).ExecuteCommandAsync();
        _ = await _db.Updateable<SysMenu>().SetColumns(menu => new SysMenu { Sort = 7, Status = EnableStatus.Disabled, MenuName = "被改掉的菜单" }).Where(menu => menu.BasicId > 0).ExecuteCommandAsync();
        _ = await _db.Updateable<SysPermission>().SetColumns(permission => new SysPermission { Status = EnableStatus.Disabled, PermissionName = "被改掉的权限" }).Where(permission => permission.BasicId > 0).ExecuteCommandAsync();
        _ = await _db.Updateable<SysTenant>().SetColumns(tenant => tenant.TenantName == "运营改名").Where(tenant => tenant.TenantCode == "demo-pro").ExecuteCommandAsync();

        await SeedAsync(demo: true);

        var realigned = await _db.Queryable<SysConfig>().FirstAsync(item => item.ConfigKey == "saas.log.retention-days");
        Assert.Equal("30", realigned.ConfigValue);
        Assert.Equal("日志保留天数", realigned.ConfigName);
        Assert.Equal(1, (await _db.Queryable<SysTenantEdition>().SingleAsync(edition => edition.EditionCode == SaasEditionSeeder.Codes.Pro)).Price);
        Assert.False((await _db.Queryable<SysStorageConfig>().SingleAsync()).IsDefault);
        Assert.All(await _db.Queryable<SysMessageTemplate>().ToListAsync(), template => Assert.Equal("运营改过", template.Content));
        Assert.All(await _db.Queryable<SysTask>().ToListAsync(), task => Assert.Equal("0 0 * * *", task.CronExpression));
        var menus = await _db.Queryable<SysMenu>().ToListAsync();
        Assert.All(menus, menu => Assert.Equal((7, EnableStatus.Disabled), (menu.Sort, menu.Status)));
        Assert.DoesNotContain(menus, menu => menu.MenuName == "被改掉的菜单");
        var catalog = await _db.Queryable<SysPermission>().ToListAsync();
        Assert.All(catalog, permission => Assert.Equal(EnableStatus.Disabled, permission.Status));
        Assert.DoesNotContain(catalog, permission => permission.PermissionName == "被改掉的权限");
        Assert.Equal("运营改名", (await _db.Queryable<SysTenant>().SingleAsync(tenant => tenant.TenantCode == "demo-pro")).TenantName);
    }

    /// <summary>
    /// 运营删掉的只插入型数据不补回：套餐、消息模板、演示租户。
    /// </summary>
    [Fact]
    public async Task Reseeding_ShouldNotResurrectDeletedRows()
    {
        await SeedAsync(demo: true);
        _ = await _db.Updateable<SysTenantEdition>().SetColumns(edition => edition.IsDeleted == true).Where(edition => edition.EditionCode == SaasEditionSeeder.Codes.Basic).ExecuteCommandAsync();
        _ = await _db.Updateable<SysMessageTemplate>().SetColumns(template => template.IsDeleted == true).Where(template => template.BasicId > 0).ExecuteCommandAsync();
        _ = await _db.Updateable<SysTenant>().SetColumns(tenant => tenant.IsDeleted == true).Where(tenant => tenant.TenantCode == "demo-free").ExecuteCommandAsync();

        await SeedAsync(demo: true);

        Assert.False(await _db.Queryable<SysTenantEdition>().AnyAsync(edition => edition.EditionCode == SaasEditionSeeder.Codes.Basic));
        Assert.Equal(0, await _db.Queryable<SysMessageTemplate>().CountAsync());
        Assert.False(await _db.Queryable<SysTenant>().AnyAsync(tenant => tenant.TenantCode == "demo-free"));
    }

    /// <summary>
    /// 关掉连接，内存库随之消失
    /// </summary>
    public void Dispose()
    {
        _db.Dispose();
        _keepAlive.Dispose();
    }

    private async Task SeedAsync(bool demo)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [DemoDataSeederBase.EnableDemoDataKey] = demo ? "true" : "false" })
            .Build();
        var protector = new Mock<IConfigValueSecretProtector>();
        protector.Setup(value => value.Protect(It.IsAny<string?>())).Returns((string? plaintext) => $"enc:{plaintext}");
        using var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddSingleton<ICurrentTenant>(_tenant)
            .AddSingleton(protector.Object)
            .BuildServiceProvider();

        var resolver = new Mock<ISqlSugarClientResolver>();
        resolver.Setup(value => value.GetCurrentClient()).Returns(_db);
        resolver.Setup(value => value.GetClientForEntity(It.IsAny<Type>())).Returns(_db);
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(value => value.HashPassword(It.IsAny<string>())).Returns((string password) => $"hash:{password}");

        IDataSeeder[] seeders =
        [
            new SaasSuperAdminSeeder(resolver.Object, NullLogger<SaasSuperAdminSeeder>.Instance, services, hasher.Object),
            new SaasOperationSeeder(resolver.Object, NullLogger<SaasOperationSeeder>.Instance, services),
            new SaasPermissionCatalogSeeder(resolver.Object, NullLogger<SaasPermissionCatalogSeeder>.Instance, services),
            new SaasMenuSeeder(resolver.Object, NullLogger<SaasMenuSeeder>.Instance, services),
            new SaasEditionSeeder(resolver.Object, NullLogger<SaasEditionSeeder>.Instance, services),
            new SaasSettingSeeder(resolver.Object, NullLogger<SaasSettingSeeder>.Instance, services),
            new SaasStorageSeeder(resolver.Object, NullLogger<SaasStorageSeeder>.Instance, services),
            new SaasMessageTemplateSeeder(resolver.Object, NullLogger<SaasMessageTemplateSeeder>.Instance, services),
            new SaasOAuthAppSeeder(resolver.Object, NullLogger<SaasOAuthAppSeeder>.Instance, services),
            new SaasTaskSeeder(resolver.Object, NullLogger<SaasTaskSeeder>.Instance, services),
            new SaasDemoSeeder(resolver.Object, NullLogger<SaasDemoSeeder>.Instance, services, hasher.Object),
            new SaasDemoNotificationSeeder(resolver.Object, NullLogger<SaasDemoNotificationSeeder>.Instance, services),
            new SaasDemoDictSeeder(resolver.Object, NullLogger<SaasDemoDictSeeder>.Instance, services),
        ];

        foreach (var seeder in seeders.OrderBy(seeder => seeder.Order))
        {
            await seeder.SeedAsync();
            Assert.Null(_tenant.Id);
        }
    }

    private async Task<Dictionary<string, int>> CountAllAsync()
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var table in Tables)
        {
            var name = _db.EntityMaintenance.GetTableName(table);
            counts[table.Name] = await _db.Ado.GetIntAsync($"SELECT COUNT(1) FROM {name}");
        }

        return counts;
    }
}
