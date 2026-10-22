// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using XiHan.BasicApp.Saas.Application.Conditions;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 权限条件的写路径校验：属性要登记、运算符要适用、值要能解析、值类型与属性相符
/// </summary>
public sealed class PermissionConditionValidatorTests
{
    private const long RolePermissionId = 10;
    private const long UserPermissionId = 20;
    private const long RegisteredPermissionId = 5;
    private const long PlainPermissionId = 6;

    private readonly Mock<IRolePermissionRepository> _rolePermissions = new();
    private readonly Mock<IUserPermissionRepository> _userPermissions = new();
    private readonly Mock<IPermissionRepository> _permissions = new();

    /// <summary>
    /// 构造测试上下文：角色权限绑在登记了条件资源的权限上，用户直授绑在没登记的权限上
    /// </summary>
    public PermissionConditionValidatorTests()
    {
        _rolePermissions
            .Setup(repository => repository.GetByIdAsync(RolePermissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SysRolePermission { PermissionId = RegisteredPermissionId });
        _userPermissions
            .Setup(repository => repository.GetByIdAsync(UserPermissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SysUserPermission { PermissionId = PlainPermissionId });
        _permissions
            .Setup(repository => repository.GetByIdAsync(RegisteredPermissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SysPermission { PermissionCode = "saas:position:update" });
        _permissions
            .Setup(repository => repository.GetByIdAsync(PlainPermissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SysPermission { PermissionCode = "saas:position:read" });
    }

    /// <summary>
    /// 能正常求值的条件放行：旧名别名、列表、跨零点时段、布尔、登记过的数据行属性
    /// </summary>
    [Theory]
    [InlineData("subject.role", ConditionOperator.In, ConfigDataType.String, "admin,auditor")]
    [InlineData("environment.ip", ConditionOperator.In, ConfigDataType.Array, "[\"10.0.0.0/8\",\"192.168.1.0/24\"]")]
    [InlineData("environment.time", ConditionOperator.Between, ConfigDataType.String, "22:00,06:00")]
    [InlineData("environment.hour", ConditionOperator.Between, ConfigDataType.Json, "{\"min\":9,\"max\":18}")]
    [InlineData("subject.isImpersonating", ConditionOperator.Equals, ConfigDataType.Boolean, "true")]
    [InlineData("resource.status", ConditionOperator.Equals, ConfigDataType.String, "Enabled")]
    public async Task Accepts_EvaluableConditions(string attribute, ConditionOperator @operator, ConfigDataType valueType, string value)
    {
        await CreateValidator().ValidateAsync(RolePermissionId, null, attribute, @operator, false, valueType, value);
    }

    /// <summary>
    /// 不能正常求值的条件拒绝保存：未登记属性、数据行属性没登记、IP 写错、运算符不适用、值类型不符
    /// </summary>
    [Theory]
    [InlineData(RolePermissionId, null, "subject.unknown", ConditionOperator.Equals, ConfigDataType.String, "x")]
    [InlineData(null, UserPermissionId, "resource.status", ConditionOperator.Equals, ConfigDataType.String, "Enabled")]
    [InlineData(RolePermissionId, null, "environment.ip", ConditionOperator.In, ConfigDataType.String, "10.0.0.1/8")]
    [InlineData(RolePermissionId, null, "environment.ip", ConditionOperator.StartsWith, ConfigDataType.String, "192.168.")]
    [InlineData(RolePermissionId, null, "subject.roleCodes", ConditionOperator.GreaterThan, ConfigDataType.String, "a")]
    [InlineData(RolePermissionId, null, "environment.ip", ConditionOperator.Equals, ConfigDataType.Number, "10")]
    [InlineData(RolePermissionId, null, "environment.hour", ConditionOperator.Equals, ConfigDataType.Boolean, "true")]
    public async Task Rejects_UnevaluableConditions(long? rolePermissionId, long? userPermissionId, string attribute, ConditionOperator @operator, ConfigDataType valueType, string value)
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateValidator().ValidateAsync(rolePermissionId, userPermissionId, attribute, @operator, false, valueType, value));
    }

    private PermissionConditionValidator CreateValidator()
    {
        var resources = PermissionConditionTestKit.Resources(
            PermissionResourceRegistration.Create<SysPosition>("saas:position:update", PermissionResourceKeys.FromArgument("id")));
        return new PermissionConditionValidator(
            PermissionConditionTestKit.Compiler(resources),
            _rolePermissions.Object,
            _userPermissions.Object,
            _permissions.Object);
    }
}
