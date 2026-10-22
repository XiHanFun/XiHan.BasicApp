// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Caching.Memory;

namespace XiHan.BasicApp.Saas.Application.AccessPolicies;

/// <summary>
/// 「这个 IP 没被封」的本地短时缓存，最多 10 万项
/// </summary>
public sealed class LoginBanCheckCache : IDisposable
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = 100_000 });

    /// <summary>
    /// 是否已知没被封
    /// </summary>
    public bool IsKnownNotBanned(string ip)
    {
        return _cache.TryGetValue(ip, out _);
    }

    /// <summary>
    /// 记下没被封
    /// </summary>
    public void MarkNotBanned(string ip, TimeSpan lifetime)
    {
        _ = _cache.Set(ip, true, new MemoryCacheEntryOptions { Size = 1, AbsoluteExpirationRelativeToNow = lifetime });
    }

    /// <summary>
    /// 封禁或解封后清掉这个 IP 的缓存
    /// </summary>
    public void Forget(string ip)
    {
        _cache.Remove(ip);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _cache.Dispose();
    }
}
