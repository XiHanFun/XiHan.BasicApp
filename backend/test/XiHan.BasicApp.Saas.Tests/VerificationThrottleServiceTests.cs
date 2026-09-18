// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.Framework.Core.Exceptions;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 验证码防刷限流服务测试：发送间隔 / 目标日配额 / IP 日配额。
/// </summary>
/// <remarks>
/// 覆盖的正是 <c>AuthAppService.PhoneLoginCodeAsync</c> 现在复用的机制：
/// 原先手机验证码发送只有一个 60 秒 phone+IP 的间隔键，61 秒发一条能刷一整天。
/// 这里直接测 <see cref="VerificationThrottleService"/> 本体（<c>AuthAppService</c> 构造函数参数太多，
/// 不值得为这一条限流逻辑搭一整套 mock），证明日配额确实会在第 N+1 次发送时拦下。
/// </remarks>
public sealed class VerificationThrottleServiceTests
{
    /// <summary>
    /// 同一目标（手机号/邮箱）每日发送上限用尽后，第 N+1 次发送必须拒绝。
    /// </summary>
    /// <remarks>
    /// 假缓存不模拟 TTL 过期，60 秒发送间隔键会一直占着；每次发送成功后手动清掉间隔键，
    /// 等效于"又过了 60 秒"，从而只让日配额这一个维度继续累计。
    /// </remarks>
    [Fact]
    public async Task EnsureSendAllowedAsync_TargetDailyQuota_ShouldBlockNPlus1ThSend()
    {
        var cache = new FakeDistributedCache();
        var service = new VerificationThrottleService(cache, CreateHttpContextAccessor(null));

        for (var i = 0; i < VerificationThrottleConsts.DailyTargetQuota; i++)
        {
            await service.EnsureSendAllowedAsync(1L, ProfileVerificationPurpose.PhoneLoginCode, "+15550100000");
            cache.ClearIntervalMarkers();
        }

        await Assert.ThrowsAsync<UserFriendlyException>(
            () => service.EnsureSendAllowedAsync(1L, ProfileVerificationPurpose.PhoneLoginCode, "+15550100000"));
    }

    /// <summary>
    /// 不同目标的日配额互相独立：目标 A 用尽不影响目标 B。
    /// </summary>
    [Fact]
    public async Task EnsureSendAllowedAsync_DifferentTargets_ShouldHaveIndependentDailyQuota()
    {
        var cache = new FakeDistributedCache();
        var service = new VerificationThrottleService(cache, CreateHttpContextAccessor(null));

        for (var i = 0; i < VerificationThrottleConsts.DailyTargetQuota; i++)
        {
            await service.EnsureSendAllowedAsync(1L, ProfileVerificationPurpose.PhoneLoginCode, "+15550100000");
            cache.ClearIntervalMarkers();
        }

        // 目标 B 不受目标 A 用尽的影响
        await service.EnsureSendAllowedAsync(2L, ProfileVerificationPurpose.PhoneLoginCode, "+15550100001");

        await Assert.ThrowsAsync<UserFriendlyException>(
            () => service.EnsureSendAllowedAsync(1L, ProfileVerificationPurpose.PhoneLoginCode, "+15550100000"));
    }

    /// <summary>
    /// 同一来源 IP 每日发送上限用尽后，即便换了目标也要拒绝。
    /// </summary>
    [Fact]
    public async Task EnsureSendAllowedAsync_IpDailyQuota_ShouldBlockAfterLimitAcrossTargets()
    {
        var cache = new FakeDistributedCache();
        var service = new VerificationThrottleService(cache, CreateHttpContextAccessor("9.9.9.9"));

        for (var i = 0; i < VerificationThrottleConsts.DailyIpQuota; i++)
        {
            // 每次换一个新目标，绕开目标日配额，只累计 IP 维度
            await service.EnsureSendAllowedAsync(1L, ProfileVerificationPurpose.PhoneLoginCode, $"+1555010{i:0000}");
        }

        await Assert.ThrowsAsync<UserFriendlyException>(
            () => service.EnsureSendAllowedAsync(1L, ProfileVerificationPurpose.PhoneLoginCode, "+15559999999"));
    }

    /// <summary>
    /// 60 秒发送间隔：同一目标短时间内重发必须拒绝（独立于日配额维度）。
    /// </summary>
    [Fact]
    public async Task EnsureSendAllowedAsync_WithinInterval_ShouldReject()
    {
        var cache = new FakeDistributedCache();
        var service = new VerificationThrottleService(cache, CreateHttpContextAccessor(null));

        await service.EnsureSendAllowedAsync(1L, ProfileVerificationPurpose.PhoneLoginCode, "+15550100000");

        await Assert.ThrowsAsync<UserFriendlyException>(
            () => service.EnsureSendAllowedAsync(1L, ProfileVerificationPurpose.PhoneLoginCode, "+15550100000"));
    }

    /// <summary>
    /// 封禁期内（校验连续失败达阈值）发送与校验均拒绝。
    /// </summary>
    [Fact]
    public async Task EnsureSendAllowedAsync_WhileBanned_ShouldReject()
    {
        var cache = new FakeDistributedCache();
        var service = new VerificationThrottleService(cache, CreateHttpContextAccessor(null));

        for (var i = 0; i < VerificationThrottleConsts.MaxVerifyFailures; i++)
        {
            await service.OnVerifyFailedAsync(1L, ProfileVerificationPurpose.PhoneLoginCode);
        }

        await Assert.ThrowsAsync<UserFriendlyException>(
            () => service.EnsureVerifyAllowedAsync(1L, ProfileVerificationPurpose.PhoneLoginCode));
        await Assert.ThrowsAsync<UserFriendlyException>(
            () => service.EnsureSendAllowedAsync(1L, ProfileVerificationPurpose.PhoneLoginCode, "+15550100000"));
    }

    private static IHttpContextAccessor CreateHttpContextAccessor(string? remoteIp)
    {
        var accessor = new Mock<IHttpContextAccessor>();
        if (remoteIp is null)
        {
            accessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);
            return accessor.Object;
        }

        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(remoteIp);
        accessor.Setup(a => a.HttpContext).Returns(httpContext);
        return accessor.Object;
    }

    /// <summary>
    /// 内存版分布式缓存假实现：固定窗口计数的读写语义 + 手动清间隔键模拟时间流逝。
    /// .NET 9+ 的字符串方法（GetString/SetString 系列）是接口默认实现并委托给 byte[] 成员，故只实现 byte[] 成员。
    /// </summary>
    private sealed class FakeDistributedCache : IDistributedCache
    {
        private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);

        /// <summary>
        /// 清掉所有发送间隔标记，等效于"又过了发送间隔窗口"，只让日配额继续累计。
        /// </summary>
        public void ClearIntervalMarkers()
        {
            foreach (var key in _values.Keys.Where(k => k.Contains(":interval:", StringComparison.Ordinal)).ToList())
            {
                _values.Remove(key);
            }
        }

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
