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
/// 字段级安全领域服务测试：FLS 策略写入的读写语义不变量与目标可用性校验。
/// </summary>
public sealed class FieldLevelSecurityDomainServiceTests
{
    /// <summary>
    /// 不可读字段不能设置为可编辑。
    /// </summary>
    [Fact]
    public async Task Create_UnreadableButEditable_ShouldThrow()
    {
        var fixture = CreateFixture();
        var command = CreateCommand(isReadable: false, isEditable: true);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreateAsync(command));

        Assert.Contains("不可读字段不能设置为可编辑", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 不可读字段必须指定脱敏策略。
    /// </summary>
    [Fact]
    public async Task Create_UnreadableWithoutMaskStrategy_ShouldThrow()
    {
        var fixture = CreateFixture();
        var command = CreateCommand(
            isReadable: false,
            isEditable: false,
            maskStrategy: FieldMaskStrategy.None);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreateAsync(command));

        Assert.Contains("不可读字段必须指定脱敏策略", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 目标主键必须大于 0。
    /// </summary>
    [Fact]
    public async Task Create_WithInvalidTargetId_ShouldThrow()
    {
        var fixture = CreateFixture();
        var command = CreateCommand(targetId: 0);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Service.CreateAsync(command));
    }

    /// <summary>
    /// 资源主键必须大于 0。
    /// </summary>
    [Fact]
    public async Task Create_WithInvalidResourceId_ShouldThrow()
    {
        var fixture = CreateFixture();
        var command = CreateCommand(resourceId: 0);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Service.CreateAsync(command));
    }

    /// <summary>
    /// 优先级不能为负数。
    /// </summary>
    [Fact]
    public async Task Create_WithNegativePriority_ShouldThrow()
    {
        var fixture = CreateFixture();
        var command = CreateCommand(priority: -1);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Service.CreateAsync(command));
    }

    /// <summary>
    /// 停用资源不能配置字段级安全策略。
    /// </summary>
    [Fact]
    public async Task Create_WithDisabledResource_ShouldThrow()
    {
        var fixture = CreateFixture();
        fixture.ResourceRepository
            .Setup(repo => repo.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateResource(1, EnableStatus.Disabled));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreateAsync(CreateCommand()));

        Assert.Contains("停用资源不能配置字段级安全策略", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 停用角色不能配置字段级安全策略。
    /// </summary>
    [Fact]
    public async Task Create_WithDisabledRole_ShouldThrow()
    {
        var fixture = CreateFixture();
        fixture.ResourceRepository
            .Setup(repo => repo.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateResource(1));
        fixture.RoleRepository
            .Setup(repo => repo.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRole(10, EnableStatus.Disabled));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreateAsync(CreateCommand()));

        Assert.Contains("停用角色不能配置字段级安全策略", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 同目标+资源+字段的策略已存在时必须拒绝。
    /// </summary>
    [Fact]
    public async Task Create_WithDuplicatePolicy_ShouldThrow()
    {
        var fixture = CreateFixture();
        fixture.SetupValidResourceAndRole();
        fixture.FieldLevelSecurityRepository
            .Setup(repo => repo.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<SysFieldLevelSecurity, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreateAsync(CreateCommand()));

        Assert.Contains("字段级安全策略已存在", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 不脱敏策略应清空脱敏模式（即使调用方传入了模式）。
    /// </summary>
    [Fact]
    public async Task Create_WithNoneStrategy_ShouldNormalizeMaskPatternToNull()
    {
        var fixture = CreateFixture();
        fixture.SetupValidResourceAndRole();
        var command = CreateCommand(maskStrategy: FieldMaskStrategy.None, maskPattern: "ignored");

        var result = await fixture.Service.CreateAsync(command);

        Assert.Null(result.Policy.MaskPattern);
    }

    /// <summary>
    /// 合法策略创建成功：字段映射、目标摘要与持久化正确。
    /// </summary>
    [Fact]
    public async Task Create_WithValidInput_ShouldPersistPolicy()
    {
        var fixture = CreateFixture();
        fixture.SetupValidResourceAndRole();
        var command = CreateCommand();

        var result = await fixture.Service.CreateAsync(command);

        Assert.Equal(300, result.Policy.BasicId);
        Assert.Equal("Phone", result.Policy.FieldName);
        Assert.Equal(FieldSecurityTargetType.Role, result.Policy.TargetType);
        Assert.Equal(10, result.Policy.TargetId);
        Assert.Equal(1, result.Policy.ResourceId);
        Assert.Equal("keep:3,4", result.Policy.MaskPattern);
        Assert.Equal(FieldMaskStrategy.PartialMask, result.Policy.MaskStrategy);
        Assert.NotNull(result.Resource);
        Assert.Equal("ROLE-10", result.TargetCode);
        fixture.FieldLevelSecurityRepository.Verify(
            repo => repo.AddAsync(It.IsAny<SysFieldLevelSecurity>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 更新命令主键必须大于 0。
    /// </summary>
    [Fact]
    public async Task Update_WithInvalidBasicId_ShouldThrow()
    {
        var fixture = CreateFixture();
        var command = CreateUpdateCommand(basicId: 0);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Service.UpdateAsync(command));
    }

    /// <summary>
    /// 删除不存在的策略必须拒绝。
    /// </summary>
    [Fact]
    public async Task Delete_WhenPolicyMissing_ShouldThrow()
    {
        var fixture = CreateFixture();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.DeleteAsync(404));

        Assert.Contains("字段级安全策略不存在", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 构造字段级安全创建命令。
    /// </summary>
    private static FieldLevelSecurityCreateCommand CreateCommand(
        FieldSecurityTargetType targetType = FieldSecurityTargetType.Role,
        long targetId = 10,
        long resourceId = 1,
        bool isReadable = true,
        bool isEditable = true,
        FieldMaskStrategy maskStrategy = FieldMaskStrategy.PartialMask,
        string? maskPattern = "keep:3,4",
        int priority = 0)
    {
        return new FieldLevelSecurityCreateCommand(
            targetType,
            targetId,
            resourceId,
            "Phone",
            isReadable,
            isEditable,
            maskStrategy,
            maskPattern,
            priority,
            null,
            EnableStatus.Enabled,
            null);
    }

    /// <summary>
    /// 构造字段级安全更新命令。
    /// </summary>
    private static FieldLevelSecurityUpdateCommand CreateUpdateCommand(long basicId)
    {
        return new FieldLevelSecurityUpdateCommand(
            basicId,
            FieldSecurityTargetType.Role,
            10,
            1,
            "Phone",
            true,
            true,
            FieldMaskStrategy.PartialMask,
            "keep:3,4",
            0,
            null,
            null);
    }

    /// <summary>
    /// 构造启用/停用资源。
    /// </summary>
    private static SysResource CreateResource(long id, EnableStatus status = EnableStatus.Enabled)
    {
        var resource = new SysResource
        {
            TenantId = 7,
            ResourceCode = "SysUser",
            ResourceName = "用户",
            Status = status
        };
        SaasTestHelper.SetBasicId(resource, id);
        return resource;
    }

    /// <summary>
    /// 构造启用/停用角色。
    /// </summary>
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
    /// 创建带仓储模拟的字段级安全测试夹具。
    /// </summary>
    private static FieldSecurityFixture CreateFixture()
    {
        var fieldLevelSecurityRepository = new Mock<IFieldLevelSecurityRepository>();
        fieldLevelSecurityRepository
            .Setup(repo => repo.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<SysFieldLevelSecurity, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        fieldLevelSecurityRepository
            .Setup(repo => repo.AddAsync(It.IsAny<SysFieldLevelSecurity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysFieldLevelSecurity entity, CancellationToken _) =>
            {
                SaasTestHelper.SetBasicId(entity, 300);
                return entity;
            });

        var resourceRepository = new Mock<IResourceRepository>();
        var roleRepository = new Mock<IRoleRepository>();
        var permissionRepository = new Mock<IPermissionRepository>();
        var departmentRepository = new Mock<IDepartmentRepository>();
        var tenantUserRepository = new Mock<ITenantUserRepository>();
        var currentTenant = new Mock<ICurrentTenant>();
        currentTenant.SetupGet(tenant => tenant.Id).Returns((long?)7);

        var service = new FieldLevelSecurityDomainService(
            fieldLevelSecurityRepository.Object,
            resourceRepository.Object,
            roleRepository.Object,
            permissionRepository.Object,
            departmentRepository.Object,
            tenantUserRepository.Object,
            currentTenant.Object);
        return new FieldSecurityFixture(service, fieldLevelSecurityRepository, resourceRepository, roleRepository);
    }

    /// <summary>
    /// 字段级安全测试依赖集合。
    /// </summary>
    private sealed record FieldSecurityFixture(
        FieldLevelSecurityDomainService Service,
        Mock<IFieldLevelSecurityRepository> FieldLevelSecurityRepository,
        Mock<IResourceRepository> ResourceRepository,
        Mock<IRoleRepository> RoleRepository)
    {
        /// <summary>
        /// 预设有效资源与有效角色目标。
        /// </summary>
        public void SetupValidResourceAndRole()
        {
            ResourceRepository
                .Setup(repo => repo.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateResource(1));
            RoleRepository
                .Setup(repo => repo.GetByIdAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateRole(10));
        }
    }
}
