// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.Framework.EventBus.Abstractions.Local;

namespace XiHan.BasicApp.Saas.Application.EventHandlers;

/// <summary>
/// 字段级安全变更事件处理器
/// </summary>
/// <remarks>
/// 当字段级安全策略（可读/可编辑/脱敏策略）发生变更时，失效相关的授权缓存快照。
/// 字段级安全变更直接影响 API 响应中的字段可见性和脱敏行为。
/// </remarks>
public sealed class FieldLevelSecurityChangedEventHandler : ILocalEventHandler<FieldLevelSecurityChangedDomainEvent>
{
    private readonly ISaasCacheInvalidator _cacheInvalidator;
    private readonly ILogger<FieldLevelSecurityChangedEventHandler> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public FieldLevelSecurityChangedEventHandler(
        ISaasCacheInvalidator cacheInvalidator,
        ILogger<FieldLevelSecurityChangedEventHandler> logger)
    {
        _cacheInvalidator = cacheInvalidator ?? throw new ArgumentNullException(nameof(cacheInvalidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 处理字段级安全变更事件
    /// </summary>
    /// <param name="eventData">事件数据</param>
    public async Task HandleEventAsync(FieldLevelSecurityChangedDomainEvent eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        _logger.LogInformation(
            "[FieldLevelSecurityChanged] FLS changed: FieldSecurityId={FieldSecurityId}, TargetType={TargetType}, TargetId={TargetId}, FieldName={FieldName}, Readable={IsReadable}, Editable={IsEditable}, MaskStrategy={MaskStrategy}",
            eventData.FieldSecurityId, eventData.TargetType, eventData.TargetId,
            eventData.FieldName, eventData.IsReadable, eventData.IsEditable, eventData.MaskStrategy);

        try
        {
            // 字段级安全变更需要全量失效授权缓存，因为 FLS 策略影响所有相关用户的数据视图
            await _cacheInvalidator.InvalidateAuthorizationAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[FieldLevelSecurityChanged] Failed to invalidate caches for FieldSecurityId={FieldSecurityId}",
                eventData.FieldSecurityId);
        }
    }
}
