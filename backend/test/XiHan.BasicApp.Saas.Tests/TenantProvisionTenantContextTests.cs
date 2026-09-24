// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Core.Exceptions;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 租户开通链路的租户上下文测试。
/// </summary>
/// <remarks>
/// <para>
/// 平台就是 0 号租户，写只能落在当前作用域：开通时租户注册表与版本白名单是平台数据，在平台作用域读写；
/// 管理员账号、成员关系、Owner 角色与授权绑定是被开通租户的数据，必须切入该租户写（行的 TenantId 由作用域决定）。
/// </para>
/// <para>
/// 账号、成员关系、角色与授权固定在平台库，库隔离租户也一样；开通时连带写的租户数据（日志等）在租户自己的库里，
/// 所以库隔离租户要等独立库配置完成才能开通管理员，Schema 隔离尚未实装一律拒绝。
/// </para>
/// </remarks>
public sealed class TenantProvisionTenantContextTests
{
    /// <summary>
    /// 被开通租户主键
    /// </summary>
    private const long TenantId = 4001;

    /// <summary>
    /// 被开通租户绑定的版本主键
    /// </summary>
    private const long EditionId = 55;

    /// <summary>
    /// 版本白名单内的权限主键
    /// </summary>
    private const long WhitelistPermissionId = 9001;

    /// <summary>
    /// 调用方进入开通时所处的租户上下文
    /// </summary>
    private const long AmbientTenantId = 77;

    /// <summary>
    /// 账号、安全信息、成员关系、角色、授权与角色绑定都在被开通租户的作用域内写入。
    /// </summary>
    [Fact]
    public async Task ProvisionTenantAdmin_ShouldWriteTenantRowsInsideTargetTenant()
    {
        var fixture = CreateFixture(ambientTenantId: AmbientTenantId);

        _ = await fixture.Service.ProvisionTenantAdminAsync(fixture.Tenant, "owner", "owner@example.com", "hash");

        var writes = fixture.Observations.Where(observation => observation.IsWrite).ToList();
        Assert.Equal(
            ["User.Add", "UserSecurity.Add", "TenantUser.Add", "Role.Add", "RolePermission.AddRange", "UserRole.Add"],
            writes.Select(observation => observation.Operation));
        Assert.All(writes, observation => Assert.Equal(TenantId, observation.TenantId));
    }

    /// <summary>
    /// 版本白名单是平台数据，在平台作用域读取（不借用被开通租户的读共享）。
    /// </summary>
    [Fact]
    public async Task ProvisionTenantAdmin_ShouldReadEditionWhitelistInPlatformScope()
    {
        var fixture = CreateFixture(ambientTenantId: AmbientTenantId);

        _ = await fixture.Service.ProvisionTenantAdminAsync(fixture.Tenant, "owner", "owner@example.com", "hash");

        var whitelistReads = fixture.Observations
            .Where(observation => observation.Operation == "TenantEditionPermission.GetByEditionId")
            .ToList();
        Assert.NotEmpty(whitelistReads);
        Assert.All(whitelistReads, observation => Assert.Null(observation.TenantId));
    }

    /// <summary>
    /// 开通结束后还原调用方的租户上下文。
    /// </summary>
    [Fact]
    public async Task ProvisionTenantAdmin_ShouldRestoreCallerTenantContext()
    {
        var fixture = CreateFixture(ambientTenantId: AmbientTenantId);

        _ = await fixture.Service.ProvisionTenantAdminAsync(fixture.Tenant, "owner", "owner@example.com", "hash");

        Assert.Equal(AmbientTenantId, fixture.CurrentTenant.Id);
    }

    /// <summary>
    /// 用户名判重的租户范围必须由入参显式传入，不能靠当前上下文的全局过滤器。
    /// </summary>
    [Fact]
    public async Task ProvisionTenantAdmin_ShouldScopeUserNameCheckToTargetTenant()
    {
        var fixture = CreateFixture();

        _ = await fixture.Service.ProvisionTenantAdminAsync(fixture.Tenant, "owner", "owner@example.com", "hash");

        fixture.UserRepository.Verify(
            repo => repo.ExistsUserNameInTenantAsync(TenantId, "owner", null, It.IsAny<CancellationToken>()),
            Times.Once);
        fixture.UserRepository.Verify(
            repo => repo.ExistsUserNameAsync(It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 库隔离租户的独立库还没建好：开通管理员直接拒绝，不写任何行。
    /// </summary>
    [Fact]
    public async Task ProvisionTenantAdmin_DatabaseTenantWithoutDatabase_ShouldBeRejectedWithoutWriting()
    {
        var fixture = CreateFixture(isolationMode: TenantIsolationMode.Database, configStatus: TenantConfigStatus.Pending);

        _ = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.ProvisionTenantAdminAsync(fixture.Tenant, "owner", "owner@example.com", "hash"));

        Assert.DoesNotContain(fixture.Observations, observation => observation.IsWrite);
    }

    /// <summary>
    /// 库隔离租户的独立库已配置完成：与字段隔离同一口径，全部在被开通租户的作用域内写入。
    /// </summary>
    [Fact]
    public async Task ProvisionTenantAdmin_DatabaseTenantWithConfiguredDatabase_ShouldWriteInsideTargetTenant()
    {
        var fixture = CreateFixture(ambientTenantId: AmbientTenantId, isolationMode: TenantIsolationMode.Database);

        _ = await fixture.Service.ProvisionTenantAdminAsync(fixture.Tenant, "owner", "owner@example.com", "hash");

        var writes = fixture.Observations.Where(observation => observation.IsWrite).ToList();
        Assert.Equal(
            ["User.Add", "UserSecurity.Add", "TenantUser.Add", "Role.Add", "RolePermission.AddRange", "UserRole.Add"],
            writes.Select(observation => observation.Operation));
        Assert.All(writes, observation => Assert.Equal(TenantId, observation.TenantId));
    }

    /// <summary>
    /// Schema 隔离尚未实装：拒绝开通，不写任何行。
    /// </summary>
    [Fact]
    public async Task ProvisionTenantAdmin_SchemaTenant_ShouldBeRejectedWithoutWriting()
    {
        var fixture = CreateFixture(isolationMode: TenantIsolationMode.Schema);

        _ = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.ProvisionTenantAdminAsync(fixture.Tenant, "owner", "owner@example.com", "hash"));

        Assert.DoesNotContain(fixture.Observations, observation => observation.IsWrite);
    }

    /// <summary>
    /// 独立库已配置、还没有所有者的库隔离租户可以初始化管理员；所有者检查在该租户作用域内进行。
    /// </summary>
    [Fact]
    public async Task GetTenantAwaitingAdmin_ConfiguredDatabaseTenantWithoutOwner_ShouldReturnTenant()
    {
        var fixture = CreateFixture(ambientTenantId: AmbientTenantId, isolationMode: TenantIsolationMode.Database);

        var tenant = await fixture.Service.GetTenantAwaitingAdminAsync(TenantId);

        Assert.Same(fixture.Tenant, tenant);
        Assert.Null(Assert.Single(fixture.Observations, observation => observation.Operation == "Tenant.GetById").TenantId);
        Assert.Equal(TenantId, Assert.Single(fixture.Observations, observation => observation.Operation == "TenantUser.AnyOwner").TenantId);
        Assert.Equal(AmbientTenantId, fixture.CurrentTenant.Id);
    }

    /// <summary>
    /// 字段隔离租户同样是建好之后再初始化管理员：没有所有者就可以初始化。
    /// </summary>
    [Fact]
    public async Task GetTenantAwaitingAdmin_FieldTenantWithoutOwner_ShouldReturnTenant()
    {
        var fixture = CreateFixture();

        var tenant = await fixture.Service.GetTenantAwaitingAdminAsync(TenantId);

        Assert.Same(fixture.Tenant, tenant);
    }

    /// <summary>
    /// Schema 隔离尚未实装：不能初始化管理员。
    /// </summary>
    [Fact]
    public async Task GetTenantAwaitingAdmin_SchemaTenant_ShouldReject()
    {
        var fixture = CreateFixture(isolationMode: TenantIsolationMode.Schema);

        _ = await Assert.ThrowsAsync<UserFriendlyException>(() => fixture.Service.GetTenantAwaitingAdminAsync(TenantId));
    }

    /// <summary>
    /// 独立库还没建好：先初始化数据库。
    /// </summary>
    [Fact]
    public async Task GetTenantAwaitingAdmin_DatabaseNotConfigured_ShouldReject()
    {
        var fixture = CreateFixture(isolationMode: TenantIsolationMode.Database, configStatus: TenantConfigStatus.Failed);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() => fixture.Service.GetTenantAwaitingAdminAsync(TenantId));

        Assert.Contains("先初始化数据库", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 已有所有者：管理员已开通，不能重复初始化。
    /// </summary>
    [Fact]
    public async Task GetTenantAwaitingAdmin_OwnerExists_ShouldReject()
    {
        var fixture = CreateFixture(isolationMode: TenantIsolationMode.Database, ownerExists: true);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() => fixture.Service.GetTenantAwaitingAdminAsync(TenantId));

        Assert.Contains("已开通管理员", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 开通管理员不改租户注册表：版本在建租户时已经定下，没有版本的租户按未启用门控处理，Owner 角色不授权。
    /// </summary>
    [Fact]
    public async Task ProvisionTenantAdmin_WithoutEdition_ShouldNotTouchTenantRegistry()
    {
        var fixture = CreateFixture(ambientTenantId: AmbientTenantId, editionId: null);

        _ = await fixture.Service.ProvisionTenantAdminAsync(fixture.Tenant, "owner", "owner@example.com", "hash");

        Assert.Null(fixture.Tenant.EditionId);
        Assert.DoesNotContain(fixture.Observations, observation => observation.Operation == "Tenant.Update");
        Assert.DoesNotContain(fixture.Observations, observation => observation.Operation == "RolePermission.AddRange");
    }

    /// <summary>
    /// 套餐回收：版本白名单在平台作用域读取，授权绑定在被开通租户的作用域内读取与回写。
    /// </summary>
    [Fact]
    public async Task ReconcileTenantAuthorization_ShouldReadWhitelistInPlatformAndRecycleInsideTenant()
    {
        var fixture = CreateFixture(ambientTenantId: AmbientTenantId);
        var stale = new SysRolePermission
        {
            TenantId = TenantId,
            PermissionId = 9999,
            Status = ValidityStatus.Valid
        };
        _ = fixture.RolePermissionRepository
            .Setup(repo => repo.GetListAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<SysRolePermission, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                fixture.Record("RolePermission.GetList");
                return new List<SysRolePermission> { stale };
            });

        var recycled = await fixture.Service.ReconcileTenantAuthorizationWithEditionAsync(fixture.Tenant);

        Assert.Equal(1, recycled);
        Assert.Equal(ValidityStatus.Invalid, stale.Status);
        Assert.Null(Assert.Single(fixture.Observations, observation => observation.Operation == "TenantEditionPermission.GetByEditionId").TenantId);
        Assert.All(
            fixture.Observations.Where(observation => observation.Operation is "RolePermission.GetList" or "RolePermission.UpdateRange" or "UserPermission.GetList"),
            observation => Assert.Equal(TenantId, observation.TenantId));
        Assert.Equal(AmbientTenantId, fixture.CurrentTenant.Id);
    }

    /// <summary>
    /// 构造被测服务及其依赖替身，并在每个仓储调用点记录当时的租户上下文。
    /// </summary>
    /// <param name="ambientTenantId">调用方进入本服务时所处的租户上下文</param>
    /// <param name="isolationMode">被开通租户的隔离模式</param>
    /// <param name="editionId">被开通租户绑定的版本（null 表示未指定，取默认版本）</param>
    /// <param name="configStatus">被开通租户的配置状态</param>
    /// <param name="ownerExists">被开通租户是否已有所有者</param>
    private static ProvisionFixture CreateFixture(
        long? ambientTenantId = null,
        TenantIsolationMode isolationMode = TenantIsolationMode.Field,
        long? editionId = EditionId,
        TenantConfigStatus configStatus = TenantConfigStatus.Configured,
        bool ownerExists = false)
    {
        var currentTenant = new TestCurrentTenant(ambientTenantId);
        var observations = new List<Observation>();

        void Record(string operation) => observations.Add(new Observation(operation, currentTenant.Id, IsWrite: false));
        void RecordWrite(string operation) => observations.Add(new Observation(operation, currentTenant.Id, IsWrite: true));

        var tenant = new SysTenant
        {
            TenantName = "业务租户",
            EditionId = editionId,
            IsolationMode = isolationMode,
            ConfigStatus = configStatus
        };
        SaasTestHelper.SetBasicId(tenant, TenantId);

        var userRepository = new Mock<IUserRepository>();
        _ = userRepository
            .Setup(repo => repo.ExistsEmailGloballyAsync(It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                Record("User.ExistsEmailGlobally");
                return false;
            });
        _ = userRepository
            .Setup(repo => repo.ExistsUserNameInTenantAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                Record("User.ExistsUserNameInTenant");
                return false;
            });
        _ = userRepository
            .Setup(repo => repo.AddAsync(It.IsAny<SysUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysUser _, CancellationToken _) =>
            {
                RecordWrite("User.Add");
                var saved = new SysUser();
                SaasTestHelper.SetBasicId(saved, 101);
                return saved;
            });

        var userSecurityRepository = new Mock<IUserSecurityRepository>();
        _ = userSecurityRepository
            .Setup(repo => repo.AddAsync(It.IsAny<SysUserSecurity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysUserSecurity security, CancellationToken _) =>
            {
                RecordWrite("UserSecurity.Add");
                return security;
            });

        var tenantUserRepository = new Mock<ITenantUserRepository>();
        _ = tenantUserRepository
            .Setup(repo => repo.AddAsync(It.IsAny<SysTenantUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysTenantUser member, CancellationToken _) =>
            {
                RecordWrite("TenantUser.Add");
                return member;
            });
        _ = tenantUserRepository
            .Setup(repo => repo.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<SysTenantUser, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                Record("TenantUser.AnyOwner");
                return ownerExists;
            });

        var roleRepository = new Mock<IRoleRepository>();
        _ = roleRepository
            .Setup(repo => repo.AddAsync(It.IsAny<SysRole>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysRole _, CancellationToken _) =>
            {
                RecordWrite("Role.Add");
                var saved = new SysRole();
                SaasTestHelper.SetBasicId(saved, 201);
                return saved;
            });

        var rolePermissionRepository = new Mock<IRolePermissionRepository>();
        _ = rolePermissionRepository
            .Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<SysRolePermission>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<SysRolePermission> grants, CancellationToken _) =>
            {
                RecordWrite("RolePermission.AddRange");
                return grants.ToList();
            });
        _ = rolePermissionRepository
            .Setup(repo => repo.UpdateRangeAsync(It.IsAny<IEnumerable<SysRolePermission>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<SysRolePermission> rows, CancellationToken _) =>
            {
                Record("RolePermission.UpdateRange");
                return rows.ToList();
            });

        var userRoleRepository = new Mock<IUserRoleRepository>();
        _ = userRoleRepository
            .Setup(repo => repo.AddAsync(It.IsAny<SysUserRole>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysUserRole userRole, CancellationToken _) =>
            {
                RecordWrite("UserRole.Add");
                return userRole;
            });

        var userPermissionRepository = new Mock<IUserPermissionRepository>();
        _ = userPermissionRepository
            .Setup(repo => repo.GetListAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<SysUserPermission, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                Record("UserPermission.GetList");
                return new List<SysUserPermission>();
            });

        var tenantEditionPermissionRepository = new Mock<ITenantEditionPermissionRepository>();
        _ = tenantEditionPermissionRepository
            .Setup(repo => repo.GetByEditionIdAsync(EditionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                Record("TenantEditionPermission.GetByEditionId");
                return new List<SysTenantEditionPermission>
                {
                    new()
                    {
                        EditionId = EditionId,
                        PermissionId = WhitelistPermissionId,
                        Status = ValidityStatus.Valid
                    }
                };
            });

        var defaultEdition = new SysTenantEdition { IsDefault = true };
        SaasTestHelper.SetBasicId(defaultEdition, EditionId);
        var tenantEditionRepository = new Mock<ITenantEditionRepository>();
        _ = tenantEditionRepository
            .Setup(repo => repo.GetDefaultEditionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                Record("TenantEdition.GetDefault");
                return defaultEdition;
            });

        var tenantRepository = new Mock<ITenantRepository>();
        _ = tenantRepository
            .Setup(repo => repo.GetByIdAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                Record("Tenant.GetById");
                return tenant;
            });
        _ = tenantRepository
            .Setup(repo => repo.UpdateAsync(It.IsAny<SysTenant>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysTenant updated, CancellationToken _) =>
            {
                Record("Tenant.Update");
                return updated;
            });

        var service = new TenantProvisionDomainService(
            userRepository.Object,
            userSecurityRepository.Object,
            userRoleRepository.Object,
            tenantUserRepository.Object,
            tenantEditionRepository.Object,
            tenantRepository.Object,
            roleRepository.Object,
            rolePermissionRepository.Object,
            userPermissionRepository.Object,
            tenantEditionPermissionRepository.Object,
            currentTenant);

        return new ProvisionFixture(
            service,
            tenant,
            currentTenant,
            userRepository,
            rolePermissionRepository,
            observations,
            Record);
    }

    /// <summary>
    /// 一次仓储调用及其发生时的租户上下文。
    /// </summary>
    private sealed record Observation(string Operation, long? TenantId, bool IsWrite);

    /// <summary>
    /// 开通测试依赖集合。
    /// </summary>
    private sealed record ProvisionFixture(
        TenantProvisionDomainService Service,
        SysTenant Tenant,
        TestCurrentTenant CurrentTenant,
        Mock<IUserRepository> UserRepository,
        Mock<IRolePermissionRepository> RolePermissionRepository,
        List<Observation> Observations,
        Action<string> Record);
}
