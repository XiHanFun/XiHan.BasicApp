// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.Framework.Security.Jwt;

namespace XiHan.BasicApp.Identity.Infrastructure;

/// <summary>
/// 签发 HS256 访问令牌：与框架 <c>Security.Jwt</c> 共用 <c>XiHan:Security:Jwt</c> 的密钥、签发者与受众，由它校验。
/// 声明：<c>sub</c> 用户 ID、<c>name</c>、<c>preferred_username</c>、<c>tenant</c>（平台账号没有）、<c>sid</c> 会话、<c>role</c>（可多条）、<c>act</c>（模仿会话才有：发起人用户 ID）。
/// </summary>
public sealed class AccessTokenIssuer
{
    private readonly JsonWebTokenHandler _handler = new() { SetDefaultTimesOnTokenCreation = false };
    private readonly SigningCredentials _credentials;
    private readonly JwtOptions _jwt;
    private readonly IdentityOptions _options;
    private readonly TimeProvider _clock;

    /// <summary>
    /// 构造签发器。
    /// </summary>
    public AccessTokenIssuer(JwtOptions jwt, IdentityOptions options, TimeProvider clock)
    {
        _jwt = jwt ?? throw new ArgumentNullException(nameof(jwt));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)), SecurityAlgorithms.HmacSha256);
    }

    /// <summary>
    /// 为用户的某个会话签发访问令牌。
    /// </summary>
    /// <param name="user">用户。</param>
    /// <param name="sessionId">会话 ID。</param>
    /// <param name="tenantId">当前租户；平台账号传 <see langword="null"/>。</param>
    /// <param name="roles">角色（进 <c>role</c> 声明）。</param>
    /// <param name="actorId">模仿会话的发起人；普通会话传 <see langword="null"/>。</param>
    public IssuedToken Issue(SysUser user, Guid sessionId, string? tenantId, IReadOnlyList<string> roles, Guid? actorId = null)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(roles);
        var now = _clock.GetUtcNow();
        var expires = now + _options.AccessTokenLifetime;
        var claims = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [IdentityClaims.Subject] = user.Id.ToString("D"),
            [IdentityClaims.Name] = user.DisplayName,
            [IdentityClaims.UserName] = user.UserName,
            [IdentityClaims.Session] = sessionId.ToString("D"),
        };
        if (tenantId is not null)
        {
            claims[IdentityClaims.Tenant] = tenantId;
        }

        if (actorId is { } actor)
        {
            claims[IdentityClaims.Actor] = actor.ToString("D");
        }

        if (roles.Count == 1)
        {
            claims[IdentityClaims.Role] = roles[0];
        }
        else if (roles.Count > 1)
        {
            claims[IdentityClaims.Role] = roles.ToArray();
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _jwt.Issuer,
            Audience = _jwt.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            Claims = claims,
            SigningCredentials = _credentials,
        };
        return new IssuedToken(_handler.CreateToken(descriptor), expires);
    }
}
