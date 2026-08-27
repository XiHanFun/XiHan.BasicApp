// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Caching.Distributed;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.Framework.Core.Exceptions;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 登录节流服务测试：账号 + IP 与纯 IP 双维度固定窗口计数。
/// </summary>
public sealed class LoginThrottleServiceTests
{
    /// <summary>
    /// 窗口内允许的尝试次数用尽后必须拒绝。
    /// </summary>
    [Fact]
    public async Task EnsureLoginAllowed_UnderLimit_ShouldAllowAndCount()
    {
        var cache = new FakeDistributedCache();
        var service = new LoginThrottleService(cache);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await service.EnsureLoginAllowedAsync("admin", "1.2.3.4");
        }

        await Assert.ThrowsAsync<UserFriendlyException>(
            () => service.EnsureLoginAllowedAsync("admin", "1.2.3.4"));
    }

    /// <summary>
    /// 账号维度独立计数：不同账号互不影响。
    /// </summary>
    [Fact]
    public async Task EnsureLoginAllowed_DifferentAccounts_ShouldHaveIndependentWindows()
    {
        var cache = new FakeDistributedCache();
        var service = new LoginThrottleService(cache);

        // 账号 A 用尽窗口
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await service.EnsureLoginAllowedAsync("account-a", "1.2.3.4");
        }

        // 账号 B 不受影响
        await service.EnsureLoginAllowedAsync("account-b", "1.2.3.4");

        await Assert.ThrowsAsync<UserFriendlyException>(
            () => service.EnsureLoginAllowedAsync("account-a", "1.2.3.4"));
    }

    /// <summary>
    /// 纯 IP 维度限流：同 IP 多账号扫号在 IP 窗口用尽后整体拒绝。
    /// </summary>
    [Fact]
    public async Task EnsureLoginAllowed_ManyAccountsFromSameIp_ShouldExhaustIpWindow()
    {
        var cache = new FakeDistributedCache();
        var service = new LoginThrottleService(cache);

        for (var account = 1; account <= 30; account++)
        {
            await service.EnsureLoginAllowedAsync($"user-{account}", "10.0.0.1");
        }

        await Assert.ThrowsAsync<UserFriendlyException>(
            () => service.EnsureLoginAllowedAsync("user-31", "10.0.0.1"));
    }

    /// <summary>
    /// 账号维度大小写不敏感（邮箱用户名大小写混写不应绕过计数）。
    /// </summary>
    [Fact]
    public async Task EnsureLoginAllowed_AccountKey_ShouldBeCaseInsensitive()
    {
        var cache = new FakeDistributedCache();
        var service = new LoginThrottleService(cache);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await service.EnsureLoginAllowedAsync("Admin@Example.COM", "1.2.3.4");
        }

        await Assert.ThrowsAsync<UserFriendlyException>(
            () => service.EnsureLoginAllowedAsync("admin@example.com", "1.2.3.4"));
    }

    /// <summary>
    /// 空 IP 归入 unknown 桶，不因空 IP 绕过限流。
    /// </summary>
    [Fact]
    public async Task EnsureLoginAllowed_WithoutIp_ShouldUseUnknownBucket()
    {
        var cache = new FakeDistributedCache();
        var service = new LoginThrottleService(cache);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await service.EnsureLoginAllowedAsync("admin", null);
        }

        await Assert.ThrowsAsync<UserFriendlyException>(
            () => service.EnsureLoginAllowedAsync("admin", "  "));
    }

    /// <summary>
    /// 空账号必须拒绝。
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EnsureLoginAllowed_WithBlankAccount_ShouldThrow(string? account)
    {
        var service = new LoginThrottleService(new FakeDistributedCache());

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => service.EnsureLoginAllowedAsync(account!, "1.2.3.4"));
    }

    /// <summary>
    /// 已取消令牌必须立即抛出且不写缓存。
    /// </summary>
    [Fact]
    public async Task EnsureLoginAllowed_Cancelled_ShouldThrowWithoutWritingCache()
    {
        var cache = new FakeDistributedCache();
        var service = new LoginThrottleService(cache);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.EnsureLoginAllowedAsync("admin", "1.2.3.4", cts.Token));

        Assert.Empty(cache.Values);
    }

    /// <summary>
    /// 内存版分布式缓存假实现：固定窗口计数的读写语义。
    /// .NET 9+ 的字符串方法（GetString/SetString 系列）是接口默认实现并委托给 byte[] 成员，故只实现 byte[] 成员。
    /// </summary>
    private sealed class FakeDistributedCache : IDistributedCache
    {
        private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);

        public IReadOnlyDictionary<string, string> Values => _values;

        public byte[]? Get(string key)
        {
            return _values.TryGetValue(key, out var value) ? System.Text.Encoding.UTF8.GetBytes(value) : null;
        }

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(Get(key));
        }

        public void Refresh(string key)
        {
        }

        public Task RefreshAsync(string key, CancellationToken token = default)
        {
            return Task.CompletedTask;
        }

        public void Remove(string key)
        {
            _values.Remove(key);
        }

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            _values.Remove(key);
            return Task.CompletedTask;
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            _values[key] = System.Text.Encoding.UTF8.GetString(value);
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            Set(key, value, options);
            return Task.CompletedTask;
        }
    }
}
