#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IOAuthServerService
// Guid:1d5f2a70-8c41-4e93-b2a6-0f7c1b9d4e21
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// OAuth2 授权服务端协议服务（授权码 + PKCE + 令牌签发/刷新/撤销）
/// </summary>
/// <remarks>
/// 定位：承载授权服务端协议逻辑的<b>普通 Scoped 服务</b>（非 <c>[DynamicApi]</c>、不被 Castle 代理），
/// 因此既可被同意页 AppService（SPA + Bearer + JSON）正常调用，也可被匿名 <c>/connect/token</c>
/// （表单编码、标准路径）的 Minimal-API 端点<b>直接注入调用</b>而不触发"匿名端点跨代理接口边界挂起"。
/// 无租户上下文场景（匿名令牌端点）下的库读一律走仓储的"跨租户"方法（Code/ClientId/RefreshToken 全局唯一）；
/// 令牌在授权码所属租户上下文内签发，与第一方登录 <c>IssueLoginTokenAsync</c> 的快照/声明构建一致。
/// </remarks>
public interface IOAuthServerService
{
    /// <summary>
    /// 校验授权请求（供同意页预览：返回客户端展示信息与解析后的授权范围）
    /// </summary>
    Task<OAuthAuthorizeValidation> ValidateAuthorizeRequestAsync(OAuthAuthorizeRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 用户同意后创建授权码，返回携带 code/state 的最终跳转地址
    /// </summary>
    Task<OAuthCreateCodeResult> CreateAuthorizationCodeAsync(OAuthCreateCodeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 令牌端点：按 grant_type 分发（authorization_code / refresh_token / client_credentials）
    /// </summary>
    Task<OAuthTokenOutcome> ExchangeTokenAsync(OAuthTokenRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销端点：撤销刷新令牌（并按令牌族吊销）；未知令牌按 RFC 7009 静默成功
    /// </summary>
    Task RevokeTokenAsync(OAuthRevokeRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// 授权请求（/connect/authorize 与同意流共用）
/// </summary>
public sealed record OAuthAuthorizeRequest(
    string? ResponseType,
    string? ClientId,
    string? RedirectUri,
    string? Scope,
    string? State,
    string? CodeChallenge,
    string? CodeChallengeMethod);

/// <summary>
/// 授权请求校验结果（供同意页预览与创建授权码前置校验）
/// </summary>
/// <param name="Success">是否通过校验</param>
/// <param name="Error">OAuth 错误码（invalid_request/unauthorized_client/...）</param>
/// <param name="ErrorDescription">错误描述</param>
/// <param name="RedirectUriValid">redirect_uri 是否为已注册合法回调（false 时严禁向其跳转错误）</param>
/// <param name="Client">客户端展示信息（校验通过时）</param>
/// <param name="Scopes">解析后授权范围（收窄至客户端已注册范围）</param>
public sealed record OAuthAuthorizeValidation(
    bool Success,
    string? Error,
    string? ErrorDescription,
    bool RedirectUriValid,
    OAuthClientInfo? Client,
    IReadOnlyList<string> Scopes);

/// <summary>
/// OAuth 客户端展示信息（同意页渲染用）
/// </summary>
public sealed record OAuthClientInfo(
    string ClientId,
    string AppName,
    string? AppDescription,
    string? Logo,
    string? Homepage,
    bool SkipConsent);

/// <summary>
/// 创建授权码命令（用户已同意）
/// </summary>
public sealed record OAuthCreateCodeCommand(
    OAuthAuthorizeRequest Request,
    long UserId,
    long? UserTenantId);

/// <summary>
/// 创建授权码结果
/// </summary>
public sealed record OAuthCreateCodeResult(
    bool Success,
    string? Error,
    string? ErrorDescription,
    string? RedirectUri);

/// <summary>
/// 令牌请求（/connect/token；ClientId/ClientSecret 已从 Basic 头或表单解析）
/// </summary>
public sealed record OAuthTokenRequest(
    string? GrantType,
    string? Code,
    string? RedirectUri,
    string? CodeVerifier,
    string? RefreshToken,
    string? Scope,
    string? ClientId,
    string? ClientSecret);

/// <summary>
/// 令牌端点结果（成功返回令牌，失败返回 OAuth 错误码与建议 HTTP 状态码）
/// </summary>
public sealed record OAuthTokenOutcome(
    bool Success,
    int StatusCode,
    OAuthTokenResponse? Response,
    string? Error,
    string? ErrorDescription);

/// <summary>
/// 令牌端点成功响应体（OAuth2 标准字段）
/// </summary>
public sealed record OAuthTokenResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    string? RefreshToken,
    string? Scope);

/// <summary>
/// 撤销请求（RFC 7009）
/// </summary>
public sealed record OAuthRevokeRequest(
    string? Token,
    string? TokenTypeHint,
    string? ClientId,
    string? ClientSecret);
