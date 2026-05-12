#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:HierarchyChangedEventHandler
// Guid:0d1e2f34-5678-90ab-cdef-890123456789
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
/// 层级关系变更事件处理器
/// </summary>
/// <remarks>
/// 当组织层级（部门树、角色继承链等）发生变更时，失效导航缓存。
/// 层级变更会影响菜单路由展示和权限继承关系。
/// </remarks>
public sealed class HierarchyChangedEventHandler : ILocalEventHandler<HierarchyChangedDomainEvent>
{
    private readonly ISaasCacheInvalidator _cacheInvalidator;
    private readonly ILogger<HierarchyChangedEventHandler> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public HierarchyChangedEventHandler(
        ISaasCacheInvalidator cacheInvalidator,
        ILogger<HierarchyChangedEventHandler> logger)
    {
        _cacheInvalidator = cacheInvalidator ?? throw new ArgumentNullException(nameof(cacheInvalidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 处理层级关系变更事件
    /// </summary>
    /// <param name="eventData">事件数据</param>
    public async Task HandleEventAsync(HierarchyChangedDomainEvent eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        _logger.LogInformation(
            "[HierarchyChanged] Hierarchy changed: Type={HierarchyType}, NodeId={NodeId}, ParentId={ParentId}",
            eventData.HierarchyType, eventData.NodeId, eventData.ParentId);

        try
        {
            // 层级变更影响导航缓存（菜单路由树可能变化）
            await _cacheInvalidator.InvalidateNavigationAsync();

            // 如果层级类型是角色层级（Role），还需失效授权缓存快照
            if (string.Equals(eventData.HierarchyType, "Role", StringComparison.OrdinalIgnoreCase))
            {
                await _cacheInvalidator.InvalidateAuthorizationAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[HierarchyChanged] Failed to invalidate caches for HierarchyType={HierarchyType}, NodeId={NodeId}",
                eventData.HierarchyType, eventData.NodeId);
        }
    }
}
