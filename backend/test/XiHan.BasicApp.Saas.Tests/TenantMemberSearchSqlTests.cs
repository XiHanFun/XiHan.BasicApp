// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Infrastructure.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Domain.Entities.Abstracts;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 租户成员关键字检索（真实 SQLite + 与生产同口径的租户读过滤器）：关键字既匹配成员显示名，
/// 也匹配账号的用户名 / 昵称 / 姓名 / 邮箱；外部成员的账号在别的租户，照样检索得到。
/// </summary>
public sealed class TenantMemberSearchSqlTests : IDisposable
{
    private const long TenantId = 1;
    private const long OtherTenantId = 2;

    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"xihan-member-search-{Guid.NewGuid():N}.db");
    private readonly SqlSugarClient _client;
    private readonly TestCurrentTenant _currentTenant = new();
    private readonly TenantUserRepository _repository;

    /// <summary>
    /// 建库、挂与生产同口径的租户过滤器
    /// </summary>
    public TenantMemberSearchSqlTests()
    {
        _client = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = $"DataSource={_databasePath};Pooling=False",
            DbType = DbType.Sqlite,
            IsAutoCloseConnection = true
        });
        _client.QueryFilter.AddTableFilter<ISoftDelete>(entity => !entity.IsDeleted);
        _client.QueryFilter.AddTableFilter<IMultiTenantEntity>(entity => entity.TenantId == 0 || entity.TenantId == (_currentTenant.Id ?? 0));
        _client.CodeFirst.InitTables<SysUser>();
        _client.CodeFirst.InitTables<SysTenantUser>();

        // 本租户成员：alice（本租户账号）、bob（外部成员，账号在另一个租户）
        Insert(new SysUser { TenantId = TenantId, UserName = "alice", NickName = "Alice", Email = "alice@acme.test" }, 11);
        Insert(new SysUser { TenantId = OtherTenantId, UserName = "bob", RealName = "Bob Stone" }, 12);
        Insert(new SysUser { TenantId = OtherTenantId, UserName = "bobby" }, 13);
        Insert(Member(TenantId, 11, displayName: null), 21);
        Insert(Member(TenantId, 12, displayName: "顾问老王"), 22);
        // bobby 只是另一个租户的成员，不在本租户
        Insert(Member(OtherTenantId, 13, displayName: null), 23);

        _repository = new TenantUserRepository(new FixedClientResolver(_client));
    }

    /// <summary>
    /// 按账号用户名检索：外部成员的账号在别的租户，也检索得到；非本租户成员不出现
    /// </summary>
    [Fact]
    public async Task Search_ByAccountUserName_FindsExternalMemberOnly()
    {
        using var scope = _currentTenant.Change(TenantId);

        var ids = await _repository.SearchMemberUserIdsAsync(TenantId, "BOB");

        Assert.Equal([12L], ids);
    }

    /// <summary>
    /// 按成员显示名与账号邮箱检索
    /// </summary>
    [Fact]
    public async Task Search_ByDisplayNameOrEmail()
    {
        using var scope = _currentTenant.Change(TenantId);

        Assert.Equal([12L], await _repository.SearchMemberUserIdsAsync(TenantId, "老王"));
        Assert.Equal([11L], await _repository.SearchMemberUserIdsAsync(TenantId, "acme.test"));
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

    private static SysTenantUser Member(long tenantId, long userId, string? displayName)
    {
        return new SysTenantUser
        {
            TenantId = tenantId,
            UserId = userId,
            DisplayName = displayName,
            MemberType = TenantMemberType.Member,
            InviteStatus = TenantMemberInviteStatus.Accepted,
            Status = ValidityStatus.Valid
        };
    }

    private void Insert<TEntity>(TEntity entity, long basicId) where TEntity : class, new()
    {
        SaasTestHelper.SetBasicId(entity, basicId);
        _ = _client.Insertable(entity).ExecuteCommand();
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
