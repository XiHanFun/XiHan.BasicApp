// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using System.Reflection;
using XiHan.BasicApp.Printing.Application.Contracts;
using XiHan.BasicApp.Printing.Application.Dtos;
using XiHan.BasicApp.Printing.Application.QueryServices;
using XiHan.BasicApp.Printing.Domain.Entities;
using XiHan.BasicApp.Printing.Domain.Enums;
using XiHan.BasicApp.Printing.Domain.Repositories;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.Domain.Shared.Paging.Models;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Printing.Tests;

/// <summary>
/// 打印模板查询服务的作用域解析、服务端强制条件与全局开放限制测试。
/// </summary>
/// <remarks>
/// 分页路径最关键的一条约定写在源码注释里：租户与开放状态过滤必须在字段安全门控**之后**追加，
/// 前端才无法通过自定义 conditions.filters 把 TenantId 改成别的租户。顺序一旦调换，
/// 客户端提交的同名过滤条件会与服务端条件同时存在甚至先生效，等价于跨租户读越权。
/// 这里用「客户端先塞一个假 TenantId」的用例把该顺序钉死。
/// </remarks>
public sealed class PrintingExtraTemplateQueryScopeTests
{
    private const string TemplateJson = "{\"panels\":[{\"printElements\":[]}]}";

    /// <summary>
    /// 未定义的作用域枚举值在分页与详情两条路径上都必须拒绝。
    /// </summary>
    [Fact]
    public async Task Queries_UndefinedScope_ShouldReject()
    {
        var fixture = CreateFixture(tenantId: 7);

        var page = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.GetPrintTemplatePageAsync(new PrintTemplatePageQueryDto { Scope = (PrintTemplateScope)9 }));
        var detail = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.GetPrintTemplateDetailAsync(1001, (PrintTemplateScope)9));

        Assert.Contains("作用域无效", page.Message, StringComparison.Ordinal);
        Assert.Contains("作用域无效", detail.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 平台上下文没有租户号，查询租户私有模板无从谈起，必须直接拒绝。
    /// </summary>
    [Fact]
    public async Task GetPrintTemplatePageAsync_PlatformTenantScope_ShouldReject()
    {
        var fixture = CreateFixture(tenantId: null);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.GetPrintTemplatePageAsync(new PrintTemplatePageQueryDto { Scope = PrintTemplateScope.Tenant }));

        Assert.Contains("平台上下文不能查询未指定租户的私有打印模板", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 租户的 Auto/Tenant 作用域读自己的模板：所属租户过滤为当前租户，且不追加全局开放限制。
    /// </summary>
    /// <param name="scope">查询作用域。</param>
    [Theory]
    [InlineData(PrintTemplateScope.Auto)]
    [InlineData(PrintTemplateScope.Tenant)]
    public async Task GetPrintTemplatePageAsync_TenantScope_ShouldFilterByCurrentTenantOnly(PrintTemplateScope scope)
    {
        var fixture = CreateFixture(tenantId: 7);

        _ = await fixture.Service.GetPrintTemplatePageAsync(new PrintTemplatePageQueryDto { Scope = scope });

        var conditions = RequireCapturedConditions(fixture);
        Assert.Equal(7L, LastFilterValue(conditions, nameof(SysPrintTemplate.TenantId)));
        Assert.DoesNotContain(conditions.Filters, filter => filter.Field == nameof(SysPrintTemplate.AllowTenantUse));
        fixture.CurrentTenant.Verify(tenant => tenant.Change(It.IsAny<long?>(), It.IsAny<string?>()), Times.Never);
    }

    /// <summary>
    /// 租户读全局模板时必须切到平台上下文，并叠加"已启用且已开放"的不可绕过限制。
    /// </summary>
    [Fact]
    public async Task GetPrintTemplatePageAsync_TenantReadingGlobal_ShouldEnforceOpenAndEnabled()
    {
        var fixture = CreateFixture(tenantId: 7);

        _ = await fixture.Service.GetPrintTemplatePageAsync(new PrintTemplatePageQueryDto { Scope = PrintTemplateScope.Global });

        var conditions = RequireCapturedConditions(fixture);
        Assert.Equal(0L, LastFilterValue(conditions, nameof(SysPrintTemplate.TenantId)));
        Assert.Equal(true, LastFilterValue(conditions, nameof(SysPrintTemplate.AllowTenantUse)));
        Assert.Equal(EnableStatus.Enabled, LastFilterValue(conditions, nameof(SysPrintTemplate.Status)));
        fixture.CurrentTenant.Verify(tenant => tenant.Change(null, It.IsAny<string?>()), Times.Once);
    }

    /// <summary>
    /// 平台自己读全局模板不受开放状态限制，也不需要切换租户上下文。
    /// </summary>
    [Fact]
    public async Task GetPrintTemplatePageAsync_PlatformScope_ShouldSkipOpenRestriction()
    {
        var fixture = CreateFixture(tenantId: null);

        _ = await fixture.Service.GetPrintTemplatePageAsync(new PrintTemplatePageQueryDto { Scope = PrintTemplateScope.Global });

        var conditions = RequireCapturedConditions(fixture);
        Assert.Equal(0L, LastFilterValue(conditions, nameof(SysPrintTemplate.TenantId)));
        Assert.DoesNotContain(conditions.Filters, filter => filter.Field == nameof(SysPrintTemplate.AllowTenantUse));
        fixture.CurrentTenant.Verify(tenant => tenant.Change(It.IsAny<long?>(), It.IsAny<string?>()), Times.Never);
    }

    /// <summary>
    /// 客户端自带的 TenantId 过滤不得改变实际作用域：服务端条件在字段安全门控之后追加，最终生效的是服务端值。
    /// </summary>
    [Fact]
    public async Task GetPrintTemplatePageAsync_ClientTenantFilter_ShouldNotOverrideServerScope()
    {
        var fixture = CreateFixture(tenantId: 7);
        var input = new PrintTemplatePageQueryDto { Scope = PrintTemplateScope.Tenant, Conditions = new QueryConditions() };
        _ = input.Conditions.AddFilter(nameof(SysPrintTemplate.TenantId), 999L, QueryOperator.Equal);

        _ = await fixture.Service.GetPrintTemplatePageAsync(input);

        var conditions = RequireCapturedConditions(fixture);
        Assert.Equal(7L, LastFilterValue(conditions, nameof(SysPrintTemplate.TenantId)));
        fixture.FieldSecurity.Verify(
            security => security.GuardFiltersAsync(It.IsAny<QueryConditions>(), nameof(SysPrintTemplate), It.IsAny<CancellationToken>()),
            Times.Once);
        fixture.FieldSecurity.Verify(
            security => security.GuardSortsAsync(It.IsAny<QueryConditions>(), nameof(SysPrintTemplate), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 调用方未指定排序时补默认排序（先 Sort 后 TemplateCode），保证列表顺序稳定。
    /// </summary>
    [Fact]
    public async Task GetPrintTemplatePageAsync_WithoutSorts_ShouldAppendStableDefaults()
    {
        var fixture = CreateFixture(tenantId: 7);

        _ = await fixture.Service.GetPrintTemplatePageAsync(new PrintTemplatePageQueryDto());

        var conditions = RequireCapturedConditions(fixture);
        Assert.Equal(
            [nameof(SysPrintTemplate.Sort), nameof(SysPrintTemplate.TemplateCode)],
            conditions.Sorts.OrderBy(sort => sort.Priority).Select(sort => sort.Field));
        Assert.All(conditions.Sorts, sort => Assert.Equal(SortDirection.Ascending, sort.Direction));
    }

    /// <summary>
    /// 调用方给了排序时不再追加默认排序，用户的列头排序不会被服务端覆盖。
    /// </summary>
    [Fact]
    public async Task GetPrintTemplatePageAsync_WithClientSorts_ShouldNotAppendDefaults()
    {
        var fixture = CreateFixture(tenantId: 7);
        var input = new PrintTemplatePageQueryDto { Conditions = new QueryConditions() };
        _ = input.Conditions.AddSort(nameof(SysPrintTemplate.TemplateName), SortDirection.Descending, 0);

        _ = await fixture.Service.GetPrintTemplatePageAsync(input);

        var conditions = RequireCapturedConditions(fixture);
        var sort = Assert.Single(conditions.Sorts);
        Assert.Equal(nameof(SysPrintTemplate.TemplateName), sort.Field);
        Assert.Equal(SortDirection.Descending, sort.Direction);
    }

    /// <summary>
    /// 关键字与状态两个查询条件都要落进查询请求，关键字去掉首尾空白。
    /// </summary>
    [Fact]
    public async Task GetPrintTemplatePageAsync_KeywordAndStatus_ShouldEnterRequest()
    {
        var fixture = CreateFixture(tenantId: 7);

        _ = await fixture.Service.GetPrintTemplatePageAsync(new PrintTemplatePageQueryDto
        {
            Keyword = "  订单  ",
            Status = EnableStatus.Disabled
        });

        var conditions = RequireCapturedConditions(fixture);
        Assert.NotNull(conditions.Keyword);
        Assert.Equal(EnableStatus.Disabled, LastFilterValue(conditions, nameof(SysPrintTemplate.Status)));
    }

    /// <summary>
    /// 可用全局模板分页固定读平台数据，并且无论调用方传什么作用域都叠加开放与启用限制。
    /// </summary>
    /// <param name="scope">调用方传入的作用域（应被忽略）。</param>
    [Theory]
    [InlineData(PrintTemplateScope.Auto)]
    [InlineData(PrintTemplateScope.Tenant)]
    [InlineData(PrintTemplateScope.Global)]
    public async Task GetAvailableGlobalPrintTemplatePageAsync_ShouldIgnoreScopeAndForceOpenGlobal(PrintTemplateScope scope)
    {
        var fixture = CreateFixture(tenantId: 7);

        _ = await fixture.Service.GetAvailableGlobalPrintTemplatePageAsync(new PrintTemplatePageQueryDto { Scope = scope });

        var conditions = RequireCapturedConditions(fixture);
        Assert.Equal(0L, LastFilterValue(conditions, nameof(SysPrintTemplate.TenantId)));
        Assert.Equal(true, LastFilterValue(conditions, nameof(SysPrintTemplate.AllowTenantUse)));
        Assert.Equal(EnableStatus.Enabled, LastFilterValue(conditions, nameof(SysPrintTemplate.Status)));
    }

    /// <summary>
    /// 平台调用可用全局模板分页时不需要切换上下文（本就是平台态），限制条件仍然叠加。
    /// </summary>
    [Fact]
    public async Task GetAvailableGlobalPrintTemplatePageAsync_PlatformCaller_ShouldNotSwitchTenant()
    {
        var fixture = CreateFixture(tenantId: null);

        _ = await fixture.Service.GetAvailableGlobalPrintTemplatePageAsync(new PrintTemplatePageQueryDto());

        fixture.CurrentTenant.Verify(tenant => tenant.Change(It.IsAny<long?>(), It.IsAny<string?>()), Times.Never);
        Assert.Equal(true, LastFilterValue(RequireCapturedConditions(fixture), nameof(SysPrintTemplate.AllowTenantUse)));
    }

    /// <summary>
    /// 分页结果按列表项 DTO 投影，行版本以十进制字符串输出。
    /// </summary>
    [Fact]
    public async Task GetPrintTemplatePageAsync_ShouldProjectListItems()
    {
        var template = CreateTemplate(tenantId: 7, allowTenantUse: false, status: EnableStatus.Enabled);
        var fixture = CreateFixture(tenantId: 7, pageItems: [template]);

        var page = await fixture.Service.GetPrintTemplatePageAsync(new PrintTemplatePageQueryDto());

        var item = Assert.Single(page.Items);
        Assert.Equal(template.BasicId, item.BasicId);
        Assert.Equal("ORDER", item.TemplateCode);
        Assert.Equal("4", item.RowVersion);
        Assert.False(item.IsGlobal);
    }

    /// <summary>
    /// 详情主键必须为正数，非法主键不查库直接拒绝。
    /// </summary>
    /// <param name="id">非法主键。</param>
    [Theory]
    [InlineData(0L)]
    [InlineData(-5L)]
    public async Task GetPrintTemplateDetailAsync_NonPositiveId_ShouldReject(long id)
    {
        var fixture = CreateFixture(tenantId: 7);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.GetPrintTemplateDetailAsync(id));

        Assert.Contains("主键必须大于 0", exception.Message, StringComparison.Ordinal);
        fixture.Repository.Verify(
            repository => repository.FindByIdInScopeAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 作用域内查不到模板时返回 null 而不是抛异常，让前端按"不存在"处理。
    /// </summary>
    [Fact]
    public async Task GetPrintTemplateDetailAsync_Missing_ShouldReturnNull()
    {
        var fixture = CreateFixture(tenantId: 7);

        Assert.Null(await fixture.Service.GetPrintTemplateDetailAsync(1001));
    }

    /// <summary>
    /// 租户读取未开放或已停用的全局模板详情必须被拒绝，防止绕过列表限制直接按主键取设计 JSON。
    /// </summary>
    /// <param name="allowTenantUse">全局模板是否开放。</param>
    /// <param name="status">全局模板状态。</param>
    [Theory]
    [InlineData(false, EnableStatus.Enabled)]
    [InlineData(true, EnableStatus.Disabled)]
    [InlineData(false, EnableStatus.Disabled)]
    public async Task GetPrintTemplateDetailAsync_TenantReadingClosedGlobal_ShouldReject(bool allowTenantUse, EnableStatus status)
    {
        var template = CreateTemplate(tenantId: 0, allowTenantUse, status);
        var fixture = CreateFixture(tenantId: 7, existing: template);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.GetPrintTemplateDetailAsync(template.BasicId, PrintTemplateScope.Global));

        Assert.Contains("未向当前租户开放", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 已启用且已开放的全局模板对租户可见，详情带出完整设计 JSON。
    /// </summary>
    [Fact]
    public async Task GetPrintTemplateDetailAsync_TenantReadingOpenGlobal_ShouldReturnDetail()
    {
        var template = CreateTemplate(tenantId: 0, allowTenantUse: true, status: EnableStatus.Enabled);
        var fixture = CreateFixture(tenantId: 7, existing: template);

        var detail = await fixture.Service.GetPrintTemplateDetailAsync(template.BasicId, PrintTemplateScope.Global);

        Assert.NotNull(detail);
        Assert.True(detail.IsGlobal);
        Assert.Equal(TemplateJson, detail.TemplateJson);
    }

    /// <summary>
    /// 平台读自己的全局模板不受开放状态限制，停用模板同样能打开继续编辑。
    /// </summary>
    [Fact]
    public async Task GetPrintTemplateDetailAsync_PlatformReadingClosedGlobal_ShouldReturnDetail()
    {
        var template = CreateTemplate(tenantId: 0, allowTenantUse: false, status: EnableStatus.Disabled);
        var fixture = CreateFixture(tenantId: null, existing: template);

        var detail = await fixture.Service.GetPrintTemplateDetailAsync(template.BasicId);

        Assert.NotNull(detail);
        Assert.Equal(EnableStatus.Disabled, detail.Status);
    }

    /// <summary>
    /// 按编码解析时空编码属于参数错误，抛的是参数校验异常族。
    /// </summary>
    /// <param name="templateCode">模板编码。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetResolvedPrintTemplateByCodeAsync_BlankCode_ShouldThrowArgumentFamily(string? templateCode)
    {
        var fixture = CreateFixture(tenantId: 7);

        _ = await Assert.ThrowsAnyAsync<ArgumentException>(
            () => fixture.Service.GetResolvedPrintTemplateByCodeAsync(templateCode!));
    }

    /// <summary>
    /// 解析器返回空时必须转成一句能指导排查的提示，而不是把 null 丢给前端。
    /// </summary>
    [Fact]
    public async Task GetResolvedPrintTemplateByCodeAsync_Unresolved_ShouldRaiseGuidingMessage()
    {
        var fixture = CreateFixture(tenantId: 7);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(
            () => fixture.Service.GetResolvedPrintTemplateByCodeAsync("ORDER"));

        Assert.Contains("未找到可用的打印模板", exception.Message, StringComparison.Ordinal);
        Assert.Contains("全局开放状态", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 解析成功时原样返回解析器结果，查询服务不做二次改写。
    /// </summary>
    [Fact]
    public async Task GetResolvedPrintTemplateByCodeAsync_Resolved_ShouldPassThrough()
    {
        var fixture = CreateFixture(tenantId: 7);
        var resolved = new ResolvedPrintTemplateDto
        {
            BasicId = 1001,
            TemplateCode = "ORDER",
            ResolvedScope = PrintTemplateScope.Global,
            RequestedScope = PrintTemplateScope.Auto
        };
        fixture.Resolver
            .Setup(resolver => resolver.ResolveAsync("ORDER", PrintTemplateScope.Auto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolved);

        Assert.Same(resolved, await fixture.Service.GetResolvedPrintTemplateByCodeAsync("ORDER"));
    }

    /// <summary>
    /// 三条查询路径都必须在取消令牌已触发时立刻停下。
    /// </summary>
    [Fact]
    public async Task Queries_CancelledToken_ShouldThrowBeforeDataAccess()
    {
        var fixture = CreateFixture(tenantId: 7);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => fixture.Service.GetPrintTemplatePageAsync(new PrintTemplatePageQueryDto(), cancellation.Token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => fixture.Service.GetPrintTemplateDetailAsync(1001, PrintTemplateScope.Auto, cancellation.Token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => fixture.Service.GetResolvedPrintTemplateByCodeAsync("ORDER", PrintTemplateScope.Auto, cancellation.Token));

        fixture.Repository.Verify(
            repository => repository.GetPagedAsync(It.IsAny<PageRequestDtoBase>(), It.IsAny<CancellationToken>()),
            Times.Never);
        fixture.Repository.Verify(
            repository => repository.FindByIdInScopeAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 两个分页方法都必须以空入参的 <see cref="ArgumentNullException"/> 开场。
    /// </summary>
    [Fact]
    public async Task PageQueries_NullInput_ShouldThrowArgumentNull()
    {
        var fixture = CreateFixture(tenantId: 7);

        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => fixture.Service.GetPrintTemplatePageAsync(null!));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(
            () => fixture.Service.GetAvailableGlobalPrintTemplatePageAsync(null!));
    }

    /// <summary>
    /// 取出被仓储接收到的查询条件，缺失时给出可定位的失败消息。
    /// </summary>
    private static QueryConditions RequireCapturedConditions(QueryFixture fixture)
    {
        Assert.True(fixture.CapturedRequests.Count > 0, "仓储没有收到任何分页请求，说明查询在进入仓储前就中断了。");
        return fixture.CapturedRequests[^1].Conditions;
    }

    /// <summary>
    /// 取指定字段上最后一个过滤条件的值（服务端强制条件总是最后追加）。
    /// </summary>
    private static object? LastFilterValue(QueryConditions conditions, string field)
    {
        var filters = conditions.Filters.Where(filter => filter.Field == field).ToList();
        Assert.True(filters.Count > 0, $"查询条件里缺少字段 {field} 的过滤，服务端作用域约束没有生效。");
        return filters[^1].Value;
    }

    /// <summary>
    /// 创建查询服务夹具。
    /// </summary>
    /// <param name="tenantId">当前租户；null 表示平台上下文。</param>
    /// <param name="existing">按主键可查到的模板。</param>
    /// <param name="pageItems">分页返回的模板集合。</param>
    private static QueryFixture CreateFixture(
        long? tenantId,
        SysPrintTemplate? existing = null,
        IList<SysPrintTemplate>? pageItems = null)
    {
        var capturedRequests = new List<PageRequestDtoBase>();
        var repository = new Mock<IPrintTemplateRepository>();
        repository
            .Setup(value => value.FindByIdInScopeAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((long _, long id, CancellationToken _) => existing is not null && existing.BasicId == id ? existing : null);
        repository
            .Setup(value => value.GetPagedAsync(It.IsAny<PageRequestDtoBase>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PageRequestDtoBase request, CancellationToken _) =>
            {
                capturedRequests.Add(request);
                var items = pageItems ?? [];
                return new PageResultDtoBase<SysPrintTemplate>(items, 1, 10, items.Count);
            });

        var resolver = new Mock<IPrintTemplateResolver>();
        resolver
            .Setup(value => value.ResolveAsync(It.IsAny<string>(), It.IsAny<PrintTemplateScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ResolvedPrintTemplateDto?)null);

        var fieldSecurity = new Mock<IFieldSecurityService>();
        fieldSecurity
            .Setup(value => value.GuardFiltersAsync(It.IsAny<QueryConditions>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        fieldSecurity
            .Setup(value => value.GuardSortsAsync(It.IsAny<QueryConditions>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var currentTenant = new Mock<ICurrentTenant>();
        currentTenant.SetupGet(value => value.Id).Returns(tenantId);
        currentTenant
            .Setup(value => value.Change(It.IsAny<long?>(), It.IsAny<string?>()))
            .Returns(Mock.Of<IDisposable>());

        var service = new PrintTemplateQueryService(
            repository.Object,
            resolver.Object,
            fieldSecurity.Object,
            currentTenant.Object);
        return new QueryFixture(service, repository, resolver, fieldSecurity, currentTenant, capturedRequests);
    }

    /// <summary>
    /// 创建查询用模板实体。
    /// </summary>
    private static SysPrintTemplate CreateTemplate(long tenantId, bool allowTenantUse, EnableStatus status)
    {
        var template = new SysPrintTemplate
        {
            TenantId = tenantId,
            TemplateCode = "ORDER",
            DataSourceCode = "system.print-demo",
            TemplateName = "订单模板",
            TemplateJson = TemplateJson,
            EngineVersion = "0.0.60",
            AllowTenantUse = allowTenantUse,
            Status = status,
            RowVersion = 4
        };
        typeof(SysPrintTemplate)
            .GetProperty(nameof(SysPrintTemplate.BasicId), BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(template, 1001L);
        return template;
    }

    /// <summary>
    /// 查询服务测试依赖集合。
    /// </summary>
    /// <param name="Service">被测查询服务。</param>
    /// <param name="Repository">仓储替身。</param>
    /// <param name="Resolver">解析器替身。</param>
    /// <param name="FieldSecurity">字段安全服务替身。</param>
    /// <param name="CurrentTenant">当前租户替身。</param>
    /// <param name="CapturedRequests">仓储实际收到的分页请求。</param>
    private sealed record QueryFixture(
        PrintTemplateQueryService Service,
        Mock<IPrintTemplateRepository> Repository,
        Mock<IPrintTemplateResolver> Resolver,
        Mock<IFieldSecurityService> FieldSecurity,
        Mock<ICurrentTenant> CurrentTenant,
        List<PageRequestDtoBase> CapturedRequests);
}
