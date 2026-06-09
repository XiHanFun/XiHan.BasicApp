#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AuthTokenIssueService
// Guid:08d7df56-a4de-4850-9e1d-3555fb1ef81f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Authentication.Jwt;
using XiHan.Framework.Security.Claims;

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

    /// <inheritdoc />
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
            command.DeviceId);
        var tokenResult = _jwtTokenService.GenerateAccessToken(claims);
        return new AuthAccessTokenIssueResult(tokenResult, ToLoginTokenDto(tokenResult));
    }

    /// <inheritdoc />
    public LoginTokenDto RefreshAccessToken(string accessToken, string refreshToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        var tokenResult = _jwtTokenService.RefreshAccessToken(accessToken.Trim(), refreshToken.Trim())
            ?? throw new InvalidOperationException("刷新令牌无效或已过期。");
        return ToLoginTokenDto(tokenResult);
    }

    private static List<Claim> BuildClaims(
        SysUser user,
        long? tenantId,
        string sessionBusinessId,
        string accessTokenJti,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> permissions,
        string? deviceId)
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
