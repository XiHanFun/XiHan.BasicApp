// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using System.Linq.Expressions;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Models;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 授权申请、权限委托里的人名：成员名片优先，没填时取账号的真实姓名 → 昵称 → 用户名。
/// </summary>
/// <remarks>
/// 回归锚点：演示种子没给成员填租户内显示名，申请人、委托人、接收人曾显示成一串用户主键。
/// </remarks>
public sealed class AuthorizationPartyDisplayNameTests
{
    private readonly Mock<IPermissionRequestRepository> _requests = new();
    private readonly Mock<IPermissionDelegationRepository> _delegations = new();
    private readonly Mock<ITenantUserRepository> _tenantUsers = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPermissionRepository> _permissions = new();
    private readonly Mock<IRoleRepository> _roles = new();
    private readonly Mock<IWorkflowBusinessService> _workflow = new();

    /// <summary>
    /// 构造函数：审批进度与人名无关，统一返回空
    /// </summary>
    public AuthorizationPartyDisplayNameTests()
    {
        _workflow
            .Setup(service => service.GetProgressAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, WorkflowBusinessProgress>());
    }

    /// <summary>
    /// 显示名按名片 → 真实姓名 → 昵称 → 用户名逐级回退，空白视同没填。
    /// </summary>
    [Theory]
    [InlineData("市场部 · 张三", "张三", "小张", "zhangsan", "市场部 · 张三")]
    [InlineData(null, "张三", "小张", "zhangsan", "张三")]
    [InlineData("  ", "张三", "小张", "zhangsan", "张三")]
    [InlineData(null, null, "小张", "zhangsan", "小张")]
    [InlineData(null, " ", "", "zhangsan", "zhangsan")]
    public void ToDisplayName_ShouldFallBackFromMemberNameToAccountNames(
        string? memberName, string? realName, string? nickName, string userName, string expected)
    {
        var member = new SysTenantUser { UserId = 1, DisplayName = memberName };
        var user = new SysUser { UserName = userName, RealName = realName, NickName = nickName };

        Assert.Equal(expected, TenantMemberApplicationMapper.ToDisplayName(member, user));
    }

    /// <summary>
    /// 成员名片有值时不依赖账号；成员关系与账号都查不到时不编造名字。
    /// </summary>
    [Fact]
    public void ToDisplayName_WithoutAccount_ShouldUseMemberNameOrNull()
    {
        Assert.Equal("张三", TenantMemberApplicationMapper.ToDisplayName(new SysTenantUser { DisplayName = "张三" }, null));
        Assert.Null(TenantMemberApplicationMapper.ToDisplayName(new SysTenantUser(), null));
        Assert.Null(TenantMemberApplicationMapper.ToDisplayName(null, null));
    }

    /// <summary>
    /// 申请列表与详情映射：成员没填名片时取账号姓名。
    /// </summary>
    [Fact]
    public void PermissionRequestMapper_ShouldUseAccountNameWhenMemberNameIsEmpty()
    {
        var request = Request(10, userId: 1);
        var member = Member(100, userId: 1, displayName: null);
        var account = User(1, "employee", realName: "演示员工");

        var item = PermissionRequestApplicationMapper.ToListItemDto(request, member, account, null, null, null, DateTimeOffset.UtcNow);
        var detail = PermissionRequestApplicationMapper.ToDetailDto(request, member, account, null, null, null, DateTimeOffset.UtcNow);

        Assert.Equal("演示员工", item.RequestUserDisplayName);
        Assert.Equal(100L, item.RequestTenantMemberId);
        Assert.Equal("演示员工", detail.RequestUserDisplayName);
    }

    /// <summary>
    /// 委托列表与详情映射：委托人、接收人各按自己的成员关系与账号解析。
    /// </summary>
    [Fact]
    public void PermissionDelegationMapper_ShouldResolveEachPartyIndependently()
    {
        var delegation = Delegation(20, delegatorUserId: 1, delegateeUserId: 2);
        var delegator = Member(100, userId: 1, displayName: "部门主管");
        var delegatorAccount = User(1, "manager", realName: "王经理");
        var delegatee = Member(101, userId: 2, displayName: null);
        var delegateeAccount = User(2, "assistant", nickName: "小李");

        var item = PermissionDelegationApplicationMapper.ToListItemDto(
            delegation, delegator, delegatorAccount, delegatee, delegateeAccount, null, null, DateTimeOffset.UtcNow);
        var detail = PermissionDelegationApplicationMapper.ToDetailDto(
            delegation, delegator, delegatorAccount, delegatee, delegateeAccount, null, null, DateTimeOffset.UtcNow);

        Assert.Equal("部门主管", item.DelegatorDisplayName);
        Assert.Equal("小李", item.DelegateeDisplayName);
        Assert.Equal("部门主管", detail.DelegatorDisplayName);
        Assert.Equal("小李", detail.DelegateeDisplayName);
    }

    /// <summary>
    /// 申请分页：同页的申请人账号一次批量读取，不逐条查询。
    /// </summary>
    [Fact]
    public async Task GetPermissionRequestPageAsync_ShouldResolveApplicantNamesWithOneBatchedAccountQuery()
    {
        _requests
            .Setup(repository => repository.GetPagedAsync(It.IsAny<PageRequestDtoBase>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Page(Request(10, userId: 1), Request(11, userId: 2), Request(12, userId: 1)));
        SetupMembers(Member(100, userId: 1, displayName: null), Member(101, userId: 2, displayName: "外部顾问"));
        IReadOnlyCollection<long>? requestedUserIds = null;
        _users
            .Setup(repository => repository.GetListByIdsIgnoreTenantAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<long>, CancellationToken>((ids, _) => requestedUserIds = ids)
            .ReturnsAsync([User(1, "employee", realName: "演示员工"), User(2, "consultant", realName: "顾问本名")]);

        var result = await CreateRequestQueryService().GetPermissionRequestPageAsync(new PermissionRequestPageQueryDto(), CancellationToken.None);

        Assert.Equal(new string?[] { "演示员工", "外部顾问", "演示员工" }, result.Items.Select(item => item.RequestUserDisplayName));
        Assert.Equal(new[] { 1L, 2L }, requestedUserIds!.Order());
        _users.Verify(repository => repository.GetListByIdsIgnoreTenantAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()), Times.Once);
        _users.Verify(repository => repository.GetByIdIgnoreTenantAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 申请详情：成员没填名片时取账号姓名。
    /// </summary>
    [Fact]
    public async Task GetPermissionRequestDetailAsync_ShouldUseAccountNameWhenMemberNameIsEmpty()
    {
        _requests
            .Setup(repository => repository.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Request(10, userId: 1));
        _tenantUsers
            .Setup(repository => repository.GetMembershipAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Member(100, userId: 1, displayName: null));
        _users
            .Setup(repository => repository.GetByIdIgnoreTenantAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(User(1, "employee", nickName: "小员工"));

        var detail = await CreateRequestQueryService().GetPermissionRequestDetailAsync(10, CancellationToken.None);

        Assert.NotNull(detail);
        Assert.Equal("小员工", detail.RequestUserDisplayName);
        Assert.Equal(100L, detail.RequestTenantMemberId);
    }

    /// <summary>
    /// 委托分页：委托人与接收人的账号合并去重后一次批量读取；已不在本租户的人也按账号显示名字。
    /// </summary>
    [Fact]
    public async Task GetPermissionDelegationPageAsync_ShouldResolveBothPartiesWithOneBatchedAccountQuery()
    {
        _delegations
            .Setup(repository => repository.GetPagedAsync(It.IsAny<PageRequestDtoBase>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Page(Delegation(20, delegatorUserId: 1, delegateeUserId: 2), Delegation(21, delegatorUserId: 2, delegateeUserId: 3)));
        SetupMembers(Member(100, userId: 1, displayName: "部门主管"), Member(101, userId: 2, displayName: null));
        IReadOnlyCollection<long>? requestedUserIds = null;
        _users
            .Setup(repository => repository.GetListByIdsIgnoreTenantAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<long>, CancellationToken>((ids, _) => requestedUserIds = ids)
            .ReturnsAsync([User(1, "manager", realName: "王经理"), User(2, "assistant"), User(3, "former", realName: "已离开的成员")]);

        var result = await CreateDelegationQueryService().GetPermissionDelegationPageAsync(new PermissionDelegationPageQueryDto(), CancellationToken.None);

        Assert.Collection(
            result.Items,
            item =>
            {
                Assert.Equal("部门主管", item.DelegatorDisplayName);
                Assert.Equal("assistant", item.DelegateeDisplayName);
            },
            item =>
            {
                Assert.Equal("assistant", item.DelegatorDisplayName);
                Assert.Equal("已离开的成员", item.DelegateeDisplayName);
                Assert.Null(item.DelegateeTenantMemberId);
            });
        Assert.Equal(new[] { 1L, 2L, 3L }, requestedUserIds!.Order());
        _users.Verify(repository => repository.GetListByIdsIgnoreTenantAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 委托详情：两方账号一次读取。
    /// </summary>
    [Fact]
    public async Task GetPermissionDelegationDetailAsync_ShouldResolveBothPartiesWithOneAccountQuery()
    {
        _delegations
            .Setup(repository => repository.GetByIdAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Delegation(20, delegatorUserId: 1, delegateeUserId: 2));
        _tenantUsers
            .Setup(repository => repository.GetMembershipAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Member(100, userId: 1, displayName: null));
        _tenantUsers
            .Setup(repository => repository.GetMembershipAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Member(101, userId: 2, displayName: null));
        _users
            .Setup(repository => repository.GetListByIdsIgnoreTenantAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([User(1, "manager", realName: "王经理"), User(2, "assistant", nickName: "小李")]);

        var detail = await CreateDelegationQueryService().GetPermissionDelegationDetailAsync(20, CancellationToken.None);

        Assert.NotNull(detail);
        Assert.Equal("王经理", detail.DelegatorDisplayName);
        Assert.Equal("小李", detail.DelegateeDisplayName);
        _users.Verify(repository => repository.GetListByIdsIgnoreTenantAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private PermissionRequestQueryService CreateRequestQueryService() => new(
        _requests.Object,
        _tenantUsers.Object,
        _users.Object,
        _permissions.Object,
        _roles.Object,
        _workflow.Object);

    private PermissionDelegationQueryService CreateDelegationQueryService() => new(
        _delegations.Object,
        _tenantUsers.Object,
        _users.Object,
        _permissions.Object,
        _roles.Object);

    private void SetupMembers(params SysTenantUser[] members)
    {
        _tenantUsers
            .Setup(repository => repository.GetListAsync(
                It.IsAny<Expression<Func<SysTenantUser, bool>>>(),
                It.IsAny<Expression<Func<SysTenantUser, object>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(members);
    }

    private static PageResultDtoBase<TEntity> Page<TEntity>(params TEntity[] items) =>
        new([.. items], new PageResultMetadata(1, 20, items.Length));

    private static SysPermissionRequest Request(long id, long userId)
    {
        var request = new SysPermissionRequest { RequestUserId = userId, RequestReason = "需要处理审批" };
        SaasTestHelper.SetBasicId(request, id);
        return request;
    }

    private static SysPermissionDelegation Delegation(long id, long delegatorUserId, long delegateeUserId)
    {
        var delegation = new SysPermissionDelegation
        {
            DelegatorUserId = delegatorUserId,
            DelegateeUserId = delegateeUserId,
            ExpirationTime = DateTimeOffset.UtcNow.AddDays(7)
        };
        SaasTestHelper.SetBasicId(delegation, id);
        return delegation;
    }

    private static SysTenantUser Member(long id, long userId, string? displayName)
    {
        var member = new SysTenantUser { UserId = userId, DisplayName = displayName };
        SaasTestHelper.SetBasicId(member, id);
        return member;
    }

    private static SysUser User(long id, string userName, string? realName = null, string? nickName = null)
    {
        var user = new SysUser { UserName = userName, RealName = realName, NickName = nickName };
        SaasTestHelper.SetBasicId(user, id);
        return user;
    }
}
