// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Reflection;
using XiHan.BasicApp.Printing.Domain.DataSources;
using XiHan.BasicApp.Printing.Domain.DomainServices;
using XiHan.BasicApp.Printing.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Printing.Domain.Repositories;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Printing.Tests;

/// <summary>
/// 打印模板领域不变量测试，覆盖租户边界、JSON 结构、启停删除和乐观并发版本。
/// </summary>
public sealed class PrintTemplateDomainServiceTests
{
    private const string ValidTemplateJson = "{\"panels\":[{\"printElements\":[]}]}";

    /// <summary>
    /// 租户创建模板时必须写入当前租户，且不得保留仅全局模板可用的开放标记。
    /// </summary>
    [Fact]
    public async Task CreateAsync_TenantTemplateShouldUseCurrentTenantAndClearGlobalFlag()
    {
        var fixture = CreateFixture(tenantId: 7);

        var result = await fixture.Service.CreateAsync(CreateCommand(allowTenantUse: true));

        Assert.Equal(7, result.Template.TenantId);
        Assert.False(result.Template.AllowTenantUse);
        Assert.Equal("ORDER", result.Template.TemplateCode);
        fixture.Repository.Verify(
            repository => repository.AddAsync(result.Template, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 未选择代码数据源时应创建自由模板，并把纯空白输入规范化为 null。
    /// </summary>
    [Fact]
    public async Task CreateAsync_WithoutDataSourceShouldCreateFreeTemplate()
    {
        var fixture = CreateFixture(tenantId: 7);

        var result = await fixture.Service.CreateAsync(CreateCommand() with { DataSourceCode = "   " });

        Assert.Null(result.Template.DataSourceCode);
        fixture.Repository.Verify(
            repository => repository.AddAsync(result.Template, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 同一租户中已存在同编码模板时必须在写入前给出友好错误。
    /// </summary>
    [Fact]
    public async Task CreateAsync_DuplicateCodeInTenantShouldReject()
    {
        var existing = CreateTemplate(tenantId: 7);
        var fixture = CreateFixture(tenantId: 7, existing);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.CreateAsync(CreateCommand()));

        Assert.Contains("相同编码", exception.Message, StringComparison.Ordinal);
        fixture.Repository.Verify(
            repository => repository.AddAsync(It.IsAny<SysPrintTemplate>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 非法 hiprint 根结构必须在进入仓储前被拒绝。
    /// </summary>
    /// <param name="templateJson">待校验模板 JSON。</param>
    [Theory]
    [InlineData("not-json")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("{\"panels\":[]}")]
    [InlineData("{\"panels\":[{}]}")]
    public async Task CreateAsync_InvalidTemplateJsonShouldReject(string templateJson)
    {
        var fixture = CreateFixture(tenantId: 7);
        var command = CreateCommand() with { TemplateJson = templateJson };

        await Assert.ThrowsAsync<UserFriendlyException>(() => fixture.Service.CreateAsync(command));

        fixture.Repository.Verify(
            repository => repository.AddAsync(It.IsAny<SysPrintTemplate>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 客户端旧版本不得覆盖其他用户已经提交的模板设计。
    /// </summary>
    [Fact]
    public async Task UpdateAsync_StaleRowVersionShouldReject()
    {
        var existing = CreateTemplate(tenantId: 7, rowVersion: 8);
        var fixture = CreateFixture(tenantId: 7, existing);
        var command = new PrintTemplateUpdateCommand(
            existing.BasicId,
            7,
            null,
            "新版名称",
            ValidTemplateJson,
            "0.0.60",
            false,
            10,
            null);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() => fixture.Service.UpdateAsync(command));

        Assert.Contains("其他用户修改", exception.Message, StringComparison.Ordinal);
        fixture.Repository.Verify(
            repository => repository.UpdateAsync(It.IsAny<SysPrintTemplate>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 版本匹配时允许在注册数据源与自由模式之间切换，模板编码保持不变。
    /// </summary>
    [Fact]
    public async Task UpdateAsync_ShouldAllowChangingDataSourceToFreeMode()
    {
        var existing = CreateTemplate(tenantId: 7, rowVersion: 8);
        var fixture = CreateFixture(tenantId: 7, existing);
        var command = new PrintTemplateUpdateCommand(
            existing.BasicId,
            existing.RowVersion,
            null,
            "新版名称",
            ValidTemplateJson,
            "0.0.60",
            false,
            10,
            null);

        var result = await fixture.Service.UpdateAsync(command);

        Assert.Null(result.Template.DataSourceCode);
        Assert.Equal("ORDER", result.Template.TemplateCode);
        fixture.Repository.Verify(
            repository => repository.UpdateAsync(existing, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 启用模板必须先停用，不能直接软删除。
    /// </summary>
    [Fact]
    public async Task DeleteAsync_EnabledTemplateShouldReject()
    {
        var existing = CreateTemplate(tenantId: 7, status: EnableStatus.Enabled);
        var fixture = CreateFixture(tenantId: 7, existing);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.DeleteAsync(new PrintTemplateDeleteCommand(existing.BasicId, existing.RowVersion)));

        Assert.Contains("先停用", exception.Message, StringComparison.Ordinal);
        fixture.Repository.Verify(
            repository => repository.DeleteAsync(It.IsAny<SysPrintTemplate>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 已停用且版本匹配的模板可以进入软删除仓储路径。
    /// </summary>
    [Fact]
    public async Task DeleteAsync_DisabledTemplateShouldDelete()
    {
        var existing = CreateTemplate(tenantId: 7, status: EnableStatus.Disabled);
        var fixture = CreateFixture(tenantId: 7, existing);

        await fixture.Service.DeleteAsync(new PrintTemplateDeleteCommand(existing.BasicId, existing.RowVersion));

        fixture.Repository.Verify(
            repository => repository.DeleteAsync(existing, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 创建领域服务与仓储替身。
    /// </summary>
    /// <param name="tenantId">当前租户；null 表示平台上下文。</param>
    /// <param name="existing">可选现有模板。</param>
    /// <returns>领域服务测试夹具。</returns>
    private static DomainFixture CreateFixture(long? tenantId, SysPrintTemplate? existing = null)
    {
        var repository = new Mock<IPrintTemplateRepository>();
        repository
            .Setup(value => value.FindByCodeInScopeAsync(
                tenantId ?? 0,
                It.IsAny<string>(),
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        repository
            .Setup(value => value.FindByIdInScopeAsync(
                tenantId ?? 0,
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        repository
            .Setup(value => value.AddAsync(It.IsAny<SysPrintTemplate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysPrintTemplate template, CancellationToken _) =>
            {
                SetEntityId(template, 1001);
                return template;
            });
        repository
            .Setup(value => value.UpdateAsync(It.IsAny<SysPrintTemplate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysPrintTemplate template, CancellationToken _) => template);
        repository
            .Setup(value => value.DeleteAsync(It.IsAny<SysPrintTemplate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var currentTenant = new Mock<ICurrentTenant>();
        currentTenant.SetupGet(value => value.Id).Returns(tenantId);

        var registry = new PrintDataSourceRegistry([new PrintDataSourceRegistration(BuiltInPrintDataSources.SystemPrintDemo)]);
        var service = new PrintTemplateDomainService(
            repository.Object,
            currentTenant.Object,
            registry,
            NullLogger<PrintTemplateDomainService>.Instance);
        return new DomainFixture(service, repository);
    }

    /// <summary>
    /// 创建有效的新模板命令。
    /// </summary>
    private static PrintTemplateCreateCommand CreateCommand(bool allowTenantUse = false)
    {
        return new PrintTemplateCreateCommand(
            "ORDER",
            "system.print-demo",
            "订单模板",
            ValidTemplateJson,
            "0.0.60",
            allowTenantUse,
            EnableStatus.Enabled,
            10,
            null);
    }

    /// <summary>
    /// 创建指定租户、版本和状态的模板实体。
    /// </summary>
    private static SysPrintTemplate CreateTemplate(
        long tenantId,
        long rowVersion = 3,
        EnableStatus status = EnableStatus.Enabled)
    {
        var template = new SysPrintTemplate
        {
            TenantId = tenantId,
            TemplateCode = "ORDER",
            DataSourceCode = "system.print-demo",
            TemplateName = "订单模板",
            TemplateJson = ValidTemplateJson,
            EngineVersion = "0.0.60",
            Status = status,
            RowVersion = rowVersion
        };
        SetEntityId(template, 1001);
        return template;
    }

    /// <summary>
    /// 模拟 ORM 回填受保护的实体主键。
    /// </summary>
    private static void SetEntityId(SysPrintTemplate template, long id)
    {
        var property = template.GetType().GetProperty(
            nameof(SysPrintTemplate.BasicId),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("未找到实体主键属性。");
        property.SetValue(template, id);
    }

    /// <summary>
    /// 打印模板领域测试依赖集合。
    /// </summary>
    private sealed record DomainFixture(
        PrintTemplateDomainService Service,
        Mock<IPrintTemplateRepository> Repository);
}
