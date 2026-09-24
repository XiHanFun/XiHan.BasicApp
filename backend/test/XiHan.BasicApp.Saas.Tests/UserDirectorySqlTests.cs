// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using SqlSugar;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Infrastructure.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Domain.Entities.Abstracts;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 用户目录（真实 SQLite + 与生产同口径的读共享 / 严格隔离过滤器）。
/// </summary>
/// <remarks>
/// 账号是严格隔离的账号域数据，戳注册地租户：平台只看得见平台账号；租户里看得见的是本租户已接受的成员，
/// 含注册在别的租户的外部成员与入驻的平台人员。待接受、已撤销、已删除的成员关系都不可见，
/// 仅在本租户注册却不是成员的账号也不可见。
/// </remarks>
public sealed class UserDirectorySqlTests : IDisposable
{
    private const long TenantId = 7;
    private const long OtherTenantId = 8;

    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"xihan-user-directory-{Guid.NewGuid():N}.db");
    private readonly SqlSugarClient _client;
    private readonly TestCurrentTenant _currentTenant = new();
    private readonly UserDirectory _directory;

    /// <summary>
    /// 建库、挂与生产同口径的租户过滤器，铺两租户的账号与成员关系
    /// </summary>
    public UserDirectorySqlTests()
    {
        _client = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = $"DataSource={_databasePath};Pooling=False",
            DbType = DbType.Sqlite,
            IsAutoCloseConnection = true
        });
        _client.QueryFilter.AddTableFilter<ISoftDelete>(entity => !entity.IsDeleted);
        _client.QueryFilter.AddTableFilter<IMultiTenantEntity>(entity => entity.TenantId == 0 || entity.TenantId == (_currentTenant.Id ?? 0));
        _client.QueryFilter.AddTableFilter<IStrictMultiTenantEntity>(entity => entity.TenantId == (_currentTenant.Id ?? 0));
        _client.CodeFirst.InitTables<SysUser>();
        _client.CodeFirst.InitTables<SysTenantUser>();

        // 平台账号 root（入驻了租户 7）
        InsertUser(1, 0, "root");
        // 租户 7 注册：alice 成员；dave 成员关系已撤销；erin 已停用
        InsertUser(11, TenantId, "alice");
        InsertUser(14, TenantId, "dave");
        InsertUser(15, TenantId, "erin", EnableStatus.Disabled);
        // 租户 8 注册：bob 以外部成员加入租户 7；carol 只收到邀请未接受；frank 只在租户 8
        InsertUser(12, OtherTenantId, "bob");
        InsertUser(13, OtherTenantId, "carol");
        InsertUser(16, OtherTenantId, "frank");

        InsertMember(21, TenantId, 1, TenantMemberInviteStatus.Accepted);
        InsertMember(22, TenantId, 11, TenantMemberInviteStatus.Accepted);
        InsertMember(23, TenantId, 12, TenantMemberInviteStatus.Accepted);
        InsertMember(24, TenantId, 13, TenantMemberInviteStatus.Pending);
        InsertMember(25, TenantId, 14, TenantMemberInviteStatus.Revoked);
        InsertMember(26, TenantId, 15, TenantMemberInviteStatus.Accepted);
        InsertMember(27, OtherTenantId, 12, TenantMemberInviteStatus.Accepted);
        InsertMember(28, OtherTenantId, 13, TenantMemberInviteStatus.Accepted);
        InsertMember(29, OtherTenantId, 16, TenantMemberInviteStatus.Accepted);
        // 已删除的成员关系不算数
        InsertMember(30, TenantId, 16, TenantMemberInviteStatus.Accepted, isDeleted: true);

        var repository = new UserRepository(new FixedClientResolver(_client), new Mock<IUnitOfWorkManager>().Object, _currentTenant);
        _directory = new UserDirectory(_currentTenant, repository);
    }

    /// <summary>
    /// 租户里的目录是本租户已接受的成员：含外部成员与入驻的平台人员，分页总数与之一致
    /// </summary>
    [Fact]
    public async Task GetPaged_InTenant_ListsAcceptedMembersAcrossHomeTenants()
    {
        using var scope = _currentTenant.Change(TenantId);

        var page = await _directory.GetPagedAsync(new PageRequestDtoBase());

        Assert.Equal([1L, 11L, 12L, 15L], page.Items.Select(user => user.BasicId).Order().ToArray());
        Assert.Equal(4, page.Page.TotalCount);
    }

    /// <summary>
    /// 平台里的目录只有平台账号：租户账号不再因读共享漏进平台
    /// </summary>
    [Fact]
    public async Task GetPaged_InPlatform_ListsPlatformAccountsOnly()
    {
        var page = await _directory.GetPagedAsync(new PageRequestDtoBase());

        Assert.Equal([1L], page.Items.Select(user => user.BasicId).ToArray());
    }

    /// <summary>
    /// 分页条件照常作用在成员账号上
    /// </summary>
    [Fact]
    public async Task GetPaged_InTenant_AppliesConditions()
    {
        using var scope = _currentTenant.Change(TenantId);

        var page = await _directory.GetPagedAsync(new PageRequestDtoBase().WithFilter(nameof(SysUser.UserName), "bob"));

        Assert.Equal(12L, Assert.Single(page.Items).BasicId);
    }

    /// <summary>
    /// 单个查找与目录同口径：外部成员找得到，未接受 / 已撤销 / 非成员找不到
    /// </summary>
    [Fact]
    public async Task Find_InTenant_FollowsMembership()
    {
        using var scope = _currentTenant.Change(TenantId);

        Assert.Equal("bob", (await _directory.FindAsync(12))?.UserName);
        Assert.Null(await _directory.FindAsync(13));
        Assert.Null(await _directory.FindAsync(14));
        Assert.Null(await _directory.FindAsync(16));
    }

    /// <summary>
    /// 启用账号主键只取目录内且启用的账号
    /// </summary>
    [Fact]
    public async Task GetEnabledIds_InTenant_ExcludesDisabledAndNonMembers()
    {
        using var scope = _currentTenant.Change(TenantId);

        var ids = await _directory.GetEnabledIdsAsync();

        Assert.Equal([1L, 11L, 12L], ids.Order().ToArray());
    }

    /// <summary>
    /// 本上下文注册的账号才算本地账号：外部成员与入驻的平台人员都不是
    /// </summary>
    [Fact]
    public async Task IsHomeAccount_ComparesHomeTenantWithContext()
    {
        using var scope = _currentTenant.Change(TenantId);

        var alice = await _directory.FindAsync(11);
        var bob = await _directory.FindAsync(12);
        var root = await _directory.FindAsync(1);

        Assert.True(_directory.IsHomeAccount(alice!));
        Assert.False(_directory.IsHomeAccount(bob!));
        Assert.False(_directory.IsHomeAccount(root!));
    }

    /// <summary>
    /// 释放连接并清理临时库文件
    /// </summary>
    public void Dispose()
    {
        _client.Ado.Connection.Close();
        _client.Dispose();
        SaasTestHelper.DeleteTemporaryDatabase(_databasePath);
    }

    private void InsertUser(long basicId, long tenantId, string userName, EnableStatus status = EnableStatus.Enabled)
    {
        var user = new SysUser { TenantId = tenantId, UserName = userName, Status = status, CreatedTime = DateTimeOffset.UnixEpoch };
        SaasTestHelper.SetBasicId(user, basicId);
        _ = _client.Insertable(user).ExecuteCommand();
    }

    private void InsertMember(long basicId, long tenantId, long userId, TenantMemberInviteStatus inviteStatus, bool isDeleted = false)
    {
        var member = new SysTenantUser
        {
            TenantId = tenantId,
            UserId = userId,
            MemberType = TenantMemberType.Member,
            InviteStatus = inviteStatus,
            Status = ValidityStatus.Valid,
            IsDeleted = isDeleted
        };
        SaasTestHelper.SetBasicId(member, basicId);
        _ = _client.Insertable(member).ExecuteCommand();
    }

    /// <summary>
    /// 固定返回同一客户端的解析器替身
    /// </summary>
    private sealed class FixedClientResolver(ISqlSugarClient client) : ISqlSugarClientResolver
    {
        public ISqlSugarClient GetCurrentClient() => client;

        public ISqlSugarClient GetClientForEntity(Type entityType) => client;

        public ISqlSugarClient GetClient(string configId) => client;

        public IReadOnlyCollection<string> GetAllConfigIds() => [];

        public IReadOnlyList<string> GetCurrentLayoutConfigIds() => [];

        public IEnumerable<ISqlSugarClient> GetAllClients() => [client];

        public ITenant AsTenant() => throw new NotSupportedException();
    }
}
