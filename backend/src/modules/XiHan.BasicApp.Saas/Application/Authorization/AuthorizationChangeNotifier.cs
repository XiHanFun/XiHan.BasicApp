// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.Framework.EventBus.Abstractions.Local;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Security.Users;

namespace XiHan.BasicApp.Saas.Application.Authorization;

/// <summary>
/// 授权变更通知器实现
/// </summary>
public sealed class AuthorizationChangeNotifier : IAuthorizationChangeNotifier
{
    private readonly ILocalEventBus _localEventBus;

    private readonly ICurrentUser _currentUser;

    private readonly ICurrentTenant _currentTenant;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AuthorizationChangeNotifier(
        ILocalEventBus localEventBus,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant)
    {
        _localEventBus = localEventBus;
        _currentUser = currentUser;
        _currentTenant = currentTenant;
    }

    /// <inheritdoc />
    public Task NotifyAsync(
        PermissionChangeType changeType,
        long? targetUserId,
        long? targetRoleId,
        long? permissionId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var domainEvent = new AuthorizationChangedDomainEvent(
            _currentTenant.Id ?? 0,
            changeType,
            targetUserId,
            targetRoleId,
            permissionId,
            _currentUser.UserId,
            reason);

        return _localEventBus.PublishAsync(domainEvent);
    }
}
