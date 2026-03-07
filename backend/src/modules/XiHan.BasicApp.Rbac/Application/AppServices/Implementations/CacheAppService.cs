#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CacheAppService
// Guid:9a67eaf4-3eca-4072-b5c3-0ae47f3b66ab
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 14:17:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using System.Text.RegularExpressions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Caching.Distributed.Abstracts;

namespace XiHan.BasicApp.Rbac.Application.AppServices.Implementations;

/// <summary>
/// 缓存应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.Rbac", GroupName = "系统Rbac服务")]
public class CacheAppService : ApplicationServiceBase, ICacheAppService
{
    private const string CacheKeyIndexKey = "basicapp:cache:index:keys";
    private readonly IDistributedCache _distributedCache;

    /// <summary>
    /// 构造函数
    /// </summary>
    public CacheAppService(
        IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
    }

    /// <summary>
    /// 获取缓存字符串
    /// </summary>
    public async Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        return await _distributedCache.GetStringAsync(key, cancellationToken);
    }

    /// <summary>
    /// 设置缓存字符串
    /// </summary>
    public async Task SetStringAsync(string key, string value, int expireSeconds = 300, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        ArgumentNullException.ThrowIfNull(value);

        var seconds = expireSeconds > 0 ? expireSeconds : 300;
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(seconds)
        };

        await _distributedCache.SetStringAsync(key, value, options, cancellationToken);
        await AddIndexKeyAsync(key, cancellationToken);
    }

    /// <summary>
    /// 删除缓存项
    /// </summary>
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        await _distributedCache.RemoveAsync(key, cancellationToken);
        await RemoveIndexKeysAsync([key], cancellationToken);
    }

    /// <summary>
    /// 批量删除缓存项
    /// </summary>
    public async Task RemoveManyAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keys);
        var keyArray = keys
            .Where(static key => !string.IsNullOrWhiteSpace(key))
            .Select(static key => key.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (keyArray.Length == 0)
        {
            return;
        }

        if (_distributedCache is ICacheSupportsMultipleItems multipleItemsCache)
        {
            await multipleItemsCache.RemoveManyAsync(keyArray, cancellationToken);
        }
        else
        {
            foreach (var key in keyArray)
            {
                await _distributedCache.RemoveAsync(key, cancellationToken);
            }
        }

        await RemoveIndexKeysAsync(keyArray, cancellationToken);
    }

    /// <summary>
    /// 判断缓存键是否存在
    /// </summary>
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        return await _distributedCache.GetAsync(key, cancellationToken) is not null;
    }

    /// <summary>
    /// 按模式获取缓存键
    /// </summary>
    public async Task<IReadOnlyCollection<string>> GetKeysAsync(string pattern = "*", CancellationToken cancellationToken = default)
    {
        var normalizedPattern = NormalizePattern(pattern);
        if (_distributedCache is ICacheSupportsKeyPattern keyPatternCache)
        {
            return await keyPatternCache.GetKeysAsync(normalizedPattern, cancellationToken);
        }

        var allKeys = await GetIndexKeysAsync(cancellationToken);
        return [.. allKeys.Where(key => IsMatchPattern(key, normalizedPattern))];
    }

    /// <summary>
    /// 按模式删除缓存项
    /// </summary>
    public async Task<long> RemoveByPatternAsync(string pattern = "*", CancellationToken cancellationToken = default)
    {
        var normalizedPattern = NormalizePattern(pattern);
        if (_distributedCache is ICacheSupportsKeyPattern keyPatternCache)
        {
            return await keyPatternCache.RemoveByPatternAsync(normalizedPattern, cancellationToken);
        }

        var matchedKeys = await GetKeysAsync(normalizedPattern, cancellationToken);
        if (matchedKeys.Count == 0)
        {
            return 0;
        }

        await RemoveManyAsync([.. matchedKeys], cancellationToken);
        return matchedKeys.Count;
    }

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("缓存键不能为空", nameof(key));
        }
    }

    private static string NormalizePattern(string pattern)
    {
        return string.IsNullOrWhiteSpace(pattern) ? "*" : pattern.Trim();
    }

    private async Task AddIndexKeyAsync(string key, CancellationToken cancellationToken)
    {
        var keySet = await GetIndexKeysAsync(cancellationToken);
        if (!keySet.Add(key))
        {
            return;
        }

        await SaveIndexKeysAsync(keySet, cancellationToken);
    }

    private async Task RemoveIndexKeysAsync(IEnumerable<string> keys, CancellationToken cancellationToken)
    {
        var keySet = await GetIndexKeysAsync(cancellationToken);
        var changed = false;
        foreach (var key in keys)
        {
            if (keySet.Remove(key))
            {
                changed = true;
            }
        }

        if (!changed)
        {
            return;
        }

        await SaveIndexKeysAsync(keySet, cancellationToken);
    }

    private async Task<HashSet<string>> GetIndexKeysAsync(CancellationToken cancellationToken)
    {
        var rawValue = await _distributedCache.GetStringAsync(CacheKeyIndexKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        try
        {
            var keys = JsonSerializer.Deserialize<List<string>>(rawValue) ?? [];
            return new HashSet<string>(keys.Where(static key => !string.IsNullOrWhiteSpace(key)), StringComparer.Ordinal);
        }
        catch (JsonException)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }
    }

    private async Task SaveIndexKeysAsync(HashSet<string> keySet, CancellationToken cancellationToken)
    {
        if (keySet.Count == 0)
        {
            await _distributedCache.RemoveAsync(CacheKeyIndexKey, cancellationToken);
            return;
        }

        var payload = JsonSerializer.Serialize(keySet.ToArray());
        await _distributedCache.SetStringAsync(CacheKeyIndexKey, payload, cancellationToken);
    }

    private static bool IsMatchPattern(string input, string wildcardPattern)
    {
        if (wildcardPattern == "*")
        {
            return true;
        }

        var escapedPattern = Regex.Escape(wildcardPattern).Replace("\\*", ".*", StringComparison.Ordinal);
        return Regex.IsMatch(input, $"^{escapedPattern}$", RegexOptions.CultureInvariant | RegexOptions.Singleline);
    }
}
