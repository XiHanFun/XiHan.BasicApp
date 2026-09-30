// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq.Expressions;
using Microsoft.Extensions.Options;
using Moq;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 字段级安全领域服务测试：规则写入时的实体/字段校验、读取方式与参数的一致性、目标可用性与平台规则归属。
/// </summary>
public sealed class FieldLevelSecurityDomainServiceTests
{
    /// <summary>
    /// 合法规则写入并规范化：参数只随对应读取方式保留。
    /// </summary>
    [Fact]
    public async Task Create_WithValidInput_ShouldPersistNormalizedPolicy()
    {
        var fixture = new Fixture();
        var command = CreateCommand(maskStrategy: FieldMaskStrategy.PartialMask, keepHead: 3, keepTail: 4, replacement: "忽略");

        var result = await fixture.Service.CreateAsync(command);

        Assert.Equal(nameof(SysUser), result.Policy.EntityName);
        Assert.Equal(nameof(SysUser.Phone), result.Policy.FieldName);
        Assert.Equal(3, result.Policy.MaskKeepHead);
        Assert.Equal(4, result.Policy.MaskKeepTail);
        Assert.Null(result.Policy.MaskReplacement);
        Assert.False(result.Policy.IsEditable);
        Assert.Equal("ROLE-10", result.TargetCode);
    }

    /// <summary>
    /// 实体不在目录里（未登记或拼错）直接拒绝。
    /// </summary>
    [Fact]
    public async Task Create_WithUnknownEntity_ShouldThrow()
    {
        var fixture = new Fixture();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreateAsync(CreateCommand(entityName: "SysRole")));

        Assert.Contains("不支持字段安全", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 字段不在实体里直接拒绝（字段名区分大小写）。
    /// </summary>
    [Theory]
    [InlineData("phone")]
    [InlineData("Salary")]
    [InlineData("TenantId")]
    public async Task Create_WithUnknownField_ShouldThrow(string fieldName)
    {
        var fixture = new Fixture();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreateAsync(CreateCommand(fieldName: fieldName)));

        Assert.Contains("没有字段", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 非文本字段只能明文只读或隐藏。
    /// </summary>
    [Fact]
    public async Task Create_TextStrategyOnNonTextField_ShouldThrow()
    {
        var fixture = new Fixture();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreateAsync(CreateCommand(fieldName: nameof(SysUser.Birthday), maskStrategy: FieldMaskStrategy.FullMask)));

        Assert.Contains("不是文本字段", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 非文本字段可以隐藏。
    /// </summary>
    [Fact]
    public async Task Create_HiddenOnNonTextField_ShouldPass()
    {
        var fixture = new Fixture();

        var result = await fixture.Service.CreateAsync(CreateCommand(fieldName: nameof(SysUser.Birthday), maskStrategy: FieldMaskStrategy.Hidden));

        Assert.Equal(FieldMaskStrategy.Hidden, result.Policy.MaskStrategy);
    }

    /// <summary>
    /// 明文且可编辑的规则什么都没限制。
    /// </summary>
    [Fact]
    public async Task Create_PlainAndEditable_ShouldThrow()
    {
        var fixture = new Fixture();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreateAsync(CreateCommand(maskStrategy: FieldMaskStrategy.None, isEditable: true)));

        Assert.Contains("什么都没限制", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 脱敏且可编辑是合法的「只写」规则（如只能换新密钥、看不到旧的）。
    /// </summary>
    [Fact]
    public async Task Create_MaskedAndEditable_ShouldBeAcceptedAsWriteOnly()
    {
        var fixture = new Fixture();

        var result = await fixture.Service.CreateAsync(CreateCommand(maskStrategy: FieldMaskStrategy.Hidden, isEditable: true));

        Assert.True(result.Policy.IsEditable);
        Assert.Equal(FieldMaskStrategy.Hidden, result.Policy.MaskStrategy);
    }

    /// <summary>
    /// 部分脱敏必须给出保留位数，且在范围内、不能两端都不保留。
    /// </summary>
    [Theory]
    [InlineData(null, 4)]
    [InlineData(3, null)]
    [InlineData(-1, 4)]
    [InlineData(3, 33)]
    [InlineData(0, 0)]
    public async Task Create_PartialMaskWithInvalidKeep_ShouldThrow(int? keepHead, int? keepTail)
    {
        var fixture = new Fixture();

        _ = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreateAsync(CreateCommand(maskStrategy: FieldMaskStrategy.PartialMask, keepHead: keepHead, keepTail: keepTail)));
    }

    /// <summary>
    /// 固定文本方式必须填写文字，且不超长。
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task Create_RedactWithoutReplacement_ShouldThrow(string? replacement)
    {
        var fixture = new Fixture();

        _ = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreateAsync(CreateCommand(maskStrategy: FieldMaskStrategy.Redact, replacement: replacement)));
    }

    /// <summary>
    /// 固定文本去掉首尾空白后保存，保留位数清空。
    /// </summary>
    [Fact]
    public async Task Create_Redact_ShouldTrimReplacementAndDropKeep()
    {
        var fixture = new Fixture();

        var result = await fixture.Service.CreateAsync(CreateCommand(maskStrategy: FieldMaskStrategy.Redact, keepHead: 3, keepTail: 4, replacement: "  [保密]  "));

        Assert.Equal("[保密]", result.Policy.MaskReplacement);
        Assert.Null(result.Policy.MaskKeepHead);
        Assert.Null(result.Policy.MaskKeepTail);
    }

    /// <summary>
    /// 目标主键必须大于 0。
    /// </summary>
    [Fact]
    public async Task Create_WithInvalidTargetId_ShouldThrow()
    {
        var fixture = new Fixture();

        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => fixture.Service.CreateAsync(CreateCommand(targetId: 0)));
    }

    /// <summary>
    /// 已删除的「权限」目标类型不再接受。
    /// </summary>
    [Fact]
    public async Task Create_WithRemovedTargetType_ShouldThrow()
    {
        var fixture = new Fixture();

        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Service.CreateAsync(CreateCommand() with { TargetType = (FieldSecurityTargetType)2 }));
    }

    /// <summary>
    /// 停用角色不能配置规则。
    /// </summary>
    [Fact]
    public async Task Create_WithDisabledRole_ShouldThrow()
    {
        var fixture = new Fixture();
        fixture.Roles.Setup(repo => repo.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(CreateRole(10, EnableStatus.Disabled));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.CreateAsync(CreateCommand()));

        Assert.Contains("停用角色", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 部门目标：停用部门不能配置规则。
    /// </summary>
    [Fact]
    public async Task Create_WithDisabledDepartment_ShouldThrow()
    {
        var fixture = new Fixture();
        var department = new SysDepartment { DepartmentCode = "D20", DepartmentName = "部门", Status = EnableStatus.Disabled };
        SaasTestHelper.SetBasicId(department, 20);
        fixture.Departments.Setup(repo => repo.GetByIdAsync(20, It.IsAny<CancellationToken>())).ReturnsAsync(department);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreateAsync(CreateCommand() with { TargetType = FieldSecurityTargetType.Department, TargetId = 20 }));

        Assert.Contains("停用部门", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 同一目标同一字段只能有一条规则。
    /// </summary>
    [Fact]
    public async Task Create_WithDuplicatePolicy_ShouldThrow()
    {
        var fixture = new Fixture();
        fixture.Policies
            .Setup(repo => repo.AnyAsync(It.IsAny<Expression<Func<SysFieldLevelSecurity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.CreateAsync(CreateCommand()));

        Assert.Contains("已有规则", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 租户上下文不能改平台规则（平台规则对所有租户生效）。
    /// </summary>
    [Fact]
    public async Task Update_PlatformPolicyFromTenant_ShouldThrow()
    {
        var fixture = new Fixture();
        fixture.SetupExisting(CreatePolicy(tenantId: 0));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.UpdateAsync(new FieldLevelSecurityUpdateCommand(
                300, FieldSecurityTargetType.Role, 10, nameof(SysUser), nameof(SysUser.Phone), FieldMaskStrategy.Hidden, null, null, null, false, null)));

        Assert.Contains("只能在平台维护", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 租户上下文不能删平台规则。
    /// </summary>
    [Fact]
    public async Task Delete_PlatformPolicyFromTenant_ShouldThrow()
    {
        var fixture = new Fixture();
        fixture.SetupExisting(CreatePolicy(tenantId: 0));

        _ = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.DeleteAsync(300));
    }

    /// <summary>
    /// 删除不存在的规则报错。
    /// </summary>
    [Fact]
    public async Task Delete_WhenPolicyMissing_ShouldThrow()
    {
        var fixture = new Fixture();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.DeleteAsync(999));

        Assert.Contains("不存在", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 启用前按当前代码重新校验：字段已随版本移除的规则不能再启用。
    /// </summary>
    [Fact]
    public async Task EnableStatus_WhenFieldNoLongerExists_ShouldThrow()
    {
        var fixture = new Fixture();
        var policy = CreatePolicy(tenantId: 7);
        policy.FieldName = "Salary";
        policy.Status = EnableStatus.Disabled;
        fixture.SetupExisting(policy);

        _ = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.UpdateStatusAsync(new FieldLevelSecurityStatusChangeCommand(300, EnableStatus.Enabled, null)));
    }

    /// <summary>
    /// 停用不校验定义，失效的规则也能停用。
    /// </summary>
    [Fact]
    public async Task DisableStatus_WhenFieldNoLongerExists_ShouldPass()
    {
        var fixture = new Fixture();
        var policy = CreatePolicy(tenantId: 7);
        policy.FieldName = "Salary";
        fixture.SetupExisting(policy);

        var result = await fixture.Service.UpdateStatusAsync(new FieldLevelSecurityStatusChangeCommand(300, EnableStatus.Disabled, null));

        Assert.Equal(EnableStatus.Disabled, result.Policy.Status);
    }

    private static FieldLevelSecurityCreateCommand CreateCommand(
        string entityName = nameof(SysUser),
        string fieldName = nameof(SysUser.Phone),
        FieldMaskStrategy maskStrategy = FieldMaskStrategy.Hidden,
        int? keepHead = null,
        int? keepTail = null,
        string? replacement = null,
        bool isEditable = false,
        long targetId = 10)
    {
        return new FieldLevelSecurityCreateCommand(
            FieldSecurityTargetType.Role,
            targetId,
            entityName,
            fieldName,
            maskStrategy,
            keepHead,
            keepTail,
            replacement,
            isEditable,
            EnableStatus.Enabled,
            null);
    }

    private static SysFieldLevelSecurity CreatePolicy(long tenantId)
    {
        var policy = new SysFieldLevelSecurity
        {
            TenantId = tenantId,
            TargetType = FieldSecurityTargetType.Role,
            TargetId = 10,
            EntityName = nameof(SysUser),
            FieldName = nameof(SysUser.Phone),
            MaskStrategy = FieldMaskStrategy.Hidden,
            Status = EnableStatus.Enabled
        };
        SaasTestHelper.SetBasicId(policy, 300);
        return policy;
    }

    private static SysRole CreateRole(long id, EnableStatus status = EnableStatus.Enabled)
    {
        var role = new SysRole
        {
            TenantId = 7,
            RoleCode = $"ROLE-{id}",
            RoleName = $"角色{id}",
            Status = status
        };
        SaasTestHelper.SetBasicId(role, id);
        return role;
    }

    /// <summary>
    /// 领域服务测试夹具：租户 7 上下文，目录只登记 SysUser，角色 10 可用
    /// </summary>
    private sealed class Fixture
    {
        public Fixture()
        {
            Policies
                .Setup(repo => repo.AnyAsync(It.IsAny<Expression<Func<SysFieldLevelSecurity, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            Policies
                .Setup(repo => repo.AddAsync(It.IsAny<SysFieldLevelSecurity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SysFieldLevelSecurity entity, CancellationToken _) =>
                {
                    SaasTestHelper.SetBasicId(entity, 300);
                    return entity;
                });
            Policies
                .Setup(repo => repo.UpdateAsync(It.IsAny<SysFieldLevelSecurity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SysFieldLevelSecurity entity, CancellationToken _) => entity);
            Roles.Setup(repo => repo.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(CreateRole(10));

            var currentTenant = new Mock<ICurrentTenant>();
            currentTenant.SetupGet(tenant => tenant.Id).Returns(7L);

            Service = new FieldLevelSecurityDomainService(
                Policies.Object,
                new FieldSecurityEntityCatalog(Options.Create(new FieldSecurityEntityOptions().Add<SysUser>())),
                Roles.Object,
                Departments.Object,
                new Mock<ITenantUserRepository>().Object,
                new Mock<IUserRepository>().Object,
                currentTenant.Object);
        }

        public FieldLevelSecurityDomainService Service { get; }

        public Mock<IFieldLevelSecurityRepository> Policies { get; } = new();

        public Mock<IRoleRepository> Roles { get; } = new();

        public Mock<IDepartmentRepository> Departments { get; } = new();

        public void SetupExisting(SysFieldLevelSecurity policy)
        {
            Policies.Setup(repo => repo.GetByIdAsync(policy.BasicId, It.IsAny<CancellationToken>())).ReturnsAsync(policy);
        }
    }
}
