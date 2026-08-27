// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using XiHan.BasicApp.Printing.Application.QueryServices;
using XiHan.BasicApp.Printing.Domain.DataSources;
using XiHan.BasicApp.Printing.Domain.Permissions;
using XiHan.Framework.Authorization.Permissions;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Security.Users;

namespace XiHan.BasicApp.Printing.Tests;

/// <summary>
/// 打印数据源目录端点的命令式权限判定与 DTO 投影测试。
/// </summary>
/// <remarks>
/// 该端点是全模块唯一一个把权限判定写进方法体的动态 API：目录同时服务
/// 「模板管理」（print-template:read）与「业务打印」（print-template:use）两条路径，
/// 单个 <c>PermissionAuthorize</c> 表达不了「二者取一」，所以下沉成 <c>IPermissionChecker</c>
/// 的两次短路检查，并在 Api.Tests 的自助端点白名单登记。属性扫描看不见这条判定，
/// 因此「两个都没有则拒绝、任一持有即放行」必须由本文件的用例守住。
/// </remarks>
public sealed class PrintingExtraDataSourceCatalogAccessTests
{
    /// <summary>
    /// 两个权限都不持有时必须拒绝，字段契约与样例数据不对无关账号暴露。
    /// </summary>
    [Fact]
    public async Task GetListAsync_WithoutReadAndUse_ShouldReject()
    {
        var fixture = CreateFixture(hasRead: false, hasUse: false);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() => fixture.Service.GetListAsync());

        Assert.Contains("缺少打印数据源目录访问权限", exception.Message, StringComparison.Ordinal);
        fixture.PermissionChecker.Verify(
            checker => checker.IsGrantedAsync("99", PrintingPermissionCodes.Read, It.IsAny<CancellationToken>()),
            Times.Once);
        fixture.PermissionChecker.Verify(
            checker => checker.IsGrantedAsync("99", PrintingPermissionCodes.Use, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 只持有 read（模板管理路径）即可读取目录，此时不再去查 use。
    /// </summary>
    [Fact]
    public async Task GetListAsync_WithOnlyRead_ShouldAllowAndShortCircuit()
    {
        var fixture = CreateFixture(hasRead: true, hasUse: false);

        var catalog = await fixture.Service.GetListAsync();

        Assert.Single(catalog);
        fixture.PermissionChecker.Verify(
            checker => checker.IsGrantedAsync("99", PrintingPermissionCodes.Use, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 只持有 use（业务打印路径）同样可以读取目录，这正是"二者取一"存在的理由。
    /// </summary>
    [Fact]
    public async Task GetListAsync_WithOnlyUse_ShouldAllow()
    {
        var fixture = CreateFixture(hasRead: false, hasUse: true);

        var catalog = await fixture.Service.GetListAsync();

        Assert.Single(catalog);
    }

    /// <summary>
    /// 未登录时必须以"当前用户未登录"拒绝，不允许拿空用户号去查权限。
    /// </summary>
    [Fact]
    public async Task GetListAsync_WithoutUser_ShouldReject()
    {
        var fixture = CreateFixture(hasRead: true, hasUse: true, userId: null);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() => fixture.Service.GetListAsync());

        Assert.Contains("当前用户未登录", exception.Message, StringComparison.Ordinal);
        fixture.PermissionChecker.Verify(
            checker => checker.IsGrantedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 取消令牌必须透传给权限检查器，取消才能在真实的权限查询处生效。
    /// </summary>
    [Fact]
    public async Task GetListAsync_ShouldForwardCancellationTokenToPermissionChecker()
    {
        var fixture = CreateFixture(hasRead: true, hasUse: false);
        using var cancellation = new CancellationTokenSource();

        _ = await fixture.Service.GetListAsync(cancellation.Token);

        fixture.PermissionChecker.Verify(
            checker => checker.IsGrantedAsync("99", PrintingPermissionCodes.Read, cancellation.Token),
            Times.Once);
    }

    /// <summary>
    /// 回归锚点：入口必须先检查取消。权限检查器可能走缓存命中而不做任何异步 IO，
    /// 只把令牌透传下去并不能让已取消的请求停下来，目录投影仍会整份跑完。
    /// 这与 PrintTemplateQueryService 三个查询方法「方法体第一行检查取消」是同一口径。
    /// </summary>
    [Fact]
    public async Task GetListAsync_WithCancelledToken_ShouldThrowBeforeAnyWork()
    {
        var fixture = CreateFixture(hasRead: true, hasUse: true);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.Service.GetListAsync(cancellation.Token));

        fixture.PermissionChecker.Verify(
            checker => checker.IsGrantedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 目录投影必须完整搬运字段与明细表列，设计器素材面板依赖这份结构生成拖拽元素。
    /// </summary>
    [Fact]
    public async Task GetListAsync_ShouldProjectFieldsAndTableColumns()
    {
        var fixture = CreateFixture(hasRead: true, hasUse: false);

        var source = Assert.Single(await fixture.Service.GetListAsync());

        Assert.Equal("system.print-demo", source.Code);
        Assert.Equal("系统打印示例", source.Name);
        Assert.Equal(BuiltInPrintDataSources.SystemPrintDemo.SampleDataJson, source.SampleDataJson);
        Assert.Equal(
            BuiltInPrintDataSources.SystemPrintDemo.Fields.Select(field => field.Key),
            source.Fields.Select(field => field.Key));

        var createdTime = source.Fields.Single(field => field.Key == "createdTime");
        Assert.Equal("datetime", createdTime.InputType);
        Assert.Null(createdTime.Columns);

        var items = source.Fields.Single(field => field.Key == "items");
        Assert.NotNull(items.Columns);
        Assert.Equal(["sku", "name", "quantity", "unit"], items.Columns.Select(column => column.Field));
        Assert.Equal(90, items.Columns[0].Width);
        Assert.Equal("number", items.Columns[2].InputType);
    }

    /// <summary>
    /// 目录顺序沿用注册表的序数排序，前端下拉不需要再排一次。
    /// </summary>
    [Fact]
    public async Task GetListAsync_ShouldReturnCatalogInRegistryOrder()
    {
        var registry = new PrintDataSourceRegistry(
        [
            new PrintDataSourceRegistration(new PrintDataSourceDefinition("wms.pick", "拣货单", [new("f", "字段")], "{}")),
            new PrintDataSourceRegistration(BuiltInPrintDataSources.SystemPrintDemo),
            new PrintDataSourceRegistration(new PrintDataSourceDefinition("erp.order", "订单", [new("f", "字段")], "{}"))
        ]);
        var fixture = CreateFixture(hasRead: true, hasUse: false, registry: registry);

        var catalog = await fixture.Service.GetListAsync();

        Assert.Equal(["erp.order", "system.print-demo", "wms.pick"], catalog.Select(source => source.Code));
    }

    /// <summary>
    /// 创建数据源目录查询服务夹具。
    /// </summary>
    /// <param name="hasRead">是否持有 print-template:read。</param>
    /// <param name="hasUse">是否持有 print-template:use。</param>
    /// <param name="userId">当前用户号；null 表示未登录。</param>
    /// <param name="registry">数据源注册表；默认只含内置示例。</param>
    private static CatalogFixture CreateFixture(
        bool hasRead,
        bool hasUse,
        long? userId = 99,
        PrintDataSourceRegistry? registry = null)
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(user => user.UserId).Returns(userId);

        var permissionChecker = new Mock<IPermissionChecker>();
        permissionChecker
            .Setup(checker => checker.IsGrantedAsync(
                It.IsAny<string>(), PrintingPermissionCodes.Read, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hasRead);
        permissionChecker
            .Setup(checker => checker.IsGrantedAsync(
                It.IsAny<string>(), PrintingPermissionCodes.Use, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hasUse);

        var service = new PrintDataSourceQueryService(
            registry ?? new PrintDataSourceRegistry([new PrintDataSourceRegistration(BuiltInPrintDataSources.SystemPrintDemo)]),
            currentUser.Object,
            permissionChecker.Object);
        return new CatalogFixture(service, permissionChecker);
    }

    /// <summary>
    /// 数据源目录测试依赖集合。
    /// </summary>
    /// <param name="Service">被测查询服务。</param>
    /// <param name="PermissionChecker">权限检查器替身。</param>
    private sealed record CatalogFixture(
        PrintDataSourceQueryService Service,
        Mock<IPermissionChecker> PermissionChecker);
}
