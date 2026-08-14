// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Identity;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Authentication.Jwt;
using XiHan.Framework.Domain.Repositories;
using XiHan.Framework.Web.Core.Clients;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 登录会话领域服务实现
/// </summary>
public sealed class LoginSessionDomainService
    : ILoginSessionDomainService
{
    private readonly IUserRepository _userRepository;

    private readonly IUserSecurityRepository _userSecurityRepository;

    private readonly ITenantUserRepository _tenantUserRepository;

    private readonly IUserSessionRepository _userSessionRepository;

    private readonly IOAuthTokenRepository _oauthTokenRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public LoginSessionDomainService(
        IUserRepository userRepository,
        IUserSecurityRepository userSecurityRepository,
        ITenantUserRepository tenantUserRepository,
        IUserSessionRepository userSessionRepository,
        IOAuthTokenRepository oauthTokenRepository)
    {
        _userRepository = userRepository;
        _userSecurityRepository = userSecurityRepository;
        _tenantUserRepository = tenantUserRepository;
        _userSessionRepository = userSessionRepository;
        _oauthTokenRepository = oauthTokenRepository;
    }

    /// <summary>
    /// 签发密码登录会话与 OAuth Token
    /// </summary>
    /// <param name="user">用户</param>
    /// <param name="security">用户安全配置</param>
    /// <param name="tenantId">租户标识</param>
    /// <param name="sessionBusinessId">业务会话标识</param>
    /// <param name="accessTokenJti">访问令牌 JTI</param>
    /// <param name="tokenResult">令牌结果</param>
    /// <param name="deviceId">设备标识</param>
    /// <param name="client">客户端信息</param>
    /// <param name="now">当前时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>登录会话签发结果</returns>
    public async Task<LoginSessionIssueResult> IssuePasswordLoginAsync(
        SysUser user,
        SysUserSecurity? security,
        long? tenantId,
        string sessionBusinessId,
        string accessTokenJti,
        JwtTokenResult tokenResult,
        string? deviceId,
        ClientInfo client,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(tokenResult);
        ArgumentNullException.ThrowIfNull(client);
        cancellationToken.ThrowIfCancellationRequested();

        user.LastLoginIp = client.IpAddress;
        if (security is not null)
        {
            security.LastSecurityCheckTime = now;

            // 用户主体数据自有行写入：平台归属用户（行 TenantId=0）登录/切换租户时在租户态更新自己的安全信息，
            // 写路径租户边界须显式豁免（行归属键是 UserId，TenantId 只是注册地元数据）
            using (TenantWriteGuard.Suppress())
            {
                _ = await _userSecurityRepository.UpdateAsync(security, cancellationToken);
            }
        }

        // 同设备重新登录：旧活跃会话自动下线（静默替换，不发通知、不推强制登出）。
        // 不清理则旧会话滞留到过期为止：设备列表越积越多，且每次重新登录都会误报「账号在其它设备登录」。
        var supersededSessionBusinessIds = new List<string>();
        var normalizedDeviceId = NormalizeNullable(deviceId, 200);
        if (normalizedDeviceId is not null)
        {
            var staleSessions = await _userSessionRepository.GetActiveByUserAndDeviceIgnoreTenantAsync(user.BasicId, normalizedDeviceId, cancellationToken);
            if (staleSessions.Count > 0)
            {
                foreach (var stale in staleSessions)
                {
                    stale.Status = SessionStatus.Revoked;
                    stale.RevokedTime = now;
                    stale.RevokedReason = "同设备重新登录，自动下线";
                    stale.LogoutTime = now;
                }

                // 旧会话行带「发起登录时租户」的戳，与本次登录上下文可能不同，写路径租户边界须显式豁免
                using (TenantWriteGuard.Suppress())
                {
                    _ = await _userSessionRepository.UpdateRangeAsync([.. staleSessions], cancellationToken);
                }

                _ = await _oauthTokenRepository.RevokeBySessionIdsAsync([.. staleSessions.Select(item => item.BasicId)], now, cancellationToken);
                supersededSessionBusinessIds.AddRange(staleSessions.Select(item => item.UserSessionId));
            }
        }

        var session = new SysUserSession
        {
            UserId = user.BasicId,
            CurrentAccessTokenJti = accessTokenJti,
            UserSessionId = sessionBusinessId,
            DeviceType = DeviceType.Web,
            DeviceName = NormalizeNullable(client.DeviceName, 200) ?? "Web",
            DeviceId = NormalizeNullable(deviceId, 200),
            Browser = NormalizeNullable(client.Browser, 100),
            OperatingSystem = NormalizeNullable(client.OperatingSystem, 100),
            IpAddress = NormalizeNullable(client.IpAddress, 50),
            Location = NormalizeNullable(client.Location, 200),
            LoginTime = now,
            LastActivityTime = now,
            Status = SessionStatus.Active,
            ExpirationTime = ToDateTimeOffset(tokenResult.RefreshTokenExpiresAt)
        };

        session = await _userSessionRepository.AddAsync(session, cancellationToken);

        var oauthToken = new SysOAuthToken
        {
            SessionId = session.BasicId,
            AccessTokenJti = accessTokenJti,
            AccessToken = null,
            RefreshToken = tokenResult.RefreshToken,
            TokenType = tokenResult.TokenType,
            ClientId = SaasOAuthClientIds.Web,
            UserId = user.BasicId,
            GrantType = GrantType.Password,
            Scopes = SaasOAuthClientIds.DefaultScope,
            Status = EnableStatus.Enabled,
            AccessTokenExpirationTime = ToDateTimeOffset(tokenResult.ExpiresAt),
            RefreshTokenExpirationTime = ToDateTimeOffset(tokenResult.RefreshTokenExpiresAt),
            IsRevoked = false
        };

        _ = await _oauthTokenRepository.AddAsync(oauthToken, cancellationToken);

        // 用户主体数据自有行写入：回写当前登录用户自己的 LastLoginIp（平台归属用户行 TenantId=0，租户态直写会被写边界拒绝）
        using (TenantWriteGuard.Suppress())
        {
            _ = await _userRepository.UpdateAsync(user, cancellationToken);
        }

        if (tenantId.HasValue)
        {
            var membership = await _tenantUserRepository.GetMembershipAsync(user.BasicId, cancellationToken);
            if (membership is not null)
            {
                membership.LastActiveTime = now;
                _ = await _tenantUserRepository.UpdateAsync(membership, cancellationToken);
            }
        }

        return new LoginSessionIssueResult(session, supersededSessionBusinessIds);
    }

    /// <summary>
    /// 切换租户：复用当前登录会话，轮换访问令牌并把会话行租户戳迁移到目标上下文
    /// </summary>
    /// <remarks>
    /// 切换租户是同一登录会话的上下文迁移，不是一次新登录：不新建会话行（避免设备列表每切一次多一台「设备」），
    /// 也不发布登录成功事件（避免每切一次误报「账号在新设备登录」）。
    /// </remarks>
    /// <param name="session">当前登录会话</param>
    /// <param name="targetTenantId">目标租户标识；空表示平台运维态（租户戳落 0）</param>
    /// <param name="accessTokenJti">新访问令牌 JTI</param>
    /// <param name="tokenResult">新令牌结果</param>
    /// <param name="now">当前时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新后的用户会话</returns>
    public async Task<SysUserSession> SwitchTenantAsync(
        SysUserSession session,
        long? targetTenantId,
        string accessTokenJti,
        JwtTokenResult tokenResult,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessTokenJti);
        ArgumentNullException.ThrowIfNull(tokenResult);
        cancellationToken.ThrowIfCancellationRequested();

        // 会话行租户戳迁移到目标上下文（平台态戳 0）：在线用户等按租户查会话的视图以「用户当前所在上下文」为准
        session.TenantId = targetTenantId ?? 0;
        session.CurrentAccessTokenJti = accessTokenJti;
        session.LastActivityTime = now;
        session.ExpirationTime = ToDateTimeOffset(tokenResult.RefreshTokenExpiresAt);

        // 用户主体数据自有行写入：行归属键是 UserId，租户戳只是上下文元数据，写路径租户边界须显式豁免
        using (TenantWriteGuard.Suppress())
        {
            _ = await _userSessionRepository.UpdateAsync(session, cancellationToken);
        }

        // 令牌台账与登录同构：旧令牌记录吊销、落新令牌记录（刷新链无状态，此处仅维护台账）
        _ = await _oauthTokenRepository.RevokeBySessionIdsAsync([session.BasicId], now, cancellationToken);

        var oauthToken = new SysOAuthToken
        {
            SessionId = session.BasicId,
            AccessTokenJti = accessTokenJti,
            AccessToken = null,
            RefreshToken = tokenResult.RefreshToken,
            TokenType = tokenResult.TokenType,
            ClientId = SaasOAuthClientIds.Web,
            UserId = session.UserId,
            GrantType = GrantType.Password,
            Scopes = SaasOAuthClientIds.DefaultScope,
            Status = EnableStatus.Enabled,
            AccessTokenExpirationTime = ToDateTimeOffset(tokenResult.ExpiresAt),
            RefreshTokenExpirationTime = ToDateTimeOffset(tokenResult.RefreshTokenExpiresAt),
            IsRevoked = false
        };

        _ = await _oauthTokenRepository.AddAsync(oauthToken, cancellationToken);

        return session;
    }

    /// <summary>
    /// 退出当前登录会话并撤销关联 OAuth Token
    /// </summary>
    /// <param name="userId">用户标识</param>
    /// <param name="sessionBusinessId">业务会话标识</param>
    /// <param name="now">当前时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已退出的用户会话，不存在时返回空</returns>
    public async Task<SysUserSession?> LogoutAsync(
        long userId,
        string sessionBusinessId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), "用户标识必须大于 0。");
        }

        if (string.IsNullOrWhiteSpace(sessionBusinessId))
        {
            throw new ArgumentException("业务会话标识不能为空。", nameof(sessionBusinessId));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var normalizedSessionBusinessId = sessionBusinessId.Trim();
        var session = await _userSessionRepository.GetFirstAsync(
            item => item.UserId == userId && item.UserSessionId == normalizedSessionBusinessId,
            cancellationToken);
        if (session is null)
        {
            return null;
        }

        session.Status = SessionStatus.Revoked;
        session.RevokedTime = now;
        session.RevokedReason = "用户主动退出";
        session.LogoutTime = now;

        // 用户主体数据自有行写入：会话/令牌行带「发起登录时租户」的戳，
        // 用户切换到其他租户后登出，当前租户 ≠ 行租户戳，须显式豁免写路径租户边界（行归属键是 UserId/SessionId）
        using (TenantWriteGuard.Suppress())
        {
            _ = await _userSessionRepository.UpdateAsync(session, cancellationToken);

            var tokens = await _oauthTokenRepository.GetListAsync(item => item.SessionId == session.BasicId && !item.IsRevoked, cancellationToken);
            foreach (var token in tokens)
            {
                token.IsRevoked = true;
                token.RevokedTime = now;
            }

            if (tokens.Count > 0)
            {
                _ = await _oauthTokenRepository.UpdateRangeAsync(tokens, cancellationToken);
            }
        }

        return session;
    }

    private static DateTimeOffset ToDateTimeOffset(DateTime value)
    {
        return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }

    private static string? NormalizeNullable(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length > maxLength ? normalized[..maxLength] : normalized;
    }
}
