// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 租户开通链路的租户上下文测试。
/// </summary>
/// <remarks>
/// <para>
/// 这组测试守的是一条不变式：<b>开通全程不得进入被开通租户的上下文</b>。
/// 租户上下文决定连接解析（<c>SqlSugarClientResolver</c> 经 <c>SaasTenantConnectionProvider</c> 按当前租户选库），
/// 而库隔离租户的独立库在开通期还不存在——创建出来时 <c>ConfigStatus=Pending</c>，建库是 <c>InitializeDatabase</c>
/// 的独立步骤。进了租户上下文，判重与写入就都打到那个还没建的库上。
/// </para>
/// <para>
/// 字段隔离部署下平台库与租户库本就是同一个库，这条不变式不改变其行为：落点由各实体显式置的
/// <c>TenantId</c> 决定，平台态插入保留该预置值，所以第三条用例把「每行都带目标租户 Id」一并钉死。
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
    /// 开通期每一次仓储调用都必须发生在平台态，而不是被开通租户的上下文里。
    /// </summary>
    [Fact]
    public async Task ProvisionTenantAdmin_ShouldRunEveryRepositoryCallInPlatformContext()
    {
        var fixture = CreateFixture(ambientTenantId: 77);

        _ = await fixture.Service.ProvisionTenantAdminAsync(fixture.Tenant, "owner", "owner@example.com", "hash");

        Assert.NotEmpty(fixture.Observations);
        var inTenantContext = fixture.Observations
            .Where(observation => observation.TenantId is not null)
            .Select(observation => $"{observation.Operation}=>{observation.TenantId}")
            .ToList();
        Assert.Empty(inTenantContext);
    }

    /// <summary>
    /// 用户名判重的租户范围必须由入参显式传入，不能靠当前上下文的全局过滤器。
    /// </summary>
    /// <remarks>
    /// 平台态下全局租户过滤器是放行全部的，若继续走按上下文过滤的 <c>ExistsUserNameAsync</c>，
    /// 「租户内唯一」会被悄悄放大成「全平台唯一」——另一个租户已有同名管理员就建不出来了。
    /// </remarks>
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
    /// 平台态写入时落点全靠实体自带的 TenantId，因此开通写下的每一行都必须带目标租户 Id。
    /// </summary>
    [Fact]
    public async Task ProvisionTenantAdmin_ShouldStampTargetTenantIdOnEveryWrittenRow()
    {
        var fixture = CreateFixture();

        _ = await fixture.Service.ProvisionTenantAdminAsync(fixture.Tenant, "owner", "owner@example.com", "hash");

        Assert.NotEmpty(fixture.WrittenTenantIds);
        Assert.All(fixture.WrittenTenantIds, written => Assert.Equal(TenantId, written.TenantId));
    }

    /// <summary>
    /// 套餐回收读写的是同一批授权绑定行，落库位置必须与开通期一致（平台态），否则库隔离下回收永远命中 0 行。
    /// </summary>
    [Fact]
    public async Task ReconcileTenantAuthorization_ShouldRunInPlatformContext()
    {
        var fixture = CreateFixture(ambientTenantId: 77);
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
        var inTenantContext = fixture.Observations
            .Where(observation => observation.TenantId is not null)
            .Select(observation => $"{observation.Operation}=>{observation.TenantId}")
            .ToList();
        Assert.Empty(inTenantContext);
    }

    /// <summary>
    /// 构造被测服务及其依赖替身，并在每个仓储调用点记录当时的租户上下文与写入行的 TenantId。
    /// </summary>
    /// <param name="ambientTenantId">调用方进入本服务时所处的租户上下文</param>
    private static ProvisionFixture CreateFixture(long? ambientTenantId = null)
    {
        var currentTenant = new TestCurrentTenant(ambientTenantId);
        var observations = new List<(string Operation, long? TenantId)>();
        var writtenTenantIds = new List<(string Operation, long TenantId)>();

        void Record(string operation) => observations.Add((operation, currentTenant.Id));
        void RecordWrite(string operation, long tenantId)
        {
            Record(operation);
            writtenTenantIds.Add((operation, tenantId));
        }

        var tenant = new TestTenant(TenantId)
        {
            TenantName = "库隔离租户",
            EditionId = EditionId,
            IsolationMode = TenantIsolationMode.Database
        };

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
            .ReturnsAsync((SysUser user, CancellationToken _) =>
            {
                RecordWrite("User.Add", user.TenantId);
                return new TestUser(101);
            });

        var userSecurityRepository = new Mock<IUserSecurityRepository>();
        _ = userSecurityRepository
            .Setup(repo => repo.AddAsync(It.IsAny<SysUserSecurity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysUserSecurity security, CancellationToken _) =>
            {
                RecordWrite("UserSecurity.Add", security.TenantId);
                return security;
            });

        var tenantUserRepository = new Mock<ITenantUserRepository>();
        _ = tenantUserRepository
            .Setup(repo => repo.AddAsync(It.IsAny<SysTenantUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysTenantUser member, CancellationToken _) =>
            {
                RecordWrite("TenantUser.Add", member.TenantId);
                return member;
            });

        var roleRepository = new Mock<IRoleRepository>();
        _ = roleRepository
            .Setup(repo => repo.AddAsync(It.IsAny<SysRole>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysRole role, CancellationToken _) =>
            {
                RecordWrite("Role.Add", role.TenantId);
                return new TestRole(201) { TenantId = role.TenantId };
            });

        var rolePermissionRepository = new Mock<IRolePermissionRepository>();
        _ = rolePermissionRepository
            .Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<SysRolePermission>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<SysRolePermission> grants, CancellationToken _) =>
            {
                var rows = grants.ToList();
                foreach (var grant in rows)
                {
                    RecordWrite("RolePermission.AddRange", grant.TenantId);
                }

                return rows;
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
                RecordWrite("UserRole.Add", userRole.TenantId);
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

        var service = new TenantProvisionDomainService(
            userRepository.Object,
            userSecurityRepository.Object,
            userRoleRepository.Object,
            tenantUserRepository.Object,
            new Mock<ITenantEditionRepository>().Object,
            new Mock<ITenantRepository>().Object,
            roleRepository.Object,
            rolePermissionRepository.Object,
            userPermissionRepository.Object,
            tenantEditionPermissionRepository.Object,
            currentTenant);

        return new ProvisionFixture(
            service,
            tenant,
            userRepository,
            rolePermissionRepository,
            observations,
            writtenTenantIds,
            Record);
    }

    /// <summary>
    /// 租户测试替身：主键 setter 对外不可见，经派生类构造赋值。
    /// </summary>
    private sealed class TestTenant : SysTenant
    {
        public TestTenant(long basicId)
        {
            BasicId = basicId;
        }
    }

    /// <summary>
    /// 用户测试替身。
    /// </summary>
    private sealed class TestUser : SysUser
    {
        public TestUser(long basicId)
        {
            BasicId = basicId;
        }
    }

    /// <summary>
    /// 角色测试替身。
    /// </summary>
    private sealed class TestRole : SysRole
    {
        public TestRole(long basicId)
        {
            BasicId = basicId;
        }
    }

    /// <summary>
    /// 开通测试依赖集合。
    /// </summary>
    private sealed record ProvisionFixture(
        TenantProvisionDomainService Service,
        SysTenant Tenant,
        Mock<IUserRepository> UserRepository,
        Mock<IRolePermissionRepository> RolePermissionRepository,
        List<(string Operation, long? TenantId)> Observations,
        List<(string Operation, long TenantId)> WrittenTenantIds,
        Action<string> Record);
}
