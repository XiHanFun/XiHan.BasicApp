// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq.Expressions;
using Moq;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Security.Users;
using XiHan.BasicApp.Saas.Domain.Permissions;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 字段级安全服务端测试：用户/角色规则的 deny-overrides 合并与最严脱敏选择。
/// </summary>
public sealed class FieldSecurityServiceTests
{
    /// <summary>
    /// 未登录（无用户主键）时返回空规则集。
    /// </summary>
    [Fact]
    public async Task Resolve_WithoutLogin_ShouldReturnEmpty()
    {
        var fixture = CreateFixture(userId: null);

        var rules = await fixture.Service.ResolveAsync(SaasPermissionCodes.User.Group);

        Assert.Empty(rules);
        fixture.ResourceRepository.Verify(
            repo => repo.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 资源编码为空时返回空规则集。
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Resolve_WithBlankResourceCode_ShouldReturnEmpty(string? resourceCode)
    {
        var fixture = CreateFixture(userId: 7);

        var rules = await fixture.Service.ResolveAsync(resourceCode!);

        Assert.Empty(rules);
    }

    /// <summary>
    /// 资源不存在时返回空规则集。
    /// </summary>
    [Fact]
    public async Task Resolve_WhenResourceMissing_ShouldReturnEmpty()
    {
        var fixture = CreateFixture(userId: 7);

        var rules = await fixture.Service.ResolveAsync(SaasPermissionCodes.User.Group);

        Assert.Empty(rules);
    }

    /// <summary>
    /// 用户规则与角色规则同字段合并时 deny-overrides：任一不可读/不可编辑即不可读/不可编辑。
    /// </summary>
    [Fact]
    public async Task Resolve_ShouldMergeUserAndRoleRulesWithDenyOverrides()
    {
        var fixture = CreateFixture(userId: 7);
        fixture.SetupResource(1);
        fixture.SetupUserRoles(7, [11]);
        fixture.SetupRules(
        [
            Rule(FieldSecurityTargetType.Role, 11, "Phone", isReadable: false, isEditable: false, FieldMaskStrategy.PartialMask),
            Rule(FieldSecurityTargetType.User, 7, "Phone", isReadable: true, isEditable: true, FieldMaskStrategy.None)
        ]);

        var rules = await fixture.Service.ResolveAsync(SaasPermissionCodes.User.Group);

        var phone = Assert.Single(rules);
        Assert.False(phone.Value.IsReadable);
        Assert.False(phone.Value.IsEditable);
    }

    /// <summary>
    /// 列表脱敏按元素逐个改写：传 List 必须每一项都打上码。
    /// </summary>
    /// <remarks>
    /// 回归锚点：集合版曾与单对象版同名，传 List 时单对象版以 T = List 胜出，只反射到 List 本身，
    /// 用户列表的用户名规则建了也原样返回明文。
    /// </remarks>
    [Fact]
    public async Task ApplyMany_WithList_ShouldMaskEveryItem()
    {
        var fixture = CreateFixture(userId: 7);
        fixture.SetupResource(1);
        fixture.SetupUserRoles(7, []);
        fixture.SetupRules([Rule(FieldSecurityTargetType.User, 7, nameof(MaskTarget.UserName), maskStrategy: FieldMaskStrategy.FullMask)]);
        var items = new List<MaskTarget> { new() { UserName = "owner" }, new() { UserName = "admin" } };

        await fixture.Service.ApplyManyAsync(SaasPermissionCodes.User.Group, items);

        Assert.Equal(["*****", "*****"], items.Select(item => item.UserName));
    }

    /// <summary>
    /// 单对象版收到集合当场报错，不再静默放过明文。
    /// </summary>
    [Fact]
    public async Task Apply_WithCollection_ShouldThrow()
    {
        var fixture = CreateFixture(userId: 7);
        var items = new List<MaskTarget> { new() { UserName = "owner" } };

        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Service.ApplyAsync(SaasPermissionCodes.User.Group, items));
        Assert.Equal("owner", items[0].UserName);
    }

    /// <summary>
    /// 单个对象照常按规则脱敏。
    /// </summary>
    [Fact]
    public async Task Apply_WithSingleItem_ShouldMaskIt()
    {
        var fixture = CreateFixture(userId: 7);
        fixture.SetupResource(1);
        fixture.SetupUserRoles(7, []);
        fixture.SetupRules([Rule(FieldSecurityTargetType.User, 7, nameof(MaskTarget.UserName), maskStrategy: FieldMaskStrategy.FullMask)]);
        var item = new MaskTarget { UserName = "owner" };

        await fixture.Service.ApplyAsync(SaasPermissionCodes.User.Group, item);

        Assert.Equal("*****", item.UserName);
    }

    /// <summary>
    /// 脱敏策略取最严者（完全脱敏优先于部分脱敏）。
    /// </summary>
    [Fact]
    public async Task Resolve_ShouldPickStrongestMask()
    {
        var fixture = CreateFixture(userId: 7);
        fixture.SetupResource(1);
        fixture.SetupUserRoles(7, [11]);
        fixture.SetupRules(
        [
            Rule(FieldSecurityTargetType.Role, 11, "Phone", maskStrategy: FieldMaskStrategy.FullMask),
            Rule(FieldSecurityTargetType.User, 7, "Phone", maskStrategy: FieldMaskStrategy.PartialMask)
        ]);

        var rules = await fixture.Service.ResolveAsync(SaasPermissionCodes.User.Group);

        var phone = Assert.Single(rules);
        Assert.Equal(FieldMaskStrategy.FullMask, phone.Value.MaskStrategy);
    }

    /// <summary>
    /// 面向其他用户的用户级规则不得生效。
    /// </summary>
    [Fact]
    public async Task Resolve_ShouldIgnoreRulesForOtherUsers()
    {
        var fixture = CreateFixture(userId: 7);
        fixture.SetupResource(1);
        fixture.SetupUserRoles(7, [11]);
        fixture.SetupRules([Rule(FieldSecurityTargetType.User, 99, "Phone", maskStrategy: FieldMaskStrategy.FullMask)]);

        var rules = await fixture.Service.ResolveAsync(SaasPermissionCodes.User.Group);

        Assert.Empty(rules);
    }

    /// <summary>
    /// 服务端仅应用用户/角色目标规则，权限/部门目标规则不参与（另行处理）。
    /// </summary>
    [Fact]
    public async Task Resolve_ShouldIgnoreUnsupportedTargetTypes()
    {
        var fixture = CreateFixture(userId: 7);
        fixture.SetupResource(1);
        fixture.SetupUserRoles(7, [11]);
        fixture.SetupRules(
        [
            Rule(FieldSecurityTargetType.Permission, 201, "Phone", maskStrategy: FieldMaskStrategy.FullMask),
            Rule(FieldSecurityTargetType.Department, 5, "Phone", maskStrategy: FieldMaskStrategy.FullMask)
        ]);

        var rules = await fixture.Service.ResolveAsync(SaasPermissionCodes.User.Group);

        Assert.Empty(rules);
    }

    /// <summary>
    /// 角色规则仅匹配用户当前有效角色。
    /// </summary>
    [Fact]
    public async Task Resolve_ShouldOnlyApplyRulesOfUserRoles()
    {
        var fixture = CreateFixture(userId: 7);
        fixture.SetupResource(1);
        fixture.SetupUserRoles(7, [11]);
        fixture.SetupRules(
        [
            Rule(FieldSecurityTargetType.Role, 11, "Phone", maskStrategy: FieldMaskStrategy.FullMask),
            Rule(FieldSecurityTargetType.Role, 12, "Phone", maskStrategy: FieldMaskStrategy.PartialMask)
        ]);

        var rules = await fixture.Service.ResolveAsync(SaasPermissionCodes.User.Group);

        var phone = Assert.Single(rules);
        Assert.Equal(FieldMaskStrategy.FullMask, phone.Value.MaskStrategy);
    }

    /// <summary>
    /// 不同字段规则分别产出条目。
    /// </summary>
    [Fact]
    public async Task Resolve_ShouldGroupByFieldName()
    {
        var fixture = CreateFixture(userId: 7);
        fixture.SetupResource(1);
        fixture.SetupUserRoles(7, [11]);
        fixture.SetupRules(
        [
            Rule(FieldSecurityTargetType.Role, 11, "Phone", maskStrategy: FieldMaskStrategy.FullMask),
            Rule(FieldSecurityTargetType.Role, 11, "Email", maskStrategy: FieldMaskStrategy.PartialMask)
        ]);

        var rules = await fixture.Service.ResolveAsync(SaasPermissionCodes.User.Group);

        Assert.Equal(2, rules.Count);
        Assert.True(rules.ContainsKey("Phone"));
        Assert.True(rules.ContainsKey("Email"));
    }

    /// <summary>
    /// 构造 FLS 规则。
    /// </summary>
    private static SysFieldLevelSecurity Rule(
        FieldSecurityTargetType targetType,
        long targetId,
        string fieldName,
        bool isReadable = true,
        bool isEditable = true,
        FieldMaskStrategy maskStrategy = FieldMaskStrategy.None)
    {
        return new SysFieldLevelSecurity
        {
            TargetType = targetType,
            TargetId = targetId,
            ResourceId = 1,
            FieldName = fieldName,
            IsReadable = isReadable,
            IsEditable = isEditable,
            MaskStrategy = maskStrategy,
            Status = EnableStatus.Enabled
        };
    }

    /// <summary>
    /// 脱敏测试用的返回对象：属性名与字段规则的字段名对应。
    /// </summary>
    private sealed class MaskTarget
    {
        public string? UserName { get; set; }
    }

    /// <summary>
    /// 创建带仓储模拟的字段安全服务夹具。
    /// </summary>
    private static ServiceFixture CreateFixture(long? userId)
    {
        var fieldLevelSecurityRepository = new Mock<IFieldLevelSecurityRepository>();
        var resourceRepository = new Mock<IResourceRepository>();
        var userRoleRepository = new Mock<IUserRoleRepository>();
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(user => user.UserId).Returns(userId);

        var service = new FieldSecurityService(
            fieldLevelSecurityRepository.Object,
            resourceRepository.Object,
            userRoleRepository.Object,
            currentUser.Object);
        return new ServiceFixture(service, fieldLevelSecurityRepository, resourceRepository, userRoleRepository);
    }

    /// <summary>
    /// 字段安全服务测试依赖集合。
    /// </summary>
    private sealed record ServiceFixture(
        FieldSecurityService Service,
        Mock<IFieldLevelSecurityRepository> FieldLevelSecurityRepository,
        Mock<IResourceRepository> ResourceRepository,
        Mock<IUserRoleRepository> UserRoleRepository)
    {
        /// <summary>
        /// 预设资源。
        /// </summary>
        public void SetupResource(long resourceId)
        {
            var resource = new SysResource
            {
                TenantId = 7,
                ResourceCode = SaasPermissionCodes.User.Group,
                ResourceName = "用户",
                Status = EnableStatus.Enabled
            };
            SaasTestHelper.SetBasicId(resource, resourceId);
            ResourceRepository
                .Setup(repo => repo.GetByCodeAsync(SaasPermissionCodes.User.Group, It.IsAny<CancellationToken>()))
                .ReturnsAsync(resource);
        }

        /// <summary>
        /// 预设用户有效角色。
        /// </summary>
        public void SetupUserRoles(long userId, IEnumerable<long> roleIds)
        {
            var userRoles = roleIds
                .Select(roleId => new SysUserRole { UserId = userId, RoleId = roleId })
                .ToArray();
            UserRoleRepository
                .Setup(repo => repo.GetValidByUserIdAsync(userId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(userRoles);
        }

        /// <summary>
        /// 预设仓储返回的 FLS 规则集合。
        /// </summary>
        public void SetupRules(IReadOnlyList<SysFieldLevelSecurity> rules)
        {
            FieldLevelSecurityRepository
                .Setup(repo => repo.GetListAsync(
                    It.IsAny<Expression<Func<SysFieldLevelSecurity, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(rules);
        }
    }
}
