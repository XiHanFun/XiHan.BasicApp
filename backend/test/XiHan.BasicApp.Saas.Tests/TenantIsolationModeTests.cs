// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Core.Exceptions;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 建租户：隔离模式创建时定下、之后不能改，Schema 隔离尚未实装；版本在创建时定下
/// </summary>
/// <remarks>
/// 数据按创建时的模式落库（字段隔离在平台库、库隔离在独立库），改模式等于迁移数据，不是改一个字段。
/// </remarks>
public sealed class TenantIsolationModeTests
{
    [Fact]
    public void 更新契约里没有隔离模式()
    {
        Assert.Null(typeof(TenantUpdateDto).GetProperty("IsolationMode"));
        Assert.Null(typeof(TenantUpdateCommand).GetProperty("IsolationMode"));
    }

    [Fact]
    public async Task 创建_Schema隔离_拒绝且不落库()
    {
        var (service, tenants) = CreateService();

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => service.CreateTenantAsync(CreateCommand(TenantIsolationMode.Schema)));

        Assert.Contains("Schema", exception.Message, StringComparison.Ordinal);
        tenants.Verify(repo => repo.AddAsync(It.IsAny<SysTenant>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task 创建_库隔离_待初始化()
    {
        var (service, _) = CreateService();

        var result = await service.CreateTenantAsync(CreateCommand(TenantIsolationMode.Database));

        Assert.Equal(TenantIsolationMode.Database, result.Tenant.IsolationMode);
        Assert.Equal(TenantConfigStatus.Pending, result.Tenant.ConfigStatus);
    }

    [Fact]
    public async Task 创建_未指定版本_取默认版本()
    {
        var (service, tenants) = CreateService(defaultEditionId: 55);

        var result = await service.CreateTenantAsync(CreateCommand(TenantIsolationMode.Field));

        Assert.Equal(55, result.Tenant.EditionId);
        tenants.Verify(repo => repo.AddAsync(It.Is<SysTenant>(tenant => tenant.EditionId == 55), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task 更新_不改动隔离模式与配置状态()
    {
        var existing = new SysTenant
        {
            TenantCode = "t9",
            TenantName = "租户9",
            IsolationMode = TenantIsolationMode.Database,
            DatabaseType = TenantDatabaseType.PostgreSql,
            ConnectionString = "protected",
            ConfigStatus = TenantConfigStatus.Configured
        };
        SaasTestHelper.SetBasicId(existing, 9);
        var (service, tenants) = CreateService(existing);

        var result = await service.UpdateTenantAsync(new TenantUpdateCommand(
            9, "租户9改名", null, null, null, null, null, null, null, 1, null, TenantDatabaseType.PostgreSql, null));

        Assert.Equal(TenantIsolationMode.Database, result.Tenant.IsolationMode);
        Assert.Equal(TenantConfigStatus.Configured, result.Tenant.ConfigStatus);
        tenants.Verify(repo => repo.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static TenantCreateCommand CreateCommand(TenantIsolationMode isolationMode) => new(
        "t9",
        "租户9",
        null,
        null,
        null,
        null,
        isolationMode,
        null,
        null,
        null,
        1,
        null,
        isolationMode == TenantIsolationMode.Database ? TenantDatabaseType.PostgreSql : null,
        isolationMode == TenantIsolationMode.Database ? "Host=db;Database=t9" : null);

    private static (TenantDomainService Service, Mock<ITenantRepository> Tenants) CreateService(SysTenant? existing = null, long? defaultEditionId = null)
    {
        var tenants = new Mock<ITenantRepository>();
        _ = tenants
            .Setup(repo => repo.AddAsync(It.IsAny<SysTenant>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysTenant tenant, CancellationToken _) => tenant);
        _ = tenants
            .Setup(repo => repo.UpdateAsync(It.IsAny<SysTenant>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysTenant tenant, CancellationToken _) => tenant);
        if (existing is not null)
        {
            _ = tenants
                .Setup(repo => repo.GetByIdAsync(existing.BasicId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);
        }

        var protector = new Mock<ITenantConnectionSecretProtector>();
        _ = protector.Setup(value => value.Protect(It.IsAny<string>())).Returns((string? plaintext) => $"protected:{plaintext}");

        var provision = new Mock<ITenantProvisionDomainService>();
        _ = provision
            .Setup(value => value.AssignDefaultEditionAsync(It.IsAny<SysTenant>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysTenant tenant, CancellationToken _) =>
            {
                tenant.EditionId = defaultEditionId;
                return defaultEditionId;
            });

        var service = new TenantDomainService(
            tenants.Object,
            new Mock<ITenantUserRepository>().Object,
            new Mock<IUserRepository>().Object,
            provision.Object,
            new Mock<ITenantQuotaDomainService>().Object,
            new TestCurrentTenant(),
            protector.Object,
            new Mock<ITenantConnectionCacheInvalidator>().Object);
        return (service, tenants);
    }
}
