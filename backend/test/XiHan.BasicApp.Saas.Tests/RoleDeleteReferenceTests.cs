// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq.Expressions;
using Moq;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 删除角色的引用检查：只有有效的分配、授权、数据范围才算引用。
/// </summary>
/// <remarks>
/// 回归锚点：撤销是把行置为失效而不删行，检查曾把失效的历史行也算作引用，
/// 角色只要有过成员或授权，移出、收回之后仍提示「已分配给用户」，界面上永远删不掉。
/// </remarks>
public sealed class RoleDeleteReferenceTests
{
    private const long TenantId = 7;
    private const long RoleId = 5;

    private readonly List<SysUserRole> _userRoles = [];
    private readonly List<SysRolePermission> _rolePermissions = [];
    private readonly List<SysRoleDataScope> _dataScopes = [];
    private readonly List<SysFieldLevelSecurity> _fieldSecurities = [];
    private readonly Mock<IRoleRepository> _roles = new();

    /// <summary>
    /// 只剩失效的历史行时可以删除。
    /// </summary>
    [Fact]
    public async Task DeleteRole_WithOnlyRevokedBindings_ShouldDelete()
    {
        _userRoles.Add(new SysUserRole { TenantId = TenantId, RoleId = RoleId, UserId = 1, Status = ValidityStatus.Invalid });
        _rolePermissions.Add(new SysRolePermission { TenantId = TenantId, RoleId = RoleId, PermissionId = 100, Status = ValidityStatus.Invalid });
        _dataScopes.Add(new SysRoleDataScope { TenantId = TenantId, RoleId = RoleId, DepartmentId = 9, Status = ValidityStatus.Invalid });

        await CreateService().DeleteRoleAsync(RoleId);

        _roles.Verify(repo => repo.DeleteAsync(It.Is<SysRole>(role => role.BasicId == RoleId), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 仍有有效的分配、授权或数据范围时拒绝删除。
    /// </summary>
    /// <param name="binding">有效行的种类。</param>
    /// <param name="expectedMessage">预期提示。</param>
    [Theory]
    [InlineData("user", "已分配给用户")]
    [InlineData("permission", "已绑定权限")]
    [InlineData("dataScope", "已配置数据范围")]
    [InlineData("fieldSecurity", "已配置字段安全规则")]
    public async Task DeleteRole_WithValidBinding_IsRejected(string binding, string expectedMessage)
    {
        switch (binding)
        {
            case "user":
                _userRoles.Add(new SysUserRole { TenantId = TenantId, RoleId = RoleId, UserId = 1, Status = ValidityStatus.Valid });
                break;
            case "permission":
                _rolePermissions.Add(new SysRolePermission { TenantId = TenantId, RoleId = RoleId, PermissionId = 100, Status = ValidityStatus.Valid });
                break;
            case "fieldSecurity":
                _fieldSecurities.Add(new SysFieldLevelSecurity { TenantId = TenantId, TargetType = FieldSecurityTargetType.Role, TargetId = RoleId, EntityName = nameof(SysUser), FieldName = nameof(SysUser.Phone), MaskStrategy = FieldMaskStrategy.Hidden });
                break;
            default:
                _dataScopes.Add(new SysRoleDataScope { TenantId = TenantId, RoleId = RoleId, DepartmentId = 9, Status = ValidityStatus.Valid });
                break;
        }

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService().DeleteRoleAsync(RoleId));

        Assert.Contains(expectedMessage, exception.Message, StringComparison.Ordinal);
        _roles.Verify(repo => repo.DeleteAsync(It.IsAny<SysRole>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private RoleDomainService CreateService()
    {
        var role = new SysRole { TenantId = TenantId, RoleCode = "custom", RoleName = "自定义", RoleType = RoleType.Custom, Status = EnableStatus.Enabled };
        SaasTestHelper.SetBasicId(role, RoleId);
        _ = _roles.Setup(repo => repo.GetByIdAsync(RoleId, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        _ = _roles.Setup(repo => repo.DeleteAsync(It.IsAny<SysRole>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var userRoles = new Mock<IUserRoleRepository>();
        _ = userRoles
            .Setup(repo => repo.AnyAsync(It.IsAny<Expression<Func<SysUserRole, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<SysUserRole, bool>> predicate, CancellationToken _) => _userRoles.Any(predicate.Compile()));
        var rolePermissions = new Mock<IRolePermissionRepository>();
        _ = rolePermissions
            .Setup(repo => repo.AnyAsync(It.IsAny<Expression<Func<SysRolePermission, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<SysRolePermission, bool>> predicate, CancellationToken _) => _rolePermissions.Any(predicate.Compile()));
        var dataScopes = new Mock<IRoleDataScopeRepository>();
        _ = dataScopes
            .Setup(repo => repo.AnyAsync(It.IsAny<Expression<Func<SysRoleDataScope, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<SysRoleDataScope, bool>> predicate, CancellationToken _) => _dataScopes.Any(predicate.Compile()));

        var fieldSecurities = new Mock<IFieldLevelSecurityRepository>();
        _ = fieldSecurities
            .Setup(repo => repo.AnyAsync(It.IsAny<Expression<Func<SysFieldLevelSecurity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<SysFieldLevelSecurity, bool>> predicate, CancellationToken _) => _fieldSecurities.Any(predicate.Compile()));

        return new RoleDomainService(
            _roles.Object,
            userRoles.Object,
            rolePermissions.Object,
            new Mock<IRoleHierarchyRepository>().Object,
            dataScopes.Object,
            new Mock<IPermissionRepository>().Object,
            new Mock<IDepartmentRepository>().Object,
            fieldSecurities.Object,
            new TestCurrentTenant(TenantId));
    }
}
