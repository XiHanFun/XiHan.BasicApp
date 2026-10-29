// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.OpenPlatform.Domain.Entities;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.OpenPlatform.Domain.Services;

/// <summary>
/// OAuth 应用的内容（新建与修改共用；ClientId 与客户端类型只在新建时定）。
/// </summary>
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
public sealed record OAuthAppDraft(
    string AppName,
    string? AppDescription,
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
    string? Remark);

/// <summary>
/// OAuth 应用的规则：授权类型只有授权码与刷新令牌（令牌只用于身份，没有用户的客户端凭证模式用不上，4.x 有）、授权范围只有身份类的 <c>openid profile email phone</c>（且须带 <c>openid</c>）；
/// 回调地址须是绝对地址、不带片段，http 只认本机回环（原生应用的自定义协议照收，RFC 8252）；ClientId 全局唯一（新建不填即 32 位十六进制）；机密客户端的密钥是 64 位十六进制、只返回一次、只存 SHA-256。
/// </summary>
/// <param name="apps">应用仓储。</param>
/// <param name="ids">标识生成器。</param>
public sealed partial class OAuthAppService(IRepository<SysOAuthApp, Guid> apps, IIdGenerator<Guid> ids)
{
    /// <summary>
    /// 授权码模式。
    /// </summary>
    public const string AuthorizationCode = "authorization_code";

    /// <summary>
    /// 刷新令牌模式。
    /// </summary>
    public const string RefreshToken = "refresh_token";

    /// <summary>
    /// 回调地址最多条数。
    /// </summary>
    public const int MaxRedirectUris = 10;

    /// <summary>
    /// 可用的授权类型。
    /// </summary>
    public static IReadOnlyList<string> SupportedGrantTypes { get; } = [AuthorizationCode, RefreshToken];

    /// <summary>
    /// 可用的授权范围（身份类）。
    /// </summary>
    public static IReadOnlyList<string> SupportedScopes { get; } = ["openid", "profile", "email", "phone"];

    /// <summary>
    /// 我的应用的固定授权类型（4.x 同）。
    /// </summary>
    public static IReadOnlyList<string> PersonalGrantTypes { get; } = [AuthorizationCode, RefreshToken];

    /// <summary>
    /// 我的应用的固定授权范围（4.x 是 <c>basicapp</c>，6.0 的令牌只用于身份）。
    /// </summary>
    public static IReadOnlyList<string> PersonalScopes { get; } = ["openid", "profile", "email"];

    /// <summary>
    /// 取一个应用；不存在即 <c>OPENPLATFORM_NOTFOUND_001</c>。
    /// </summary>
    /// <param name="id">应用 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>应用。</returns>
    public async Task<Result<SysOAuthApp>> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        var found = await apps.FindAsync(id, cancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var app) ? app : NotFound(id);
    }

    /// <summary>
    /// 取本人的一个应用；别人的与平台的都当作不存在。
    /// </summary>
    /// <param name="userId">用户。</param>
    /// <param name="id">应用 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>应用。</returns>
    public async Task<Result<SysOAuthApp>> FindOwnedAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var found = await apps.FindAsync(id, cancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var app) && app.OwnerUserId == userId ? app : NotFound(id);
    }

    /// <summary>
    /// 本人的全部应用（按创建先后）。
    /// </summary>
    /// <param name="userId">用户。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>应用。</returns>
    public async Task<IReadOnlyList<SysOAuthApp>> ListOwnedAsync(Guid userId, CancellationToken cancellationToken)
    {
        var rows = await apps.ListAsync(EntityQuery.All<SysOAuthApp>().Where(new Specification<SysOAuthApp>(a => a.OwnerUserId == userId)), cancellationToken).ConfigureAwait(false);
        return [.. rows.OrderBy(static a => a.CreatedAt).ThenBy(static a => a.ClientId, StringComparer.Ordinal)];
    }

    /// <summary>
    /// 新建应用：ClientId 不填即生成；机密客户端返回密钥明文（只此一次）。
    /// </summary>
    /// <param name="clientId">ClientId；为空即生成。</param>
    /// <param name="clientType">客户端类型。</param>
    /// <param name="draft">内容。</param>
    /// <param name="ownerUserId">归属人；平台建的为空。</param>
    /// <param name="ownerUserName">归属人的登录名。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>应用与密钥明文（公开客户端为空）。</returns>
    public async Task<Result<(SysOAuthApp App, string? Secret)>> CreateAsync(string? clientId, OAuthClientType clientType, OAuthAppDraft draft, Guid? ownerUserId, string? ownerUserName, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(draft);
        var id = string.IsNullOrWhiteSpace(clientId) ? Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16)) : clientId.Trim();
        if (!ClientIdPattern().IsMatch(id))
        {
            return Error.Validation(OpenPlatformErrors.ClientIdInvalid, OpenPlatformMessages.ClientIdInvalid, MessageArguments.Of(("value", id)));
        }

        if (await apps.CountAsync(EntityQuery.All<SysOAuthApp>().Where(new Specification<SysOAuthApp>(a => a.ClientId == id)), cancellationToken).ConfigureAwait(false) > 0)
        {
            return Error.Conflict(OpenPlatformErrors.ClientIdTaken, OpenPlatformMessages.ClientIdTaken, MessageArguments.Of(("clientId", id)));
        }

        var app = new SysOAuthApp { Id = ids.NewId(), ClientId = id, ClientType = clientType, OwnerUserId = ownerUserId, OwnerUserName = ownerUserName };
        var applied = Apply(app, draft);
        if (applied.IsFailure)
        {
            return applied.Error!;
        }

        string? secret = null;
        if (clientType == OAuthClientType.Confidential)
        {
            secret = NewSecret();
            app.ClientSecretHash = HashSecret(secret);
        }

        var added = await apps.AddAsync(app, cancellationToken).ConfigureAwait(false);
        return added.IsFailure ? added.Error! : (app, secret);
    }

    /// <summary>
    /// 修改（整体覆盖；ClientId、客户端类型与密钥不变）。
    /// </summary>
    /// <param name="app">应用。</param>
    /// <param name="draft">内容。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>结果。</returns>
    public async Task<Result> UpdateAsync(SysOAuthApp app, OAuthAppDraft draft, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(app);
        var applied = Apply(app, draft);
        return applied.IsFailure ? applied : await apps.UpdateAsync(app, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 重置密钥：旧密钥立即失效；公开客户端没有密钥即 <c>OPENPLATFORM_CONFLICT_003</c>（4.x 会把公开客户端重置成机密客户端）。
    /// </summary>
    /// <param name="app">应用。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>新密钥明文。</returns>
    public async Task<Result<string>> ResetSecretAsync(SysOAuthApp app, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(app);
        if (app.ClientType == OAuthClientType.Public)
        {
            return Error.Conflict(OpenPlatformErrors.PublicClientHasNoSecret, OpenPlatformMessages.PublicClientHasNoSecret);
        }

        var secret = NewSecret();
        app.ClientSecretHash = HashSecret(secret);
        var updated = await apps.UpdateAsync(app, cancellationToken).ConfigureAwait(false);
        return updated.IsFailure ? updated.Error! : secret;
    }

    /// <summary>
    /// 批量删除（软删除，发出的令牌随应用失效）；<paramref name="ownerUserId"/> 不为空时只删本人的，别人的逐项 <c>OPENPLATFORM_NOTFOUND_001</c>。
    /// </summary>
    /// <param name="appIds">应用 ID。</param>
    /// <param name="ownerUserId">只删这个人的；平台管理为空。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>批量结果。</returns>
    public async Task<BatchResult> DeleteAsync(IReadOnlyList<Guid> appIds, Guid? ownerUserId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(appIds);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in appIds.Distinct())
        {
            var found = ownerUserId is { } owner ? await FindOwnedAsync(owner, id, cancellationToken).ConfigureAwait(false) : await FindAsync(id, cancellationToken).ConfigureAwait(false);
            if (!found.TryGetValue(out var app))
            {
                failed.Add(new BatchFailure(id, found.Error!.Code));
            }
            else if ((await apps.RemoveAsync(app, cancellationToken).ConfigureAwait(false)) is { IsFailure: true } removed)
            {
                failed.Add(new BatchFailure(id, removed.Error!.Code));
            }
            else
            {
                succeeded.Add(id);
            }
        }

        return new BatchResult(succeeded, failed);
    }

    /// <summary>
    /// 密钥的 SHA-256（小写十六进制）。
    /// </summary>
    /// <param name="secret">密钥明文。</param>
    /// <returns>散列。</returns>
    public static string HashSecret(string secret) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));

    /// <summary>
    /// 拆分存储的列表（空白分隔）。
    /// </summary>
    /// <param name="value">原文。</param>
    /// <returns>各项。</returns>
    public static IReadOnlyList<string> Split(string? value)
        => string.IsNullOrWhiteSpace(value) ? [] : value.Split([' ', '\n', '\r', '\t', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static Result Apply(SysOAuthApp app, OAuthAppDraft draft)
    {
        var grants = Normalize(draft.GrantTypes, ignoreCase: true);
        if (grants.FirstOrDefault(g => !SupportedGrantTypes.Contains(g, StringComparer.Ordinal)) is { } unsupportedGrant)
        {
            return Error.Validation(OpenPlatformErrors.GrantTypeUnsupported, OpenPlatformMessages.GrantTypeUnsupported, MessageArguments.Of(("value", unsupportedGrant)));
        }

        if (!grants.Contains(AuthorizationCode, StringComparer.Ordinal))
        {
            return Error.Validation(OpenPlatformErrors.GrantTypeUnsupported, OpenPlatformMessages.GrantTypeUnsupported, MessageArguments.Of(("value", string.Join(' ', grants))));
        }

        var scopes = Normalize(draft.Scopes, ignoreCase: true);
        if (scopes.FirstOrDefault(s => !SupportedScopes.Contains(s, StringComparer.Ordinal)) is { } unsupportedScope)
        {
            return Error.Validation(OpenPlatformErrors.ScopeUnsupported, OpenPlatformMessages.ScopeUnsupported, MessageArguments.Of(("value", unsupportedScope)));
        }

        if (!scopes.Contains("openid", StringComparer.Ordinal))
        {
            return Error.Validation(OpenPlatformErrors.ScopeUnsupported, OpenPlatformMessages.ScopeUnsupported, MessageArguments.Of(("value", string.Join(' ', scopes))));
        }

        var redirects = Normalize(draft.RedirectUris, ignoreCase: false);
        if (redirects.Count == 0 || redirects.Count > MaxRedirectUris)
        {
            return Error.Validation(OpenPlatformErrors.RedirectUriInvalid, OpenPlatformMessages.RedirectUriInvalid, MessageArguments.Of(("value", redirects.Count == 0 ? string.Empty : redirects[MaxRedirectUris])));
        }

        if (redirects.FirstOrDefault(static r => !IsRedirectUri(r)) is { } invalidRedirect)
        {
            return Error.Validation(OpenPlatformErrors.RedirectUriInvalid, OpenPlatformMessages.RedirectUriInvalid, MessageArguments.Of(("value", invalidRedirect)));
        }

        if (!IsWebUrl(draft.Homepage) || !IsWebUrl(draft.Logo))
        {
            return Error.Validation(OpenPlatformErrors.UrlInvalid, OpenPlatformMessages.UrlInvalid, MessageArguments.Of(("value", IsWebUrl(draft.Homepage) ? draft.Logo : draft.Homepage)));
        }

        app.AppName = draft.AppName.Trim();
        app.AppDescription = Blank(draft.AppDescription);
        app.AppType = draft.AppType;
        app.GrantTypes = string.Join(' ', grants);
        app.RedirectUris = string.Join('\n', redirects);
        app.Scopes = string.Join(' ', scopes);
        app.AccessTokenLifetime = draft.AccessTokenLifetime;
        app.RefreshTokenLifetime = draft.RefreshTokenLifetime;
        app.AuthorizationCodeLifetime = draft.AuthorizationCodeLifetime;
        app.Logo = Blank(draft.Logo);
        app.Homepage = Blank(draft.Homepage);
        app.SkipConsent = draft.SkipConsent;
        app.IsEnabled = draft.IsEnabled;
        app.Remark = Blank(draft.Remark);
        return Result.Success();
    }

    private static List<string> Normalize(IReadOnlyList<string>? values, bool ignoreCase)
    {
        var result = new List<string>();
        foreach (var raw in values ?? [])
        {
            foreach (var item in Split(raw))
            {
                var value = ignoreCase ? item.ToLowerInvariant() : item;
                if (!result.Contains(value, StringComparer.Ordinal))
                {
                    result.Add(value);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 回调地址：绝对地址、不带片段与凭据；http 只认本机回环，<c>javascript</c> / <c>data</c> / <c>file</c> 不收，其余协议（含原生应用的自定义协议）照收。
    /// </summary>
    private static bool IsRedirectUri(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Fragment.Length > 0 || uri.UserInfo.Length > 0 || value.Length > 500)
        {
            return false;
        }

        return uri.Scheme.ToLowerInvariant() switch
        {
            "https" => true,
            "http" => uri.IsLoopback,
            "javascript" or "data" or "file" or "vbscript" => false,
            _ => true,
        };
    }

    private static bool IsWebUrl(string? value)
        => string.IsNullOrWhiteSpace(value)
            || (Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp));

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NewSecret() => Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));

    private static Error NotFound(Guid id) => Error.NotFound(OpenPlatformErrors.OAuthAppNotFound, OpenPlatformMessages.OAuthAppNotFound, MessageArguments.Of(("id", id)));

    [GeneratedRegex("^[A-Za-z0-9._-]{3,100}$")]
    private static partial Regex ClientIdPattern();
}
