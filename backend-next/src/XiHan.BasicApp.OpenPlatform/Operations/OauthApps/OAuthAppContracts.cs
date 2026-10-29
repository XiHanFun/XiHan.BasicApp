// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.OpenPlatform.Domain.Entities;
using XiHan.BasicApp.OpenPlatform.Domain.Services;

namespace XiHan.BasicApp.OpenPlatform.Operations.OauthApps;

/// <summary>
/// 一个 OAuth 应用（密钥不回显）。
/// </summary>
/// <param name="Id">应用 ID。</param>
/// <param name="AppName">应用名称。</param>
/// <param name="AppDescription">描述。</param>
/// <param name="ClientId">ClientId。</param>
/// <param name="ClientType">客户端类型。</param>
/// <param name="AppType">应用类型。</param>
/// <param name="GrantTypes">授权类型。</param>
/// <param name="RedirectUris">回调地址。</param>
/// <param name="Scopes">授权范围。</param>
/// <param name="AccessTokenLifetime">访问令牌有效期（秒）。</param>
/// <param name="RefreshTokenLifetime">刷新令牌有效期（秒）。</param>
/// <param name="AuthorizationCodeLifetime">授权码有效期（秒）。</param>
/// <param name="Logo">Logo 地址。</param>
/// <param name="Homepage">主页。</param>
/// <param name="SkipConsent">跳过同意页。</param>
/// <param name="IsEnabled">是否启用。</param>
/// <param name="OwnerUserId">归属人（我的应用）；平台建的为空。</param>
/// <param name="OwnerUserName">归属人的登录名。</param>
/// <param name="Remark">备注。</param>
/// <param name="CreatedAt">创建时间。</param>
/// <param name="ModifiedAt">修改时间。</param>
public sealed record OAuthAppResponse(
    Guid Id,
    string AppName,
    string? AppDescription,
    string ClientId,
    OAuthClientType ClientType,
    OAuthAppType AppType,
    IReadOnlyList<string> GrantTypes,
    IReadOnlyList<string> RedirectUris,
    IReadOnlyList<string> Scopes,
    int AccessTokenLifetime,
    int RefreshTokenLifetime,
    int AuthorizationCodeLifetime,
    string? Logo,
    string? Homepage,
    bool SkipConsent,
    bool IsEnabled,
    Guid? OwnerUserId,
    string? OwnerUserName,
    string? Remark,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt)
{
    /// <summary>
    /// 从实体投影。
    /// </summary>
    public static OAuthAppResponse From(SysOAuthApp app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return new OAuthAppResponse(
            app.Id,
            app.AppName,
            app.AppDescription,
            app.ClientId,
            app.ClientType,
            app.AppType,
            OAuthAppService.Split(app.GrantTypes),
            OAuthAppService.Split(app.RedirectUris),
            OAuthAppService.Split(app.Scopes),
            app.AccessTokenLifetime,
            app.RefreshTokenLifetime,
            app.AuthorizationCodeLifetime,
            app.Logo,
            app.Homepage,
            app.SkipConsent,
            app.IsEnabled,
            app.OwnerUserId,
            app.OwnerUserName,
            app.Remark,
            app.CreatedAt,
            app.ModifiedAt);
    }
}

/// <summary>
/// 新建或重置密钥后的应用：机密客户端的密钥只在这一次返回，公开客户端为空。
/// </summary>
/// <param name="Id">应用 ID。</param>
/// <param name="ClientId">ClientId。</param>
/// <param name="ClientType">客户端类型。</param>
/// <param name="ClientSecret">密钥明文。</param>
public sealed record OAuthAppSecretResponse(Guid Id, string ClientId, OAuthClientType ClientType, string? ClientSecret);

/// <summary>
/// <c>oauth-apps.create</c> 的请求。
/// </summary>
/// <param name="AppName">应用名称。</param>
/// <param name="AppDescription">描述。</param>
/// <param name="ClientId">ClientId（3 到 100 位字母、数字、<c>. _ -</c>）；不填即 32 位十六进制。</param>
/// <param name="ClientType">客户端类型（新建后不能改）。</param>
/// <param name="AppType">应用类型。</param>
/// <param name="GrantTypes">授权类型（<c>authorization_code</c> 必选，可加 <c>refresh_token</c>）。</param>
/// <param name="RedirectUris">回调地址（1 到 10 条）。</param>
/// <param name="Scopes">授权范围（<c>openid</c> 必选，可加 <c>profile email phone</c>）。</param>
/// <param name="AccessTokenLifetime">访问令牌有效期（秒，60 到 86400）。</param>
/// <param name="RefreshTokenLifetime">刷新令牌有效期（秒，3600 到 31536000）。</param>
/// <param name="AuthorizationCodeLifetime">授权码有效期（秒，30 到 600）。</param>
/// <param name="Logo">Logo 地址（http / https）。</param>
/// <param name="Homepage">主页（http / https）。</param>
/// <param name="SkipConsent">跳过同意页。</param>
/// <param name="IsEnabled">是否启用。</param>
/// <param name="Remark">备注。</param>
public sealed record CreateOAuthAppRequest(
    [Required, StringLength(100, MinimumLength = 1)] string AppName,
    [StringLength(500)] string? AppDescription,
    [StringLength(100)] string? ClientId,
    OAuthClientType ClientType,
    OAuthAppType AppType,
    [Required, MaxLength(4)] IReadOnlyList<string> GrantTypes,
    [Required, MaxLength(20)] IReadOnlyList<string> RedirectUris,
    [Required, MaxLength(8)] IReadOnlyList<string> Scopes,
    [Range(60, 86400)] int AccessTokenLifetime,
    [Range(3600, 31536000)] int RefreshTokenLifetime,
    [Range(30, 600)] int AuthorizationCodeLifetime,
    [StringLength(500)] string? Logo,
    [StringLength(500)] string? Homepage,
    bool SkipConsent,
    bool IsEnabled,
    [StringLength(500)] string? Remark)
{
    /// <summary>
    /// 转成内容。
    /// </summary>
    public OAuthAppDraft ToDraft() => new(AppName, AppDescription, AppType, GrantTypes, RedirectUris, Scopes, AccessTokenLifetime, RefreshTokenLifetime, AuthorizationCodeLifetime, Logo, Homepage, SkipConsent, IsEnabled, Remark);
}

/// <summary>
/// <c>oauth-apps.update</c> 的请求（整体覆盖；ClientId 与客户端类型不能改）。
/// </summary>
/// <param name="Id">应用 ID。</param>
/// <param name="AppName">应用名称。</param>
/// <param name="AppDescription">描述。</param>
/// <param name="AppType">应用类型。</param>
/// <param name="GrantTypes">授权类型。</param>
/// <param name="RedirectUris">回调地址。</param>
/// <param name="Scopes">授权范围。</param>
/// <param name="AccessTokenLifetime">访问令牌有效期（秒）。</param>
/// <param name="RefreshTokenLifetime">刷新令牌有效期（秒）。</param>
/// <param name="AuthorizationCodeLifetime">授权码有效期（秒）。</param>
/// <param name="Logo">Logo 地址。</param>
/// <param name="Homepage">主页。</param>
/// <param name="SkipConsent">跳过同意页。</param>
/// <param name="IsEnabled">是否启用。</param>
/// <param name="Remark">备注。</param>
public sealed record UpdateOAuthAppRequest(
    Guid Id,
    [Required, StringLength(100, MinimumLength = 1)] string AppName,
    [StringLength(500)] string? AppDescription,
    OAuthAppType AppType,
    [Required, MaxLength(4)] IReadOnlyList<string> GrantTypes,
    [Required, MaxLength(20)] IReadOnlyList<string> RedirectUris,
    [Required, MaxLength(8)] IReadOnlyList<string> Scopes,
    [Range(60, 86400)] int AccessTokenLifetime,
    [Range(3600, 31536000)] int RefreshTokenLifetime,
    [Range(30, 600)] int AuthorizationCodeLifetime,
    [StringLength(500)] string? Logo,
    [StringLength(500)] string? Homepage,
    bool SkipConsent,
    bool IsEnabled,
    [StringLength(500)] string? Remark)
{
    /// <summary>
    /// 转成内容。
    /// </summary>
    public OAuthAppDraft ToDraft() => new(AppName, AppDescription, AppType, GrantTypes, RedirectUris, Scopes, AccessTokenLifetime, RefreshTokenLifetime, AuthorizationCodeLifetime, Logo, Homepage, SkipConsent, IsEnabled, Remark);
}

/// <summary>
/// 按 ID 取或重置一个应用的请求。
/// </summary>
/// <param name="Id">应用 ID。</param>
public sealed record OAuthAppIdRequest(Guid Id);

/// <summary>
/// 批量删除的请求。
/// </summary>
/// <param name="Ids">应用 ID，1 到 100 个。</param>
public sealed record OAuthAppIdsRequest([Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids);
