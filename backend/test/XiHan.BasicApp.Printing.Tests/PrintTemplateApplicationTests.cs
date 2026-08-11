// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Reflection;
using XiHan.BasicApp.Printing.Application.AppServices;
using XiHan.BasicApp.Printing.Application.Caching;
using XiHan.BasicApp.Printing.Application.Dtos;
using XiHan.BasicApp.Printing.Domain.DomainServices;
using XiHan.BasicApp.Printing.Domain.Entities;
using XiHan.BasicApp.Printing.Domain.Enums;
using XiHan.BasicApp.Printing.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Authorization.Permissions;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Security.Users;

namespace XiHan.BasicApp.Printing.Tests;

/// <summary>
/// 打印模板应用编排、权限注册和缓存失效测试。
/// </summary>
public sealed class PrintTemplateApplicationTests
{
    private const string TemplateJson = "{\"panels\":[{\"printElements\":[]}]}";

    /// <summary>
    /// 权限清单必须完整覆盖七码，且只有全局管理为平台专属、不进可授租户集合。
    /// </summary>
    [Fact]
    public void Permissions_ShouldRegisterAllCodesAndKeepOnlyGlobalManagePlatformExclusive()
    {
        string[] codes =
        [
            PrintingPermissionCodes.Read,
            PrintingPermissionCodes.Create,
            PrintingPermissionCodes.Update,
            PrintingPermissionCodes.Status,
            PrintingPermissionCodes.Delete,
            PrintingPermissionCodes.Use,
            PrintingPermissionCodes.GlobalManage
        ];

        Assert.All(codes, code => Assert.Contains(code, PrintingPermissionCodes.All));
        Assert.Equal(codes.Length, PrintingPermissionCodes.All.Count);
        Assert.DoesNotContain(PrintingPermissionCodes.GlobalManage, PrintingPermissionCodes.TenantGrantable);
        Assert.Equal(
            codes.Where(code => code != PrintingPermissionCodes.GlobalManage),
            PrintingPermissionCodes.TenantGrantable);

        // 模块登记进 Saas 平台专属清单后，版本白名单与租户授权对全局管理保持同一排除口径
        SaasPlatformPermissions.ContributePlatformOnly(PrintingPermissionCodes.GlobalManage);
        Assert.Contains(PrintingPermissionCodes.GlobalManage, SaasPlatformPermissions.PlatformOnlyCodes);
        Assert.False(SaasPlatformPermissions.IsTenantGrantable(PrintingPermissionCodes.GlobalManage));
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
        var cacheInvalidator = new Mock<IPrintingCacheInvalidator>();
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
