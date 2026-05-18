#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CacheManagementService
// Guid:3e7194b2-12ee-4375-802e-5f5690b9d6d9
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Caching.Distributed;
using XiHan.Framework.Caching.Distributed.Abstracts;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 缓存管理应用服务实现
/// </summary>
public sealed class CacheManagementService
    : ICacheManagementService
{
    private readonly IDistributedCache _distributedCache;

    /// <summary>
    /// 构造函数
    /// </summary>
    public CacheManagementService(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
    }

    /// <inheritdoc />
    public bool Exists(string key)
    {
        ValidateKey(key);
        return _distributedCache.Get(key) is not null;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> GetKeys(string pattern = "*")
    {
        var normalizedPattern = NormalizePattern(pattern);
        if (_distributedCache is ICacheSupportsKeyPattern keyPatternCache)
        {
            return keyPatternCache.GetKeys(normalizedPattern);
        }

        return [];
    }

    /// <inheritdoc />
    public string? GetString(string key)
    {
        ValidateKey(key);
        return _distributedCache.GetString(key);
    }

    /// <inheritdoc />
    public void Remove(string key)
    {
        ValidateKey(key);
        _distributedCache.Remove(key);
    }

    /// <inheritdoc />
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

        foreach (var key in matchedKeys)
        {
            _distributedCache.Remove(key);
        }

        return matchedKeys.Count;
    }

    private static string NormalizePattern(string pattern)
    {
        return string.IsNullOrWhiteSpace(pattern) ? "*" : pattern.Trim();
    }

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("缓存键不能为空", nameof(key));
        }
    }
}
