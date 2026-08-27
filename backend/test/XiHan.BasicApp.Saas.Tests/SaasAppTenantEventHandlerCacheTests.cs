// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SqlSugar;
using System.Reflection;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.EventHandlers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 租户状态 / 租户成员两个事件处理器的"改库 + 清缓存"配对测试。
/// </summary>
/// <remarks>
/// 这两个处理器是**鉴权失效面**：它们改的两张表（会话表、用户角色表）都有读侧缓存挡在前面，
/// 只改库不清缓存的后果不是"慢一点"，而是**改动在缓存过期前完全不生效**——
/// 租户停用了用户还在线（会话闸门每请求读 60 秒 TTL 的会话状态缓存）、
/// 成员撤销了角色权限还照旧生效（鉴权读的是授权快照缓存）。
/// <para>
/// 因此这里不做纯替身断言，而是跑在临时 SQLite 文件库上执行真实的改库语句，
/// 同时用替身盯住"那一刀缓存失效有没有落下"，把两件事绑在同一条用例里。
/// 不连任何外部数据库；库文件在 Dispose 中删除。
/// </para>
/// </remarks>
public sealed class SaasAppTenantEventHandlerCacheTests : IDisposable
{
    private const long TargetTenantId = 7L;
    private const long OtherTenantId = 8L;
    private const long TargetUserId = 90L;

    private readonly SqlSugarClient _client;
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"xihan-saas-tenant-event-{Guid.NewGuid():N}.db");
    private readonly Mock<ISaasCacheInvalidator> _invalidator = new();
    private readonly ISqlSugarClientResolver _resolver;

    /// <summary>
    /// 建临时库并按生产实体建表。
    /// </summary>
    public SaasAppTenantEventHandlerCacheTests()
    {
        _client = new SqlSugarClient(new ConnectionConfig
        {
            // 关闭连接池，否则驱动会缓存连接、在用例结束后仍持有临时库文件句柄导致无法清理。
            ConnectionString = $"DataSource={_databasePath};Pooling=False",
            DbType = DbType.Sqlite,
            IsAutoCloseConnection = false
        });
        _client.CodeFirst.InitTables<SysUserSession>();
        _client.CodeFirst.InitTables<SysUserRole>();
        _resolver = new SingleClientResolver(_client);
    }

    /// <summary>
    /// 释放连接并删除临时库文件。
    /// </summary>
    public void Dispose()
    {
        _client.Close();
        _client.Dispose();
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }

    /// <summary>
    /// 回归锚点：租户被停用时，除了把会话改成已撤销，**必须**同时失效会话状态缓存。
    /// </summary>
    /// <remarks>
    /// 修复前处理器根本没有注入 <see cref="ISaasCacheInvalidator"/>，改完库就返回，
    /// 被停租户的用户最长还能再通过会话闸门 60 秒（缓存 TTL）。
    /// </remarks>
    /// <param name="newStatus">导致吊销的新租户状态。</param>
    [Theory]
    [InlineData(TenantStatus.Suspended)]
    [InlineData(TenantStatus.Expired)]
    [InlineData(TenantStatus.Disabled)]
    public async Task TenantStatusChanged_WhenTenantStopped_ShouldRevokeSessionsAndInvalidateSessionStateCache(TenantStatus newStatus)
    {
        InsertSession(1, TargetTenantId, "sess-a", SessionStatus.Active);
        InsertSession(2, TargetTenantId, "sess-b", SessionStatus.Active);
        InsertSession(3, OtherTenantId, "sess-c", SessionStatus.Active);

        await BuildStatusHandler().HandleEventAsync(new TenantStatusChangedDomainEvent(
            0, TargetTenantId, TenantStatus.Normal, newStatus, operatorUserId: 1, reason: "欠费停用"));

        Assert.Equal(SessionStatus.Revoked, ReadSession(1).Status);
        Assert.Equal(SessionStatus.Revoked, ReadSession(2).Status);
        Assert.Equal("欠费停用", ReadSession(1).RevokedReason, StringComparer.Ordinal);
        // 其它租户的会话不受影响
        Assert.Equal(SessionStatus.Active, ReadSession(3).Status);

        _invalidator.Verify(target => target.InvalidateAllSessionStatesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 该租户没有活跃会话时什么都没改，也就没有缓存要清——不做无谓的整体失效。
    /// </summary>
    [Fact]
    public async Task TenantStatusChanged_WithoutActiveSession_ShouldNotTouchCache()
    {
        InsertSession(1, TargetTenantId, "sess-a", SessionStatus.Offline);

        await BuildStatusHandler().HandleEventAsync(new TenantStatusChangedDomainEvent(
            0, TargetTenantId, TenantStatus.Normal, TenantStatus.Suspended));

        Assert.Equal(SessionStatus.Offline, ReadSession(1).Status);
        _invalidator.Verify(target => target.InvalidateAllSessionStatesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 租户恢复正常不吊销任何会话，自然也不清缓存。
    /// </summary>
    [Fact]
    public async Task TenantStatusChanged_BackToNormal_ShouldNotRevokeOrInvalidate()
    {
        InsertSession(1, TargetTenantId, "sess-a", SessionStatus.Active);

        await BuildStatusHandler().HandleEventAsync(new TenantStatusChangedDomainEvent(
            0, TargetTenantId, TenantStatus.Suspended, TenantStatus.Normal));

        Assert.Equal(SessionStatus.Active, ReadSession(1).Status);
        _invalidator.Verify(target => target.InvalidateAllSessionStatesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 缓存失效本身失败时只记日志：已经落库的吊销不能因为一次缓存故障被整个事件处理炸掉。
    /// </summary>
    [Fact]
    public async Task TenantStatusChanged_WhenInvalidationFails_ShouldNotRethrowAndKeepRevocation()
    {
        _invalidator
            .Setup(target => target.InvalidateAllSessionStatesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("缓存不可用"));
        InsertSession(1, TargetTenantId, "sess-a", SessionStatus.Active);

        await BuildStatusHandler().HandleEventAsync(new TenantStatusChangedDomainEvent(
            0, TargetTenantId, TenantStatus.Normal, TenantStatus.Suspended));

        Assert.Equal(SessionStatus.Revoked, ReadSession(1).Status);
    }

    /// <summary>
    /// 构造函数拒绝 null 依赖——缺了失效器就等于整条会话缓存一致性链路静默失效。
    /// </summary>
    [Fact]
    public void TenantStatusChanged_NullDependencies_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new TenantStatusChangedEventHandler(null!, _invalidator.Object, NullLogger<TenantStatusChangedEventHandler>.Instance));
        Assert.Throws<ArgumentNullException>(() =>
            new TenantStatusChangedEventHandler(_resolver, null!, NullLogger<TenantStatusChangedEventHandler>.Instance));
        Assert.Throws<ArgumentNullException>(() =>
            new TenantStatusChangedEventHandler(_resolver, _invalidator.Object, null!));
    }

    /// <summary>
    /// 事件为 null 时立即抛错，不允许静默跳过一次吊销。
    /// </summary>
    [Fact]
    public async Task TenantStatusChanged_NullEvent_ShouldThrow()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => BuildStatusHandler().HandleEventAsync(null!));
    }

    /// <summary>
    /// 回归锚点：成员身份被撤销/过期时，删完角色绑定**必须**按用户失效授权快照缓存。
    /// </summary>
    /// <remarks>
    /// 修复前处理器只注入了数据库解析器与日志，删完绑定就返回；
    /// 而鉴权决策读的是授权快照缓存，被移除的角色权限会继续生效到缓存过期为止。
    /// </remarks>
    /// <param name="inviteStatus">导致解绑的成员状态。</param>
    [Theory]
    [InlineData(TenantMemberInviteStatus.Revoked)]
    [InlineData(TenantMemberInviteStatus.Expired)]
    public async Task TenantMembershipChanged_WhenRevoked_ShouldRemoveBindingsAndInvalidateAuthorization(TenantMemberInviteStatus inviteStatus)
    {
        InsertUserRole(1, TargetTenantId, TargetUserId, roleId: 11);
        InsertUserRole(2, TargetTenantId, TargetUserId, roleId: 12);
        InsertUserRole(3, OtherTenantId, TargetUserId, roleId: 13);
        InsertUserRole(4, TargetTenantId, userId: 91, roleId: 14);

        await BuildMembershipHandler().HandleEventAsync(new TenantMembershipChangedDomainEvent(
            TargetTenantId, TargetUserId, inviteStatus, operatorUserId: 1, reason: "离职"));

        var remaining = _client.Queryable<SysUserRole>().ToList();
        Assert.Equal(2, remaining.Count);
        Assert.DoesNotContain(remaining, item => item.TenantId == TargetTenantId && item.UserId == TargetUserId);

        _invalidator.Verify(target => target.InvalidateAuthorizationAsync(TargetUserId, It.IsAny<CancellationToken>()), Times.Once);
        _invalidator.Verify(target => target.InvalidateAuthorizationAsync(null, It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 该用户在此租户下本就没有角色绑定时不产生任何写入，也没有快照需要重建。
    /// </summary>
    [Fact]
    public async Task TenantMembershipChanged_WithoutBindings_ShouldNotTouchCache()
    {
        InsertUserRole(1, OtherTenantId, TargetUserId, roleId: 11);

        await BuildMembershipHandler().HandleEventAsync(new TenantMembershipChangedDomainEvent(
            TargetTenantId, TargetUserId, TenantMemberInviteStatus.Revoked));

        Assert.Single(_client.Queryable<SysUserRole>().ToList());
        _invalidator.Verify(
            target => target.InvalidateAuthorizationAsync(It.IsAny<long?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 成员被接受/邀请中等非终止状态不解绑角色，也不清缓存。
    /// </summary>
    [Fact]
    public async Task TenantMembershipChanged_Accepted_ShouldKeepBindings()
    {
        InsertUserRole(1, TargetTenantId, TargetUserId, roleId: 11);

        await BuildMembershipHandler().HandleEventAsync(new TenantMembershipChangedDomainEvent(
            TargetTenantId, TargetUserId, TenantMemberInviteStatus.Accepted));

        Assert.Single(_client.Queryable<SysUserRole>().ToList());
        _invalidator.Verify(
            target => target.InvalidateAuthorizationAsync(It.IsAny<long?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 缓存失效失败不回滚已落库的解绑，也不向外冒泡。
    /// </summary>
    [Fact]
    public async Task TenantMembershipChanged_WhenInvalidationFails_ShouldNotRethrowAndKeepDeletion()
    {
        _invalidator
            .Setup(target => target.InvalidateAuthorizationAsync(It.IsAny<long?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("缓存不可用"));
        InsertUserRole(1, TargetTenantId, TargetUserId, roleId: 11);

        await BuildMembershipHandler().HandleEventAsync(new TenantMembershipChangedDomainEvent(
            TargetTenantId, TargetUserId, TenantMemberInviteStatus.Revoked));

        Assert.Empty(_client.Queryable<SysUserRole>().ToList());
    }

    /// <summary>
    /// 构造函数拒绝 null 依赖。
    /// </summary>
    [Fact]
    public void TenantMembershipChanged_NullDependencies_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new TenantMembershipChangedEventHandler(null!, _invalidator.Object, NullLogger<TenantMembershipChangedEventHandler>.Instance));
        Assert.Throws<ArgumentNullException>(() =>
            new TenantMembershipChangedEventHandler(_resolver, null!, NullLogger<TenantMembershipChangedEventHandler>.Instance));
        Assert.Throws<ArgumentNullException>(() =>
            new TenantMembershipChangedEventHandler(_resolver, _invalidator.Object, null!));
    }

    /// <summary>
    /// 事件为 null 时立即抛错。
    /// </summary>
    [Fact]
    public async Task TenantMembershipChanged_NullEvent_ShouldThrow()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => BuildMembershipHandler().HandleEventAsync(null!));
    }

    /// <summary>
    /// 构造租户状态变更处理器。
    /// </summary>
    /// <returns>被测处理器。</returns>
    private TenantStatusChangedEventHandler BuildStatusHandler()
    {
        return new TenantStatusChangedEventHandler(
            _resolver, _invalidator.Object, NullLogger<TenantStatusChangedEventHandler>.Instance);
    }

    /// <summary>
    /// 构造租户成员变更处理器。
    /// </summary>
    /// <returns>被测处理器。</returns>
    private TenantMembershipChangedEventHandler BuildMembershipHandler()
    {
        return new TenantMembershipChangedEventHandler(
            _resolver, _invalidator.Object, NullLogger<TenantMembershipChangedEventHandler>.Instance);
    }

    /// <summary>
    /// 插入一条会话行。
    /// </summary>
    /// <param name="basicId">主键。</param>
    /// <param name="tenantId">租户标识。</param>
    /// <param name="userSessionId">会话业务标识。</param>
    /// <param name="status">会话状态。</param>
    private void InsertSession(long basicId, long tenantId, string userSessionId, SessionStatus status)
    {
        var session = new SysUserSession
        {
            TenantId = tenantId,
            UserId = TargetUserId,
            UserSessionId = userSessionId,
            Status = status,
            LoginTime = DateTimeOffset.UnixEpoch,
            LastActivityTime = DateTimeOffset.UnixEpoch,
            CreatedTime = DateTimeOffset.UnixEpoch
        };
        SetBasicId(session, basicId);
        _ = _client.Insertable(session).ExecuteCommand();
    }

    /// <summary>
    /// 插入一条用户角色绑定行。
    /// </summary>
    /// <param name="basicId">主键。</param>
    /// <param name="tenantId">租户标识。</param>
    /// <param name="userId">用户标识。</param>
    /// <param name="roleId">角色标识。</param>
    private void InsertUserRole(long basicId, long tenantId, long userId, long roleId)
    {
        var binding = new SysUserRole
        {
            TenantId = tenantId,
            UserId = userId,
            RoleId = roleId,
            CreatedTime = DateTimeOffset.UnixEpoch
        };
        SetBasicId(binding, basicId);
        _ = _client.Insertable(binding).ExecuteCommand();
    }

    /// <summary>
    /// 从库中读回会话，确保断言看到的是落库后的值。
    /// </summary>
    /// <param name="basicId">主键。</param>
    /// <returns>会话行。</returns>
    private SysUserSession ReadSession(long basicId)
    {
        return _client.Queryable<SysUserSession>().Single(session => session.BasicId == basicId);
    }

    /// <summary>
    /// 主键 setter 对外不可见，测试沿用仓储用例一致的反射回填方式模拟 SqlSugar 的主键赋值。
    /// </summary>
    /// <param name="entity">实体。</param>
    /// <param name="basicId">主键。</param>
    private static void SetBasicId(object entity, long basicId)
    {
        entity.GetType()
            .GetProperty("BasicId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(entity, basicId);
    }

    /// <summary>
    /// 把处理器固定接到单一测试连接上的解析器替身。
    /// </summary>
    /// <param name="client">测试连接。</param>
    private sealed class SingleClientResolver(ISqlSugarClient client) : ISqlSugarClientResolver
    {
        /// <summary>
        /// 获取当前租户对应的客户端
        /// </summary>
        /// <returns>当前 Scope 级客户端</returns>
        public ISqlSugarClient GetCurrentClient() => client;

        /// <summary>
        /// 按 ConfigId 获取指定客户端
        /// </summary>
        /// <param name="configId">连接配置标识</param>
        /// <returns>Scope 级客户端</returns>
        public ISqlSugarClient GetClient(string configId) => client;

        /// <summary>
        /// 获取全部连接配置标识
        /// </summary>
        /// <returns>连接配置标识集合</returns>
        public IReadOnlyCollection<string> GetAllConfigIds() => [];

        /// <summary>
        /// 按顺序获取所有库的客户端
        /// </summary>
        /// <returns>客户端集合</returns>
        public IEnumerable<ISqlSugarClient> GetAllClients() => [client];

        /// <summary>
        /// 底层 SqlSugarScope
        /// </summary>
        /// <returns>多库切换入口</returns>
        public ITenant AsTenant() => throw new NotSupportedException();
    }
}
