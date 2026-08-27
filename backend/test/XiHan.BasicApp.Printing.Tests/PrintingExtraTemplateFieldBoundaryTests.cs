// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Reflection;
using XiHan.BasicApp.Printing.Domain.DataSources;
using XiHan.BasicApp.Printing.Domain.DomainServices;
using XiHan.BasicApp.Printing.Domain.Entities;
using XiHan.BasicApp.Printing.Domain.Repositories;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Printing.Tests;

/// <summary>
/// 打印模板可编辑字段与 hiprint 模板 JSON 的边界值测试。
/// </summary>
/// <remarks>
/// 领域服务的长度上限（编码 100、名称 100、引擎版本 32、备注 500）与实体列长度一一对应：
/// 校验一旦放宽，落库时会被数据库静默截断或直接报驱动级错误。这里逐条钉住"恰好通过"和"多一个字符即拒绝"。
/// </remarks>
public sealed class PrintingExtraTemplateFieldBoundaryTests
{
    private const string ValidTemplateJson = "{\"panels\":[{\"printElements\":[]}]}";

    /// <summary>
    /// 模板编码在写入前要去掉首尾空白，规范化结果才是唯一索引与缓存键的实际取值。
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldTrimCodesAndNames()
    {
        var fixture = CreateFixture();

        var result = await fixture.Service.CreateAsync(CreateCommand() with
        {
            TemplateCode = "  ORDER  ",
            DataSourceCode = "  system.print-demo  ",
            TemplateName = "  订单模板  ",
            EngineVersion = "  0.0.60  ",
            Remark = "  备注  "
        });

        Assert.Equal("ORDER", result.Template.TemplateCode);
        Assert.Equal("system.print-demo", result.Template.DataSourceCode);
        Assert.Equal("订单模板", result.Template.TemplateName);
        Assert.Equal("0.0.60", result.Template.EngineVersion);
        Assert.Equal("备注", result.Template.Remark);
    }

    /// <summary>
    /// 编码内部的空白字符会让路由、缓存键与调用契约产生歧义，模板编码与数据源编码都必须拒绝。
    /// </summary>
    /// <param name="templateCode">模板编码。</param>
    /// <param name="dataSourceCode">数据源编码。</param>
    [Theory]
    [InlineData("OR DER", "system.print-demo")]
    [InlineData("OR\tDER", "system.print-demo")]
    [InlineData("ORDER", "system print-demo")]
    public async Task CreateAsync_CodeWithInnerWhitespace_ShouldReject(string templateCode, string dataSourceCode)
    {
        var fixture = CreateFixture();
        var command = CreateCommand() with { TemplateCode = templateCode, DataSourceCode = dataSourceCode };

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() => fixture.Service.CreateAsync(command));

        Assert.Contains("不能包含空白字符", exception.Message, StringComparison.Ordinal);
        VerifyNoWrite(fixture);
    }

    /// <summary>
    /// 模板编码上限 100：恰好 100 通过，101 拒绝且提示带出上限数值。
    /// </summary>
    [Fact]
    public async Task CreateAsync_TemplateCodeLength_ShouldStopAtHundred()
    {
        var fixture = CreateFixture();

        var accepted = await fixture.Service.CreateAsync(CreateCommand() with { TemplateCode = new string('A', 100) });
        Assert.Equal(100, accepted.Template.TemplateCode.Length);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.CreateAsync(CreateCommand() with { TemplateCode = new string('A', 101) }));
        Assert.Contains("模板编码不能超过 100 个字符", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 数据源编码越界时的提示必须点名"数据源编码"，否则前端无法定位是哪个字段超长。
    /// </summary>
    [Fact]
    public async Task CreateAsync_DataSourceCodeTooLong_ShouldNameTheField()
    {
        var fixture = CreateFixture();

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.CreateAsync(CreateCommand() with { DataSourceCode = new string('a', 101) }));

        Assert.Contains("数据源编码不能超过 100 个字符", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 模板名称上限 100，判定按去空白后的长度进行。
    /// </summary>
    [Fact]
    public async Task CreateAsync_TemplateNameLength_ShouldStopAtHundred()
    {
        var fixture = CreateFixture();

        var accepted = await fixture.Service.CreateAsync(CreateCommand() with { TemplateName = new string('名', 100) });
        Assert.Equal(100, accepted.Template.TemplateName.Length);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.CreateAsync(CreateCommand() with { TemplateName = new string('名', 101) }));
        Assert.Contains("模板名称不能超过 100 个字符", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 引擎版本上限 32，超长必须拒绝而不是留给数据库截断。
    /// </summary>
    [Fact]
    public async Task CreateAsync_EngineVersionLength_ShouldStopAtThirtyTwo()
    {
        var fixture = CreateFixture();

        var accepted = await fixture.Service.CreateAsync(CreateCommand() with { EngineVersion = new string('9', 32) });
        Assert.Equal(32, accepted.Template.EngineVersion.Length);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.CreateAsync(CreateCommand() with { EngineVersion = new string('9', 33) }));
        Assert.Contains("打印引擎版本不能超过 32 个字符", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 备注上限 500；恰好 500 通过，501 拒绝。
    /// </summary>
    [Fact]
    public async Task CreateAsync_RemarkLength_ShouldStopAtFiveHundred()
    {
        var fixture = CreateFixture();

        var accepted = await fixture.Service.CreateAsync(CreateCommand() with { Remark = new string('备', 500) });
        Assert.Equal(500, accepted.Template.Remark!.Length);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.CreateAsync(CreateCommand() with { Remark = new string('备', 501) }));
        Assert.Contains("模板备注不能超过 500 个字符", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 纯空白备注等同于没有备注，必须归一为 null 而不是落库一个空串。
    /// </summary>
    /// <param name="remark">备注输入。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_BlankRemark_ShouldNormalizeToNull(string? remark)
    {
        var fixture = CreateFixture();

        var result = await fixture.Service.CreateAsync(CreateCommand() with { Remark = remark });

        Assert.Null(result.Template.Remark);
    }

    /// <summary>
    /// 排序不允许为负数；0 是合法的最小值。
    /// </summary>
    [Fact]
    public async Task CreateAsync_NegativeSort_ShouldRejectAndZeroShouldPass()
    {
        var fixture = CreateFixture();

        var accepted = await fixture.Service.CreateAsync(CreateCommand() with { Sort = 0 });
        Assert.Equal(0, accepted.Template.Sort);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.CreateAsync(CreateCommand() with { Sort = -1 }));
        Assert.Contains("模板排序不能小于 0", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 未定义的状态枚举值必须被拒绝，避免落库一个前端无法翻译的状态数字。
    /// </summary>
    [Fact]
    public async Task CreateAsync_UndefinedStatus_ShouldReject()
    {
        var fixture = CreateFixture();

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.CreateAsync(CreateCommand() with { Status = (EnableStatus)99 }));

        Assert.Contains("模板状态无效", exception.Message, StringComparison.Ordinal);
        VerifyNoWrite(fixture);
    }

    /// <summary>
    /// 名称、模板 JSON、引擎版本为 null 或纯空白时抛的是参数校验异常族（null 走派生的空引用异常）。
    /// </summary>
    /// <param name="templateName">模板名称。</param>
    /// <param name="templateJson">模板 JSON。</param>
    /// <param name="engineVersion">引擎版本。</param>
    [Theory]
    [InlineData(null, ValidTemplateJson, "0.0.60")]
    [InlineData("  ", ValidTemplateJson, "0.0.60")]
    [InlineData("订单模板", null, "0.0.60")]
    [InlineData("订单模板", "   ", "0.0.60")]
    [InlineData("订单模板", ValidTemplateJson, null)]
    [InlineData("订单模板", ValidTemplateJson, " ")]
    public async Task CreateAsync_BlankRequiredFields_ShouldThrowArgumentFamily(
        string? templateName,
        string? templateJson,
        string? engineVersion)
    {
        var fixture = CreateFixture();
        var command = CreateCommand() with
        {
            TemplateName = templateName!,
            TemplateJson = templateJson!,
            EngineVersion = engineVersion!
        };

        _ = await Assert.ThrowsAnyAsync<ArgumentException>(() => fixture.Service.CreateAsync(command));
        VerifyNoWrite(fixture);
    }

    /// <summary>
    /// 模板编码为 null 或纯空白时同样抛参数校验异常族，而不是走 UserFriendlyException。
    /// </summary>
    /// <param name="templateCode">模板编码。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public async Task CreateAsync_BlankTemplateCode_ShouldThrowArgumentFamily(string? templateCode)
    {
        var fixture = CreateFixture();

        _ = await Assert.ThrowsAnyAsync<ArgumentException>(
            () => fixture.Service.CreateAsync(CreateCommand() with { TemplateCode = templateCode! }));
        VerifyNoWrite(fixture);
    }

    /// <summary>
    /// hiprint 结构校验必须逐个面板检查：只要有一个面板缺少 printElements 数组就整体拒绝。
    /// </summary>
    /// <param name="templateJson">多面板模板 JSON。</param>
    [Theory]
    [InlineData("{\"panels\":[{\"printElements\":[]},{\"printElements\":{}}]}")]
    [InlineData("{\"panels\":[{\"printElements\":[]},{\"name\":\"第二页\"}]}")]
    [InlineData("{\"panels\":[{\"printElements\":[]},null]}")]
    [InlineData("{\"panels\":[{\"printElements\":[]},1]}")]
    [InlineData("{\"panels\":[{\"printElements\":[]},\"panel\"]}")]
    public async Task CreateAsync_SecondPanelBroken_ShouldReject(string templateJson)
    {
        var fixture = CreateFixture();

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.CreateAsync(CreateCommand() with { TemplateJson = templateJson }));

        Assert.Contains("printElements", exception.Message, StringComparison.Ordinal);
        VerifyNoWrite(fixture);
    }

    /// <summary>
    /// 多面板且元素非空的正常模板必须放行，模板正文原样保留（不做任何美化或重排）。
    /// </summary>
    [Fact]
    public async Task CreateAsync_MultiPanelTemplate_ShouldBeStoredVerbatim()
    {
        const string TemplateJson = "{\"panels\":[{\"printElements\":[{\"options\":{\"field\":\"title\"}}]},{\"printElements\":[]}],\"extra\":1}";
        var fixture = CreateFixture();

        var result = await fixture.Service.CreateAsync(CreateCommand() with { TemplateJson = TemplateJson });

        Assert.Equal(TemplateJson, result.Template.TemplateJson);
    }

    /// <summary>
    /// panels 不是数组或为空数组时，提示必须点名 panels 面板而不是笼统的 JSON 错误。
    /// </summary>
    /// <param name="templateJson">panels 结构异常的模板 JSON。</param>
    [Theory]
    [InlineData("{\"panels\":{}}")]
    [InlineData("{\"panels\":null}")]
    [InlineData("{\"panels\":[]}")]
    [InlineData("{\"Panels\":[{\"printElements\":[]}]}")]
    public async Task CreateAsync_BrokenPanelsRoot_ShouldMentionPanels(string templateJson)
    {
        var fixture = CreateFixture();

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.CreateAsync(CreateCommand() with { TemplateJson = templateJson }));

        Assert.Contains("panels", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// JSON 语法错误与结构错误走不同提示：语法错误保留原始 <see cref="System.Text.Json.JsonException"/> 作为内部异常，便于排障。
    /// </summary>
    [Fact]
    public async Task CreateAsync_MalformedJson_ShouldKeepParserExceptionAsInner()
    {
        var fixture = CreateFixture();

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.CreateAsync(CreateCommand() with { TemplateJson = "{\"panels\":[" }));

        Assert.Contains("不是有效的 JSON", exception.Message, StringComparison.Ordinal);
        Assert.IsAssignableFrom<System.Text.Json.JsonException>(exception.InnerException);
    }

    /// <summary>
    /// 空命令必须以 <see cref="ArgumentNullException"/> 拒绝，不允许进入任何后续校验。
    /// </summary>
    [Fact]
    public async Task CreateAsync_NullCommand_ShouldThrowArgumentNull()
    {
        var fixture = CreateFixture();

        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => fixture.Service.CreateAsync(null!));
    }

    /// <summary>
    /// 已取消的令牌必须在任何校验与仓储访问之前生效。
    /// </summary>
    [Fact]
    public async Task CreateAsync_CancelledToken_ShouldThrowBeforeRepositoryAccess()
    {
        var fixture = CreateFixture();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => fixture.Service.CreateAsync(CreateCommand(), cancellation.Token));

        fixture.Repository.Verify(
            repository => repository.FindByCodeInScopeAsync(
                It.IsAny<long>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
        VerifyNoWrite(fixture);
    }

    /// <summary>
    /// 断言校验失败时没有触达仓储写入。
    /// </summary>
    private static void VerifyNoWrite(BoundaryFixture fixture)
    {
        fixture.Repository.Verify(
            repository => repository.AddAsync(It.IsAny<SysPrintTemplate>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 创建租户 7 上下文的领域服务夹具，仓储不存在同编码模板。
    /// </summary>
    private static BoundaryFixture CreateFixture()
    {
        var repository = new Mock<IPrintTemplateRepository>();
        repository
            .Setup(value => value.FindByCodeInScopeAsync(
                It.IsAny<long>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysPrintTemplate?)null);
        repository
            .Setup(value => value.AddAsync(It.IsAny<SysPrintTemplate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysPrintTemplate template, CancellationToken _) =>
            {
                typeof(SysPrintTemplate)
                    .GetProperty(nameof(SysPrintTemplate.BasicId), BindingFlags.Instance | BindingFlags.Public)!
                    .SetValue(template, 1001L);
                return template;
            });

        var currentTenant = new Mock<ICurrentTenant>();
        currentTenant.SetupGet(value => value.Id).Returns(7L);

        var registry = new PrintDataSourceRegistry([new PrintDataSourceRegistration(BuiltInPrintDataSources.SystemPrintDemo)]);
        var service = new PrintTemplateDomainService(
            repository.Object,
            currentTenant.Object,
            registry,
            NullLogger<PrintTemplateDomainService>.Instance);
        return new BoundaryFixture(service, repository);
    }

    /// <summary>
    /// 创建一条各字段都合法的创建命令。
    /// </summary>
    private static PrintTemplateCreateCommand CreateCommand()
    {
        return new PrintTemplateCreateCommand(
            "ORDER",
            "system.print-demo",
            "订单模板",
            ValidTemplateJson,
            "0.0.60",
            false,
            EnableStatus.Enabled,
            10,
            null);
    }

    /// <summary>
    /// 边界值测试依赖集合。
    /// </summary>
    /// <param name="Service">被测领域服务。</param>
    /// <param name="Repository">仓储替身。</param>
    private sealed record BoundaryFixture(
        PrintTemplateDomainService Service,
        Mock<IPrintTemplateRepository> Repository);
}
