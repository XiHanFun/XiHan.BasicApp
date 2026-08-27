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
using XiHan.Framework.Authorization.Permissions;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Security.Users;

namespace XiHan.BasicApp.Printing.Tests;

/// <summary>
/// 打印模板命令服务的写作用域门控与缓存失效编排测试。
/// </summary>
/// <remarks>
/// <c>EnsureWritableScopeAsync</c> 是四个写命令共享的第一道闸，它同时承担两件事：
/// 一是把"租户不能写全局、平台不能写租户私有"这条双向禁令挡在领域层之前，
/// 二是给平台写全局模板补一次 <c>print-template:global-manage</c> 检查——
/// 方法级 <c>[PermissionAuthorize]</c> 只声明了 create/update/status/delete，
/// 全局管理权限属性表达不了，只能在方法体里补。少了这一步，任何持有 create 的平台账号
/// 都能建全局模板并对所有租户开放。
/// </remarks>
public sealed class PrintingExtraAppServiceScopeTests
{
    private const string TemplateJson = "{\"panels\":[{\"printElements\":[]}]}";

    /// <summary>
    /// 未定义的作用域枚举值必须先于任何租户判定被拒绝。
    /// </summary>
    [Fact]
    public async Task CreatePrintTemplateAsync_UndefinedScope_ShouldReject()
    {
        var fixture = CreateFixture(tenantId: 7);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.CreatePrintTemplateAsync(CreateDto((PrintTemplateScope)9)));

        Assert.Contains("作用域无效", exception.Message, StringComparison.Ordinal);
        VerifyNoDomainWrite(fixture);
    }

    /// <summary>
    /// 租户上下文写全局模板必须拒绝：一个租户的改动不允许影响所有租户。
    /// </summary>
    [Fact]
    public async Task CreatePrintTemplateAsync_TenantWritingGlobal_ShouldReject()
    {
        var fixture = CreateFixture(tenantId: 7);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.CreatePrintTemplateAsync(CreateDto(PrintTemplateScope.Global)));

        Assert.Contains("租户上下文只能维护租户私有打印模板", exception.Message, StringComparison.Ordinal);
        VerifyNoDomainWrite(fixture);
    }

    /// <summary>
    /// 平台上下文写租户私有模板必须拒绝：没有租户号就没有明确归属。
    /// </summary>
    [Fact]
    public async Task CreatePrintTemplateAsync_PlatformWritingTenantScope_ShouldReject()
    {
        var fixture = CreateFixture(tenantId: null);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.CreatePrintTemplateAsync(CreateDto(PrintTemplateScope.Tenant)));

        Assert.Contains("平台上下文不能维护未指定租户的私有打印模板", exception.Message, StringComparison.Ordinal);
        VerifyNoDomainWrite(fixture);
    }

    /// <summary>
    /// 租户上下文的 Auto 与 Tenant 作用域直接放行，且完全不触发全局管理权限检查。
    /// </summary>
    /// <param name="scope">租户可写的作用域。</param>
    [Theory]
    [InlineData(PrintTemplateScope.Auto)]
    [InlineData(PrintTemplateScope.Tenant)]
    public async Task CreatePrintTemplateAsync_TenantWritableScopes_ShouldSkipGlobalPermissionCheck(PrintTemplateScope scope)
    {
        var fixture = CreateFixture(tenantId: 7);

        _ = await fixture.Service.CreatePrintTemplateAsync(CreateDto(scope));

        fixture.PermissionChecker.Verify(
            checker => checker.IsGrantedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 平台上下文写模板时若拿不到当前用户，必须以"未登录"拒绝而不是拿 null 去查权限。
    /// </summary>
    [Fact]
    public async Task CreatePrintTemplateAsync_PlatformWithoutUser_ShouldReject()
    {
        var fixture = CreateFixture(tenantId: null, userId: null);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.CreatePrintTemplateAsync(CreateDto(PrintTemplateScope.Global)));

        Assert.Contains("当前用户未登录", exception.Message, StringComparison.Ordinal);
        fixture.PermissionChecker.Verify(
            checker => checker.IsGrantedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 平台账号缺少全局管理权限时不得创建全局模板，检查用的正是当前用户号与 global-manage 权限码。
    /// </summary>
    [Fact]
    public async Task CreatePrintTemplateAsync_PlatformWithoutGlobalManage_ShouldReject()
    {
        var fixture = CreateFixture(tenantId: null, userId: 99, hasGlobalManage: false);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.CreatePrintTemplateAsync(CreateDto(PrintTemplateScope.Global)));

        Assert.Contains("缺少全局打印模板管理权限", exception.Message, StringComparison.Ordinal);
        fixture.PermissionChecker.Verify(
            checker => checker.IsGrantedAsync("99", PrintingPermissionCodes.GlobalManage, It.IsAny<CancellationToken>()),
            Times.Once);
        VerifyNoDomainWrite(fixture);
    }

    /// <summary>
    /// 平台账号持有全局管理权限时放行，并在领域写成功后失效模板解析缓存。
    /// </summary>
    [Fact]
    public async Task CreatePrintTemplateAsync_PlatformWithGlobalManage_ShouldPassAndInvalidateCache()
    {
        var fixture = CreateFixture(tenantId: null, userId: 99, hasGlobalManage: true);

        var result = await fixture.Service.CreatePrintTemplateAsync(CreateDto(PrintTemplateScope.Global));

        Assert.Equal(1001, result.BasicId);
        fixture.CacheInvalidator.Verify(
            invalidator => invalidator.InvalidatePrintTemplateAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 平台的 Auto 作用域同样落在全局写路径上，必须一并要求全局管理权限。
    /// </summary>
    [Fact]
    public async Task CreatePrintTemplateAsync_PlatformAutoScope_ShouldStillRequireGlobalManage()
    {
        var fixture = CreateFixture(tenantId: null, userId: 99, hasGlobalManage: false);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.CreatePrintTemplateAsync(CreateDto(PrintTemplateScope.Auto)));

        Assert.Contains("缺少全局打印模板管理权限", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 租户号为 0 与"无租户"是同一种平台态，写租户私有模板同样拒绝。
    /// </summary>
    [Fact]
    public async Task CreatePrintTemplateAsync_TenantZeroIsPlatform_ShouldRejectTenantScope()
    {
        var fixture = CreateFixture(tenantId: 0);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.CreatePrintTemplateAsync(CreateDto(PrintTemplateScope.Tenant)));

        Assert.Contains("平台上下文不能维护未指定租户的私有打印模板", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 更新命令成功后必须失效模板解析缓存，否则打印端会在 30 分钟内继续拿到旧设计。
    /// </summary>
    [Fact]
    public async Task UpdatePrintTemplateAsync_ShouldInvalidateCache()
    {
        var fixture = CreateFixture(tenantId: 7);

        var result = await fixture.Service.UpdatePrintTemplateAsync(new PrintTemplateUpdateDto
        {
            BasicId = 1001,
            Scope = PrintTemplateScope.Tenant,
            RowVersion = "3",
            DataSourceCode = "system.print-demo",
            TemplateName = "订单模板",
            TemplateJson = TemplateJson,
            EngineVersion = "0.0.60",
            Sort = 10
        });

        Assert.Equal(1001, result.BasicId);
        fixture.CacheInvalidator.Verify(
            invalidator => invalidator.InvalidatePrintTemplateAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 启停命令成功后必须失效模板解析缓存，停用的模板不能继续被解析出来。
    /// </summary>
    [Fact]
    public async Task UpdatePrintTemplateStatusAsync_ShouldInvalidateCache()
    {
        var fixture = CreateFixture(tenantId: 7);

        var result = await fixture.Service.UpdatePrintTemplateStatusAsync(new PrintTemplateStatusUpdateDto
        {
            BasicId = 1001,
            Scope = PrintTemplateScope.Tenant,
            RowVersion = "3",
            Status = EnableStatus.Disabled
        });

        Assert.Equal(1001, result.BasicId);
        fixture.CacheInvalidator.Verify(
            invalidator => invalidator.InvalidatePrintTemplateAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 删除命令成功后必须失效模板解析缓存，被删模板不能继续从缓存返回。
    /// </summary>
    [Fact]
    public async Task DeletePrintTemplateAsync_ShouldInvalidateCache()
    {
        var fixture = CreateFixture(tenantId: 7);

        await fixture.Service.DeletePrintTemplateAsync(new PrintTemplateDeleteDto
        {
            BasicId = 1001,
            Scope = PrintTemplateScope.Tenant,
            RowVersion = "3"
        });

        fixture.DomainService.Verify(
            domain => domain.DeleteAsync(It.IsAny<PrintTemplateDeleteCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);
        fixture.CacheInvalidator.Verify(
            invalidator => invalidator.InvalidatePrintTemplateAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 领域写失败时异常原样上抛，且绝不能失效缓存——失败的写不该把别人的热缓存清掉。
    /// </summary>
    [Fact]
    public async Task CreatePrintTemplateAsync_DomainFailure_ShouldRethrowWithoutCacheInvalidation()
    {
        var fixture = CreateFixture(tenantId: 7);
        fixture.DomainService
            .Setup(domain => domain.CreateAsync(It.IsAny<PrintTemplateCreateCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UserFriendlyException("当前作用域中已存在相同编码的打印模板。"));

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.CreatePrintTemplateAsync(CreateDto(PrintTemplateScope.Tenant)));

        Assert.Contains("相同编码", exception.Message, StringComparison.Ordinal);
        fixture.CacheInvalidator.Verify(
            invalidator => invalidator.InvalidatePrintTemplateAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 行版本字符串不是十进制非负整数时，映射阶段就要拒绝，领域服务不该看到脏版本号。
    /// </summary>
    /// <param name="rowVersion">客户端提交的行版本字符串。</param>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("-1")]
    [InlineData("+1")]
    [InlineData("1.0")]
    [InlineData("abc")]
    [InlineData("1 ")]
    public async Task DeletePrintTemplateAsync_InvalidRowVersion_ShouldRejectBeforeDomain(string rowVersion)
    {
        var fixture = CreateFixture(tenantId: 7);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.DeletePrintTemplateAsync(new PrintTemplateDeleteDto
            {
                BasicId = 1001,
                Scope = PrintTemplateScope.Tenant,
                RowVersion = rowVersion
            }));

        Assert.Contains("行版本无效", exception.Message, StringComparison.Ordinal);
        fixture.DomainService.Verify(
            domain => domain.DeleteAsync(It.IsAny<PrintTemplateDeleteCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 四个写命令都必须以空入参的 <see cref="ArgumentNullException"/> 开场。
    /// </summary>
    [Fact]
    public async Task Commands_NullInput_ShouldThrowArgumentNull()
    {
        var fixture = CreateFixture(tenantId: 7);

        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => fixture.Service.CreatePrintTemplateAsync(null!));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => fixture.Service.UpdatePrintTemplateAsync(null!));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => fixture.Service.UpdatePrintTemplateStatusAsync(null!));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => fixture.Service.DeletePrintTemplateAsync(null!));
    }

    /// <summary>
    /// 取消令牌必须一路透传到领域服务与缓存失效器。
    /// </summary>
    [Fact]
    public async Task CreatePrintTemplateAsync_ShouldForwardCancellationToken()
    {
        var fixture = CreateFixture(tenantId: 7);
        using var cancellation = new CancellationTokenSource();

        _ = await fixture.Service.CreatePrintTemplateAsync(CreateDto(PrintTemplateScope.Tenant), cancellation.Token);

        fixture.DomainService.Verify(
            domain => domain.CreateAsync(It.IsAny<PrintTemplateCreateCommand>(), cancellation.Token),
            Times.Once);
        fixture.CacheInvalidator.Verify(
            invalidator => invalidator.InvalidatePrintTemplateAsync(cancellation.Token),
            Times.Once);
    }

    /// <summary>
    /// 创建 DTO 的每个字段都必须原样进入领域命令，映射层不得做隐式改写。
    /// </summary>
    [Fact]
    public async Task CreatePrintTemplateAsync_ShouldForwardEveryDtoFieldToCommand()
    {
        var fixture = CreateFixture(tenantId: null, userId: 99, hasGlobalManage: true);
        PrintTemplateCreateCommand? captured = null;
        fixture.DomainService
            .Setup(domain => domain.CreateAsync(It.IsAny<PrintTemplateCreateCommand>(), It.IsAny<CancellationToken>()))
            .Callback((PrintTemplateCreateCommand command, CancellationToken _) => captured = command)
            .ReturnsAsync(new PrintTemplateCommandResult(CreateTemplate()));

        _ = await fixture.Service.CreatePrintTemplateAsync(new PrintTemplateCreateDto
        {
            Scope = PrintTemplateScope.Global,
            TemplateCode = "SHIP",
            DataSourceCode = "system.print-demo",
            TemplateName = "发货单",
            TemplateJson = TemplateJson,
            EngineVersion = "0.0.61",
            AllowTenantUse = true,
            Status = EnableStatus.Disabled,
            Sort = 42,
            Remark = "平台模板"
        });

        Assert.NotNull(captured);
        Assert.Equal("SHIP", captured.TemplateCode);
        Assert.Equal("system.print-demo", captured.DataSourceCode);
        Assert.Equal("发货单", captured.TemplateName);
        Assert.Equal(TemplateJson, captured.TemplateJson);
        Assert.Equal("0.0.61", captured.EngineVersion);
        Assert.True(captured.AllowTenantUse);
        Assert.Equal(EnableStatus.Disabled, captured.Status);
        Assert.Equal(42, captured.Sort);
        Assert.Equal("平台模板", captured.Remark);
    }

    /// <summary>
    /// 断言作用域门控失败时领域服务完全没有被调用。
    /// </summary>
    private static void VerifyNoDomainWrite(AppServiceFixture fixture)
    {
        fixture.DomainService.Verify(
            domain => domain.CreateAsync(It.IsAny<PrintTemplateCreateCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 构造一条合法的创建 DTO。
    /// </summary>
    private static PrintTemplateCreateDto CreateDto(PrintTemplateScope scope)
    {
        return new PrintTemplateCreateDto
        {
            Scope = scope,
            TemplateCode = "ORDER",
            DataSourceCode = "system.print-demo",
            TemplateName = "订单模板",
            TemplateJson = TemplateJson,
            EngineVersion = "0.0.60",
            Status = EnableStatus.Enabled,
            Sort = 10
        };
    }

    /// <summary>
    /// 创建命令服务夹具，领域服务默认成功返回一个带主键的模板。
    /// </summary>
    /// <param name="tenantId">当前租户；null 或 0 表示平台态。</param>
    /// <param name="userId">当前用户号；null 表示未登录。</param>
    /// <param name="hasGlobalManage">当前用户是否持有全局管理权限。</param>
    private static AppServiceFixture CreateFixture(long? tenantId, long? userId = 99, bool hasGlobalManage = true)
    {
        var template = CreateTemplate();
        var domainService = new Mock<IPrintTemplateDomainService>();
        domainService
            .Setup(domain => domain.CreateAsync(It.IsAny<PrintTemplateCreateCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PrintTemplateCommandResult(template));
        domainService
            .Setup(domain => domain.UpdateAsync(It.IsAny<PrintTemplateUpdateCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PrintTemplateCommandResult(template));
        domainService
            .Setup(domain => domain.UpdateStatusAsync(It.IsAny<PrintTemplateStatusChangeCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PrintTemplateCommandResult(template));
        domainService
            .Setup(domain => domain.DeleteAsync(It.IsAny<PrintTemplateDeleteCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cacheInvalidator = new Mock<IPrintingCacheInvalidator>();
        cacheInvalidator
            .Setup(invalidator => invalidator.InvalidatePrintTemplateAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var currentTenant = new Mock<ICurrentTenant>();
        currentTenant.SetupGet(tenant => tenant.Id).Returns(tenantId);

        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(user => user.UserId).Returns(userId);

        var permissionChecker = new Mock<IPermissionChecker>();
        permissionChecker
            .Setup(checker => checker.IsGrantedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(hasGlobalManage);

        var service = new PrintTemplateAppService(
            domainService.Object,
            cacheInvalidator.Object,
            currentTenant.Object,
            currentUser.Object,
            permissionChecker.Object,
            NullLogger<PrintTemplateAppService>.Instance);
        return new AppServiceFixture(service, domainService, cacheInvalidator, permissionChecker);
    }

    /// <summary>
    /// 创建带稳定主键的模板实体。
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
            .GetProperty(nameof(SysPrintTemplate.BasicId), BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(template, 1001L);
        return template;
    }

    /// <summary>
    /// 命令服务测试依赖集合。
    /// </summary>
    /// <param name="Service">被测命令服务。</param>
    /// <param name="DomainService">领域服务替身。</param>
    /// <param name="CacheInvalidator">缓存失效器替身。</param>
    /// <param name="PermissionChecker">权限检查器替身。</param>
    private sealed record AppServiceFixture(
        PrintTemplateAppService Service,
        Mock<IPrintTemplateDomainService> DomainService,
        Mock<IPrintingCacheInvalidator> CacheInvalidator,
        Mock<IPermissionChecker> PermissionChecker);
}
