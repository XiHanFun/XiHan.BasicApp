#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DataScopeChangedEventHandler
// Guid:8b9c0d1e-2f34-5678-bcde-678901234567
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
/// 数据权限范围变更事件处理器
/// </summary>
/// <remarks>
/// 当角色或用户的数据权限范围发生变更时，失效相关的授权缓存快照。
/// 数据范围变更直接影响后续查询的数据过滤行为。
/// </remarks>
public sealed class DataScopeChangedEventHandler : ILocalEventHandler<DataScopeChangedDomainEvent>
{
    private readonly ISaasCacheInvalidator _cacheInvalidator;
    private readonly ILogger<DataScopeChangedEventHandler> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DataScopeChangedEventHandler(
        ISaasCacheInvalidator cacheInvalidator,
        ILogger<DataScopeChangedEventHandler> logger)
    {
        _cacheInvalidator = cacheInvalidator ?? throw new ArgumentNullException(nameof(cacheInvalidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 处理数据权限范围变更事件
    /// </summary>
    /// <param name="eventData">事件数据</param>
    public async Task HandleEventAsync(DataScopeChangedDomainEvent eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        _logger.LogInformation(
            "[DataScopeChanged] Data scope changed: TargetType={TargetType}, TargetId={TargetId}, DataScope={DataScope}",
            eventData.TargetType, eventData.TargetId, eventData.DataScope);

        try
        {
            // 如果目标是用户类型，精准失效该用户的授权快照
            if (string.Equals(eventData.TargetType, "User", StringComparison.OrdinalIgnoreCase))
            {
                await _cacheInvalidator.InvalidateAuthorizationAsync(eventData.TargetId);
            }
            else
            {
                // 角色数据范围变更，全量失效
                await _cacheInvalidator.InvalidateAuthorizationAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[DataScopeChanged] Failed to invalidate caches for TargetType={TargetType}, TargetId={TargetId}",
                eventData.TargetType, eventData.TargetId);
        }
    }
}
