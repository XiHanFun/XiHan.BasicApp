// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Authentication.Jwt;
using XiHan.Framework.Authentication.Oidc;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Security.Claims;
using XiHan.Framework.Security.Password;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// OAuth2 授权服务端协议服务实现
/// </summary>
public sealed class OAuthServerService : IOAuthServerService
{
    private const string GrantTypeAuthorizationCode = "authorization_code";
    private const string GrantTypeRefreshToken = "refresh_token";
    private const string GrantTypeClientCredentials = "client_credentials";
    private const string ResponseTypeCode = "code";
    private const string PkceMethodS256 = "S256";
    private const string PkceMethodPlain = "plain";

    private readonly IOAuthAppRepository _oauthAppRepository;
    private readonly IOAuthCodeRepository _oauthCodeRepository;
    private readonly IOAuthTokenRepository _oauthTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IAuthTokenIssueService _authTokenIssueService;
    private readonly IAuthorizationSnapshotQueryService _authorizationSnapshotQueryService;
    private readonly IIdTokenService _idTokenService;
    private readonly IOptions<OidcOptions> _oidcOptions;
    private readonly ICurrentTenant _currentTenant;
    private readonly ILogger<OAuthServerService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public OAuthServerService(
        IOAuthAppRepository oauthAppRepository,
        IOAuthCodeRepository oauthCodeRepository,
        IOAuthTokenRepository oauthTokenRepository,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IAuthTokenIssueService authTokenIssueService,
        IAuthorizationSnapshotQueryService authorizationSnapshotQueryService,
        IIdTokenService idTokenService,
        IOptions<OidcOptions> oidcOptions,
        ICurrentTenant currentTenant,
        ILogger<OAuthServerService> logger)
    {
        _oauthAppRepository = oauthAppRepository;
        _oauthCodeRepository = oauthCodeRepository;
        _oauthTokenRepository = oauthTokenRepository;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _authTokenIssueService = authTokenIssueService;
        _authorizationSnapshotQueryService = authorizationSnapshotQueryService;
        _idTokenService = idTokenService;
        _oidcOptions = oidcOptions;
        _currentTenant = currentTenant;
        _logger = logger;
    }

    #region 授权（同意页）

    /// <summary>
    /// 校验授权请求（供同意页预览：返回客户端展示信息与解析后的授权范围）
    /// </summary>
    public async Task<OAuthAuthorizeValidation> ValidateAuthorizeRequestAsync(OAuthAuthorizeRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var clientId = request.ClientId?.Trim();
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return Invalid("invalid_request", "缺少 client_id 参数。");
        }

        var app = await _oauthAppRepository.GetByClientIdIgnoreTenantAsync(clientId, cancellationToken);
        if (app is null || app.Status != EnableStatus.Enabled)
        {
            return Invalid("unauthorized_client", "客户端不存在或已停用。");
        }

        var redirectUri = request.RedirectUri?.Trim();
        var redirectValid = !string.IsNullOrWhiteSpace(redirectUri) && IsRegisteredRedirectUri(app, redirectUri!);
        if (!redirectValid)
        {
            // redirect_uri 非法：严禁向其跳转，只能在同意页内呈现错误
            return Invalid("invalid_request", "redirect_uri 未注册或不匹配。");
        }

        if (!string.Equals(request.ResponseType?.Trim(), ResponseTypeCode, StringComparison.Ordinal))
        {
            return InvalidWithRedirect("unsupported_response_type", "仅支持 response_type=code。", app);
        }

        if (!ClientSupportsGrant(app, GrantTypeAuthorizationCode))
        {
            return InvalidWithRedirect("unauthorized_client", "客户端未启用授权码模式。", app);
        }

        // PKCE：公开客户端（无密钥）强制要求 code_challenge
        var challenge = request.CodeChallenge?.Trim();
        if (IsPublicClient(app) && string.IsNullOrWhiteSpace(challenge))
        {
            return InvalidWithRedirect("invalid_request", "公开客户端必须提供 PKCE code_challenge。", app);
        }

        if (!string.IsNullOrWhiteSpace(challenge) && !IsSupportedPkceMethod(request.CodeChallengeMethod))
        {
            return InvalidWithRedirect("invalid_request", "不支持的 code_challenge_method（仅 S256/plain）。", app);
        }

        var scopes = ResolveGrantedScopes(app, request.Scope);
        return new OAuthAuthorizeValidation(true, null, null, true, ToClientInfo(app), scopes);
    }

    /// <summary>
    /// 用户同意后创建授权码，返回携带 code/state 的最终跳转地址
    /// </summary>
    public async Task<OAuthCreateCodeResult> CreateAuthorizationCodeAsync(OAuthCreateCodeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.UserId <= 0)
        {
            return new OAuthCreateCodeResult(false, "access_denied", "当前用户未登录。", null);
        }

        // 服务端再次完整校验（不信任前端回传），单一事实源
        var validation = await ValidateAuthorizeRequestAsync(command.Request, cancellationToken);
        if (!validation.Success)
        {
            return new OAuthCreateCodeResult(false, validation.Error, validation.ErrorDescription, null);
        }

        var request = command.Request;
        var challenge = request.CodeChallenge?.Trim();
        var code = GenerateUrlSafeToken(32);

        var app = await _oauthAppRepository.GetByClientIdIgnoreTenantAsync(request.ClientId!.Trim(), cancellationToken);
        var lifetimeSeconds = app is { AuthorizationCodeLifetime: > 0 } ? app.AuthorizationCodeLifetime : 300;
        var now = DateTimeOffset.UtcNow;

        var entity = new SysOAuthCode
        {
            Code = code,
            ClientId = request.ClientId!.Trim(),
            UserId = command.UserId,
            RedirectUri = request.RedirectUri!.Trim(),
            Scopes = JoinScopes(validation.Scopes),
            CsrfState = Truncate(request.State?.Trim(), 200),
            CodeChallenge = Truncate(challenge, 100),
            CodeChallengeMethod = string.IsNullOrWhiteSpace(challenge) ? null : NormalizePkceMethod(request.CodeChallengeMethod),
            Nonce = Truncate(request.Nonce?.Trim(), 200),
            ExpirationTime = now.AddSeconds(lifetimeSeconds),
            IsUsed = false
        };

        // 授权码落库于用户所属租户上下文：令牌端点据 code.TenantId 还原租户并签发令牌
        var effectiveTenantId = command.UserTenantId is > 0 ? command.UserTenantId : null;
        using (_currentTenant.Change(effectiveTenantId, effectiveTenantId?.ToString()))
        {
            _ = await _oauthCodeRepository.AddAsync(entity, cancellationToken);
        }

        var redirectUri = AppendQuery(request.RedirectUri!.Trim(), "code", code);
        if (!string.IsNullOrWhiteSpace(request.State))
        {
            redirectUri = AppendQuery(redirectUri, "state", request.State!);
        }

        _logger.LogInformation("OAuth: 授权码已签发 clientId={ClientId} userId={UserId}", entity.ClientId, command.UserId);
        return new OAuthCreateCodeResult(true, null, null, redirectUri);
    }

    #endregion 授权（同意页）

    #region 令牌端点

    /// <summary>
    /// 令牌端点：按 grant_type 分发（authorization_code / refresh_token / client_credentials）
    /// </summary>
    public async Task<OAuthTokenOutcome> ExchangeTokenAsync(OAuthTokenRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var grantType = request.GrantType?.Trim();
        return grantType switch
        {
            GrantTypeAuthorizationCode => await ExchangeAuthorizationCodeAsync(request, cancellationToken),
            GrantTypeRefreshToken => await ExchangeRefreshTokenAsync(request, cancellationToken),
            GrantTypeClientCredentials => await ExchangeClientCredentialsAsync(request, cancellationToken),
            _ => TokenError(400, "unsupported_grant_type", "不支持的 grant_type。")
        };
    }

    private async Task<OAuthTokenOutcome> ExchangeAuthorizationCodeAsync(OAuthTokenRequest request, CancellationToken cancellationToken)
    {
        var codeValue = request.Code?.Trim();
        if (string.IsNullOrWhiteSpace(codeValue))
        {
            return TokenError(400, "invalid_request", "缺少 code 参数。");
        }

        var code = await _oauthCodeRepository.GetByCodeIgnoreTenantAsync(codeValue, cancellationToken);
        if (code is null)
        {
            return TokenError(400, "invalid_grant", "授权码无效。");
        }

        // 已使用：疑似重放，吊销该客户端下该用户的整个令牌族
        if (code.IsUsed)
        {
            _ = await _oauthTokenRepository.RevokeFamilyAsync(code.UserId, code.ClientId, DateTimeOffset.UtcNow, cancellationToken);
            _logger.LogWarning("OAuth: 授权码重放检测 clientId={ClientId} userId={UserId}", code.ClientId, code.UserId);
            return TokenError(400, "invalid_grant", "授权码已被使用。");
        }

        if (!string.Equals(code.ClientId, request.ClientId?.Trim(), StringComparison.Ordinal))
        {
            return TokenError(400, "invalid_grant", "授权码与客户端不匹配。");
        }

        if (code.ExpirationTime <= DateTimeOffset.UtcNow)
        {
            return TokenError(400, "invalid_grant", "授权码已过期。");
        }

        if (!string.Equals(code.RedirectUri, request.RedirectUri?.Trim(), StringComparison.Ordinal))
        {
            return TokenError(400, "invalid_grant", "redirect_uri 与授权时不一致。");
        }

        // 客户端认证：机密客户端必须校验密钥；公开客户端依赖 PKCE
        var auth = await AuthenticateClientAsync(request.ClientId, request.ClientSecret, requireConfidential: false, cancellationToken);
        if (auth.App is null)
        {
            return TokenError(auth.StatusCode, auth.Error!, auth.ErrorDescription);
        }

        var app = auth.App;
        if (!ClientSupportsGrant(app, GrantTypeAuthorizationCode))
        {
            return TokenError(400, "unauthorized_client", "客户端未启用授权码模式。");
        }

        // PKCE 校验
        if (!string.IsNullOrWhiteSpace(code.CodeChallenge))
        {
            var verifier = request.CodeVerifier?.Trim();
            if (string.IsNullOrWhiteSpace(verifier) || !VerifyPkce(code.CodeChallenge!, code.CodeChallengeMethod, verifier!))
            {
                return TokenError(400, "invalid_grant", "PKCE code_verifier 校验失败。");
            }
        }
        else if (IsPublicClient(app))
        {
            // 公开客户端但授权码无 PKCE 挑战：拒绝（防止公开客户端绕过 PKCE）
            return TokenError(400, "invalid_grant", "缺少 PKCE 校验信息。");
        }

        // 原子消费授权码（防并发重放）：赢得竞态者方可继续
        if (!await _oauthCodeRepository.TryConsumeAsync(code.BasicId, DateTimeOffset.UtcNow, cancellationToken))
        {
            _ = await _oauthTokenRepository.RevokeFamilyAsync(code.UserId, code.ClientId, DateTimeOffset.UtcNow, cancellationToken);
            return TokenError(400, "invalid_grant", "授权码已被使用。");
        }

        var user = await _userRepository.GetByIdIgnoreTenantAsync(code.UserId, cancellationToken);
        if (user is null || user.Status != EnableStatus.Enabled)
        {
            return TokenError(400, "invalid_grant", "授权用户不存在或已被禁用。");
        }

        var response = await IssueUserTokenAsync(
            app,
            user,
            code.TenantId,
            GrantType.AuthorizationCode,
            code.Scopes,
            issueRefresh: ClientSupportsGrant(app, GrantTypeRefreshToken),
            parentTokenId: null,
            cancellationToken,
            code.Nonce);
        return new OAuthTokenOutcome(true, 200, response, null, null);
    }

    private async Task<OAuthTokenOutcome> ExchangeRefreshTokenAsync(OAuthTokenRequest request, CancellationToken cancellationToken)
    {
        var refreshToken = request.RefreshToken?.Trim();
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return TokenError(400, "invalid_request", "缺少 refresh_token 参数。");
        }

        var auth = await AuthenticateClientAsync(request.ClientId, request.ClientSecret, requireConfidential: false, cancellationToken);
        if (auth.App is null)
        {
            return TokenError(auth.StatusCode, auth.Error!, auth.ErrorDescription);
        }

        var app = auth.App;
        var stored = await _oauthTokenRepository.GetByRefreshTokenIgnoreTenantAsync(refreshToken, cancellationToken);
        if (stored is null || stored.UserId is null or <= 0)
        {
            return TokenError(400, "invalid_grant", "刷新令牌无效。");
        }

        if (!string.Equals(stored.ClientId, app.ClientId, StringComparison.Ordinal))
        {
            return TokenError(400, "invalid_grant", "刷新令牌与客户端不匹配。");
        }

        // 已撤销的刷新令牌被再次使用：视为令牌被窃取/重放，吊销整个令牌族
        if (stored.IsRevoked)
        {
            _ = await _oauthTokenRepository.RevokeFamilyAsync(stored.UserId!.Value, stored.ClientId, DateTimeOffset.UtcNow, cancellationToken);
            _logger.LogWarning("OAuth: 刷新令牌重放检测 clientId={ClientId} userId={UserId}", stored.ClientId, stored.UserId);
            return TokenError(400, "invalid_grant", "刷新令牌已失效。");
        }

        if (stored.RefreshTokenExpirationTime is { } refreshExpiry && refreshExpiry <= DateTimeOffset.UtcNow)
        {
            return TokenError(400, "invalid_grant", "刷新令牌已过期。");
        }

        if (!ClientSupportsGrant(app, GrantTypeRefreshToken))
        {
            return TokenError(400, "unauthorized_client", "客户端未启用刷新令牌模式。");
        }

        var user = await _userRepository.GetByIdIgnoreTenantAsync(stored.UserId!.Value, cancellationToken);
        if (user is null || user.Status != EnableStatus.Enabled)
        {
            return TokenError(400, "invalid_grant", "授权用户不存在或已被禁用。");
        }

        // 令牌轮换：签发新令牌（父指向旧），再作废旧令牌并回填正向链
        var response = await IssueUserTokenAsync(
            app,
            user,
            stored.TenantId,
            GrantType.RefreshToken,
            stored.Scopes,
            issueRefresh: true,
            parentTokenId: stored.BasicId,
            cancellationToken);

        stored.IsRevoked = true;
        stored.RevokedTime = DateTimeOffset.UtcNow;
        stored.ReplacedByToken = Truncate(response.RefreshToken, 200);
        _ = await _oauthTokenRepository.UpdateAsync(stored, cancellationToken);

        return new OAuthTokenOutcome(true, 200, response, null, null);
    }

    private async Task<OAuthTokenOutcome> ExchangeClientCredentialsAsync(OAuthTokenRequest request, CancellationToken cancellationToken)
    {
        // 客户端凭证模式必须是机密客户端
        var auth = await AuthenticateClientAsync(request.ClientId, request.ClientSecret, requireConfidential: true, cancellationToken);
        if (auth.App is null)
        {
            return TokenError(auth.StatusCode, auth.Error!, auth.ErrorDescription);
        }

        var app = auth.App;
        if (!ClientSupportsGrant(app, GrantTypeClientCredentials))
        {
            return TokenError(400, "unauthorized_client", "客户端未启用客户端凭证模式。");
        }

        var scopes = JoinScopes(ResolveGrantedScopes(app, request.Scope));
        var response = await IssueClientTokenAsync(app, scopes, cancellationToken);
        return new OAuthTokenOutcome(true, 200, response, null, null);
    }

    #endregion 令牌端点

    #region 撤销端点

    /// <summary>
    /// 撤销端点：撤销刷新令牌（并按令牌族吊销）；未知令牌按 RFC 7009 静默成功
    /// </summary>
    public async Task RevokeTokenAsync(OAuthRevokeRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var token = request.Token?.Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        // 尝试作为刷新令牌撤销
        var stored = await _oauthTokenRepository.GetByRefreshTokenIgnoreTenantAsync(token, cancellationToken);

        // 未命中刷新令牌且形似 JWT：按访问令牌 JTI 撤销
        if (stored is null && token.Count(c => c == '.') == 2)
        {
            var jti = TryGetJwtId(token);
            if (!string.IsNullOrWhiteSpace(jti))
            {
                stored = await _oauthTokenRepository.GetByAccessTokenIgnoreTenantAsync(jti!, cancellationToken);
            }
        }

        // RFC 7009：未知令牌静默成功
        if (stored is null || stored.IsRevoked)
        {
            return;
        }

        // 若提供了客户端凭证，撤销的令牌必须属于该客户端（防止跨客户端撤销）
        var clientId = request.ClientId?.Trim();
        if (!string.IsNullOrWhiteSpace(clientId))
        {
            var auth = await AuthenticateClientAsync(clientId, request.ClientSecret, requireConfidential: false, cancellationToken);
            if (auth.App is null || !string.Equals(auth.App.ClientId, stored.ClientId, StringComparison.Ordinal))
            {
                return;
            }
        }

        stored.IsRevoked = true;
        stored.RevokedTime = DateTimeOffset.UtcNow;
        _ = await _oauthTokenRepository.UpdateAsync(stored, cancellationToken);
        _logger.LogInformation("OAuth: 令牌已撤销 clientId={ClientId} userId={UserId}", stored.ClientId, stored.UserId);
    }

    #endregion 撤销端点

    #region 令牌签发

    /// <summary>
    /// 在授权码/刷新令牌所属租户上下文内为用户签发访问令牌并落库（复用第一方 claim/快照构建）。
    /// </summary>
    private async Task<OAuthTokenResponse> IssueUserTokenAsync(
        SysOAuthApp app,
        SysUser user,
        long tenantId,
        GrantType grantType,
        string? scopes,
        bool issueRefresh,
        long? parentTokenId,
        CancellationToken cancellationToken,
        string? nonce = null)
    {
        var now = DateTimeOffset.UtcNow;
        var effectiveTenantId = tenantId > 0 ? tenantId : (long?)null;

        using var tenantScope = _currentTenant.Change(effectiveTenantId, effectiveTenantId?.ToString());

        var snapshot = await _authorizationSnapshotQueryService.BuildAsync(user.BasicId, now, cancellationToken);
        // OAuth 令牌不建 Web 会话；session_id 仅为 claim 占位（鉴权期"会话查不到→放行"，见 SaasPermissionChecker）
        var sessionBusinessId = Guid.NewGuid().ToString("N");
        var accessTokenJti = Guid.NewGuid().ToString("N");
        IReadOnlyCollection<string> tokenPermissions = snapshot.Permissions.Contains("*") ? ["*"] : [];

        var issue = _authTokenIssueService.IssueAccessToken(
            new AuthAccessTokenIssueCommand(
                user,
                effectiveTenantId,
                sessionBusinessId,
                accessTokenJti,
                snapshot.Roles,
                tokenPermissions,
                DeviceId: null));
        var jwt = issue.TokenResult;
        var refreshToken = issueRefresh ? jwt.RefreshToken : null;

        var entity = new SysOAuthToken
        {
            SessionId = null,
            AccessTokenJti = accessTokenJti,
            AccessToken = null,
            RefreshToken = refreshToken,
            ParentTokenId = parentTokenId,
            TokenType = jwt.TokenType,
            ClientId = app.ClientId,
            UserId = user.BasicId,
            GrantType = grantType,
            Scopes = scopes,
            Status = EnableStatus.Enabled,
            AccessTokenExpirationTime = ToDateTimeOffset(jwt.ExpiresAt),
            RefreshTokenExpirationTime = issueRefresh ? now.AddSeconds(Math.Max(1, app.RefreshTokenLifetime)) : null,
            IsRevoked = false
        };
        _ = await _oauthTokenRepository.AddAsync(entity, cancellationToken);

        var idToken = TryIssueIdToken(user, app.ClientId, scopes, jwt.AccessToken, nonce);

        return new OAuthTokenResponse(jwt.AccessToken, jwt.TokenType, jwt.ExpiresIn, refreshToken, scopes, idToken);
    }

    /// <summary>
    /// 已授予 openid 范围时签发 id_token，否则返回 null（退化为纯 OAuth2）。
    /// </summary>
    private string? TryIssueIdToken(SysUser user, string clientId, string? scopes, string accessToken, string? nonce)
    {
        if (!_oidcOptions.Value.IsEnabled)
        {
            return null;
        }

        var granted = SplitScopes(scopes);
        if (!granted.Contains(OidcConstants.Scopes.OpenId, StringComparer.Ordinal))
        {
            return null;
        }

        return _idTokenService.Issue(new IdTokenRequest(
            Subject: user.BasicId.ToString(CultureInfo.InvariantCulture),
            Audience: clientId,
            Nonce: nonce,
            AuthenticationTime: DateTimeOffset.UtcNow,
            AccessToken: accessToken,
            AdditionalClaims: BuildProfileClaims(user, granted)));
    }

    /// <summary>
    /// 按已授予 scope 解析用户资料声明。scope 未授予的字段一律不下发。
    /// </summary>
    public static IReadOnlyCollection<Claim> BuildProfileClaims(SysUser user, IReadOnlyCollection<string> grantedScopes)
    {
        var claims = new List<Claim>();

        if (grantedScopes.Contains(OidcConstants.Scopes.Profile, StringComparer.Ordinal))
        {
            claims.Add(new Claim(OidcConstants.Claims.PreferredUserName, user.UserName));
            if (!string.IsNullOrWhiteSpace(user.NickName))
            {
                claims.Add(new Claim(OidcConstants.Claims.Name, user.NickName));
            }
            if (!string.IsNullOrWhiteSpace(user.Avatar))
            {
                claims.Add(new Claim(OidcConstants.Claims.Picture, user.Avatar));
            }
        }

        // 不下发 email_verified / phone_number_verified：验证状态在 SysUserSecurity 上，
        // 令牌签发路径不加载该实体。二者是可选声明，宁可缺省也不臆断。
        if (grantedScopes.Contains(OidcConstants.Scopes.Email, StringComparer.Ordinal) && !string.IsNullOrWhiteSpace(user.Email))
        {
            claims.Add(new Claim(OidcConstants.Claims.Email, user.Email));
        }

        if (grantedScopes.Contains(OidcConstants.Scopes.Phone, StringComparer.Ordinal) && !string.IsNullOrWhiteSpace(user.Phone))
        {
            claims.Add(new Claim(OidcConstants.Claims.PhoneNumber, user.Phone));
        }

        return claims;
    }

    /// <summary>
    /// 客户端凭证模式签发：无用户主体，仅承载客户端身份与授权范围（不签发刷新令牌）。
    /// </summary>
    private async Task<OAuthTokenResponse> IssueClientTokenAsync(SysOAuthApp app, string? scopes, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var accessTokenJti = Guid.NewGuid().ToString("N");
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, app.ClientId),
            new(JwtRegisteredClaimNames.Jti, accessTokenJti),
            new("client_id", app.ClientId)
        };
        if (app.TenantId > 0)
        {
            claims.Add(new Claim(XiHanClaimTypes.TenantId, app.TenantId.ToString()));
        }

        if (!string.IsNullOrWhiteSpace(scopes))
        {
            claims.Add(new Claim("scope", scopes));
        }

        var jwt = _jwtTokenService.GenerateAccessToken(claims);

        var effectiveTenantId = app.TenantId > 0 ? app.TenantId : (long?)null;
        using (_currentTenant.Change(effectiveTenantId, effectiveTenantId?.ToString()))
        {
            var entity = new SysOAuthToken
            {
                SessionId = null,
                AccessTokenJti = accessTokenJti,
                AccessToken = null,
                RefreshToken = null,
                TokenType = jwt.TokenType,
                ClientId = app.ClientId,
                UserId = null,
                GrantType = GrantType.ClientCredentials,
                Scopes = scopes,
                Status = EnableStatus.Enabled,
                AccessTokenExpirationTime = ToDateTimeOffset(jwt.ExpiresAt),
                RefreshTokenExpirationTime = null,
                IsRevoked = false
            };
            _ = await _oauthTokenRepository.AddAsync(entity, cancellationToken);
        }

        return new OAuthTokenResponse(jwt.AccessToken, jwt.TokenType, jwt.ExpiresIn, null, scopes);
    }

    #endregion 令牌签发

    #region 客户端认证

    /// <summary>
    /// 客户端认证：加载客户端并按机密/公开类型校验密钥。
    /// </summary>
    /// <param name="clientId">客户端标识</param>
    /// <param name="clientSecret">客户端密钥（机密客户端必填）</param>
    /// <param name="requireConfidential">是否强制要求机密客户端（如 client_credentials）</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task<ClientAuthResult> AuthenticateClientAsync(string? clientId, string? clientSecret, bool requireConfidential, CancellationToken cancellationToken)
    {
        var id = clientId?.Trim();
        if (string.IsNullOrWhiteSpace(id))
        {
            return ClientAuthResult.Fail(401, "invalid_client", "缺少 client_id。");
        }

        var app = await _oauthAppRepository.GetByClientIdIgnoreTenantAsync(id, cancellationToken);
        if (app is null || app.Status != EnableStatus.Enabled)
        {
            return ClientAuthResult.Fail(401, "invalid_client", "客户端不存在或已停用。");
        }

        if (IsPublicClient(app))
        {
            // 公开客户端无密钥：当调用方要求机密客户端时拒绝
            return requireConfidential
                ? ClientAuthResult.Fail(401, "invalid_client", "该操作要求机密客户端。")
                : ClientAuthResult.Success(app);
        }

        // 机密客户端：必须提供且校验通过密钥
        if (string.IsNullOrWhiteSpace(clientSecret) || !_passwordHasher.VerifyPassword(app.ClientSecret, clientSecret))
        {
            return ClientAuthResult.Fail(401, "invalid_client", "客户端密钥校验失败。");
        }

        return ClientAuthResult.Success(app);
    }

    private sealed record ClientAuthResult(SysOAuthApp? App, int StatusCode, string? Error, string? ErrorDescription)
    {
        public static ClientAuthResult Success(SysOAuthApp app) => new(app, 200, null, null);

        public static ClientAuthResult Fail(int statusCode, string error, string description) => new(null, statusCode, error, description);
    }

    #endregion 客户端认证

    #region 辅助

    private static OAuthAuthorizeValidation Invalid(string error, string description)
    {
        return new OAuthAuthorizeValidation(false, error, description, false, null, []);
    }

    private static OAuthAuthorizeValidation InvalidWithRedirect(string error, string description, SysOAuthApp app)
    {
        return new OAuthAuthorizeValidation(false, error, description, true, ToClientInfo(app), []);
    }

    private static OAuthTokenOutcome TokenError(int statusCode, string error, string? description)
    {
        return new OAuthTokenOutcome(false, statusCode, null, error, description);
    }

    private static OAuthClientInfo ToClientInfo(SysOAuthApp app)
    {
        return new OAuthClientInfo(app.ClientId, app.AppName, app.AppDescription, app.Logo, app.Homepage, app.SkipConsent);
    }

    private static bool IsPublicClient(SysOAuthApp app)
    {
        return string.IsNullOrWhiteSpace(app.ClientSecret);
    }

    private static bool ClientSupportsGrant(SysOAuthApp app, string grantType)
    {
        return SplitCsv(app.GrantTypes).Any(item => string.Equals(item, grantType, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsRegisteredRedirectUri(SysOAuthApp app, string redirectUri)
    {
        // 回调地址按逗号/换行分隔，严格逐字匹配（大小写敏感，符合 OAuth2 安全实践）
        return SplitRedirectUris(app.RedirectUris).Any(item => string.Equals(item, redirectUri, StringComparison.Ordinal));
    }

    private static bool IsSupportedPkceMethod(string? method)
    {
        var normalized = method?.Trim();
        return string.IsNullOrWhiteSpace(normalized)
            || string.Equals(normalized, PkceMethodS256, StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, PkceMethodPlain, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePkceMethod(string? method)
    {
        var normalized = method?.Trim();
        return string.Equals(normalized, PkceMethodS256, StringComparison.OrdinalIgnoreCase) ? PkceMethodS256 : PkceMethodPlain;
    }

    private static bool VerifyPkce(string codeChallenge, string? method, string codeVerifier)
    {
        // 创建授权码时已把 method 规范化为 "S256" 或 "plain"（NormalizePkceMethod）
        if (string.Equals(method?.Trim(), PkceMethodS256, StringComparison.OrdinalIgnoreCase))
        {
            var hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
            var computed = Base64UrlEncode(hash);
            return FixedTimeStringEquals(computed, codeChallenge);
        }

        // plain（含缺省）：直接比较
        return FixedTimeStringEquals(codeVerifier, codeChallenge);
    }

    private static bool FixedTimeStringEquals(string left, string right)
    {
        var leftBytes = Encoding.ASCII.GetBytes(left);
        var rightBytes = Encoding.ASCII.GetBytes(right);
        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static IReadOnlyList<string> ResolveGrantedScopes(SysOAuthApp app, string? requestedScope)
    {
        var registered = SplitScopes(app.Scopes);
        var requested = SplitScopes(requestedScope);
        if (requested.Count == 0)
        {
            return registered;
        }

        // 收窄至已注册范围（绝不授予客户端未注册的 scope）
        var granted = requested
            .Where(scope => registered.Any(item => string.Equals(item, scope, StringComparison.OrdinalIgnoreCase)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return granted;
    }

    private static string? JoinScopes(IReadOnlyList<string> scopes)
    {
        return scopes.Count == 0 ? null : string.Join(' ', scopes);
    }

    private static List<string> SplitScopes(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? []
            : [.. value.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct(StringComparer.OrdinalIgnoreCase)];
    }

    private static List<string> SplitCsv(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? []
            : [.. value.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
    }

    private static List<string> SplitRedirectUris(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? []
            : [.. value.Split([',', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
    }

    private static string GenerateUrlSafeToken(int byteLength)
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(byteLength))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static string? TryGetJwtId(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token))
            {
                return null;
            }

            var jwt = handler.ReadJwtToken(token);
            return jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
        }
        catch
        {
            return null;
        }
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }

    private static string AppendQuery(string url, string key, string value)
    {
        var separator = url.Contains('?') ? '&' : '?';
        return $"{url}{separator}{key}={Uri.EscapeDataString(value)}";
    }

    private static DateTimeOffset ToDateTimeOffset(DateTime value)
    {
        return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }

    #endregion 辅助
}
