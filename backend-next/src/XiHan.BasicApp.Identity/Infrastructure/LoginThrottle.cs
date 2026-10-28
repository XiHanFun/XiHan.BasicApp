// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using XiHan.Framework.Caching.Abstractions.Caches;
using XiHan.Framework.Caching.Abstractions.Stores;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Infrastructure;

/// <summary>
/// 登录节流（单例）：按固定窗口（<see cref="IdentityOptions.LoginThrottleWindow"/>）数失败，两个计数——账号 × 地址（同一账号在同一地址上）与地址（跨账号累计）。
/// 达到上限后本窗口内的登录尝试直接拒绝（不核对验证码与密码），下个窗口自动恢复。只数失败：验证码错误、账号或密码错误、双因素码错误。
/// 计数在共享缓存里先读后写，不是原子的，并发下可能少数几次；节流只是减速，不当作安全边界（账号锁定另有 <see cref="LoginAttempts"/>）。
/// 内部调用没有远端地址，只数账号。
/// </summary>
/// <param name="cache">共享缓存。</param>
/// <param name="options">身份配置。</param>
/// <param name="clock">时间源。</param>
public sealed class LoginThrottle(ISharedCache cache, IdentityOptions options, TimeProvider clock)
{
    /// <summary>
    /// 本窗口是否还允许尝试；超限返回 <see cref="IdentityErrors.LoginThrottled"/>（带 <c>retryAfter</c> 秒数）。
    /// </summary>
    /// <param name="account">账号键（注册地租户与登录名）。</param>
    /// <param name="address">远端地址；没有为 <see langword="null"/>。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task<Result> CheckAsync(string account, string? address, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(account);
        var (window, retryAfter) = CurrentWindow();
        if (await CountAsync(AccountKey(window, account, address), cancellationToken).ConfigureAwait(false) >= options.LoginThrottleAccountLimit)
        {
            return Throttled(retryAfter);
        }

        if (address is not null && await CountAsync(AddressKey(window, address), cancellationToken).ConfigureAwait(false) >= options.LoginThrottleIpLimit)
        {
            return Throttled(retryAfter);
        }

        return Result.Success();
    }

    /// <summary>
    /// 记一次失败（两个计数各加一）。
    /// </summary>
    /// <param name="account">账号键。</param>
    /// <param name="address">远端地址。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task RecordFailureAsync(string account, string? address, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(account);
        var (window, retryAfter) = CurrentWindow();
        // 计数比窗口多活一秒：窗口末尾写入的计数不会在判定之前先过期
        var entry = CacheEntryOptions.Expires(TimeSpan.FromSeconds(retryAfter + 1));
        await IncrementAsync(AccountKey(window, account, address), entry, cancellationToken).ConfigureAwait(false);
        if (address is not null)
        {
            await IncrementAsync(AddressKey(window, address), entry, cancellationToken).ConfigureAwait(false);
        }
    }

    private (long Window, int RetryAfter) CurrentWindow()
    {
        var seconds = Math.Max(1, (long)options.LoginThrottleWindow.TotalSeconds);
        var now = clock.GetUtcNow().ToUnixTimeSeconds();
        var window = now / seconds;
        return (window, (int)(((window + 1) * seconds) - now));
    }

    private async Task<int> CountAsync(CacheKey key, CancellationToken cancellationToken)
        => (await cache.GetAsync<string>(key, cancellationToken).ConfigureAwait(false)).TryGetValue(out var raw) && int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var count) ? count : 0;

    private async Task IncrementAsync(CacheKey key, CacheEntryOptions entry, CancellationToken cancellationToken)
    {
        var next = await CountAsync(key, cancellationToken).ConfigureAwait(false) + 1;
        await cache.SetAsync(key, next.ToString(CultureInfo.InvariantCulture), entry, cancellationToken).ConfigureAwait(false);
    }

    // 账号键里有用户输入：散列后再进缓存键，长度与字符都受控
    private static CacheKey AccountKey(long window, string account, string? address)
        => new(string.Create(CultureInfo.InvariantCulture, $"identity:throttle:account:{window}:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(account + "\n" + address)))}"));

    private static CacheKey AddressKey(long window, string address)
        => new(string.Create(CultureInfo.InvariantCulture, $"identity:throttle:address:{window}:{address}"));

    private static Error Throttled(int retryAfter)
        => Error.Unavailable(IdentityErrors.LoginThrottled, IdentityMessages.LoginThrottled, MessageArguments.Of(("retryAfter", retryAfter)));
}
