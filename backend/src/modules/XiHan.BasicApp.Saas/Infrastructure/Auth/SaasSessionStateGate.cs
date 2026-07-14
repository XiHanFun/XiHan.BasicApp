#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasSessionStateGate
// Guid:1f6c8b25-7e04-4a93-9d51-3b8a0e7f2c46
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/15 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using Microsoft.Extensions.Caching.Distributed;
using XiHan.Framework.Caching.Distributed.Abstracts;
using XiHan.Framework.Web.Api.Session;

namespace XiHan.BasicApp.Saas.Infrastructure.Auth;

/// <summary>
/// SaaS 会话状态闸门：把 <see cref="SysUserSession"/> 的有效性与锁屏位喂给框架的会话中间件
/// </summary>
/// <remarks>
/// <b>fail-closed</b>：查不到会话 → 判定失效（401）。
/// 这与旧的 <c>SaasPermissionChecker</c> 行为相反——后者查不到就放行（fail-open），
/// 意味着 DB 抖动期间吊销全面失效。会话是安全边界，宁可误伤也不能漏放。
/// <para>
/// 唯一例外是<b>缓存/DB 异常</b>：此时若一律 401 会把一次数据库抖动放大成全站登出，
/// 故降级为放行并记 Error——权限码端点仍有 <c>SaasPermissionChecker</c> 兜底。
/// </para>
/// </remarks>
public sealed class SaasSessionStateGate : ISessionStateGate
{
    private readonly IDistributedCache<SaasSessionStateCacheItem, string> _cache;
    private readonly IUserSessionRepository _userSessionRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<SaasSessionStateGate> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SaasSessionStateGate(
        IDistributedCache<SaasSessionStateCacheItem, string> cache,
        IUserSessionRepository userSessionRepository,
        IUserRepository userRepository,
        ILogger<SaasSessionStateGate> logger)
    {
        _cache = cache;
        _userSessionRepository = userSessionRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SessionGateDecision> EvaluateAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        SaasSessionStateCacheItem? state;
        try
        {
            state = await _cache.GetOrAddAsync(
                SaasCacheKeys.SessionState(sessionId),
                () => LoadAsync(sessionId, cancellationToken),
                // 短 TTL 兜底：会话写路径散落在多个领域服务里，漏掉任何一处显式失效，
                // 最多 60 秒自愈（踢下线最迟 60s 生效）。锁屏/解锁走显式失效，即时生效。
                () => new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
                },
                hideErrors: false,
                token: cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // 缓存/DB 异常：放行 + 告警。一次 DB 抖动不该演变成全站强制登出。
            _logger.LogError(ex, "会话闸门评估失败，已放行：{SessionId}", sessionId);
            return SessionGateDecision.Allow;
        }

        if (state is null || !state.Exists)
        {
            return new SessionGateDecision(SessionGateStatus.Invalid);
        }

        if (state.Status != SessionStatus.Active)
        {
            return new SessionGateDecision(SessionGateStatus.Invalid);
        }

        if (state.ExpirationTime.HasValue && state.ExpirationTime.Value <= DateTimeOffset.UtcNow)
        {
            return new SessionGateDecision(SessionGateStatus.Invalid);
        }

        return state.IsLocked
            ? new SessionGateDecision(SessionGateStatus.Locked, state.DisplayName, state.AvatarUrl)
            : SessionGateDecision.Allow;
    }

    /// <summary>
    /// 回源：按 session_id 读会话 + 用户展示信息（锁屏页要显示"是谁锁的"）
    /// </summary>
    private async Task<SaasSessionStateCacheItem> LoadAsync(string sessionId, CancellationToken cancellationToken)
    {
        var session = await _userSessionRepository.GetByUserSessionIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            // 把"不存在"也缓存下来，避免无效 session_id 每请求穿透查库
            return new SaasSessionStateCacheItem { Exists = false, CachedAt = DateTimeOffset.UtcNow };
        }

        string? displayName = null;
        string? avatarUrl = null;

        // 仅锁屏时才需要展示信息，避免给每个请求都加一次用户查询
        if (session.IsLocked)
        {
            var user = await _userRepository.GetByIdIgnoreTenantAsync(session.UserId, cancellationToken);
            displayName = user?.NickName ?? user?.RealName ?? user?.UserName;
            avatarUrl = user?.Avatar;
        }

        return new SaasSessionStateCacheItem
        {
            Exists = true,
            UserId = session.UserId,
            Status = session.Status,
            IsLocked = session.IsLocked,
            ExpirationTime = session.ExpirationTime,
            DisplayName = displayName,
            AvatarUrl = avatarUrl,
            CachedAt = DateTimeOffset.UtcNow
        };
    }
}
