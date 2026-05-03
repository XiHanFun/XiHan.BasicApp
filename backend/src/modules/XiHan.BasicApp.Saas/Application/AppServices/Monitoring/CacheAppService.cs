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

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Caching.Distributed.Abstracts;

namespace XiHan.BasicApp.Saas.Application.AppServices.Monitoring;

/// <summary>
/// 缓存管理服务（只读+删除，不暴露写入操作）
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统Saas服务")]
public class CacheAppService : ApplicationServiceBase
{
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<CacheAppService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public CacheAppService(IDistributedCache distributedCache, ILogger<CacheAppService> logger)
    {
        _distributedCache = distributedCache;
        _logger = logger;
    }

    /// <summary>
    /// 获取缓存字符串值
    /// </summary>
    public string? GetString(string key)
    {
        ValidateKey(key);
        try
        {
            return _distributedCache.GetString(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取缓存键 {Key} 失败，缓存服务可能不可用", key);
            return null;
        }
    }

    /// <summary>
    /// 判断缓存键是否存在
    /// </summary>
    public bool Exists(string key)
    {
        ValidateKey(key);
        try
        {
            return _distributedCache.Get(key) is not null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "检查缓存键 {Key} 存在性失败，缓存服务可能不可用", key);
            return false;
        }
    }

    /// <summary>
    /// 按模式获取缓存键列表
    /// </summary>
    public IReadOnlyCollection<string> GetKeys(string pattern = "*")
    {
        var normalizedPattern = NormalizePattern(pattern);
        try
        {
            if (_distributedCache is ICacheSupportsKeyPattern keyPatternCache)
            {
                return keyPatternCache.GetKeys(normalizedPattern);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "按模式 {Pattern} 获取缓存键失败，缓存服务可能不可用", normalizedPattern);
        }

        return [];
    }

    /// <summary>
    /// 删除指定缓存键
    /// </summary>
    public void Remove(string key)
    {
        ValidateKey(key);
        try
        {
            _distributedCache.Remove(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "删除缓存键 {Key} 失败，缓存服务可能不可用", key);
        }
    }

    /// <summary>
    /// 按模式批量删除缓存键
    /// </summary>
    public long RemoveByPattern(string pattern = "*")
    {
        var normalizedPattern = NormalizePattern(pattern);
        try
        {
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
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "按模式 {Pattern} 删除缓存键失败，缓存服务可能不可用", normalizedPattern);
            return 0;
        }
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
}
