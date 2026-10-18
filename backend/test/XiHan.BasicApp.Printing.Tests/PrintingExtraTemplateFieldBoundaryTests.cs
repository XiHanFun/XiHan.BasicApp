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
/// 打印模板可编辑字段与打印模板 JSON 的边界值测试。
/// </summary>
/// <remarks>
/// 领域服务的长度上限（编码 100、名称 100、备注 500）与实体列长度一一对应：
/// 校验一旦放宽，落库时会被数据库静默截断或直接报驱动级错误。这里逐条钉住"恰好通过"和"多一个字符即拒绝"。
/// </remarks>
public sealed class PrintingExtraTemplateFieldBoundaryTests
{
    private const string ValidTemplateJson = "{\"schemaVersion\":1,\"pages\":[{}]}";

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
            Remark = "  备注  "
        });

        Assert.Equal("ORDER", result.Template.TemplateCode);
        Assert.Equal("system.print-demo", result.Template.DataSourceCode);
        Assert.Equal("订单模板", result.Template.TemplateName);
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
    /// 名称、模板 JSON 为 null 或纯空白时抛的是参数校验异常族（null 走派生的空引用异常）。
    /// </summary>
    /// <param name="templateName">模板名称。</param>
    /// <param name="templateJson">模板 JSON。</param>
    [Theory]
    [InlineData(null, ValidTemplateJson)]
    [InlineData("  ", ValidTemplateJson)]
    [InlineData("订单模板", null)]
    [InlineData("订单模板", "   ")]
    public async Task CreateAsync_BlankRequiredFields_ShouldThrowArgumentFamily(
        string? templateName,
        string? templateJson)
    {
        var fixture = CreateFixture();
        var command = CreateCommand() with
        {
            TemplateName = templateName!,
            TemplateJson = templateJson!
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
    /// 根节点不是对象时拒绝，提示点名根节点。
    /// </summary>
    /// <param name="templateJson">根节点不是对象的模板 JSON。</param>
    [Theory]
    [InlineData("[]")]
    [InlineData("[{\"schemaVersion\":1,\"pages\":[{}]}]")]
    [InlineData("1")]
    [InlineData("\"template\"")]
    [InlineData("null")]
    public async Task CreateAsync_RootNotObject_ShouldReject(string templateJson)
    {
        var fixture = CreateFixture();

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.CreateAsync(CreateCommand() with { TemplateJson = templateJson }));

        Assert.Contains("根节点必须是对象", exception.Message, StringComparison.Ordinal);
        VerifyNoWrite(fixture);
    }

    /// <summary>
    /// schemaVersion 缺失或不是数字 1 时拒绝，提示点名 schemaVersion。
    /// </summary>
    /// <param name="templateJson">schemaVersion 异常的模板 JSON。</param>
    /// <param name="expectedMessage">提示中必须出现的片段。</param>
    [Theory]
    [InlineData("{\"pages\":[{}]}", "缺少 schemaVersion 字段")]
    [InlineData("{\"SchemaVersion\":1,\"pages\":[{}]}", "缺少 schemaVersion 字段")]
    [InlineData("{\"schemaVersion\":2,\"pages\":[{}]}", "不支持的打印模板格式版本：schemaVersion 必须是数字 1")]
    [InlineData("{\"schemaVersion\":0,\"pages\":[{}]}", "不支持的打印模板格式版本：schemaVersion 必须是数字 1")]
    [InlineData("{\"schemaVersion\":1.5,\"pages\":[{}]}", "不支持的打印模板格式版本：schemaVersion 必须是数字 1")]
    [InlineData("{\"schemaVersion\":\"1\",\"pages\":[{}]}", "不支持的打印模板格式版本：schemaVersion 必须是数字 1")]
    [InlineData("{\"schemaVersion\":null,\"pages\":[{}]}", "不支持的打印模板格式版本：schemaVersion 必须是数字 1")]
    [InlineData("{\"schemaVersion\":true,\"pages\":[{}]}", "不支持的打印模板格式版本：schemaVersion 必须是数字 1")]
    [InlineData("{\"schemaVersion\":[1],\"pages\":[{}]}", "不支持的打印模板格式版本：schemaVersion 必须是数字 1")]
    public async Task CreateAsync_BrokenSchemaVersion_ShouldNameSchemaVersion(string templateJson, string expectedMessage)
    {
        var fixture = CreateFixture();

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.CreateAsync(CreateCommand() with { TemplateJson = templateJson }));

        Assert.Contains(expectedMessage, exception.Message, StringComparison.Ordinal);
        VerifyNoWrite(fixture);
    }

    /// <summary>
    /// schemaVersion 写成 1.0 时与数字 1 等值，照常保存且正文不变。
    /// </summary>
    [Fact]
    public async Task CreateAsync_SchemaVersionWrittenAsOnePointZero_ShouldBeAccepted()
    {
        const string TemplateJson = "{\"schemaVersion\":1.0,\"pages\":[{}]}";
        var fixture = CreateFixture();

        var result = await fixture.Service.CreateAsync(CreateCommand() with { TemplateJson = TemplateJson });

        Assert.Equal(TemplateJson, result.Template.TemplateJson);
    }

    /// <summary>
    /// pages 缺失、不是数组或为空数组时拒绝，提示点名 pages。
    /// </summary>
    /// <param name="templateJson">pages 异常的模板 JSON。</param>
    /// <param name="expectedMessage">提示中必须出现的片段。</param>
    [Theory]
    [InlineData("{\"schemaVersion\":1}", "缺少 pages 字段")]
    [InlineData("{\"schemaVersion\":1,\"Pages\":[{}]}", "缺少 pages 字段")]
    [InlineData("{\"schemaVersion\":1,\"pages\":{}}", "pages 必须是数组")]
    [InlineData("{\"schemaVersion\":1,\"pages\":null}", "pages 必须是数组")]
    [InlineData("{\"schemaVersion\":1,\"pages\":1}", "pages 必须是数组")]
    [InlineData("{\"schemaVersion\":1,\"pages\":\"[{}]\"}", "pages 必须是数组")]
    [InlineData("{\"schemaVersion\":1,\"pages\":[]}", "pages 至少要有一页")]
    public async Task CreateAsync_BrokenPages_ShouldNamePages(string templateJson, string expectedMessage)
    {
        var fixture = CreateFixture();

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.CreateAsync(CreateCommand() with { TemplateJson = templateJson }));

        Assert.Contains(expectedMessage, exception.Message, StringComparison.Ordinal);
        VerifyNoWrite(fixture);
    }

    /// <summary>
    /// pages 逐项检查：任一页不是对象就整体拒绝，提示点名 pages 及出错的页序号。
    /// </summary>
    /// <param name="templateJson">含非对象页的模板 JSON。</param>
    /// <param name="expectedMessage">提示中必须出现的片段。</param>
    [Theory]
    [InlineData("{\"schemaVersion\":1,\"pages\":[null]}", "pages 第 1 项必须是对象")]
    [InlineData("{\"schemaVersion\":1,\"pages\":[{},null]}", "pages 第 2 项必须是对象")]
    [InlineData("{\"schemaVersion\":1,\"pages\":[{},1]}", "pages 第 2 项必须是对象")]
    [InlineData("{\"schemaVersion\":1,\"pages\":[{},\"page\"]}", "pages 第 2 项必须是对象")]
    [InlineData("{\"schemaVersion\":1,\"pages\":[{},[]]}", "pages 第 2 项必须是对象")]
    [InlineData("{\"schemaVersion\":1,\"pages\":[{},{},true]}", "pages 第 3 项必须是对象")]
    public async Task CreateAsync_PageNotObject_ShouldNamePages(string templateJson, string expectedMessage)
    {
        var fixture = CreateFixture();

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.CreateAsync(CreateCommand() with { TemplateJson = templateJson }));

        Assert.Contains(expectedMessage, exception.Message, StringComparison.Ordinal);
        VerifyNoWrite(fixture);
    }

    /// <summary>
    /// 多页且带额外字段的合法模板照常保存，模板正文原样保留（不做任何美化或重排）。
    /// </summary>
    [Fact]
    public async Task CreateAsync_MultiPageTemplateWithExtraFields_ShouldBeStoredVerbatim()
    {
        const string TemplateJson = "{ \"schemaVersion\": 1, \"name\": \"订单\", \"pages\": [{\"id\":\"p1\",\"elements\":[{\"type\":\"text\",\"field\":\"title\"}]}, {}], \"meta\": {\"author\":\"x\"}, \"extensions\": {\"pro\":{}}, \"extra\": 1 }";
        var fixture = CreateFixture();

        var result = await fixture.Service.CreateAsync(CreateCommand() with { TemplateJson = TemplateJson });

        Assert.Equal(TemplateJson, result.Template.TemplateJson);
    }

    /// <summary>
    /// JSON 语法错误与结构错误走不同提示：语法错误保留原始 <see cref="System.Text.Json.JsonException"/> 作为内部异常，便于排障。
    /// </summary>
    [Fact]
    public async Task CreateAsync_MalformedJson_ShouldKeepParserExceptionAsInner()
    {
        var fixture = CreateFixture();

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.CreateAsync(CreateCommand() with { TemplateJson = "{\"schemaVersion\":1,\"pages\":[" }));

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
