// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.EventHandlers;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.BasicApp.Saas.Hubs;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Web.RealTime.Services;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 事件处理器的缓存失效编排测试。
/// </summary>
/// <remarks>
/// 授权/数据范围/字段安全/组织层级四类变更事件的唯一职责就是"清对缓存"：
/// 清少了 = 变更不生效（改完权限用户还是老权限），清多了 = 无谓的缓存雪崩。
/// 这里逐条锁定"哪种事件清哪几份缓存"以及"用户级变更必须走精准失效而不是全量"。
/// 另外锁定所有处理器共有的一条约定：缓存失效失败只记日志、不能把事件处理整个炸掉。
/// </remarks>
public sealed class SaasAppEventHandlerCacheTests
{
    private readonly Mock<ISaasCacheInvalidator> _invalidator = new();

    /// <summary>
    /// 用户级授权变更（直授权限、用户角色）走按用户精准失效，不清空全量快照。
    /// </summary>
    [Fact]
    public async Task AuthorizationChanged_WithTargetUser_ShouldInvalidateThatUserOnly()
    {
        var handler = new AuthorizationChangedEventHandler(_invalidator.Object, NullLogger<AuthorizationChangedEventHandler>.Instance);

        await handler.HandleEventAsync(new AuthorizationChangedDomainEvent(
            1, PermissionChangeType.UserGrantPermission, targetUserId: 42, targetRoleId: null, permissionId: 7));

        _invalidator.Verify(target => target.InvalidateAuthorizationAsync(42, It.IsAny<CancellationToken>()), Times.Once);
        _invalidator.Verify(target => target.InvalidateAuthorizationAsync(null, It.IsAny<CancellationToken>()), Times.Never);
        _invalidator.Verify(target => target.InvalidateNavigationAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 目标用户为 0 或负数视为"非用户级"，退回角色级的全量失效路径。
    /// </summary>
    /// <param name="targetUserId">事件携带的目标用户标识。</param>
    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public async Task AuthorizationChanged_NonPositiveTargetUser_ShouldFallBackToFullInvalidation(long targetUserId)
    {
        var handler = new AuthorizationChangedEventHandler(_invalidator.Object, NullLogger<AuthorizationChangedEventHandler>.Instance);

        await handler.HandleEventAsync(new AuthorizationChangedDomainEvent(
            1, PermissionChangeType.RoleGrantPermission, targetUserId, targetRoleId: 5, permissionId: null));

        _invalidator.Verify(target => target.InvalidateAuthorizationAsync(null, It.IsAny<CancellationToken>()), Times.Once);
        _invalidator.Verify(target => target.InvalidateNavigationAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 角色级授权变更影响任意持有该角色的用户，必须同时全量失效授权快照与菜单导航。
    /// </summary>
    [Fact]
    public async Task AuthorizationChanged_RoleLevel_ShouldInvalidateAuthorizationAndNavigation()
    {
        var handler = new AuthorizationChangedEventHandler(_invalidator.Object, NullLogger<AuthorizationChangedEventHandler>.Instance);

        await handler.HandleEventAsync(new AuthorizationChangedDomainEvent(
            1, PermissionChangeType.RoleRevokePermission, targetUserId: null, targetRoleId: 9, permissionId: 3));

        _invalidator.Verify(target => target.InvalidateAuthorizationAsync(null, It.IsAny<CancellationToken>()), Times.Once);
        _invalidator.Verify(target => target.InvalidateNavigationAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 事件为 null 时立即抛错，不允许静默跳过一次缓存失效。
    /// </summary>
    [Fact]
    public async Task AuthorizationChanged_NullEvent_ShouldThrow()
    {
        var handler = new AuthorizationChangedEventHandler(_invalidator.Object, NullLogger<AuthorizationChangedEventHandler>.Instance);

        await Assert.ThrowsAsync<ArgumentNullException>(() => handler.HandleEventAsync(null!));
    }

    /// <summary>
    /// 构造函数拒绝 null 依赖（缺失失效器会让整条缓存一致性链路静默失效）。
    /// </summary>
    [Fact]
    public void AuthorizationChanged_NullDependencies_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new AuthorizationChangedEventHandler(null!, NullLogger<AuthorizationChangedEventHandler>.Instance));
        Assert.Throws<ArgumentNullException>(() =>
            new AuthorizationChangedEventHandler(_invalidator.Object, null!));
    }

    /// <summary>
    /// 缓存失效抛异常时只记日志，不向外冒泡——事件总线上的一次缓存故障不该拖垮业务写事务。
    /// </summary>
    [Fact]
    public async Task AuthorizationChanged_WhenInvalidatorThrows_ShouldSwallowAndNotRethrow()
    {
        _invalidator
            .Setup(target => target.InvalidateAuthorizationAsync(It.IsAny<long?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("缓存不可用"));

        var handler = new AuthorizationChangedEventHandler(_invalidator.Object, NullLogger<AuthorizationChangedEventHandler>.Instance);

        await handler.HandleEventAsync(new AuthorizationChangedDomainEvent(
            1, PermissionChangeType.UserGrantPermission, 42, null, null));
    }

    /// <summary>
    /// 用户维度的数据范围变更按用户精准失效。
    /// </summary>
    /// <param name="targetType">事件里的目标类型文本。</param>
    [Theory]
    [InlineData("User")]
    [InlineData("user")]
    [InlineData("USER")]
    public async Task DataScopeChanged_UserTarget_ShouldInvalidateThatUser(string targetType)
    {
        var handler = new DataScopeChangedEventHandler(_invalidator.Object, NullLogger<DataScopeChangedEventHandler>.Instance);

        await handler.HandleEventAsync(new DataScopeChangedDomainEvent(1, targetType, 55, DataPermissionScope.SelfOnly));

        _invalidator.Verify(target => target.InvalidateAuthorizationAsync(55, It.IsAny<CancellationToken>()), Times.Once);
        _invalidator.Verify(target => target.InvalidateAuthorizationAsync(null, It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 角色维度的数据范围变更影响面不可枚举，走全量失效。
    /// </summary>
    [Fact]
    public async Task DataScopeChanged_RoleTarget_ShouldInvalidateEverything()
    {
        var handler = new DataScopeChangedEventHandler(_invalidator.Object, NullLogger<DataScopeChangedEventHandler>.Instance);

        await handler.HandleEventAsync(new DataScopeChangedDomainEvent(1, "Role", 55, DataPermissionScope.All));

        _invalidator.Verify(target => target.InvalidateAuthorizationAsync(null, It.IsAny<CancellationToken>()), Times.Once);
        _invalidator.Verify(target => target.InvalidateAuthorizationAsync(55, It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 字段级安全策略影响所有相关用户的数据视图，只能整体失效授权缓存。
    /// </summary>
    [Fact]
    public async Task FieldLevelSecurityChanged_ShouldAlwaysInvalidateAllAuthorization()
    {
        var handler = new FieldLevelSecurityChangedEventHandler(_invalidator.Object, NullLogger<FieldLevelSecurityChangedEventHandler>.Instance);

        await handler.HandleEventAsync(new FieldLevelSecurityChangedDomainEvent(
            1, 10, FieldSecurityTargetType.Role, 20, 30, "Phone", isReadable: true, isEditable: false, FieldMaskStrategy.PartialMask));

        _invalidator.Verify(target => target.InvalidateAuthorizationAsync(null, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 组织层级变更影响菜单树，必须失效导航缓存。
    /// </summary>
    [Fact]
    public async Task HierarchyChanged_ShouldAlwaysInvalidateNavigation()
    {
        var handler = new HierarchyChangedEventHandler(_invalidator.Object, NullLogger<HierarchyChangedEventHandler>.Instance);

        await handler.HandleEventAsync(new HierarchyChangedDomainEvent(1, "Department", 8, 3));

        _invalidator.Verify(target => target.InvalidateNavigationAsync(It.IsAny<CancellationToken>()), Times.Once);
        _invalidator.Verify(target => target.InvalidateAuthorizationAsync(It.IsAny<long?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 角色继承链变化会改变权限继承结果，导航之外还要再清一次授权快照。
    /// </summary>
    /// <param name="hierarchyType">层级类型文本。</param>
    [Theory]
    [InlineData("Role")]
    [InlineData("role")]
    public async Task HierarchyChanged_RoleHierarchy_ShouldAlsoInvalidateAuthorization(string hierarchyType)
    {
        var handler = new HierarchyChangedEventHandler(_invalidator.Object, NullLogger<HierarchyChangedEventHandler>.Instance);

        await handler.HandleEventAsync(new HierarchyChangedDomainEvent(1, hierarchyType, 8, null));

        _invalidator.Verify(target => target.InvalidateNavigationAsync(It.IsAny<CancellationToken>()), Times.Once);
        _invalidator.Verify(target => target.InvalidateAuthorizationAsync(null, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 单会话吊销必须按 session_id 精准失效会话状态缓存——这一刀决定了"踢下线是不是真踢得掉"。
    /// </summary>
    [Fact]
    public async Task UserSessionRevoked_SingleSession_ShouldInvalidateThatSessionState()
    {
        var handler = BuildSessionRevokedHandler();

        await handler.HandleEventAsync(new UserSessionRevokedDomainEvent(
            1, userId: 9, sessionId: 100, userSessionId: "sess-x", accessTokenJti: "jti"));

        _invalidator.Verify(target => target.InvalidateSessionStateAsync("sess-x", It.IsAny<CancellationToken>()), Times.Once);
        _invalidator.Verify(target => target.InvalidateAllSessionStatesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 批量吊销该用户全部会话时整体清空会话状态缓存。
    /// </summary>
    [Fact]
    public async Task UserSessionRevoked_RevokeAll_ShouldInvalidateAllSessionStates()
    {
        var handler = BuildSessionRevokedHandler();

        await handler.HandleEventAsync(new UserSessionRevokedDomainEvent(
            1, userId: 9, sessionId: null, userSessionId: "sess-x", accessTokenJti: null, revokeAllUserSessions: true));

        _invalidator.Verify(target => target.InvalidateAllSessionStatesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _invalidator.Verify(target => target.InvalidateSessionStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 拿不到 session_id 时无法精准定位，只能整体清空——宁可多清也不能漏清。
    /// </summary>
    /// <param name="userSessionId">事件携带的会话业务标识。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UserSessionRevoked_WithoutSessionId_ShouldFallBackToInvalidateAll(string? userSessionId)
    {
        var handler = BuildSessionRevokedHandler();

        await handler.HandleEventAsync(new UserSessionRevokedDomainEvent(
            1, userId: 9, sessionId: 100, userSessionId, accessTokenJti: null));

        _invalidator.Verify(target => target.InvalidateAllSessionStatesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 会话吊销的收尾动作里，只有实时推送是"通知前端自己登出"，服务端硬拦截靠会话状态缓存被清掉。
    /// 这里锁定：即使日志/通知/推送全部失败，缓存失效那一刀也必须已经落下。
    /// </summary>
    [Fact]
    public async Task UserSessionRevoked_WhenSideEffectsFail_ShouldStillInvalidateCacheFirst()
    {
        var realtime = new Mock<IRealtimeNotificationService<BasicAppNotificationHub>>();
        realtime
            .Setup(target => target.SendToUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object[]>()))
            .ThrowsAsync(new InvalidOperationException("SignalR 不可用"));

        var dispatch = new Mock<IUserNotificationDispatchService>();
        dispatch
            .Setup(target => target.DispatchToUserAsync(
                It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<NotificationType>(),
                It.IsAny<string?>(), It.IsAny<long?>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("站内信不可用"));

        var handler = new UserSessionRevokedEventHandler(
            new Mock<ISqlSugarClientResolver>().Object,
            realtime.Object,
            dispatch.Object,
            _invalidator.Object,
            NullLogger<UserSessionRevokedEventHandler>.Instance);

        await handler.HandleEventAsync(new UserSessionRevokedDomainEvent(
            1, userId: 9, sessionId: 100, userSessionId: "sess-x", accessTokenJti: null, operatorUserId: 1));

        _invalidator.Verify(target => target.InvalidateSessionStateAsync("sess-x", It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 缓存失效本身失败时不阻断吊销主流程（缓存有 60 秒短 TTL 兜底）。
    /// </summary>
    [Fact]
    public async Task UserSessionRevoked_WhenInvalidationFails_ShouldNotRethrow()
    {
        _invalidator
            .Setup(target => target.InvalidateSessionStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("缓存不可用"));

        var handler = BuildSessionRevokedHandler();

        await handler.HandleEventAsync(new UserSessionRevokedDomainEvent(
            1, userId: 9, sessionId: 100, userSessionId: "sess-x", accessTokenJti: null));
    }

    /// <summary>
    /// 自己踢自己的设备（个人中心「登出其他设备」）不发站内信；管理员踢他人才发。
    /// </summary>
    [Fact]
    public async Task UserSessionRevoked_SelfInitiated_ShouldNotSendNotification()
    {
        var dispatch = new Mock<IUserNotificationDispatchService>();
        var handler = BuildSessionRevokedHandler(dispatch);

        await handler.HandleEventAsync(new UserSessionRevokedDomainEvent(
            1, userId: 9, sessionId: 100, userSessionId: "sess-x", accessTokenJti: null, operatorUserId: 9));

        dispatch.Verify(
            target => target.DispatchToUserAsync(
                It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<NotificationType>(),
                It.IsAny<string?>(), It.IsAny<long?>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 管理员撤销他人会话时必须发出安全类站内信，且指向个人中心。
    /// </summary>
    [Fact]
    public async Task UserSessionRevoked_ByAnotherOperator_ShouldSendSecurityNotification()
    {
        var dispatch = new Mock<IUserNotificationDispatchService>();
        var handler = BuildSessionRevokedHandler(dispatch);

        await handler.HandleEventAsync(new UserSessionRevokedDomainEvent(
            1, userId: 9, sessionId: 100, userSessionId: "sess-x", accessTokenJti: null, operatorUserId: 1));

        dispatch.Verify(
            target => target.DispatchToUserAsync(
                9, "会话已撤销", It.IsAny<string?>(), NotificationType.Security,
                "auth.session.revoked", 100L, It.IsAny<bool>(), "/workbench/profile", "lucide:shield-alert",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 单会话吊销的实时推送必须带上目标会话列表，避免把该用户其它设备一起踢掉。
    /// </summary>
    [Fact]
    public async Task UserSessionRevoked_SingleSession_ShouldPushForceLogoutToThatUser()
    {
        var realtime = new Mock<IRealtimeNotificationService<BasicAppNotificationHub>>();
        var handler = BuildSessionRevokedHandler(realtime: realtime);

        await handler.HandleEventAsync(new UserSessionRevokedDomainEvent(
            1, userId: 9, sessionId: 100, userSessionId: "sess-x", accessTokenJti: null));

        realtime.Verify(
            target => target.SendToUserAsync("9", It.IsAny<string>(), It.IsAny<object[]>()),
            Times.Once);
    }

    /// <summary>
    /// 构造会话吊销处理器（依赖全部替身，SqlSugar 解析器返回空客户端，落库分支会被自身 try/catch 吞掉）。
    /// </summary>
    /// <param name="dispatch">站内信投递替身。</param>
    /// <param name="realtime">实时推送替身。</param>
    /// <returns>被测处理器。</returns>
    private UserSessionRevokedEventHandler BuildSessionRevokedHandler(
        Mock<IUserNotificationDispatchService>? dispatch = null,
        Mock<IRealtimeNotificationService<BasicAppNotificationHub>>? realtime = null)
    {
        return new UserSessionRevokedEventHandler(
            new Mock<ISqlSugarClientResolver>().Object,
            (realtime ?? new Mock<IRealtimeNotificationService<BasicAppNotificationHub>>()).Object,
            (dispatch ?? new Mock<IUserNotificationDispatchService>()).Object,
            _invalidator.Object,
            NullLogger<UserSessionRevokedEventHandler>.Instance);
    }
}
