// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;
using XiHan.BasicApp.OpenPlatform.Domain.Entities;
using XiHan.BasicApp.OpenPlatform.Domain.Services;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.OpenPlatform.Infrastructure.OAuth;

/// <summary>
/// 一次授权请求（<c>/api/oauth2/authorize</c> 的查询参数，同意页原样上送）。
/// </summary>
/// <param name="ResponseType">response_type。</param>
/// <param name="ClientId">client_id。</param>
/// <param name="RedirectUri">redirect_uri。</param>
/// <param name="Scope">scope（空格分隔）。</param>
/// <param name="State">state。</param>
/// <param name="CodeChallenge">code_challenge。</param>
/// <param name="CodeChallengeMethod">code_challenge_method。</param>
/// <param name="Nonce">nonce（OIDC）。</param>
public sealed record OAuthAuthorizeInput(string? ResponseType, string? ClientId, string? RedirectUri, string? Scope, string? State, string? CodeChallenge, string? CodeChallengeMethod, string? Nonce);

/// <summary>
/// 授权请求的校验结果。
/// </summary>
/// <param name="App">应用（客户端与回调地址都对上了才有）。</param>
/// <param name="Scopes">本次授予的范围。</param>
/// <param name="Error">OAuth 错误码；通过为空。</param>
/// <param name="ErrorDescription">错误说明。</param>
public sealed record OAuthAuthorizeCheck(SysOAuthApp? App, IReadOnlyList<string> Scopes, string? Error, string? ErrorDescription)
{
    /// <summary>
    /// 是否通过。
    /// </summary>
    public bool IsValid => Error is null;

    /// <summary>
    /// 出错时能否带着错误跳回应用（客户端与回调地址都对上了才能跳，否则只在同意页上显示，RFC 6749 §4.1.2.1）。
    /// </summary>
    public bool CanRedirect => App is not null;
}

/// <summary>
/// 授权端点的规则（4.x 的校验顺序）：client_id 缺失、客户端不存在或停用、回调地址没登记（逐字匹配）都不跳回应用；
/// response_type 只认 <c>code</c>、应用须开着授权码模式、公开客户端须带 PKCE、PKCE 方法只认 <c>S256</c> / <c>plain</c>（不写即 plain）、范围取请求与登记的交集（请求为空即全部登记的）且须含 <c>openid</c>，这些错带着错误跳回应用。
/// 同意即发授权码（256 位随机数、只存散列）并累积同意记录；同意过的范围覆盖本次请求的就不再弹同意页（4.x 不记同意，除了跳过同意页的应用每次都要确认）。
/// </summary>
/// <param name="apps">应用仓储。</param>
/// <param name="codes">授权码仓储。</param>
/// <param name="consents">同意记录仓储。</param>
/// <param name="ids">标识生成器。</param>
/// <param name="clock">时间源。</param>
public sealed partial class OAuthAuthorizer(IRepository<SysOAuthApp, Guid> apps, IRepository<SysOAuthCode, Guid> codes, IRepository<SysOAuthConsent, Guid> consents, IIdGenerator<Guid> ids, TimeProvider clock)
{
    /// <summary>
    /// 校验授权请求。
    /// </summary>
    /// <param name="input">授权请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>校验结果。</returns>
    public async Task<OAuthAuthorizeCheck> CheckAsync(OAuthAuthorizeInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.ClientId))
        {
            return Fail(null, OAuthErrors.InvalidRequest, "client_id is required.");
        }

        var app = await FindAppAsync(input.ClientId.Trim(), cancellationToken).ConfigureAwait(false);
        if (app is null)
        {
            return Fail(null, OAuthErrors.UnauthorizedClient, "The client does not exist or is disabled.");
        }

        if (string.IsNullOrEmpty(input.RedirectUri) || !OAuthAppService.Split(app.RedirectUris).Contains(input.RedirectUri, StringComparer.Ordinal))
        {
            return Fail(null, OAuthErrors.InvalidRequest, "redirect_uri is not registered for this client.");
        }

        if (!string.Equals(input.ResponseType, "code", StringComparison.Ordinal))
        {
            return Fail(app, OAuthErrors.UnsupportedResponseType, "Only response_type=code is supported.");
        }

        if (!OAuthAppService.Split(app.GrantTypes).Contains(OAuthAppService.AuthorizationCode, StringComparer.Ordinal))
        {
            return Fail(app, OAuthErrors.UnauthorizedClient, "The client is not allowed to use the authorization code flow.");
        }

        if (string.IsNullOrEmpty(input.CodeChallenge))
        {
            if (app.ClientType == OAuthClientType.Public)
            {
                return Fail(app, OAuthErrors.InvalidRequest, "Public clients must use PKCE (code_challenge).");
            }
        }
        else if (!ChallengePattern().IsMatch(input.CodeChallenge) || MethodOf(input.CodeChallengeMethod) is null)
        {
            return Fail(app, OAuthErrors.InvalidRequest, "Invalid code_challenge or code_challenge_method (S256 or plain).");
        }

        var registered = OAuthAppService.Split(app.Scopes);
        var requested = OAuthAppService.Split(input.Scope).Select(static s => s.ToLowerInvariant()).Distinct(StringComparer.Ordinal).ToList();
        var granted = requested.Count == 0 ? registered.ToList() : requested.Where(s => registered.Contains(s, StringComparer.Ordinal)).ToList();
        if (!granted.Contains("openid", StringComparer.Ordinal))
        {
            return Fail(app, OAuthErrors.InvalidScope, "The openid scope is required.");
        }

        return new OAuthAuthorizeCheck(app, granted, null, null);
    }

    /// <summary>
    /// 用户此前同意过的范围是否已覆盖本次的（覆盖或应用跳过同意页即不再弹同意页）。
    /// </summary>
    /// <param name="userId">用户。</param>
    /// <param name="check">校验通过的结果。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>是否不必再确认。</returns>
    public async Task<bool> ConsentedAsync(Guid userId, OAuthAuthorizeCheck check, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(check);
        if (check.App is not { } app)
        {
            return false;
        }

        if (app.SkipConsent)
        {
            return true;
        }

        var consent = await FindConsentAsync(userId, app.ClientId, cancellationToken).ConfigureAwait(false);
        var agreed = OAuthAppService.Split(consent?.Scopes);
        return consent is not null && check.Scopes.All(s => agreed.Contains(s, StringComparer.Ordinal));
    }

    /// <summary>
    /// 同意：发授权码并累积同意记录，返回带 <c>code</c> 与 <c>state</c> 的回调地址。
    /// </summary>
    /// <param name="userId">用户。</param>
    /// <param name="input">授权请求。</param>
    /// <param name="check">校验通过的结果。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>回调地址。</returns>
    public async Task<Result<string>> ApproveAsync(Guid userId, OAuthAuthorizeInput input, OAuthAuthorizeCheck check, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(check);
        var app = check.App!;
        var now = clock.GetUtcNow();
        var code = OAuthProtocol.NewToken();
        var row = new SysOAuthCode
        {
            Id = ids.NewId(),
            CodeHash = OAuthProtocol.Hash(code),
            ClientId = app.ClientId,
            UserId = userId,
            RedirectUri = input.RedirectUri!,
            Scopes = string.Join(' ', check.Scopes),
            CodeChallenge = string.IsNullOrEmpty(input.CodeChallenge) ? null : input.CodeChallenge,
            CodeChallengeMethod = string.IsNullOrEmpty(input.CodeChallenge) ? null : MethodOf(input.CodeChallengeMethod),
            Nonce = string.IsNullOrEmpty(input.Nonce) ? null : input.Nonce[..Math.Min(input.Nonce.Length, 200)],
            ExpiresAt = now.AddSeconds(app.AuthorizationCodeLifetime),
        };
        var added = await codes.AddAsync(row, cancellationToken).ConfigureAwait(false);
        if (added.IsFailure)
        {
            return added.Error!;
        }

        var consent = await FindConsentAsync(userId, app.ClientId, cancellationToken).ConfigureAwait(false);
        if (consent is null)
        {
            var created = await consents.AddAsync(new SysOAuthConsent { Id = ids.NewId(), UserId = userId, ClientId = app.ClientId, Scopes = row.Scopes, GrantedAt = now }, cancellationToken).ConfigureAwait(false);
            if (created.IsFailure)
            {
                return created.Error!;
            }
        }
        else
        {
            consent.Scopes = string.Join(' ', OAuthAppService.Split(consent.Scopes).Concat(check.Scopes).Distinct(StringComparer.Ordinal));
            consent.GrantedAt = now;
            var updated = await consents.UpdateAsync(consent, cancellationToken).ConfigureAwait(false);
            if (updated.IsFailure)
            {
                return updated.Error!;
            }
        }

        return Redirect(input.RedirectUri!, ("code", code), ("state", input.State));
    }

    /// <summary>
    /// 带着错误跳回应用的地址（<c>error</c>、<c>error_description</c>、<c>state</c>）。
    /// </summary>
    /// <param name="redirectUri">登记过的回调地址。</param>
    /// <param name="error">错误码。</param>
    /// <param name="description">说明。</param>
    /// <param name="state">state。</param>
    /// <returns>地址。</returns>
    public static string ErrorRedirect(string redirectUri, string error, string? description, string? state)
        => Redirect(redirectUri, ("error", error), ("error_description", description), ("state", state));

    private async Task<SysOAuthApp?> FindAppAsync(string clientId, CancellationToken cancellationToken)
    {
        var rows = await apps.ListAsync(EntityQuery.All<SysOAuthApp>().Where(new Specification<SysOAuthApp>(a => a.ClientId == clientId && a.IsEnabled)).Page(0, 1), cancellationToken).ConfigureAwait(false);
        return rows.Count == 0 ? null : rows[0];
    }

    private async Task<SysOAuthConsent?> FindConsentAsync(Guid userId, string clientId, CancellationToken cancellationToken)
    {
        var rows = await consents.ListAsync(EntityQuery.All<SysOAuthConsent>().Where(new Specification<SysOAuthConsent>(c => c.UserId == userId && c.ClientId == clientId)).Page(0, 1), cancellationToken).ConfigureAwait(false);
        return rows.Count == 0 ? null : rows[0];
    }

    private static string? MethodOf(string? method)
        => string.IsNullOrEmpty(method) ? OAuthProtocol.Plain : method switch
        {
            OAuthProtocol.S256 => OAuthProtocol.S256,
            OAuthProtocol.Plain => OAuthProtocol.Plain,
            _ => null,
        };

    private static string Redirect(string redirectUri, params (string Key, string? Value)[] parameters)
    {
        var values = parameters.Where(static p => !string.IsNullOrEmpty(p.Value)).ToDictionary(static p => p.Key, static p => (string?)p.Value);
        return QueryHelpers.AddQueryString(redirectUri, values);
    }

    private static OAuthAuthorizeCheck Fail(SysOAuthApp? app, string error, string description) => new(app, [], error, description);

    [GeneratedRegex("^[A-Za-z0-9._~-]{43,128}$")]
    private static partial Regex ChallengePattern();
}

/// <summary>
/// OAuth 协议的小工具：随机令牌、散列、PKCE 与客户端密钥核对。
/// </summary>
public static class OAuthProtocol
{
    /// <summary>
    /// PKCE S256。
    /// </summary>
    public const string S256 = "S256";

    /// <summary>
    /// PKCE plain。
    /// </summary>
    public const string Plain = "plain";

    /// <summary>
    /// 新的不透明令牌（256 位随机数，Base64Url）。
    /// </summary>
    public static string NewToken() => Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    /// <summary>
    /// 令牌与授权码的 SHA-256（小写十六进制）。
    /// </summary>
    public static string Hash(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    /// <summary>
    /// PKCE 核对（恒定时间）。
    /// </summary>
    public static bool VerifyPkce(string challenge, string? method, string? verifier)
    {
        if (string.IsNullOrEmpty(verifier) || verifier.Length is < 43 or > 128)
        {
            return false;
        }

        var expected = method == S256 ? Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))) : verifier;
        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(challenge));
    }

    /// <summary>
    /// 客户端密钥核对（散列恒定时间比较）。
    /// </summary>
    public static bool VerifySecret(string? hash, string? secret)
        => hash is not null && secret is not null
            && CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(OAuthAppService.HashSecret(secret)), Encoding.ASCII.GetBytes(hash));

    /// <summary>
    /// Base64Url（不补等号）。
    /// </summary>
    public static string Base64UrlEncode(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

/// <summary>
/// OAuth 2.0 / OIDC 的标准错误码。
/// </summary>
public static class OAuthErrors
{
    /// <summary>
    /// 请求参数不对。
    /// </summary>
    public const string InvalidRequest = "invalid_request";

    /// <summary>
    /// 客户端认证失败。
    /// </summary>
    public const string InvalidClient = "invalid_client";

    /// <summary>
    /// 授权码或刷新令牌无效。
    /// </summary>
    public const string InvalidGrant = "invalid_grant";

    /// <summary>
    /// 客户端不允许这种用法。
    /// </summary>
    public const string UnauthorizedClient = "unauthorized_client";

    /// <summary>
    /// 不支持的授权类型。
    /// </summary>
    public const string UnsupportedGrantType = "unsupported_grant_type";

    /// <summary>
    /// 不支持的 response_type。
    /// </summary>
    public const string UnsupportedResponseType = "unsupported_response_type";

    /// <summary>
    /// 范围不对。
    /// </summary>
    public const string InvalidScope = "invalid_scope";

    /// <summary>
    /// 用户拒绝。
    /// </summary>
    public const string AccessDenied = "access_denied";

    /// <summary>
    /// 访问令牌无效（userinfo）。
    /// </summary>
    public const string InvalidToken = "invalid_token";
}
