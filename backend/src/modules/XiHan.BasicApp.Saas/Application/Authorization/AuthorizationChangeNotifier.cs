#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AuthorizationChangeNotifier
// Guid:c8f3a5d1-6e29-4b70-9a34-1f0d7c85be62
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/10 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
