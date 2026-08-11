// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Reflection;
using XiHan.BasicApp.Saas.Application.AppServices;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Authorization.Permissions;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Security.Users;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 打印模板应用编排、权限注册和缓存失效测试。
/// </summary>
public sealed class PrintTemplateApplicationTests
{
    private const string TemplateJson = "{\"panels\":[{\"printElements\":[]}]}";

    /// <summary>
    /// 所有打印模板权限必须进入单一权限事实源，且只有全局管理为平台专属。
    /// </summary>
    [Fact]
    public void Permissions_ShouldRegisterAllCodesAndKeepOnlyGlobalManagePlatformExclusive()
    {
        string[] codes =
        [
            SaasPermissionCodes.PrintTemplate.Read,
            SaasPermissionCodes.PrintTemplate.Create,
            SaasPermissionCodes.PrintTemplate.Update,
            SaasPermissionCodes.PrintTemplate.Status,
            SaasPermissionCodes.PrintTemplate.Delete,
            SaasPermissionCodes.PrintTemplate.Use,
            SaasPermissionCodes.PrintTemplate.GlobalManage
        ];

        Assert.All(codes, code => Assert.Contains(code, SaasPermissionCodes.All));
        Assert.All(codes, code => Assert.Contains(
            SaasPermissionDefinitions.All,
            definition => definition.PermissionCode == code));
        Assert.Contains(
            SaasPermissionCodes.PrintTemplate.GlobalManage,
            SaasPlatformPermissions.PlatformOnlyCodes);
        Assert.All(
            codes.Where(code => code != SaasPermissionCodes.PrintTemplate.GlobalManage),
            code => Assert.True(SaasPlatformPermissions.IsTenantGrantable(code)));
    }

    /// <summary>
    /// 创建命令成功后必须调用事务感知打印模板缓存失效器。
    /// </summary>
    [Fact]
    public async Task CreatePrintTemplateAsync_ShouldInvalidateCacheAfterDomainWrite()
    {
        var template = CreateTemplate();
        var domainService = new Mock<IPrintTemplateDomainService>();
        domainService
            .Setup(value => value.CreateAsync(It.IsAny<PrintTemplateCreateCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PrintTemplateCommandResult(template));
        var cacheInvalidator = new Mock<ISaasCacheInvalidator>();
        var currentTenant = new Mock<ICurrentTenant>();
        currentTenant.SetupGet(value => value.Id).Returns(7);
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(value => value.UserId).Returns(99);

        var service = new PrintTemplateAppService(
            domainService.Object,
            cacheInvalidator.Object,
            currentTenant.Object,
            currentUser.Object,
            Mock.Of<IPermissionChecker>(),
            NullLogger<PrintTemplateAppService>.Instance);

        var result = await service.CreatePrintTemplateAsync(new PrintTemplateCreateDto
        {
            Scope = PrintTemplateScope.Tenant,
            TemplateCode = template.TemplateCode,
            DataSourceCode = template.DataSourceCode,
            TemplateName = template.TemplateName,
            TemplateJson = template.TemplateJson,
            EngineVersion = template.EngineVersion,
            Status = EnableStatus.Enabled,
            Sort = 10
        });

        Assert.Equal(template.BasicId, result.BasicId);
        cacheInvalidator.Verify(
            value => value.InvalidatePrintTemplateAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 创建完整且具有稳定主键的模板实体。
    /// </summary>
    private static SysPrintTemplate CreateTemplate()
    {
        var template = new SysPrintTemplate
        {
            TenantId = 7,
            TemplateCode = "ORDER",
            DataSourceCode = "system.print-demo",
            TemplateName = "订单模板",
            TemplateJson = TemplateJson,
            EngineVersion = "0.0.60",
            Status = EnableStatus.Enabled,
            Sort = 10
        };
        typeof(SysPrintTemplate)
            .GetProperty(
                nameof(SysPrintTemplate.BasicId),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(template, 1001L);
        return template;
    }
}
