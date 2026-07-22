// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.Framework.EventBus.Abstractions.Local;

namespace XiHan.BasicApp.Saas.Application.EventHandlers;

/// <summary>
/// 授权变更事件处理器
/// </summary>
/// <remarks>
/// 当授权（角色-权限、用户-角色、用户-权限）发生变更时，失效相关的授权缓存快照，
/// 确保下次鉴权决策时使用最新权限数据。
/// </remarks>
public sealed class AuthorizationChangedEventHandler : ILocalEventHandler<AuthorizationChangedDomainEvent>
{
    private readonly ISaasCacheInvalidator _cacheInvalidator;
    private readonly ILogger<AuthorizationChangedEventHandler> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AuthorizationChangedEventHandler(
        ISaasCacheInvalidator cacheInvalidator,
        ILogger<AuthorizationChangedEventHandler> logger)
    {
        _cacheInvalidator = cacheInvalidator ?? throw new ArgumentNullException(nameof(cacheInvalidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 处理授权变更事件
    /// </summary>
    /// <param name="eventData">事件数据</param>
    public async Task HandleEventAsync(AuthorizationChangedDomainEvent eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        _logger.LogInformation(
            "[AuthorizationChanged] Authorization changed: ChangeType={ChangeType}, TargetUserId={TargetUserId}, TargetRoleId={TargetRoleId}, PermissionId={PermissionId}",
            eventData.ChangeType, eventData.TargetUserId, eventData.TargetRoleId, eventData.PermissionId);

        try
        {
            // 用户级变更（直授权限、用户角色）：精准失效该用户的授权快照
            if (eventData.TargetUserId is > 0)
            {
                await _cacheInvalidator.InvalidateAuthorizationAsync(eventData.TargetUserId);
                _logger.LogDebug(
                    "[AuthorizationChanged] Invalidated authorization cache for user {UserId}", eventData.TargetUserId);
            }
            else
            {
                // 角色级变更（角色权限）可能影响任意持有该角色的用户，全量失效授权快照
                await _cacheInvalidator.InvalidateAuthorizationAsync();

                // 同时失效导航缓存（授权变更可能影响菜单可见性）
                await _cacheInvalidator.InvalidateNavigationAsync();

                _logger.LogDebug(
                    "[AuthorizationChanged] Invalidated all authorization and navigation caches (TargetRoleId={TargetRoleId})",
                    eventData.TargetRoleId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[AuthorizationChanged] Failed to invalidate caches for ChangeType={ChangeType}, TargetUserId={TargetUserId}, TargetRoleId={TargetRoleId}",
                eventData.ChangeType, eventData.TargetUserId, eventData.TargetRoleId);
        }
    }
}
