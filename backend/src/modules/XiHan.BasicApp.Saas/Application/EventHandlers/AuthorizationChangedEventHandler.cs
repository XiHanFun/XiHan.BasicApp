#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AuthorizationChangedEventHandler
// Guid:7a8b9c0d-1e2f-3456-abcd-567890123456
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/12 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
            "[AuthorizationChanged] Authorization changed: TargetType={TargetType}, TargetId={TargetId}, PermissionId={PermissionId}, Action={Action}",
            eventData.TargetType, eventData.TargetId, eventData.PermissionId, eventData.Action);

        try
        {
            // 如果目标是用户类型，精准失效该用户的授权快照
            if (string.Equals(eventData.TargetType, "User", StringComparison.OrdinalIgnoreCase))
            {
                await _cacheInvalidator.InvalidateAuthorizationAsync(eventData.TargetId);
                _logger.LogDebug(
                    "[AuthorizationChanged] Invalidated authorization cache for user {UserId}", eventData.TargetId);
            }
            else
            {
                // 角色或其他类型的授权变更，全量失效授权快照
                await _cacheInvalidator.InvalidateAuthorizationAsync();

                // 同时失效导航缓存（授权变更可能影响菜单可见性）
                await _cacheInvalidator.InvalidateNavigationAsync();

                _logger.LogDebug(
                    "[AuthorizationChanged] Invalidated all authorization and navigation caches (TargetType={TargetType})",
                    eventData.TargetType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[AuthorizationChanged] Failed to invalidate caches for TargetType={TargetType}, TargetId={TargetId}",
                eventData.TargetType, eventData.TargetId);
        }
    }
}
