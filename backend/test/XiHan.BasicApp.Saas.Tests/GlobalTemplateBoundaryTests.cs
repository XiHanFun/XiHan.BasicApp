// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq.Expressions;
using Moq;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Core.Exceptions;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 全局模板的边界：全局行（TenantId=0）租户看得见、改不了；租户可以建自己的行；
/// 可覆盖型（参数配置）只在本上下文内判重；不可覆盖型（全局角色、字典）在租户里只读；
/// 平台维护全局行时要看所有租户的引用与占用。
/// </summary>
public sealed class GlobalTemplateBoundaryTests
{
    private const long TenantId = 7;

    #region 全局角色

    /// <summary>
    /// 租户不能给全局角色叠加授权；平台可以维护全局角色的授权。
    /// </summary>
    [Fact]
    public async Task RolePermissions_OnGlobalRole_OnlyInPlatform()
    {
        var tenant = new RoleFixture(roleTenantId: 0, contextTenantId: TenantId);
        var platform = new RoleFixture(roleTenantId: 0, contextTenantId: null);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => tenant.GrantAsync(100));
        _ = await platform.GrantAsync(100);

        Assert.Contains("平台", exception.Message, StringComparison.Ordinal);
        tenant.RolePermissions.Verify(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<SysRolePermission>>(), It.IsAny<CancellationToken>()), Times.Never);
        platform.RolePermissions.Verify(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<SysRolePermission>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 平台删除全局角色：租户成员持有它（平台看不见的行）也要挡住。
    /// </summary>
    [Fact]
    public async Task DeleteGlobalRole_AssignedInTenants_IsRejected()
    {
        var fixture = new RoleFixture(roleTenantId: 0, contextTenantId: null);
        _ = fixture.UserRoles
            .Setup(repo => repo.AnyIgnoreTenantAsync(It.IsAny<Expression<Func<SysUserRole, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.DeleteRoleAsync(RoleFixture.RoleId));

        Assert.Contains("租户成员", exception.Message, StringComparison.Ordinal);
        fixture.Roles.Verify(repo => repo.DeleteAsync(It.IsAny<SysRole>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 系统角色的编码保留给系统流程：手工建同码角色会被当成超管或所有者角色来理解
    /// </summary>
    [Theory]
    [InlineData("super_admin")]
    [InlineData("TENANT_OWNER")]
    public async Task CreateRole_WithReservedCode_IsRejected(string roleCode)
    {
        var fixture = new RoleFixture(roleTenantId: 7, contextTenantId: 7);

        _ = await Assert.ThrowsAsync<UserFriendlyException>(() => fixture.Service.CreateRoleAsync(
            new RoleCreateCommand(roleCode, "角色", null, RoleType.Custom, 0, EnableStatus.Enabled, 0, null)));

        fixture.Roles.Verify(repo => repo.AddAsync(It.IsAny<SysRole>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region 参数配置

    /// <summary>
    /// 租户可以建与全局同键的配置来覆盖它；同一上下文内同键仍然拒绝。
    /// </summary>
    [Fact]
    public async Task Config_SameKeyAsGlobal_CanBeOverriddenInTenant()
    {
        var fixture = new ConfigFixture(TenantId);
        fixture.AddConfig(1, tenantId: 0, "site.title");
        fixture.AddConfig(2, tenantId: TenantId, "site.logo");

        _ = await fixture.Service.CreateConfigAsync(ConfigFixture.Create("site.title"));
        var duplicate = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.CreateConfigAsync(ConfigFixture.Create("site.logo")));

        Assert.Contains("已存在", duplicate.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 全局配置在租户里只读：改、删、启停都拒绝。
    /// </summary>
    [Fact]
    public async Task Config_GlobalRow_IsReadOnlyInTenant()
    {
        var fixture = new ConfigFixture(TenantId);
        fixture.AddConfig(1, tenantId: 0, "site.title");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.DeleteConfigAsync(1));

        Assert.Contains("全局配置只能在平台维护", exception.Message, StringComparison.Ordinal);
        fixture.Configs.Verify(repo => repo.DeleteAsync(It.IsAny<SysConfig>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region 字典

    /// <summary>
    /// 字典项与所属字典同在一个上下文：租户不能往全局字典里加项。
    /// </summary>
    [Fact]
    public async Task DictItem_UnderGlobalDict_IsRejectedInTenant()
    {
        var fixture = new DictFixture(TenantId);
        fixture.AddDict(1, tenantId: 0, "gender");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.CreateDictItemAsync(DictFixture.CreateItem(1, "other")));

        Assert.Contains("全局字典只能在平台维护", exception.Message, StringComparison.Ordinal);
        fixture.Items.Verify(repo => repo.AddAsync(It.IsAny<SysDictItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 平台建全局字典：某个租户已占用这个编码时拒绝，否则会在那个租户里重名。
    /// </summary>
    [Fact]
    public async Task GlobalDict_CodeTakenByTenant_IsRejectedInPlatform()
    {
        var fixture = new DictFixture(contextTenantId: null);
        _ = fixture.Dicts
            .Setup(repo => repo.AnyIgnoreTenantAsync(It.IsAny<Expression<Func<SysDict, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _ = await Assert.ThrowsAnyAsync<Exception>(() => fixture.Service.CreateDictAsync(DictFixture.CreateDict("order_status")));

        fixture.Dicts.Verify(repo => repo.AddAsync(It.IsAny<SysDict>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    private sealed class RoleFixture
    {
        public const long RoleId = 5;

        public RoleFixture(long roleTenantId, long? contextTenantId)
        {
            var role = new SysRole { TenantId = roleTenantId, RoleCode = "ROLE", RoleName = "角色", RoleType = RoleType.Business, Status = EnableStatus.Enabled };
            SaasTestHelper.SetBasicId(role, RoleId);
            var permission = new SysPermission { TenantId = 0, PermissionCode = "demo:read", Status = EnableStatus.Enabled, Side = PermissionSide.Both };
            SaasTestHelper.SetBasicId(permission, 100);

            _ = Roles.Setup(repo => repo.GetByIdAsync(RoleId, It.IsAny<CancellationToken>())).ReturnsAsync(role);
            var permissions = new Mock<IPermissionRepository>();
            _ = permissions.Setup(repo => repo.GetByIdAsync(100, It.IsAny<CancellationToken>())).ReturnsAsync(permission);
            _ = permissions
                .Setup(repo => repo.GetListAsync(It.IsAny<Expression<Func<SysPermission, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([permission]);
            _ = RolePermissions
                .Setup(repo => repo.GetListAsync(It.IsAny<Expression<Func<SysRolePermission, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            _ = RolePermissions
                .Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<SysRolePermission>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IEnumerable<SysRolePermission> rows, CancellationToken _) => rows.ToArray());

            Service = new RoleDomainService(
                Roles.Object,
                UserRoles.Object,
                RolePermissions.Object,
                new Mock<IRoleHierarchyRepository>().Object,
                new Mock<IRoleDataScopeRepository>().Object,
                permissions.Object,
                new Mock<IDepartmentRepository>().Object,
                new TestCurrentTenant(contextTenantId));
        }

        public RoleDomainService Service { get; }

        public Mock<IRoleRepository> Roles { get; } = new();

        public Mock<IUserRoleRepository> UserRoles { get; } = new();

        public Mock<IRolePermissionRepository> RolePermissions { get; } = new();

        public Task<RolePermissionBatchUpdateResult> GrantAsync(long permissionId) =>
            Service.BatchUpdateRolePermissionsAsync(new RolePermissionBatchUpdateCommand(RoleId, [permissionId], []));
    }

    private sealed class ConfigFixture
    {
        private readonly List<SysConfig> _rows = [];

        public ConfigFixture(long? contextTenantId)
        {
            _ = Configs
                .Setup(repo => repo.AnyAsync(It.IsAny<Expression<Func<SysConfig, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<SysConfig, bool>> predicate, CancellationToken _) => _rows.Any(predicate.Compile()));
            _ = Configs
                .Setup(repo => repo.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((long id, CancellationToken _) => _rows.Find(row => row.BasicId == id));
            _ = Configs
                .Setup(repo => repo.AddAsync(It.IsAny<SysConfig>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SysConfig row, CancellationToken _) => row);

            Service = new ConfigDomainService(Configs.Object, new Mock<IConfigValueSecretProtector>().Object, new TestCurrentTenant(contextTenantId));
        }

        public ConfigDomainService Service { get; }

        public Mock<IConfigRepository> Configs { get; } = new();

        public static ConfigCreateCommand Create(string key) =>
            new(key, null, key, "v", null, ConfigType.Feature, ConfigDataType.String, null, false, EnableStatus.Enabled, 0, null);

        public void AddConfig(long id, long tenantId, string key)
        {
            var row = new SysConfig { TenantId = tenantId, ConfigKey = key, ConfigName = key };
            SaasTestHelper.SetBasicId(row, id);
            _rows.Add(row);
        }
    }

    private sealed class DictFixture
    {
        private readonly List<SysDict> _dicts = [];

        public DictFixture(long? contextTenantId)
        {
            _ = Dicts
                .Setup(repo => repo.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((long id, CancellationToken _) => _dicts.Find(dict => dict.BasicId == id));
            _ = Dicts
                .Setup(repo => repo.AnyAsync(It.IsAny<Expression<Func<SysDict, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            Service = new DictDomainService(Dicts.Object, Items.Object, new TestCurrentTenant(contextTenantId));
        }

        public DictDomainService Service { get; }

        public Mock<IDictRepository> Dicts { get; } = new();

        public Mock<IDictItemRepository> Items { get; } = new();

        public static DictCreateCommand CreateDict(string code) => new(code, code, "business", null, EnableStatus.Enabled, 0, null);

        public static DictItemCreateCommand CreateItem(long dictId, string code) =>
            new(dictId, null, code, code, code, null, null, false, EnableStatus.Enabled, 0, null);

        public void AddDict(long id, long tenantId, string code)
        {
            var dict = new SysDict { TenantId = tenantId, DictCode = code, DictName = code, DictType = "business", Status = EnableStatus.Enabled };
            SaasTestHelper.SetBasicId(dict, id);
            _dicts.Add(dict);
        }
    }
}
