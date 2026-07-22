// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 聊天敏感词守卫：命中即 fail-closed 拒绝发送/编辑
/// </summary>
public interface IChatSensitiveWordGuard
{
    /// <summary>
    /// 校验文本内容不含敏感词（命中抛出业务异常）
    /// </summary>
    Task EnsureAllowedAsync(string? content, CancellationToken cancellationToken = default);
}

/// <summary>
/// 聊天敏感词守卫实现
/// </summary>
/// <remarks>
/// 词库来自 SysConfig 键 <c>saas:chat:sensitive-words</c>（全局 TenantId=0，换行/中英文逗号/分号分隔，空=关闭），
/// 经系统设置页维护；进程内缓存 60s（词库变更最迟一分钟生效），OrdinalIgnoreCase 包含匹配。
/// </remarks>
public sealed class ChatSensitiveWordGuard : IChatSensitiveWordGuard, IScopedDependency
{
    /// <summary>
    /// 敏感词配置键（系统设置页维护）
    /// </summary>
    public const string ConfigKey = "saas:chat:sensitive-words";

    private const int CacheSeconds = 60;

    private static readonly char[] Separators = ['\n', '\r', ',', '，', ';', '；', '、'];

    private static volatile CachedWords? _cache;

    private readonly ISqlSugarClientResolver _clientResolver;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ChatSensitiveWordGuard(ISqlSugarClientResolver clientResolver)
    {
        _clientResolver = clientResolver;
    }

    /// <inheritdoc />
    public async Task EnsureAllowedAsync(string? content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        var words = await GetWordsAsync(cancellationToken);
        if (words.Count == 0)
        {
            return;
        }

        foreach (var word in words)
        {
            if (content.Contains(word, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("消息包含敏感词，已被拦截。");
            }
        }
    }

    private async Task<IReadOnlyList<string>> GetWordsAsync(CancellationToken cancellationToken)
    {
        var cache = _cache;
        if (cache is not null && DateTimeOffset.UtcNow - cache.LoadedAt < TimeSpan.FromSeconds(CacheSeconds))
        {
            return cache.Words;
        }

        var raw = await _clientResolver.GetCurrentClient()
            .Queryable<SysConfig>()
            .Where(config => config.ConfigKey == ConfigKey && config.TenantId == 0 && config.Status == EnableStatus.Enabled)
            .Select(config => config.ConfigValue)
            .FirstAsync(cancellationToken);

        var words = string.IsNullOrWhiteSpace(raw)
            ? []
            : raw.Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

        _cache = new CachedWords(words, DateTimeOffset.UtcNow);
        return words;
    }

    private sealed record CachedWords(IReadOnlyList<string> Words, DateTimeOffset LoadedAt);
}
