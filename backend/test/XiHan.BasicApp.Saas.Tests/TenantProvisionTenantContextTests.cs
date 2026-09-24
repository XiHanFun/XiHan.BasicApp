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
/// 库隔离租户的账号与授权数据归置尚未完成，开通时显式拒绝——不能像过去那样在平台作用域预置租户戳，
/// 把它的数据写进平台库。
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
    /// 库隔离租户的数据归置尚未完成：开通直接拒绝，不写任何行。
    /// </summary>
    [Fact]
    public async Task ProvisionTenantAdmin_DatabaseIsolatedTenant_ShouldBeRejectedWithoutWriting()
    {
        var fixture = CreateFixture(isolationMode: TenantIsolationMode.Database);

        _ = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.ProvisionTenantAdminAsync(fixture.Tenant, "owner", "owner@example.com", "hash"));

        Assert.DoesNotContain(fixture.Observations, observation => observation.IsWrite);
    }

    /// <summary>
    /// 未指定版本的租户：取默认版本并在平台作用域持久化到租户注册表，随后按该版本白名单授权。
    /// </summary>
    [Fact]
    public async Task ProvisionTenantAdmin_WithoutEdition_ShouldPersistDefaultEditionInPlatformScope()
    {
        var fixture = CreateFixture(ambientTenantId: AmbientTenantId, editionId: null);

        _ = await fixture.Service.ProvisionTenantAdminAsync(fixture.Tenant, "owner", "owner@example.com", "hash");

        Assert.Equal(EditionId, fixture.Tenant.EditionId);
        var tenantUpdate = Assert.Single(fixture.Observations, observation => observation.Operation == "Tenant.Update");
        Assert.Null(tenantUpdate.TenantId);
        Assert.Contains(fixture.Observations, observation => observation.Operation == "RolePermission.AddRange");
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
    private static ProvisionFixture CreateFixture(
        long? ambientTenantId = null,
        TenantIsolationMode isolationMode = TenantIsolationMode.Field,
        long? editionId = EditionId)
    {
        var currentTenant = new TestCurrentTenant(ambientTenantId);
        var observations = new List<Observation>();

        void Record(string operation) => observations.Add(new Observation(operation, currentTenant.Id, IsWrite: false));
        void RecordWrite(string operation) => observations.Add(new Observation(operation, currentTenant.Id, IsWrite: true));

        var tenant = new SysTenant
        {
            TenantName = "业务租户",
            EditionId = editionId,
            IsolationMode = isolationMode
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
