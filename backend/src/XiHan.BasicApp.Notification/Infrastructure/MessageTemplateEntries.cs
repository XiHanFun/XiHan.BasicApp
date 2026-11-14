// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Notification.Domain.Entities;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Caching.Abstractions.Caches;
using XiHan.Framework.Caching.Abstractions.Stores;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;

namespace XiHan.BasicApp.Notification.Infrastructure;

/// <summary>
/// 发送时用的模板
/// </summary>
/// <param name="Code">编码</param>
/// <param name="Subject">主题</param>
/// <param name="Content">正文</param>
/// <param name="IsHtml">正文是否 HTML</param>
public sealed record ResolvedTemplate(string Code, string? Subject, string Content, bool IsHtml);

/// <summary>
/// 消息模板的读穿缓存（单例）
/// </summary>
/// <remarks>
/// 按（侧、渠道、编码）各存这一侧启用的那条，发送时先看本侧的、再看平台的：平台模板改了只失效平台那一份
/// 放在共享缓存里 10 分钟，模板写入即失效
/// </remarks>
/// <param name="cache">共享缓存</param>
/// <param name="scopes">作用域工厂（缓存未命中时用只读工作单元查主库）</param>
public sealed class MessageTemplateEntries(ISharedCache cache, IServiceScopeFactory scopes)
{
    private static readonly TimeSpan _timeToLive = TimeSpan.FromMinutes(10);

    /// <summary>
    /// 一侧启用的模板
    /// </summary>
    /// <returns>这一侧没有启用的这个模板即 <c>null</c></returns>
    public async Task<ResolvedTemplate?> OfAsync(string owner, TemplateChannel channel, string code, CancellationToken cancellationToken)
    {
        var raw = await cache.GetOrCreateAsync(KeyOf(owner, channel, code), ct => LoadAsync(owner, channel, code, ct), CacheEntryOptions.Expires(_timeToLive), cancellationToken).ConfigureAwait(false);
        return raw.Length == 0 ? null : JsonSerializer.Deserialize(raw, NotificationJsonContext.Default.ResolvedTemplate);
    }

    /// <summary>
    /// 一侧一个模板失效
    /// </summary>
    /// <remarks>
    /// 调用里推到工作单元结束之后再删除（<see cref="CacheInvalidation"/>），提交前的旧数据不会被回填进缓存
    /// </remarks>
    public async Task InvalidateAsync(string owner, TemplateChannel channel, string code, CancellationToken cancellationToken)
        => await CacheInvalidation.RemoveAsync(cache, KeyOf(owner, channel, code), cancellationToken).ConfigureAwait(false);

    private static CacheKey KeyOf(string owner, TemplateChannel channel, string code) => new($"notification:templates:{owner}:{channel}:{code}");

    private async ValueTask<string> LoadAsync(string owner, TemplateChannel channel, string code, CancellationToken cancellationToken)
        => await StoreScope.ReadPrimaryAsync(scopes, async (services, ct) =>
        {
            var rows = await services.GetRequiredService<IRepository<SysMessageTemplate, Guid>>()
                .ListAsync(EntityQuery.All<SysMessageTemplate>().Where(new Specification<SysMessageTemplate>(t => t.OwnerTenantId == owner && t.Channel == channel && t.Code == code && t.IsEnabled)).Page(0, 1), ct)
                .ConfigureAwait(false);
            return rows.Count == 0
                ? string.Empty
                : JsonSerializer.Serialize(new ResolvedTemplate(rows[0].Code, rows[0].Subject, rows[0].Content, rows[0].IsHtml), NotificationJsonContext.Default.ResolvedTemplate);
        }, cancellationToken).ConfigureAwait(false);
}
