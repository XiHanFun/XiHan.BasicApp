// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Moq;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Workflow.Application.Dtos;
using XiHan.BasicApp.Workflow.Application.QueryServices;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Workflow.Tests;

/// <summary>
/// 审批人选人接口的暴露面边界测试。
/// </summary>
/// <remarks>
/// <c>WorkflowDirectoryQuery/UserOptions</c> 刻意不挂权限码（办理人转办、加签时也要选人），
/// 任何登录用户都能调用，能挡住越界的只剩这里钉住的几条：候选只来自当前上下文的用户目录、
/// 单次条数有上限、关键字检索只出启用用户且非超管看不到超管、返回字段不含联系方式。
/// 哪条被放宽，这个接口就要重新评估是否挂权限。
/// </remarks>
public sealed class WorkflowDirectoryQueryServiceTests
{
    private const int MaxOptionCount = 200;

    /// <summary>
    /// 入参为 null 时必须抛空引用参数异常。
    /// </summary>
    [Fact]
    public async Task GetUserOptionsAsync_NullInput_ShouldThrowArgumentNullException()
    {
        var context = new DirectoryContext();

        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => context.Service.GetUserOptionsAsync(null!));
    }

    /// <summary>
    /// 已取消的令牌必须在查用户目录之前抛出。
    /// </summary>
    [Fact]
    public async Task GetUserOptionsAsync_CanceledToken_ShouldThrowBeforeDirectory()
    {
        var context = new DirectoryContext();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => context.Service.GetUserOptionsAsync(new WorkflowUserOptionQueryDto(), cancellation.Token));

        context.Directory.Verify(
            value => value.GetPagedAsync(It.IsAny<PageRequestDtoBase>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 关键字检索的条数夹在 1 到上限之间，调用方传多大都不能一次拉走整个目录。
    /// </summary>
    /// <param name="limit">调用方给的条数</param>
    /// <param name="expected">实际下推的页大小</param>
    [Theory]
    [InlineData(50, 50)]
    [InlineData(10_000, MaxOptionCount)]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    public async Task GetUserOptionsAsync_Search_ShouldClampPageSize(int limit, int expected)
    {
        var context = new DirectoryContext();
        var request = context.CapturePagedRequest();

        _ = await context.Service.GetUserOptionsAsync(new WorkflowUserOptionQueryDto { Limit = limit });

        Assert.Equal(expected, request.Value!.Page.PageSize);
    }

    /// <summary>
    /// 关键字检索只出启用用户，关键字只匹配用户名与姓名、昵称，不能拿手机号、邮箱去探测账号。
    /// </summary>
    [Fact]
    public async Task GetUserOptionsAsync_Search_ShouldFilterEnabledAndMatchNamesOnly()
    {
        var context = new DirectoryContext();
        var request = context.CapturePagedRequest();

        _ = await context.Service.GetUserOptionsAsync(new WorkflowUserOptionQueryDto { Keyword = "  张三  " });

        var conditions = request.Value!.Conditions;
        var status = Assert.Single(conditions.Filters, filter => filter.Field == nameof(SysUser.Status));
        Assert.Equal(QueryOperator.Equal, status.Operator);
        Assert.Equal(EnableStatus.Enabled, status.Value);

        Assert.NotNull(conditions.Keyword);
        Assert.Equal("张三", conditions.Keyword!.Value);
        Assert.Equal(
            [nameof(SysUser.NickName), nameof(SysUser.RealName), nameof(SysUser.UserName)],
            conditions.Keyword.Fields.Order(StringComparer.Ordinal).ToArray());
    }

    /// <summary>
    /// 非超管检索时必须把超管排除在候选之外。
    /// </summary>
    [Fact]
    public async Task GetUserOptionsAsync_Search_NonSuperAdmin_ShouldExcludeProtectedUsers()
    {
        var context = new DirectoryContext();
        context.SuperAdmin.Setup(value => value.IsCurrentUserSuperAdmin()).Returns(false);
        context.SuperAdmin
            .Setup(value => value.GetProtectedUserIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([1L, 2L]);
        var request = context.CapturePagedRequest();

        _ = await context.Service.GetUserOptionsAsync(new WorkflowUserOptionQueryDto());

        var exclusion = Assert.Single(request.Value!.Conditions.Filters, filter => filter.Field == nameof(SysUser.BasicId));
        Assert.Equal(QueryOperator.NotIn, exclusion.Operator);
        Assert.Equal([1L, 2L], exclusion.Values!.Cast<long>().Order().ToArray());
    }

    /// <summary>
    /// 超管自身不受超管隐藏限制，也不必去查受保护用户。
    /// </summary>
    [Fact]
    public async Task GetUserOptionsAsync_Search_SuperAdmin_ShouldNotExcludeProtectedUsers()
    {
        var context = new DirectoryContext();
        context.SuperAdmin.Setup(value => value.IsCurrentUserSuperAdmin()).Returns(true);
        var request = context.CapturePagedRequest();

        _ = await context.Service.GetUserOptionsAsync(new WorkflowUserOptionQueryDto());

        Assert.DoesNotContain(request.Value!.Conditions.Filters, filter => filter.Field == nameof(SysUser.BasicId));
        context.SuperAdmin.Verify(
            value => value.GetProtectedUserIdsAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 按主键回显逐个经用户目录解析：目录外的主键（别的租户、非成员）不返回，非正数与重复主键不触库，
    /// 显示名按真实姓名 → 昵称 → 用户名取。
    /// </summary>
    [Fact]
    public async Task GetUserOptionsAsync_ByIds_ShouldResolveThroughDirectoryOnly()
    {
        var context = new DirectoryContext();
        context.Directory
            .Setup(value => value.FindAsync(11, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TestUser(11) { UserName = "alice", RealName = "张三", NickName = "小张", Phone = "13800000000" });
        context.Directory
            .Setup(value => value.FindAsync(13, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TestUser(13) { UserName = "carol", NickName = "Carol", Email = "carol@example.com" });

        var options = await context.Service.GetUserOptionsAsync(new WorkflowUserOptionQueryDto
        {
            Keyword = "ignored",
            Ids = [11, 12, 12, 0, -1, 13]
        });

        Assert.Equal([11L, 13L], options.Select(option => option.UserId).ToArray());
        Assert.Equal(["张三", "Carol"], options.Select(option => option.DisplayName).ToArray());
        context.Directory.Verify(value => value.FindAsync(12, It.IsAny<CancellationToken>()), Times.Once);
        context.Directory.Verify(value => value.FindAsync(It.Is<long>(id => id <= 0), It.IsAny<CancellationToken>()), Times.Never);
        context.Directory.Verify(
            value => value.GetPagedAsync(It.IsAny<PageRequestDtoBase>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 按主键回显同样受单次上限约束，传再多主键也只解析前上限个。
    /// </summary>
    [Fact]
    public async Task GetUserOptionsAsync_ByIds_ShouldCapAtMaxOptionCount()
    {
        var context = new DirectoryContext();

        _ = await context.Service.GetUserOptionsAsync(new WorkflowUserOptionQueryDto
        {
            Ids = [.. Enumerable.Range(1, MaxOptionCount + 300).Select(id => (long)id)]
        });

        context.Directory.Verify(
            value => value.FindAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Exactly(MaxOptionCount));
    }

    /// <summary>
    /// 不设权限码的选人结果只能带身份展示字段；新增字段（尤其手机号、邮箱）必须先回头评估要不要挂权限。
    /// </summary>
    [Fact]
    public void WorkflowUserOptionDto_ShouldOnlyExposeDisplayFields()
    {
        var properties = typeof(WorkflowUserOptionDto)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                nameof(WorkflowUserOptionDto.Avatar),
                nameof(WorkflowUserOptionDto.DisplayName),
                nameof(WorkflowUserOptionDto.UserId),
                nameof(WorkflowUserOptionDto.UserName)
            ],
            properties);
    }

    /// <summary>
    /// 用户测试替身：BasicId 为 protected set，经派生类构造赋值，避免反射。
    /// </summary>
    private sealed class TestUser : SysUser
    {
        public TestUser(long basicId)
        {
            BasicId = basicId;
        }
    }

    /// <summary>
    /// 选人服务及其依赖替身。
    /// </summary>
    private sealed class DirectoryContext
    {
        public DirectoryContext()
        {
            SuperAdmin
                .Setup(value => value.GetProtectedUserIdsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            Service = new WorkflowDirectoryQueryService(
                Directory.Object,
                SuperAdmin.Object,
                Mock.Of<IRoleRepository>(),
                Mock.Of<IPositionRepository>(),
                Mock.Of<IDepartmentRepository>(),
                Mock.Of<ICurrentTenant>());
        }

        public Mock<IUserDirectory> Directory { get; } = new();

        public Mock<ISuperAdminProtector> SuperAdmin { get; } = new();

        public WorkflowDirectoryQueryService Service { get; }

        /// <summary>
        /// 截获下推到用户目录的分页请求。
        /// </summary>
        public StrongBox<PageRequestDtoBase> CapturePagedRequest()
        {
            var captured = new StrongBox<PageRequestDtoBase>();
            Directory
                .Setup(value => value.GetPagedAsync(It.IsAny<PageRequestDtoBase>(), It.IsAny<CancellationToken>()))
                .Callback<PageRequestDtoBase, CancellationToken>((request, _) => captured.Value = request)
                .ReturnsAsync(new PageResultDtoBase<SysUser>([], 1, MaxOptionCount, 0));
            return captured;
        }
    }
}
