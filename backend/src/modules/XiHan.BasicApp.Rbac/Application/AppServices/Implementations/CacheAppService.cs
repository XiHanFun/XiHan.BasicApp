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
    public string? GetString(string key)
    {
        ValidateKey(key);
        return _distributedCache.GetString(key);
    }

    /// <summary>
    /// 设置缓存字符串
    /// </summary>
    public void SetString(string key, string value, int expireSeconds = 300)
    {
        ValidateKey(key);
        ArgumentNullException.ThrowIfNull(value);

        var seconds = expireSeconds > 0 ? expireSeconds : 300;
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(seconds)
        };

        _distributedCache.SetString(key, value, options);
        AddIndexKey(key);
    }

    /// <summary>
    /// 删除缓存项
    /// </summary>
    public void Remove(string key)
    {
        ValidateKey(key);
        _distributedCache.Remove(key);
        RemoveIndexKeys([key]);
    }

    /// <summary>
    /// 批量删除缓存项
    /// </summary>
    public void RemoveMany(IReadOnlyCollection<string> keys)
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
            multipleItemsCache.RemoveMany(keyArray);
        }
        else
        {
            foreach (var key in keyArray)
            {
                _distributedCache.Remove(key);
            }
        }

        RemoveIndexKeys(keyArray);
    }

    /// <summary>
    /// 判断缓存键是否存在
    /// </summary>
    public bool Exists(string key)
    {
        ValidateKey(key);
        return _distributedCache.Get(key) is not null;
    }

    /// <summary>
    /// 按模式获取缓存键
    /// </summary>
    public IReadOnlyCollection<string> GetKeys(string pattern = "*")
    {
        var normalizedPattern = NormalizePattern(pattern);
        if (_distributedCache is ICacheSupportsKeyPattern keyPatternCache)
        {
            return keyPatternCache.GetKeys(normalizedPattern);
        }

        var allKeys = GetIndexKeys();
        return [.. allKeys.Where(key => IsMatchPattern(key, normalizedPattern))];
    }

    /// <summary>
    /// 按模式删除缓存项
    /// </summary>
    public long RemoveByPattern(string pattern = "*")
    {
        var normalizedPattern = NormalizePattern(pattern);
        if (_distributedCache is ICacheSupportsKeyPattern keyPatternCache)
        {
            return keyPatternCache.RemoveByPattern(normalizedPattern);
        }

        var matchedKeys = GetKeys(normalizedPattern);
        if (matchedKeys.Count == 0)
        {
            return 0;
        }

        RemoveMany([.. matchedKeys]);
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

    private static bool IsMatchPattern(string input, string wildcardPattern)
    {
        if (wildcardPattern == "*")
        {
            return true;
        }

        var escapedPattern = Regex.Escape(wildcardPattern).Replace("\\*", ".*", StringComparison.Ordinal);
        return Regex.IsMatch(input, $"^{escapedPattern}$", RegexOptions.CultureInvariant | RegexOptions.Singleline);
    }

    private void AddIndexKey(string key)
    {
        var keySet = GetIndexKeys();
        if (!keySet.Add(key))
        {
            return;
        }

        SaveIndexKeys(keySet);
    }

    private void RemoveIndexKeys(IEnumerable<string> keys)
    {
        var keySet = GetIndexKeys();
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

        SaveIndexKeys(keySet);
    }

    private HashSet<string> GetIndexKeys()
    {
        var rawValue = _distributedCache.GetString(CacheKeyIndexKey);
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

    private void SaveIndexKeys(HashSet<string> keySet)
    {
        if (keySet.Count == 0)
        {
            _distributedCache.Remove(CacheKeyIndexKey);
            return;
        }

        var payload = JsonSerializer.Serialize(keySet.ToArray());
        _distributedCache.SetString(CacheKeyIndexKey, payload);
    }
}
