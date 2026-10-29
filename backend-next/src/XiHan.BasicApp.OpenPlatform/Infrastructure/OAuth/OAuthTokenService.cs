// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.OpenPlatform.Domain.Entities;
using XiHan.BasicApp.OpenPlatform.Domain.Services;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Persistence.SqlSugar;

namespace XiHan.BasicApp.OpenPlatform.Infrastructure.OAuth;

/// <summary>
/// 令牌端点的一次结果：成功即令牌对，失败即 OAuth 错误（401 只给客户端认证失败）。
/// </summary>
/// <param name="Tokens">令牌。</param>
/// <param name="Error">错误码。</param>
/// <param name="Description">说明。</param>
public sealed record OAuthTokenResult(OAuthIssuedTokens? Tokens, string? Error, string? Description)
{
    /// <summary>
    /// 失败。
    /// </summary>
    public static OAuthTokenResult Fail(string error, string description) => new(null, error, description);
}

/// <summary>
/// 签发出去的令牌。
/// </summary>
/// <param name="AccessToken">访问令牌。</param>
/// <param name="ExpiresIn">访问令牌有效期（秒）。</param>
/// <param name="RefreshToken">刷新令牌（应用开着刷新令牌模式才有）。</param>
/// <param name="Scope">授予的范围。</param>
/// <param name="IdToken">id_token（只在授权码兑换时签）。</param>
public sealed record OAuthIssuedTokens(string AccessToken, int ExpiresIn, string? RefreshToken, string Scope, string? IdToken);

/// <summary>
/// 令牌服务（在一次平台调用里用）：客户端认证、授权码兑换（PKCE、回调地址一致、原子消费，重放即吊销这次授权码发出的整族令牌）、刷新令牌轮换（作废的再被使用即整族吊销）、撤销与 userinfo。
/// 访问令牌与刷新令牌是不透明随机串、只存散列，只用于身份；id_token 为 RS256，受众是 ClientId，按范围带 profile / email / phone 声明（4.x 不按范围收窄）。
/// </summary>
/// <param name="apps">应用仓储。</param>
/// <param name="codes">授权码仓储。</param>
/// <param name="tokens">令牌仓储。</param>
/// <param name="users">用户仓储。</param>
/// <param name="unitOfWork">工作单元（条件更新）。</param>
/// <param name="keys">签名密钥。</param>
/// <param name="options">开放平台配置。</param>
/// <param name="ids">标识生成器。</param>
/// <param name="clock">时间源。</param>
public sealed class OAuthTokenService(
    IRepository<SysOAuthApp, Guid> apps,
    IRepository<SysOAuthCode, Guid> codes,
    IRepository<SysOAuthToken, Guid> tokens,
    IRepository<SysUser, Guid> users,
    SqlSugarUnitOfWork unitOfWork,
    OAuthSigningKeys keys,
    OpenPlatformOptions options,
    IIdGenerator<Guid> ids,
    TimeProvider clock)
{
    private readonly JsonWebTokenHandler _handler = new() { SetDefaultTimesOnTokenCreation = false };

    /// <summary>
    /// 客户端认证：<c>Authorization: Basic</c>（先 form-url 解码）优先，其次表单的 client_id / client_secret；机密客户端须核对密钥，公开客户端只认 client_id。
    /// </summary>
    /// <param name="clientId">ClientId。</param>
    /// <param name="clientSecret">密钥。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>应用；认证失败为空。</returns>
    public async Task<SysOAuthApp?> AuthenticateClientAsync(string? clientId, string? clientSecret, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(clientId))
        {
            return null;
        }

        var rows = await apps.ListAsync(EntityQuery.All<SysOAuthApp>().Where(new Specification<SysOAuthApp>(a => a.ClientId == clientId && a.IsEnabled)).Page(0, 1), cancellationToken).ConfigureAwait(false);
        if (rows.Count == 0)
        {
            return null;
        }

        var app = rows[0];
        return app.ClientType == OAuthClientType.Public || OAuthProtocol.VerifySecret(app.ClientSecretHash, clientSecret) ? app : null;
    }

    /// <summary>
    /// 授权码兑换。
    /// </summary>
    /// <param name="app">已认证的应用。</param>
    /// <param name="code">授权码。</param>
    /// <param name="redirectUri">回调地址（须与授权时逐字一致）。</param>
    /// <param name="codeVerifier">PKCE verifier。</param>
    /// <param name="issuer">签发者。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>结果。</returns>
    public async Task<OAuthTokenResult> ExchangeCodeAsync(SysOAuthApp app, string? code, string? redirectUri, string? codeVerifier, string issuer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(app);
        if (string.IsNullOrEmpty(code))
        {
            return OAuthTokenResult.Fail(OAuthErrors.InvalidRequest, "code is required.");
        }

        var hash = OAuthProtocol.Hash(code);
        var rows = await codes.ListAsync(EntityQuery.All<SysOAuthCode>().Where(new Specification<SysOAuthCode>(c => c.CodeHash == hash)).Page(0, 1), cancellationToken).ConfigureAwait(false);
        if (rows.Count == 0)
        {
            return OAuthTokenResult.Fail(OAuthErrors.InvalidGrant, "The authorization code is invalid.");
        }

        var row = rows[0];
        var now = clock.GetUtcNow();
        if (row.IsConsumed)
        {
            // 重放：这个授权码兑换出的令牌整族作废（RFC 6749 §4.1.2）
            await RevokeFamilyAsync(row.Id, now, cancellationToken).ConfigureAwait(false);
            return OAuthTokenResult.Fail(OAuthErrors.InvalidGrant, "The authorization code has already been used.");
        }

        if (!string.Equals(row.ClientId, app.ClientId, StringComparison.Ordinal))
        {
            return OAuthTokenResult.Fail(OAuthErrors.InvalidGrant, "The authorization code was issued to another client.");
        }

        if (row.ExpiresAt <= now)
        {
            return OAuthTokenResult.Fail(OAuthErrors.InvalidGrant, "The authorization code has expired.");
        }

        if (!string.Equals(row.RedirectUri, redirectUri, StringComparison.Ordinal))
        {
            return OAuthTokenResult.Fail(OAuthErrors.InvalidGrant, "redirect_uri does not match the authorization request.");
        }

        if (row.CodeChallenge is { } challenge ? !OAuthProtocol.VerifyPkce(challenge, row.CodeChallengeMethod, codeVerifier) : app.ClientType == OAuthClientType.Public)
        {
            return OAuthTokenResult.Fail(OAuthErrors.InvalidGrant, "PKCE verification failed.");
        }

        // 条件更新抢占：同时兑换的两次只有一次改得到行
        await unitOfWork.EnsureTransactionAsync(cancellationToken).ConfigureAwait(false);
        var consumed = await unitOfWork.ConnectionFor(typeof(SysOAuthCode)).Updateable<SysOAuthCode>()
            .SetColumns(c => new SysOAuthCode { IsConsumed = true })
            .Where(c => c.Id == row.Id && !c.IsConsumed)
            .ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
        if (consumed != 1)
        {
            await RevokeFamilyAsync(row.Id, now, cancellationToken).ConfigureAwait(false);
            return OAuthTokenResult.Fail(OAuthErrors.InvalidGrant, "The authorization code has already been used.");
        }

        var user = await ActiveUserAsync(row.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return OAuthTokenResult.Fail(OAuthErrors.InvalidGrant, "The user does not exist or is disabled.");
        }

        var scopes = OAuthAppService.Split(row.Scopes);
        var issued = await IssueAsync(app, user.Id, row.Id, scopes, now, cancellationToken).ConfigureAwait(false);
        var idToken = await IdTokenAsync(app, user, scopes, row.Nonce, row.CreatedAt, issued.AccessToken, issuer, now, cancellationToken).ConfigureAwait(false);
        return new OAuthTokenResult(issued with { IdToken = idToken }, null, null);
    }

    /// <summary>
    /// 刷新令牌轮换：旧的作废、同族发新的一对；作废的刷新令牌再被使用即整族吊销。
    /// </summary>
    /// <param name="app">已认证的应用。</param>
    /// <param name="refreshToken">刷新令牌。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>结果。</returns>
    public async Task<OAuthTokenResult> RefreshAsync(SysOAuthApp app, string? refreshToken, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(app);
        if (string.IsNullOrEmpty(refreshToken))
        {
            return OAuthTokenResult.Fail(OAuthErrors.InvalidRequest, "refresh_token is required.");
        }

        if (!OAuthAppService.Split(app.GrantTypes).Contains(OAuthAppService.RefreshToken, StringComparer.Ordinal))
        {
            return OAuthTokenResult.Fail(OAuthErrors.UnauthorizedClient, "The client is not allowed to use refresh tokens.");
        }

        var row = await FindAsync(refreshToken, OAuthTokenKind.Refresh, cancellationToken).ConfigureAwait(false);
        var now = clock.GetUtcNow();
        if (row is null || !string.Equals(row.ClientId, app.ClientId, StringComparison.Ordinal))
        {
            return OAuthTokenResult.Fail(OAuthErrors.InvalidGrant, "The refresh token is invalid.");
        }

        if (row.RevokedAt is not null)
        {
            await RevokeFamilyAsync(row.FamilyId, now, cancellationToken).ConfigureAwait(false);
            return OAuthTokenResult.Fail(OAuthErrors.InvalidGrant, "The refresh token has been revoked.");
        }

        if (row.ExpiresAt <= now)
        {
            return OAuthTokenResult.Fail(OAuthErrors.InvalidGrant, "The refresh token has expired.");
        }

        var user = await ActiveUserAsync(row.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return OAuthTokenResult.Fail(OAuthErrors.InvalidGrant, "The user does not exist or is disabled.");
        }

        row.RevokedAt = now;
        var revoked = await tokens.UpdateAsync(row, cancellationToken).ConfigureAwait(false);
        revoked.ThrowIfFailure();
        var issued = await IssueAsync(app, user.Id, row.FamilyId, OAuthAppService.Split(row.Scopes), now, cancellationToken).ConfigureAwait(false);
        return new OAuthTokenResult(issued, null, null);
    }

    /// <summary>
    /// 撤销（RFC 7009）：令牌属于这个客户端才撤；刷新令牌连同整族，访问令牌只撤自己；不认识的令牌静默成功。
    /// </summary>
    /// <param name="app">已认证的应用。</param>
    /// <param name="token">令牌。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    public async Task RevokeAsync(SysOAuthApp app, string? token, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(app);
        if (string.IsNullOrEmpty(token))
        {
            return;
        }

        var row = await FindAsync(token, null, cancellationToken).ConfigureAwait(false);
        if (row is null || !string.Equals(row.ClientId, app.ClientId, StringComparison.Ordinal))
        {
            return;
        }

        var now = clock.GetUtcNow();
        if (row.Kind == OAuthTokenKind.Refresh)
        {
            await RevokeFamilyAsync(row.FamilyId, now, cancellationToken).ConfigureAwait(false);
            return;
        }

        row.RevokedAt ??= now;
        (await tokens.UpdateAsync(row, cancellationToken).ConfigureAwait(false)).ThrowIfFailure();
    }

    /// <summary>
    /// userinfo：访问令牌有效、应用还启用着、用户正常才返回，声明按范围给。
    /// </summary>
    /// <param name="accessToken">访问令牌。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>声明；令牌无效为空。</returns>
    public async Task<IReadOnlyDictionary<string, string>?> UserInfoAsync(string? accessToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            return null;
        }

        var row = await FindAsync(accessToken, OAuthTokenKind.Access, cancellationToken).ConfigureAwait(false);
        if (row is null || row.RevokedAt is not null || row.ExpiresAt <= clock.GetUtcNow())
        {
            return null;
        }

        var clientId = row.ClientId;
        if (await apps.CountAsync(EntityQuery.All<SysOAuthApp>().Where(new Specification<SysOAuthApp>(a => a.ClientId == clientId && a.IsEnabled)), cancellationToken).ConfigureAwait(false) == 0)
        {
            return null;
        }

        var user = await ActiveUserAsync(row.UserId, cancellationToken).ConfigureAwait(false);
        return user is null ? null : Claims(user, OAuthAppService.Split(row.Scopes));
    }

    /// <summary>
    /// 吊销一个用户在一个应用下的全部令牌（撤销同意时用）。
    /// </summary>
    /// <param name="userId">用户。</param>
    /// <param name="clientId">ClientId。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    public async Task RevokeAllAsync(Guid userId, string clientId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        await unitOfWork.EnsureTransactionAsync(cancellationToken).ConfigureAwait(false);
        await unitOfWork.ConnectionFor(typeof(SysOAuthToken)).Updateable<SysOAuthToken>()
            .SetColumns(t => new SysOAuthToken { RevokedAt = now })
            .Where(t => t.UserId == userId && t.ClientId == clientId)
            .ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<OAuthIssuedTokens> IssueAsync(SysOAuthApp app, Guid userId, Guid familyId, IReadOnlyList<string> scopes, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var access = OAuthProtocol.NewToken();
        var scope = string.Join(' ', scopes);
        var rows = new List<SysOAuthToken>
        {
            new() { Id = ids.NewId(), TokenHash = OAuthProtocol.Hash(access), Kind = OAuthTokenKind.Access, FamilyId = familyId, ClientId = app.ClientId, UserId = userId, Scopes = scope, ExpiresAt = now.AddSeconds(app.AccessTokenLifetime) },
        };
        string? refresh = null;
        if (OAuthAppService.Split(app.GrantTypes).Contains(OAuthAppService.RefreshToken, StringComparer.Ordinal))
        {
            refresh = OAuthProtocol.NewToken();
            rows.Add(new SysOAuthToken { Id = ids.NewId(), TokenHash = OAuthProtocol.Hash(refresh), Kind = OAuthTokenKind.Refresh, FamilyId = familyId, ClientId = app.ClientId, UserId = userId, Scopes = scope, ExpiresAt = now.AddSeconds(app.RefreshTokenLifetime) });
        }

        foreach (var added in await tokens.AddRangeAsync(rows, cancellationToken).ConfigureAwait(false))
        {
            added.ThrowIfFailure();
        }

        return new OAuthIssuedTokens(access, app.AccessTokenLifetime, refresh, scope, null);
    }

    private async Task<string> IdTokenAsync(SysOAuthApp app, SysUser user, IReadOnlyList<string> scopes, string? nonce, DateTimeOffset authTime, string accessToken, string issuer, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var claims = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["auth_time"] = authTime.ToUnixTimeSeconds(),
            ["at_hash"] = OAuthProtocol.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(accessToken))[..16]),
            ["jti"] = Guid.NewGuid().ToString("N"),
        };
        foreach (var (key, value) in Claims(user, scopes))
        {
            claims[key] = value;
        }

        if (nonce is not null)
        {
            claims["nonce"] = nonce;
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = app.ClientId,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = now.AddSeconds(options.IdTokenLifetimeSeconds).UtcDateTime,
            Claims = claims,
            SigningCredentials = await keys.SigningAsync(cancellationToken).ConfigureAwait(false),
        };
        return _handler.CreateToken(descriptor);
    }

    /// <summary>
    /// 按范围给的身份声明：<c>sub</c> 总有；profile → name、preferred_username；email → email；phone → phone_number。
    /// </summary>
    private static Dictionary<string, string> Claims(SysUser user, IReadOnlyList<string> scopes)
    {
        var claims = new Dictionary<string, string>(StringComparer.Ordinal) { ["sub"] = user.Id.ToString("D") };
        if (scopes.Contains("profile", StringComparer.Ordinal))
        {
            claims["name"] = user.DisplayName;
            claims["preferred_username"] = user.UserName;
        }

        if (scopes.Contains("email", StringComparer.Ordinal) && !string.IsNullOrEmpty(user.Email))
        {
            claims["email"] = user.Email;
        }

        if (scopes.Contains("phone", StringComparer.Ordinal) && !string.IsNullOrEmpty(user.PhoneNumber))
        {
            claims["phone_number"] = user.PhoneNumber;
        }

        return claims;
    }

    private async Task<SysOAuthToken?> FindAsync(string token, OAuthTokenKind? kind, CancellationToken cancellationToken)
    {
        var hash = OAuthProtocol.Hash(token);
        var rows = await tokens.ListAsync(EntityQuery.All<SysOAuthToken>().Where(new Specification<SysOAuthToken>(t => t.TokenHash == hash)).Page(0, 1), cancellationToken).ConfigureAwait(false);
        return rows.Count == 0 || (kind is { } expected && rows[0].Kind != expected) ? null : rows[0];
    }

    private async Task<SysUser?> ActiveUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var found = await users.FindAsync(userId, cancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var user) && user.Status == UserStatus.Active ? user : null;
    }

    private async Task RevokeFamilyAsync(Guid familyId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await unitOfWork.EnsureTransactionAsync(cancellationToken).ConfigureAwait(false);
        await unitOfWork.ConnectionFor(typeof(SysOAuthToken)).Updateable<SysOAuthToken>()
            .SetColumns(t => new SysOAuthToken { RevokedAt = now })
            .Where(t => t.FamilyId == familyId)
            .ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
    }
}
