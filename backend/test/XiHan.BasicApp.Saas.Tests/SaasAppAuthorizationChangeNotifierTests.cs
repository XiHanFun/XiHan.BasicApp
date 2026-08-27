// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using XiHan.BasicApp.Saas.Application.Authorization;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.Framework.EventBus.Abstractions.Local;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Security.Users;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 授权变更通知器测试。
/// </summary>
/// <remarks>
/// 通知器是所有授权写路径（角色权限、用户直授、用户角色的授予/撤销）的统一出口：
/// 它把「当时的租户上下文 + 操作人」封进事件，下游据此做缓存失效与权限变更审计。
/// 上下文取错的后果很隐蔽——事件照发、缓存照清，但审计台账里记的租户/操作人是错的，
/// 而且平台态操作（租户上下文为 null）必须归一成 <c>TenantId = 0</c>，
/// 不然审计记录会混进一个不存在的租户号。
/// </remarks>
public sealed class SaasAppAuthorizationChangeNotifierTests
{
    private readonly Mock<ILocalEventBus> _eventBus = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<ICurrentTenant> _currentTenant = new();

    /// <summary>
    /// 通知必须原样携带变更类型与三个目标标识，下游据此决定精准失效还是全量失效。
    /// </summary>
    [Fact]
    public async Task NotifyAsync_ShouldPublishEventCarryingChangeTypeAndTargets()
    {
        var published = CaptureEvent();
        _currentTenant.SetupGet(tenant => tenant.Id).Returns(6);
        _currentUser.SetupGet(user => user.UserId).Returns(99);

        await BuildNotifier().NotifyAsync(PermissionChangeType.UserGrantPermission, 42, 7, 13, "手工授予");

        Assert.NotNull(published.Value);
        Assert.Equal(PermissionChangeType.UserGrantPermission, published.Value!.ChangeType);
        Assert.Equal(42, published.Value.TargetUserId);
        Assert.Equal(7, published.Value.TargetRoleId);
        Assert.Equal(13, published.Value.PermissionId);
        Assert.Equal("手工授予", published.Value.Reason, StringComparer.Ordinal);
    }

    /// <summary>
    /// 租户上下文原样进事件，审计台账据此归属租户。
    /// </summary>
    [Fact]
    public async Task NotifyAsync_ShouldTakeTenantFromCurrentContext()
    {
        var published = CaptureEvent();
        _currentTenant.SetupGet(tenant => tenant.Id).Returns(6);

        await BuildNotifier().NotifyAsync(PermissionChangeType.RoleGrantPermission, null, 7, 13);

        Assert.Equal(6, published.Value!.TenantId);
    }

    /// <summary>
    /// 平台态（当前租户为 null）必须归一成 <c>TenantId = 0</c>，而不是留空或造出别的租户号。
    /// </summary>
    /// <remarks>
    /// 这是 <c>BasicAppEntity</c> 全仓统一的租户语义：平台/全局记录一律 0，不用可空值表达全局。
    /// </remarks>
    [Fact]
    public async Task NotifyAsync_PlatformContext_ShouldNormalizeTenantToZero()
    {
        var published = CaptureEvent();
        _currentTenant.SetupGet(tenant => tenant.Id).Returns((long?)null);

        await BuildNotifier().NotifyAsync(PermissionChangeType.RoleGrantPermission, null, 7, 13);

        Assert.Equal(0, published.Value!.TenantId);
    }

    /// <summary>
    /// 操作人取自当前用户上下文；未登录（如后台流程触发）时留空而不是伪造一个 0。
    /// </summary>
    [Fact]
    public async Task NotifyAsync_ShouldTakeOperatorFromCurrentUserAndAllowAnonymous()
    {
        var withUser = CaptureEvent();
        _currentUser.SetupGet(user => user.UserId).Returns(99);
        await BuildNotifier().NotifyAsync(PermissionChangeType.UserAssignRole, 1, 2, null);
        Assert.Equal(99, withUser.Value!.OperatorUserId);

        var anonymous = CaptureEvent();
        _currentUser.SetupGet(user => user.UserId).Returns((long?)null);
        await BuildNotifier().NotifyAsync(PermissionChangeType.UserAssignRole, 1, 2, null);
        Assert.Null(anonymous.Value!.OperatorUserId);
    }

    /// <summary>
    /// 不传原因时事件里就是 null，不填充占位文案（审计里"没写原因"和"原因是空串"是两回事）。
    /// </summary>
    [Fact]
    public async Task NotifyAsync_WithoutReason_ShouldLeaveReasonNull()
    {
        var published = CaptureEvent();

        await BuildNotifier().NotifyAsync(PermissionChangeType.UserRemoveRole, 1, 2, null);

        Assert.Null(published.Value!.Reason);
    }

    /// <summary>
    /// 每次通知都产生一条独立事件，事件标识不复用。
    /// </summary>
    [Fact]
    public async Task NotifyAsync_ShouldPublishDistinctEventPerCall()
    {
        var events = new List<AuthorizationChangedDomainEvent>();
        _eventBus
            .Setup(bus => bus.PublishAsync(It.IsAny<AuthorizationChangedDomainEvent>(), It.IsAny<bool>()))
            .Callback<AuthorizationChangedDomainEvent, bool>((data, _) => events.Add(data))
            .Returns(Task.CompletedTask);

        var notifier = BuildNotifier();
        await notifier.NotifyAsync(PermissionChangeType.UserGrantPermission, 1, null, null);
        await notifier.NotifyAsync(PermissionChangeType.UserRevokePermission, 1, null, null);

        Assert.Equal(2, events.Count);
        Assert.NotEqual(events[0].EventId, events[1].EventId);
    }

    /// <summary>
    /// 已取消的令牌必须在发布前就抛出，不能先把事件发出去再报取消。
    /// </summary>
    [Fact]
    public async Task NotifyAsync_CancelledToken_ShouldThrowBeforePublishing()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            BuildNotifier().NotifyAsync(PermissionChangeType.UserGrantPermission, 1, null, null, null, cts.Token));

        _eventBus.Verify(
            bus => bus.PublishAsync(It.IsAny<AuthorizationChangedDomainEvent>(), It.IsAny<bool>()),
            Times.Never);
    }

    /// <summary>
    /// 实现必须满足契约接口，授权写路径统一依赖抽象而非具体实现。
    /// </summary>
    [Fact]
    public void Notifier_ShouldImplementContract()
    {
        Assert.True(typeof(IAuthorizationChangeNotifier).IsAssignableFrom(typeof(AuthorizationChangeNotifier)));
        Assert.True(typeof(AuthorizationChangeNotifier).IsSealed);
    }

    /// <summary>
    /// 捕获发布出去的事件。
    /// </summary>
    /// <returns>事件容器。</returns>
    private Capture CaptureEvent()
    {
        var capture = new Capture();
        _eventBus
            .Setup(bus => bus.PublishAsync(It.IsAny<AuthorizationChangedDomainEvent>(), It.IsAny<bool>()))
            .Callback<AuthorizationChangedDomainEvent, bool>((data, _) => capture.Value = data)
            .Returns(Task.CompletedTask);
        return capture;
    }

    /// <summary>
    /// 构造被测通知器。
    /// </summary>
    /// <returns>通知器。</returns>
    private AuthorizationChangeNotifier BuildNotifier()
    {
        return new AuthorizationChangeNotifier(_eventBus.Object, _currentUser.Object, _currentTenant.Object);
    }

    /// <summary>
    /// 事件捕获容器。
    /// </summary>
    private sealed class Capture
    {
        /// <summary>
        /// 捕获到的事件。
        /// </summary>
        public AuthorizationChangedDomainEvent? Value { get; set; }
    }
}
