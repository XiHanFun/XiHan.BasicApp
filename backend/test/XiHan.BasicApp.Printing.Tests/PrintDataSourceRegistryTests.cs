// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using XiHan.BasicApp.Printing.Domain.DataSources;
using XiHan.BasicApp.Printing.Domain.DomainServices;
using XiHan.BasicApp.Printing.Domain.Repositories;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Printing.Tests;

/// <summary>
/// 打印数据源注册表与模板写路径数据源校验测试。
/// </summary>
public sealed class PrintDataSourceRegistryTests
{
    private const string ValidTemplateJson = "{\"panels\":[{\"printElements\":[]}]}";

    /// <summary>
    /// 重复编码注册必须抛出，后注册模块不得静默覆盖既有字段契约。
    /// </summary>
    [Fact]
    public void Register_DuplicateCode_ShouldThrow()
    {
        var registry = new PrintDataSourceRegistry([]);
        registry.Register(CreateDefinition("erp.order"));

        _ = Assert.Throws<InvalidOperationException>(() => registry.Register(CreateDefinition("erp.order")));
    }

    /// <summary>
    /// 非法字段契约（未知类型/重复字段/无列明细表）在注册时立即失败。
    /// </summary>
    [Fact]
    public void Register_InvalidFieldContract_ShouldThrow()
    {
        var registry = new PrintDataSourceRegistry([]);

        _ = Assert.Throws<ArgumentException>(() => registry.Register(new PrintDataSourceDefinition(
            "bad.kind", "非法类型", [new("f", "字段", "video")], "{}")));
        _ = Assert.Throws<ArgumentException>(() => registry.Register(new PrintDataSourceDefinition(
            "bad.dup", "重复字段", [new("f", "字段一"), new("f", "字段二")], "{}")));
        _ = Assert.Throws<ArgumentException>(() => registry.Register(new PrintDataSourceDefinition(
            "bad.table", "空列明细", [new("items", "明细", "table")], "{}")));
    }

    /// <summary>
    /// DI 登记项在构造时统一收纳，目录按编码排序输出。
    /// </summary>
    [Fact]
    public void Constructor_ShouldIngestRegistrationsSorted()
    {
        var registry = new PrintDataSourceRegistry(
        [
            new PrintDataSourceRegistration(CreateDefinition("wms.pick")),
            new PrintDataSourceRegistration(CreateDefinition("erp.order"))
        ]);

        Assert.Equal(["erp.order", "wms.pick"], registry.GetAll().Select(s => s.Code));
        Assert.True(registry.IsRegistered("erp.order"));
        Assert.False(registry.IsRegistered("crm.quote"));
    }

    /// <summary>
    /// 模板绑定未注册数据源必须被写路径拒绝；自由模板（空数据源）不受影响。
    /// </summary>
    [Fact]
    public async Task CreateTemplate_UnregisteredDataSource_ShouldReject()
    {
        var repository = new Mock<IPrintTemplateRepository>();
        var currentTenant = new Mock<ICurrentTenant>();
        currentTenant.SetupGet(value => value.Id).Returns(7);
        var registry = new PrintDataSourceRegistry([new PrintDataSourceRegistration(BuiltInPrintDataSources.SystemPrintDemo)]);
        var service = new PrintTemplateDomainService(
            repository.Object,
            currentTenant.Object,
            registry,
            NullLogger<PrintTemplateDomainService>.Instance);

        var command = new PrintTemplateCreateCommand(
            "ORDER",
            "ghost.source",
            "订单模板",
            ValidTemplateJson,
            "0.0.60",
            false,
            XiHan.BasicApp.Saas.Domain.Enums.EnableStatus.Enabled,
            10,
            null);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() => service.CreateAsync(command));

        Assert.Contains("未注册", exception.Message, StringComparison.Ordinal);
        repository.Verify(
            value => value.AddAsync(It.IsAny<Domain.Entities.SysPrintTemplate>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 创建最小合法数据源定义。
    /// </summary>
    private static PrintDataSourceDefinition CreateDefinition(string code)
    {
        return new PrintDataSourceDefinition(code, $"数据源 {code}", [new("title", "标题")], "{\"title\":\"示例\"}");
    }
}
