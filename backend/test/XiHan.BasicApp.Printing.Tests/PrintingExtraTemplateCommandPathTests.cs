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
/// 打印模板更新、启停与删除三条命令路径的领域不变量测试。
/// </summary>
/// <remarks>
/// 三条路径共用 <c>GetEditableOrThrowAsync</c> + <c>EnsureExpectedVersion</c> 前置：
/// 作用域取的是当前租户上下文，平台态取 0。这意味着"租户拿着全局模板主键来改"会因为
/// 所属租户对不上而查不到，从而被当作不存在拒绝——这是跨租户写越权的最后一道闸，必须逐条锁住。
/// </remarks>
public sealed class PrintingExtraTemplateCommandPathTests
{
    private const string ValidTemplateJson = "{\"panels\":[{\"printElements\":[]}]}";

    /// <summary>
    /// 平台态创建的是全局模板：租户号落 0，且 AllowTenantUse 按命令原样保留。
    /// </summary>
    [Fact]
    public async Task CreateAsync_PlatformContextShouldKeepGlobalOpenFlag()
    {
        var fixture = CreateFixture(tenantId: null);

        var result = await fixture.Service.CreateAsync(new PrintTemplateCreateCommand(
            "ORDER", "system.print-demo", "订单模板", ValidTemplateJson, "0.0.60", true, EnableStatus.Enabled, 10, null));

        Assert.Equal(0, result.Template.TenantId);
        Assert.True(result.Template.AllowTenantUse);
        Assert.True(result.Template.IsGlobal);
    }

    /// <summary>
    /// 主键非正数时不查库直接拒绝，三条命令路径口径一致。
    /// </summary>
    /// <param name="id">非法主键。</param>
    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    [InlineData(long.MinValue)]
    public async Task Commands_NonPositiveId_ShouldRejectWithoutLookup(long id)
    {
        var fixture = CreateFixture(tenantId: 7);

        var update = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.UpdateAsync(UpdateCommand(id, 0)));
        var status = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.UpdateStatusAsync(new PrintTemplateStatusChangeCommand(id, 0, EnableStatus.Disabled, null)));
        var delete = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.DeleteAsync(new PrintTemplateDeleteCommand(id, 0)));

        Assert.Contains("主键必须大于 0", update.Message, StringComparison.Ordinal);
        Assert.Contains("主键必须大于 0", status.Message, StringComparison.Ordinal);
        Assert.Contains("主键必须大于 0", delete.Message, StringComparison.Ordinal);
        fixture.Repository.Verify(
            repository => repository.FindByIdInScopeAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 当前作用域查不到的模板一律当作不存在，租户不会因为主键正确就改到别人的模板。
    /// </summary>
    [Fact]
    public async Task UpdateAsync_TemplateOutsideScope_ShouldRejectAsNotFound()
    {
        var fixture = CreateFixture(tenantId: 7);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.UpdateAsync(UpdateCommand(1001, 0)));

        Assert.Contains("不属于当前作用域", exception.Message, StringComparison.Ordinal);
        fixture.Repository.Verify(
            repository => repository.FindByIdInScopeAsync(7, 1001, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 领域服务向仓储传入的所属租户就是当前租户上下文，平台态传 0。
    /// </summary>
    /// <param name="tenantId">当前租户；null 表示平台。</param>
    /// <param name="expectedOwnerTenantId">期望传给仓储的所属租户。</param>
    [Theory]
    [InlineData(null, 0L)]
    [InlineData(7L, 7L)]
    public async Task UpdateAsync_ShouldQueryWithCurrentScopeOwner(long? tenantId, long expectedOwnerTenantId)
    {
        var fixture = CreateFixture(tenantId);

        _ = await Assert.ThrowsAsync<UserFriendlyException>(() => fixture.Service.UpdateAsync(UpdateCommand(1001, 0)));

        fixture.Repository.Verify(
            repository => repository.FindByIdInScopeAsync(expectedOwnerTenantId, 1001, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 负数行版本是非法输入，即便与实体版本"凑巧"无关也要按并发冲突拒绝。
    /// </summary>
    [Fact]
    public async Task UpdateAsync_NegativeRowVersion_ShouldReject()
    {
        var existing = CreateTemplate(tenantId: 7, rowVersion: 0);
        var fixture = CreateFixture(tenantId: 7, existing);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.UpdateAsync(UpdateCommand(existing.BasicId, -1)));

        Assert.Contains("其他用户修改", exception.Message, StringComparison.Ordinal);
        VerifyNoUpdate(fixture);
    }

    /// <summary>
    /// 租户私有模板不得通过更新命令挂上全局开放标记，AllowTenantUse 恒为 false。
    /// </summary>
    [Fact]
    public async Task UpdateAsync_TenantTemplateShouldNotGainGlobalOpenFlag()
    {
        var existing = CreateTemplate(tenantId: 7, rowVersion: 3);
        var fixture = CreateFixture(tenantId: 7, existing);

        var result = await fixture.Service.UpdateAsync(UpdateCommand(existing.BasicId, 3) with { AllowTenantUse = true });

        Assert.False(result.Template.AllowTenantUse);
    }

    /// <summary>
    /// 全局模板的开放标记由更新命令决定，平台可以随时收回租户可用性。
    /// </summary>
    /// <param name="allowTenantUse">命令中的开放标记。</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UpdateAsync_GlobalTemplateShouldFollowCommandOpenFlag(bool allowTenantUse)
    {
        var existing = CreateTemplate(tenantId: 0, rowVersion: 3);
        existing.AllowTenantUse = !allowTenantUse;
        var fixture = CreateFixture(tenantId: null, existing);

        var result = await fixture.Service.UpdateAsync(
            UpdateCommand(existing.BasicId, 3) with { AllowTenantUse = allowTenantUse });

        Assert.Equal(allowTenantUse, result.Template.AllowTenantUse);
    }

    /// <summary>
    /// 更新不允许改动模板编码：命令里根本没有编码字段，实体编码必须原样保留。
    /// </summary>
    [Fact]
    public async Task UpdateAsync_ShouldKeepTemplateCodeImmutable()
    {
        var existing = CreateTemplate(tenantId: 7, rowVersion: 3);
        var fixture = CreateFixture(tenantId: 7, existing);

        var result = await fixture.Service.UpdateAsync(UpdateCommand(existing.BasicId, 3) with { TemplateName = "改名" });

        Assert.Equal("ORDER", result.Template.TemplateCode);
        Assert.Equal("改名", result.Template.TemplateName);
    }

    /// <summary>
    /// 更新时切换到未注册的数据源必须被拒绝，且拒绝发生在写入仓储之前。
    /// </summary>
    [Fact]
    public async Task UpdateAsync_UnregisteredDataSource_ShouldRejectBeforeWrite()
    {
        var existing = CreateTemplate(tenantId: 7, rowVersion: 3);
        var fixture = CreateFixture(tenantId: 7, existing);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.UpdateAsync(UpdateCommand(existing.BasicId, 3) with { DataSourceCode = "ghost.source" }));

        Assert.Contains("未注册", exception.Message, StringComparison.Ordinal);
        VerifyNoUpdate(fixture);
    }

    /// <summary>
    /// 启停命令在版本匹配时写入目标状态，并把实体交回仓储更新。
    /// </summary>
    /// <param name="from">变更前状态。</param>
    /// <param name="to">目标状态。</param>
    [Theory]
    [InlineData(EnableStatus.Enabled, EnableStatus.Disabled)]
    [InlineData(EnableStatus.Disabled, EnableStatus.Enabled)]
    public async Task UpdateStatusAsync_ShouldApplyTargetStatus(EnableStatus from, EnableStatus to)
    {
        var existing = CreateTemplate(tenantId: 7, rowVersion: 3, status: from);
        var fixture = CreateFixture(tenantId: 7, existing);

        var result = await fixture.Service.UpdateStatusAsync(
            new PrintTemplateStatusChangeCommand(existing.BasicId, 3, to, null));

        Assert.Equal(to, result.Template.Status);
        fixture.Repository.Verify(
            repository => repository.UpdateAsync(existing, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 启停命令的备注为空时保留原备注，不得把历史说明冲成 null。
    /// </summary>
    /// <param name="remark">命令备注。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateStatusAsync_BlankRemark_ShouldKeepExistingRemark(string? remark)
    {
        var existing = CreateTemplate(tenantId: 7, rowVersion: 3);
        existing.Remark = "原始备注";
        var fixture = CreateFixture(tenantId: 7, existing);

        var result = await fixture.Service.UpdateStatusAsync(
            new PrintTemplateStatusChangeCommand(existing.BasicId, 3, EnableStatus.Disabled, remark));

        Assert.Equal("原始备注", result.Template.Remark);
    }

    /// <summary>
    /// 启停命令给出非空备注时覆盖原备注，并去掉首尾空白。
    /// </summary>
    [Fact]
    public async Task UpdateStatusAsync_NewRemark_ShouldOverwriteTrimmed()
    {
        var existing = CreateTemplate(tenantId: 7, rowVersion: 3);
        existing.Remark = "原始备注";
        var fixture = CreateFixture(tenantId: 7, existing);

        var result = await fixture.Service.UpdateStatusAsync(
            new PrintTemplateStatusChangeCommand(existing.BasicId, 3, EnableStatus.Disabled, "  停用整改  "));

        Assert.Equal("停用整改", result.Template.Remark);
    }

    /// <summary>
    /// 启停命令的状态枚举与备注长度先于查库校验，非法输入不产生任何数据库访问。
    /// </summary>
    [Fact]
    public async Task UpdateStatusAsync_InvalidInput_ShouldRejectBeforeLookup()
    {
        var fixture = CreateFixture(tenantId: 7);

        var status = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.UpdateStatusAsync(new PrintTemplateStatusChangeCommand(1001, 0, (EnableStatus)9, null)));
        var remark = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.UpdateStatusAsync(
                new PrintTemplateStatusChangeCommand(1001, 0, EnableStatus.Disabled, new string('备', 501))));

        Assert.Contains("模板状态无效", status.Message, StringComparison.Ordinal);
        Assert.Contains("模板备注不能超过 500 个字符", remark.Message, StringComparison.Ordinal);
        fixture.Repository.Verify(
            repository => repository.FindByIdInScopeAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 启停命令同样受乐观并发保护，版本不匹配时不落库。
    /// </summary>
    [Fact]
    public async Task UpdateStatusAsync_StaleRowVersion_ShouldReject()
    {
        var existing = CreateTemplate(tenantId: 7, rowVersion: 8);
        var fixture = CreateFixture(tenantId: 7, existing);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.UpdateStatusAsync(
                new PrintTemplateStatusChangeCommand(existing.BasicId, 7, EnableStatus.Disabled, null)));

        Assert.Contains("其他用户修改", exception.Message, StringComparison.Ordinal);
        VerifyNoUpdate(fixture);
    }

    /// <summary>
    /// 删除命令同样受乐观并发保护，且校验顺序是"先版本、后启停状态"。
    /// </summary>
    [Fact]
    public async Task DeleteAsync_StaleRowVersionOnEnabledTemplate_ShouldReportConcurrencyFirst()
    {
        var existing = CreateTemplate(tenantId: 7, rowVersion: 8, status: EnableStatus.Enabled);
        var fixture = CreateFixture(tenantId: 7, existing);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.DeleteAsync(new PrintTemplateDeleteCommand(existing.BasicId, 1)));

        Assert.Contains("其他用户修改", exception.Message, StringComparison.Ordinal);
        fixture.Repository.Verify(
            repository => repository.DeleteAsync(It.IsAny<SysPrintTemplate>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 仓储报告"未删除任何行"时必须转成可读的刷新重试提示，而不是当作成功返回。
    /// </summary>
    [Fact]
    public async Task DeleteAsync_RepositoryReturnsFalse_ShouldRaiseRetryMessage()
    {
        var existing = CreateTemplate(tenantId: 7, rowVersion: 3, status: EnableStatus.Disabled);
        var fixture = CreateFixture(tenantId: 7, existing);
        fixture.Repository
            .Setup(repository => repository.DeleteAsync(It.IsAny<SysPrintTemplate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.DeleteAsync(new PrintTemplateDeleteCommand(existing.BasicId, 3)));

        Assert.Contains("删除失败", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 三条命令路径都必须以空命令的 <see cref="ArgumentNullException"/> 开场。
    /// </summary>
    [Fact]
    public async Task Commands_NullCommand_ShouldThrowArgumentNull()
    {
        var fixture = CreateFixture(tenantId: 7);

        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => fixture.Service.UpdateAsync(null!));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => fixture.Service.UpdateStatusAsync(null!));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => fixture.Service.DeleteAsync(null!));
    }

    /// <summary>
    /// 三条命令路径都必须在取消令牌已触发时立刻停下，不产生任何数据库访问。
    /// </summary>
    [Fact]
    public async Task Commands_CancelledToken_ShouldThrowBeforeLookup()
    {
        var existing = CreateTemplate(tenantId: 7, rowVersion: 3, status: EnableStatus.Disabled);
        var fixture = CreateFixture(tenantId: 7, existing);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => fixture.Service.UpdateAsync(UpdateCommand(existing.BasicId, 3), cancellation.Token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => fixture.Service.UpdateStatusAsync(
                new PrintTemplateStatusChangeCommand(existing.BasicId, 3, EnableStatus.Enabled, null), cancellation.Token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => fixture.Service.DeleteAsync(new PrintTemplateDeleteCommand(existing.BasicId, 3), cancellation.Token));

        fixture.Repository.Verify(
            repository => repository.FindByIdInScopeAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 取消令牌必须原样透传到仓储调用，取消才能在真正的数据库等待处生效。
    /// </summary>
    [Fact]
    public async Task DeleteAsync_ShouldForwardCancellationTokenToRepository()
    {
        var existing = CreateTemplate(tenantId: 7, rowVersion: 3, status: EnableStatus.Disabled);
        var fixture = CreateFixture(tenantId: 7, existing);
        using var cancellation = new CancellationTokenSource();

        await fixture.Service.DeleteAsync(new PrintTemplateDeleteCommand(existing.BasicId, 3), cancellation.Token);

        fixture.Repository.Verify(
            repository => repository.FindByIdInScopeAsync(7, existing.BasicId, cancellation.Token),
            Times.Once);
        fixture.Repository.Verify(
            repository => repository.DeleteAsync(existing, cancellation.Token),
            Times.Once);
    }

    /// <summary>
    /// 断言更新路径没有触达仓储写入。
    /// </summary>
    private static void VerifyNoUpdate(CommandFixture fixture)
    {
        fixture.Repository.Verify(
            repository => repository.UpdateAsync(It.IsAny<SysPrintTemplate>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 构造一条各字段合法的更新命令。
    /// </summary>
    private static PrintTemplateUpdateCommand UpdateCommand(long id, long expectedRowVersion)
    {
        return new PrintTemplateUpdateCommand(
            id, expectedRowVersion, "system.print-demo", "订单模板", ValidTemplateJson, "0.0.60", false, 10, null);
    }

    /// <summary>
    /// 创建领域服务夹具；仓储只在所属租户与当前上下文一致时返回既有模板。
    /// </summary>
    /// <param name="tenantId">当前租户；null 表示平台上下文。</param>
    /// <param name="existing">当前作用域内的既有模板。</param>
    private static CommandFixture CreateFixture(long? tenantId, SysPrintTemplate? existing = null)
    {
        var ownerTenantId = tenantId ?? 0;
        var repository = new Mock<IPrintTemplateRepository>();
        repository
            .Setup(value => value.FindByIdInScopeAsync(ownerTenantId, It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((long _, long id, CancellationToken _) => existing is not null && existing.BasicId == id ? existing : null);
        repository
            .Setup(value => value.FindByCodeInScopeAsync(
                It.IsAny<long>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysPrintTemplate?)null);
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
        return new CommandFixture(service, repository);
    }

    /// <summary>
    /// 创建指定租户、版本与状态的模板实体。
    /// </summary>
    private static SysPrintTemplate CreateTemplate(
        long tenantId,
        long rowVersion,
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
    /// 模拟 ORM 回填实体主键。
    /// </summary>
    private static void SetEntityId(SysPrintTemplate template, long id)
    {
        typeof(SysPrintTemplate)
            .GetProperty(nameof(SysPrintTemplate.BasicId), BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(template, id);
    }

    /// <summary>
    /// 命令路径测试依赖集合。
    /// </summary>
    /// <param name="Service">被测领域服务。</param>
    /// <param name="Repository">仓储替身。</param>
    private sealed record CommandFixture(
        PrintTemplateDomainService Service,
        Mock<IPrintTemplateRepository> Repository);
}
