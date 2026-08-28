// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Authentication.Jwt;
using XiHan.Framework.Security.Claims;
using XiHan.Framework.Security.Extensions;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 认证令牌签发服务实现
/// </summary>
public sealed class AuthTokenIssueService
    : IAuthTokenIssueService
{
    private readonly IJwtTokenService _jwtTokenService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AuthTokenIssueService(IJwtTokenService jwtTokenService)
    {
        _jwtTokenService = jwtTokenService;
    }

    /// <summary>
    /// 签发访问令牌
    /// </summary>
    public AuthAccessTokenIssueResult IssueAccessToken(AuthAccessTokenIssueCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var claims = BuildClaims(
            command.User,
            command.TenantId,
            command.SessionBusinessId,
            command.AccessTokenJti,
            command.Roles,
            command.Permissions,
            command.DeviceId,
            command.ImpersonatorUserId,
            command.ImpersonatorUserName,
            command.ImpersonatorTenantId,
            command.ImpersonatorTenantName);
        var tokenResult = _jwtTokenService.GenerateAccessToken(claims);
        return new AuthAccessTokenIssueResult(tokenResult, ToLoginTokenDto(tokenResult));
    }

    /// <summary>
    /// 刷新访问令牌
    /// </summary>
    public LoginTokenDto RefreshAccessToken(string accessToken, string refreshToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        var tokenResult = _jwtTokenService.RefreshAccessToken(accessToken.Trim(), refreshToken.Trim())
            ?? throw new InvalidOperationException("刷新令牌无效或已过期。");
        return ToLoginTokenDto(tokenResult);
    }

    /// <summary>
    /// 从访问令牌解析用户身份（不校验有效期，仅用于审计归属），解析失败返回 null
    /// </summary>
    public AuthTokenIdentity? ResolveTokenIdentity(string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        try
        {
            var claims = _jwtTokenService.GetClaimsFromToken(accessToken.Trim());
            if (claims is null || claims.Count == 0)
            {
                return null;
            }

            var userIdValue = claims.FirstOrDefault(c => c.Type == XiHanClaimTypes.UserId)?.Value
                ?? claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
            var userName = claims.FirstOrDefault(c => c.Type == XiHanClaimTypes.UserName)?.Value;
            var tenantIdValue = claims.FirstOrDefault(c => c.Type == XiHanClaimTypes.TenantId)?.Value;

            long? userId = long.TryParse(userIdValue, out var parsedUserId) ? parsedUserId : null;
            long? tenantId = long.TryParse(tenantIdValue, out var parsedTenantId) ? parsedTenantId : null;
            // 刷新令牌时要靠它校验会话是否仍然有效（被踢下线的会话不得再刷新续命）
            var sessionId = claims.FirstOrDefault(c => c.Type == XiHanClaimTypes.SessionId)?.Value;
            // 刷新令牌端点据此拒绝模仿会话续命
            var impersonatorUserIdValue = claims.FirstOrDefault(c => c.Type == XiHanClaimTypes.ImpersonatorUserId)?.Value;
            long? impersonatorUserId = long.TryParse(impersonatorUserIdValue, out var parsedImpersonatorUserId) ? parsedImpersonatorUserId : null;
            return userId is null && string.IsNullOrWhiteSpace(userName)
                ? null
                : new AuthTokenIdentity(userId, userName, tenantId, sessionId, impersonatorUserId);
        }
        catch
        {
            // 审计归属解析失败不影响主流程
            return null;
        }
    }

    private static List<Claim> BuildClaims(
        SysUser user,
        long? tenantId,
        string sessionBusinessId,
        string accessTokenJti,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> permissions,
        string? deviceId,
        long? impersonatorUserId = null,
        string? impersonatorUserName = null,
        long? impersonatorTenantId = null,
        string? impersonatorTenantName = null)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(roles);
        ArgumentNullException.ThrowIfNull(permissions);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.BasicId.ToString()),
            new(JwtRegisteredClaimNames.Jti, accessTokenJti),
            new(XiHanClaimTypes.UserId, user.BasicId.ToString()),
            new(XiHanClaimTypes.UserName, user.UserName),
            new(XiHanClaimTypes.SessionId, sessionBusinessId)
        };

        if (tenantId.HasValue)
        {
            claims.Add(new Claim(XiHanClaimTypes.TenantId, tenantId.Value.ToString()));
        }

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            claims.Add(new Claim(XiHanClaimTypes.Email, user.Email));
        }

        if (!string.IsNullOrWhiteSpace(user.Phone))
        {
            claims.Add(new Claim(XiHanClaimTypes.PhoneNumber, user.Phone));
        }

        if (!string.IsNullOrWhiteSpace(user.Avatar))
        {
            claims.Add(new Claim(XiHanClaimTypes.Picture, user.Avatar));
        }

        var normalizedDeviceId = NormalizeNullable(deviceId, 200);
        if (!string.IsNullOrWhiteSpace(normalizedDeviceId))
        {
            claims.Add(new Claim(XiHanClaimTypes.DeviceFingerprint, normalizedDeviceId));
        }

        foreach (var role in roles.Where(role => !string.IsNullOrWhiteSpace(role)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(XiHanClaimTypes.Role, role));
        }

        var permissionClaims = permissions
            .Where(permission => !string.IsNullOrWhiteSpace(permission))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (permissionClaims.Contains("*", StringComparer.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(XiHanClaimTypes.Permission, "*"));
        }
        else
        {
            foreach (var permission in permissionClaims)
            {
                claims.Add(new Claim(XiHanClaimTypes.Permission, permission));
            }
        }

        if (impersonatorUserId is > 0)
        {
            claims.AddRange(XiHanClaimsIdentityExtensions.BuildImpersonatorClaims(
                impersonatorUserId.Value,
                NormalizeNullable(impersonatorUserName, 50),
                impersonatorTenantId,
                NormalizeNullable(impersonatorTenantName, 100)));
        }

        return claims;
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

    private static LoginTokenDto ToLoginTokenDto(JwtTokenResult tokenResult)
    {
        return new LoginTokenDto
        {
            AccessToken = tokenResult.AccessToken,
            RefreshToken = tokenResult.RefreshToken,
            TokenType = tokenResult.TokenType,
            ExpiresIn = tokenResult.ExpiresIn,
            IssuedAt = tokenResult.IssuedAt,
            ExpiresAt = tokenResult.ExpiresAt
        };
    }
}
