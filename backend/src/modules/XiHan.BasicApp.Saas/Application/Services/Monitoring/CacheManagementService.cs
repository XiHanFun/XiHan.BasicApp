// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Caching.Distributed;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.Framework.Caching.Distributed.Abstracts;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 缓存管理应用服务实现
/// 仅经分布式缓存抽象操作：查询（键/值）、改字符串值、删除（单键/按模式）。不直连 Redis 原生类型。
/// </summary>
public sealed class CacheManagementService
    : ICacheManagementService
{
    /// <summary>
    /// 鉴权热路径保留命名空间：禁止经缓存浏览器「改写值」，防止缓存投毒提权。
    /// 整键删除仍允许（仅触发框架按库重建，属安全降级，不可提权）。
    /// </summary>
    private static readonly string[] ReservedWriteNamespaces =
    [
        SaasCacheNames.AuthorizationSnapshot,
        SaasCacheNames.EditionGate
    ];

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
    public void SetString(string key, string? value)
    {
        ValidateKey(key);
        EnsureMutable(key);
        _distributedCache.SetString(key, value ?? string.Empty);
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

    /// <summary>
    /// 守卫：拒绝「改写」鉴权热路径命名空间的键值（防止缓存投毒提权）。
    /// 用 Contains 匹配命名空间段，覆盖租户前缀（如 0:）等键前缀变体；整键删除不经此守卫。
    /// </summary>
    private static void EnsureMutable(string key)
    {
        foreach (var reserved in ReservedWriteNamespaces)
        {
            if (key.Contains(reserved, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"键「{key}」属于鉴权关键命名空间，禁止经缓存浏览器改写（如需失效请整键删除以触发安全重建）。");
            }
        }
    }
}
