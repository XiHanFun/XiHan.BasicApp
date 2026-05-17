#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:LoginSessionDomainService
// Guid:fa99d2c1-64f8-431e-9aae-b3ae66c0c45e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/17 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Authentication.Jwt;
using XiHan.Framework.Web.Core.Clients;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 登录会话领域服务实现
/// </summary>
public sealed class LoginSessionDomainService
    : ILoginSessionDomainService
{
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

    private readonly IUserRepository _userRepository;
    private readonly IUserSecurityRepository _userSecurityRepository;
    private readonly ITenantUserRepository _tenantUserRepository;
    private readonly IUserSessionRepository _userSessionRepository;
    private readonly IOAuthTokenRepository _oauthTokenRepository;

    /// <inheritdoc />
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
            _ = await _userSecurityRepository.UpdateAsync(security, cancellationToken);
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
            IsOnline = true,
            IsRevoked = false,
            ExpiresAt = ToDateTimeOffset(tokenResult.ExpiresAt)
        };

        session = await _userSessionRepository.AddAsync(session, cancellationToken);

        var oauthToken = new SysOAuthToken
        {
            SessionId = session.BasicId,
            AccessTokenJti = accessTokenJti,
            AccessToken = null,
            RefreshToken = tokenResult.RefreshToken,
            TokenType = tokenResult.TokenType,
            ClientId = "basicapp-web",
            UserId = user.BasicId,
            GrantType = GrantType.Password,
            Scopes = "basicapp",
            Status = EnableStatus.Enabled,
            AccessTokenExpiresTime = ToDateTimeOffset(tokenResult.ExpiresAt),
            RefreshTokenExpiresTime = now.AddDays(7),
            IsRevoked = false
        };

        _ = await _oauthTokenRepository.AddAsync(oauthToken, cancellationToken);
        _ = await _userRepository.UpdateAsync(user, cancellationToken);

        if (tenantId.HasValue)
        {
            var membership = await _tenantUserRepository.GetMembershipAsync(user.BasicId, cancellationToken);
            if (membership is not null)
            {
                membership.LastActiveTime = now;
                _ = await _tenantUserRepository.UpdateAsync(membership, cancellationToken);
            }
        }

        return new LoginSessionIssueResult(session);
    }

    /// <inheritdoc />
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

        session.IsOnline = false;
        session.IsRevoked = true;
        session.RevokedAt = now;
        session.RevokedReason = "用户主动退出";
        session.LogoutTime = now;
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
