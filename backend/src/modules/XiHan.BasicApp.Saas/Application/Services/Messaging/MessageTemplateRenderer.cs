#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MessageTemplateRenderer
// Guid:df825a7a-be19-44c5-a267-9eaf1d8eeba2
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/11 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Caching.Distributed.Abstracts;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Templating.Services;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 消息模板渲染器实现（分布式缓存 + 框架 Scriban 引擎）
/// </summary>
public sealed class MessageTemplateRenderer : IMessageTemplateRenderer
{
    private readonly IMessageTemplateRepository _messageTemplateRepository;

    private readonly ITemplateService _templateService;

    private readonly ICurrentTenant _currentTenant;

    private readonly IDistributedCache<SaasMessageTemplateCacheItem, string> _templateCache;

    private readonly ILogger<MessageTemplateRenderer> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MessageTemplateRenderer(
        IMessageTemplateRepository messageTemplateRepository,
        ITemplateService templateService,
        ICurrentTenant currentTenant,
        IDistributedCache<SaasMessageTemplateCacheItem, string> templateCache,
        ILogger<MessageTemplateRenderer> logger)
    {
        _messageTemplateRepository = messageTemplateRepository;
        _templateService = templateService;
        _currentTenant = currentTenant;
        _templateCache = templateCache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<RenderedMessage?> RenderAsync(
        MessageChannel channel,
        string templateCode,
        IDictionary<string, object?> variables,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateCode);
        ArgumentNullException.ThrowIfNull(variables);
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedCode = templateCode.Trim();
        var cacheKey = SaasCacheKeys.MessageTemplate(_currentTenant.Id, (int)channel, normalizedCode);

        // 模板源缓存（模板写路径调 InvalidateMessageTemplateAsync 整体失效）；
        // "未找到"以空 Content 哨兵负缓存，避免模板缺失时发送链路每次穿透数据库
        var item = await _templateCache.GetOrAddAsync(
            cacheKey,
            async () =>
            {
                var template = await _messageTemplateRepository.FindEnabledByCodeAsync(channel, normalizedCode, cancellationToken);
                return template is null
                    ? new SaasMessageTemplateCacheItem { Content = string.Empty, CachedAt = DateTimeOffset.UtcNow }
                    : new SaasMessageTemplateCacheItem
                    {
                        Subject = template.Subject,
                        Content = template.Content,
                        IsHtml = template.IsHtml,
                        CachedAt = DateTimeOffset.UtcNow
                    };
            },
            static () => new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            },
            hideErrors: true,
            token: cancellationToken);

        if (item is null || string.IsNullOrWhiteSpace(item.Content))
        {
            return null;
        }

        try
        {
            var content = await _templateService.RenderAsync(item.Content, variables);
            string? subject = null;
            if (!string.IsNullOrWhiteSpace(item.Subject))
            {
                subject = await _templateService.RenderAsync(item.Subject, variables);
            }

            return new RenderedMessage(subject, content, item.IsHtml);
        }
        catch (Exception exception)
        {
            // 模板源损坏不应中断发送链路：记录告警并交由调用方回退内置内容
            _logger.LogWarning(exception, "消息模板渲染失败，回退调用方内置内容。Channel={Channel}, Code={Code}", channel, normalizedCode);
            return null;
        }
    }
}
