// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Saas.Infrastructure.Auth;
using XiHan.Framework.Caching.Distributed.Abstracts;
using XiHan.Framework.Web.Core.Session;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 会话状态闸门测试。
/// </summary>
/// <remarks>
/// 闸门是服务端唯一的<b>硬</b>拦截点：SignalR 那条 ForceLogout 只是通知前端自己登出，
/// 直接 curl 就能绕过；真正把已吊销会话挡在门外的是这里读到 <c>Status != Active</c> 后返回 401。
/// 它有两条方向相反、都必须成立的规则：
/// <list type="number">
/// <item><b>fail-closed</b>：会话查不到 → 判失效。旧的 SaasPermissionChecker 是查不到就放行，
/// 那意味着数据库抖动期间吊销全面失效——会话是安全边界，宁可误伤不能漏放。</item>
/// <item><b>唯一例外是基础设施异常</b>：缓存/数据库抛异常时放行并记 Error，
/// 否则一次数据库抖动会被放大成全站强制登出。</item>
/// </list>
/// </remarks>
public sealed class SaasAppSessionStateGateTests
{
    private readonly Mock<IDistributedCache<SaasSessionStateCacheItem, string>> _cache = new();
    private readonly Mock<IUserSessionRepository> _userSessionRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();

    /// <summary>
    /// 有效的活跃会话放行。
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_ActiveSession_ShouldAllow()
    {
        var gate = BuildGate(new SaasSessionStateCacheItem { Exists = true, Status = SessionStatus.Active });

        var decision = await gate.EvaluateAsync("sess-1");

        Assert.Equal(SessionGateStatus.Allow, decision.Status);
    }

    /// <summary>
    /// 会话不存在时判失效（fail-closed），而不是放行。
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_SessionNotFound_ShouldBeInvalid()
    {
        var gate = BuildGate(new SaasSessionStateCacheItem { Exists = false });

        var decision = await gate.EvaluateAsync("sess-1");

        Assert.Equal(SessionGateStatus.Invalid, decision.Status);
    }

    /// <summary>
    /// 缓存回源返回 null 时同样判失效，不得退化成放行。
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_NullCacheItem_ShouldBeInvalid()
    {
        var gate = BuildGate(null);

        var decision = await gate.EvaluateAsync("sess-1");

        Assert.Equal(SessionGateStatus.Invalid, decision.Status);
    }

    /// <summary>
    /// 非活跃状态（登出/被踢/过期）一律判失效——这条就是"踢下线真踢得掉"的落点。
    /// </summary>
    /// <param name="status">会话状态。</param>
    [Theory]
    [InlineData(SessionStatus.Expired)]
    [InlineData(SessionStatus.Revoked)]
    [InlineData(SessionStatus.Offline)]
    public async Task EvaluateAsync_NonActiveStatus_ShouldBeInvalid(SessionStatus status)
    {
        var gate = BuildGate(new SaasSessionStateCacheItem { Exists = true, Status = status });

        var decision = await gate.EvaluateAsync("sess-1");

        Assert.Equal(SessionGateStatus.Invalid, decision.Status);
    }

    /// <summary>
    /// 已过期的会话判失效，即使状态位仍写着活跃（过期时间是独立的第二道判据）。
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_ExpiredByTimestamp_ShouldBeInvalid()
    {
        var gate = BuildGate(new SaasSessionStateCacheItem
        {
            Exists = true,
            Status = SessionStatus.Active,
            ExpirationTime = DateTimeOffset.UtcNow.AddMinutes(-1)
        });

        var decision = await gate.EvaluateAsync("sess-1");

        Assert.Equal(SessionGateStatus.Invalid, decision.Status);
    }

    /// <summary>
    /// 未到期的会话正常放行。
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_NotYetExpired_ShouldAllow()
    {
        var gate = BuildGate(new SaasSessionStateCacheItem
        {
            Exists = true,
            Status = SessionStatus.Active,
            ExpirationTime = DateTimeOffset.UtcNow.AddMinutes(30)
        });

        var decision = await gate.EvaluateAsync("sess-1");

        Assert.Equal(SessionGateStatus.Allow, decision.Status);
    }

    /// <summary>
    /// 无过期时间视为不过期，按状态位判定。
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_WithoutExpiration_ShouldAllow()
    {
        var gate = BuildGate(new SaasSessionStateCacheItem { Exists = true, Status = SessionStatus.Active, ExpirationTime = null });

        var decision = await gate.EvaluateAsync("sess-1");

        Assert.Equal(SessionGateStatus.Allow, decision.Status);
    }

    /// <summary>
    /// 锁定的活跃会话返回 Locked（423 引导解锁），并把锁定原因与展示信息透传给前端。
    /// </summary>
    /// <remarks>
    /// 关键在于它<b>不能</b>返回 Invalid：Invalid 会把用户踢回登录页，而锁定时用户仍是本人，
    /// 前端要展示"锁的是谁"并引导解锁。
    /// </remarks>
    [Fact]
    public async Task EvaluateAsync_LockedSession_ShouldReturnLockedWithDisplayInfo()
    {
        var gate = BuildGate(new SaasSessionStateCacheItem
        {
            Exists = true,
            Status = SessionStatus.Active,
            IsLocked = true,
            LockReason = "screen-lock",
            DisplayName = "张三",
            AvatarUrl = "https://example.invalid/a.png"
        });

        var decision = await gate.EvaluateAsync("sess-1");

        Assert.Equal(SessionGateStatus.Locked, decision.Status);
        Assert.Equal("screen-lock", decision.Reason, StringComparer.Ordinal);
        Assert.Equal("张三", decision.DisplayName, StringComparer.Ordinal);
        Assert.Equal("https://example.invalid/a.png", decision.AvatarUrl, StringComparer.Ordinal);
    }

    /// <summary>
    /// 状态检查排在锁定检查之前：已吊销的会话即便带着锁定位也应判失效而不是 423。
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_RevokedAndLocked_ShouldPreferInvalidOverLocked()
    {
        var gate = BuildGate(new SaasSessionStateCacheItem
        {
            Exists = true,
            Status = SessionStatus.Revoked,
            IsLocked = true,
            LockReason = "screen-lock"
        });

        var decision = await gate.EvaluateAsync("sess-1");

        Assert.Equal(SessionGateStatus.Invalid, decision.Status);
    }

    /// <summary>
    /// 缓存/数据库异常时放行——一次基础设施抖动不该演变成全站强制登出。
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_WhenCacheThrows_ShouldDegradeToAllow()
    {
        _cache
            .Setup(cache => cache.GetOrAddAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<SaasSessionStateCacheItem>>>(),
                It.IsAny<Func<DistributedCacheEntryOptions>>(),
                It.IsAny<bool?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Redis 不可用"));

        var gate = CreateGate();

        var decision = await gate.EvaluateAsync("sess-1");

        Assert.Equal(SessionGateStatus.Allow, decision.Status);
    }

    /// <summary>
    /// 取消是调用方的意图而非基础设施故障，必须原样抛出，不能被"降级放行"吞掉。
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_WhenCancelled_ShouldRethrowInsteadOfAllowing()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _cache
            .Setup(cache => cache.GetOrAddAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<SaasSessionStateCacheItem>>>(),
                It.IsAny<Func<DistributedCacheEntryOptions>>(),
                It.IsAny<bool?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException(cts.Token));

        var gate = CreateGate();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => gate.EvaluateAsync("sess-1", cts.Token));
    }

    /// <summary>
    /// 闸门读的必须是 <see cref="SaasCacheKeys.SessionState"/> 构造出来的键——
    /// 与失效器用的模式对不上，清缓存就清了个寂寞。
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_ShouldReadTheCanonicalSessionStateKey()
    {
        var gate = BuildGate(new SaasSessionStateCacheItem { Exists = true, Status = SessionStatus.Active });

        await gate.EvaluateAsync("sess-abc");

        _cache.Verify(
            cache => cache.GetOrAddAsync(
                SaasCacheKeys.SessionState("sess-abc"),
                It.IsAny<Func<Task<SaasSessionStateCacheItem>>>(),
                It.IsAny<Func<DistributedCacheEntryOptions>>(),
                It.IsAny<bool?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 缓存读取必须 <c>hideErrors:false</c>：异常要浮上来走"降级放行 + 告警"，
    /// 被静默吞掉就会退化成"查不到 → 判失效"，把一次缓存故障变成全站 401。
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_ShouldNotHideCacheErrors()
    {
        var gate = BuildGate(new SaasSessionStateCacheItem { Exists = true, Status = SessionStatus.Active });

        await gate.EvaluateAsync("sess-1");

        _cache.Verify(
            cache => cache.GetOrAddAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<SaasSessionStateCacheItem>>>(),
                It.IsAny<Func<DistributedCacheEntryOptions>>(),
                false,
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 回源查不到会话时，"不存在"这一事实本身也要被缓存，避免无效 session_id 每请求穿透查库。
    /// </summary>
    [Fact]
    public async Task LoadFactory_MissingSession_ShouldCacheTheNonExistenceFact()
    {
        _userSessionRepository
            .Setup(repository => repository.GetByUserSessionIdAsync("sess-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysUserSession?)null);

        var loaded = await RunLoadFactoryAsync("sess-1");

        Assert.False(loaded.Exists);
        _userRepository.Verify(
            repository => repository.GetByIdIgnoreTenantAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 未锁定的会话回源时不查用户表——这条查询会摊到每个请求上，只在锁定时才值得付出。
    /// </summary>
    [Fact]
    public async Task LoadFactory_UnlockedSession_ShouldNotQueryUserTable()
    {
        _userSessionRepository
            .Setup(repository => repository.GetByUserSessionIdAsync("sess-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SysUserSession { UserId = 7, Status = SessionStatus.Active, IsLocked = false });

        var loaded = await RunLoadFactoryAsync("sess-1");

        Assert.True(loaded.Exists);
        Assert.Equal(7, loaded.UserId);
        Assert.Null(loaded.DisplayName);
        _userRepository.Verify(
            repository => repository.GetByIdIgnoreTenantAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 锁定会话回源时补查用户展示信息，且必须走<b>忽略租户</b>的读取——解锁页此刻没有租户上下文可依。
    /// </summary>
    [Fact]
    public async Task LoadFactory_LockedSession_ShouldLoadDisplayInfoIgnoringTenant()
    {
        _userSessionRepository
            .Setup(repository => repository.GetByUserSessionIdAsync("sess-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SysUserSession { UserId = 7, Status = SessionStatus.Active, IsLocked = true, LockReason = "screen-lock" });
        _userRepository
            .Setup(repository => repository.GetByIdIgnoreTenantAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SysUser { UserName = "zhangsan", NickName = "张三", Avatar = "a.png" });

        var loaded = await RunLoadFactoryAsync("sess-1");

        Assert.Equal("张三", loaded.DisplayName, StringComparer.Ordinal);
        Assert.Equal("a.png", loaded.AvatarUrl, StringComparer.Ordinal);
    }

    /// <summary>
    /// 展示名回退顺序：昵称 → 真实姓名 → 登录名，保证解锁页永远显示得出一个名字。
    /// </summary>
    /// <param name="nickName">昵称。</param>
    /// <param name="realName">真实姓名。</param>
    /// <param name="userName">登录名。</param>
    /// <param name="expected">期望展示名。</param>
    [Theory]
    [InlineData("昵称", "真名", "login", "昵称")]
    [InlineData(null, "真名", "login", "真名")]
    [InlineData(null, null, "login", "login")]
    public async Task LoadFactory_DisplayName_ShouldFallBackFromNickToRealToUserName(
        string? nickName, string? realName, string userName, string expected)
    {
        _userSessionRepository
            .Setup(repository => repository.GetByUserSessionIdAsync("sess-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SysUserSession { UserId = 7, Status = SessionStatus.Active, IsLocked = true });
        _userRepository
            .Setup(repository => repository.GetByIdIgnoreTenantAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SysUser { UserName = userName, NickName = nickName, RealName = realName });

        var loaded = await RunLoadFactoryAsync("sess-1");

        Assert.Equal(expected, loaded.DisplayName, StringComparer.Ordinal);
    }

    /// <summary>
    /// 缓存条目必须带 60 秒短 TTL 兜底：会话写路径散落在多个领域服务里，
    /// 漏掉任何一处显式失效时，最多 60 秒自愈。
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_ShouldUseSixtySecondFallbackTtl()
    {
        Func<DistributedCacheEntryOptions>? optionsFactory = null;
        _cache
            .Setup(cache => cache.GetOrAddAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<SaasSessionStateCacheItem>>>(),
                It.IsAny<Func<DistributedCacheEntryOptions>>(),
                It.IsAny<bool?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Func<Task<SaasSessionStateCacheItem>>, Func<DistributedCacheEntryOptions>?, bool?, bool, CancellationToken>(
                (_, _, options, _, _, _) => optionsFactory = options)
            .ReturnsAsync(new SaasSessionStateCacheItem { Exists = true, Status = SessionStatus.Active });

        var gate = CreateGate();
        await gate.EvaluateAsync("sess-1");

        Assert.NotNull(optionsFactory);
        Assert.Equal(TimeSpan.FromSeconds(60), optionsFactory!().AbsoluteExpirationRelativeToNow);
    }

    /// <summary>
    /// 构造闸门，并让缓存直接返回给定的状态项。
    /// </summary>
    /// <param name="cached">缓存返回值（null 表示回源也拿不到）。</param>
    /// <returns>被测闸门。</returns>
    private SaasSessionStateGate BuildGate(SaasSessionStateCacheItem? cached)
    {
        _cache
            .Setup(cache => cache.GetOrAddAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<SaasSessionStateCacheItem>>>(),
                It.IsAny<Func<DistributedCacheEntryOptions>>(),
                It.IsAny<bool?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        return CreateGate();
    }

    /// <summary>
    /// 构造闸门（不预设缓存行为，供需要自己 Setup 的用例）。
    /// </summary>
    /// <returns>被测闸门。</returns>
    private SaasSessionStateGate CreateGate()
    {
        return new SaasSessionStateGate(
            _cache.Object,
            _userSessionRepository.Object,
            _userRepository.Object,
            NullLogger<SaasSessionStateGate>.Instance);
    }

    /// <summary>
    /// 触发闸门并执行缓存未命中时的回源工厂，返回回源结果。
    /// </summary>
    /// <param name="sessionId">会话标识。</param>
    /// <returns>回源得到的缓存项。</returns>
    private async Task<SaasSessionStateCacheItem> RunLoadFactoryAsync(string sessionId)
    {
        Func<Task<SaasSessionStateCacheItem>>? factory = null;
        _cache
            .Setup(cache => cache.GetOrAddAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<SaasSessionStateCacheItem>>>(),
                It.IsAny<Func<DistributedCacheEntryOptions>>(),
                It.IsAny<bool?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Func<Task<SaasSessionStateCacheItem>>, Func<DistributedCacheEntryOptions>?, bool?, bool, CancellationToken>(
                (_, loader, _, _, _, _) => factory = loader)
            .ReturnsAsync((SaasSessionStateCacheItem?)null);

        var gate = new SaasSessionStateGate(
            _cache.Object,
            _userSessionRepository.Object,
            _userRepository.Object,
            NullLogger<SaasSessionStateGate>.Instance);

        await gate.EvaluateAsync(sessionId);

        Assert.NotNull(factory);
        return await factory!();
    }
}
