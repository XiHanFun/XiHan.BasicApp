#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OAuthAppDomainService
// Guid:7cd5cf73-4475-44bc-a81a-9c3247343b58
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Security.Cryptography;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Security.Password;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// OAuth 应用领域服务实现
/// </summary>
public sealed class OAuthAppDomainService
    : IOAuthAppDomainService
{
    private readonly IOAuthAppRepository _oauthAppRepository;

    private readonly IOAuthCodeRepository _oauthCodeRepository;

    private readonly IOAuthTokenRepository _oauthTokenRepository;

    private readonly IPasswordHasher _passwordHasher;

    /// <summary>
    /// 构造函数
    /// </summary>
    public OAuthAppDomainService(
        IOAuthAppRepository oauthAppRepository,
        IOAuthCodeRepository oauthCodeRepository,
        IOAuthTokenRepository oauthTokenRepository,
        IPasswordHasher passwordHasher)
    {
        _oauthAppRepository = oauthAppRepository;
        _oauthCodeRepository = oauthCodeRepository;
        _oauthTokenRepository = oauthTokenRepository;
        _passwordHasher = passwordHasher;
    }

    /// <inheritdoc />
    public async Task<OAuthAppCommandResult> CreateOAuthAppAsync(OAuthAppCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateOAuthAppInput(command.AppName, command.ClientId, command.AppType, command.GrantTypes, command.RedirectUris, command.Scopes, command.AccessTokenLifetime, command.RefreshTokenLifetime, command.AuthorizationCodeLifetime, command.Logo, command.Homepage, command.Remark);
        EnsureEnum(command.Status, nameof(command.Status));

        var clientId = Required(command.ClientId, 100, nameof(command.ClientId), "客户端主键不能超过 100 个字符。");
        EnsureCodeHasNoWhitespace(clientId, "客户端主键不能包含空白字符。");
        if (await _oauthAppRepository.AnyAsync(app => app.ClientId == clientId, cancellationToken))
        {
            throw new InvalidOperationException("客户端主键已存在。");
        }

        // 公开客户端（SPA/移动端，IsPublic=true）：不落密钥（存空串），依赖 PKCE；机密客户端生成/哈希密钥。
        // 明文密钥仅在机密客户端创建时返回一次（公开客户端返回 null）。
        string? plaintextSecret = null;
        var storedSecret = string.Empty;
        if (!command.IsPublic)
        {
            plaintextSecret = string.IsNullOrWhiteSpace(command.ClientSecret)
                ? GenerateSecret()
                : Required(command.ClientSecret, 200, nameof(command.ClientSecret), "客户端密钥不能超过 200 个字符。");
            storedSecret = _passwordHasher.HashPassword(plaintextSecret);
        }

        var app = new SysOAuthApp
        {
            AppName = Required(command.AppName, 100, nameof(command.AppName), "应用名称不能超过 100 个字符。"),
            AppDescription = Optional(command.AppDescription, 500, nameof(command.AppDescription), "应用描述不能超过 500 个字符。"),
            ClientId = clientId,
            ClientSecret = storedSecret,
            AppType = command.AppType,
            GrantTypes = Required(command.GrantTypes, 500, nameof(command.GrantTypes), "授权类型不能超过 500 个字符。"),
            RedirectUris = Optional(command.RedirectUris, 2000, nameof(command.RedirectUris), "回调地址不能超过 2000 个字符。"),
            Scopes = Optional(command.Scopes, 1000, nameof(command.Scopes), "授权范围不能超过 1000 个字符。"),
            AccessTokenLifetime = command.AccessTokenLifetime,
            RefreshTokenLifetime = command.RefreshTokenLifetime,
            AuthorizationCodeLifetime = command.AuthorizationCodeLifetime,
            Logo = Optional(command.Logo, 500, nameof(command.Logo), "应用图标不能超过 500 个字符。"),
            Homepage = Optional(command.Homepage, 500, nameof(command.Homepage), "应用主页不能超过 500 个字符。"),
            SkipConsent = command.SkipConsent,
            Status = command.Status,
            Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。")
        };

        return new OAuthAppCommandResult(await _oauthAppRepository.AddAsync(app, cancellationToken), plaintextSecret);
    }

    /// <inheritdoc />
    public async Task DeleteOAuthAppAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var app = await GetOAuthAppOrThrowAsync(id, cancellationToken);
        if (await _oauthCodeRepository.AnyAsync(code => code.ClientId == app.ClientId, cancellationToken))
        {
            throw new InvalidOperationException("OAuth 应用存在授权码记录，不能删除。");
        }

        if (await _oauthTokenRepository.AnyAsync(token => token.ClientId == app.ClientId, cancellationToken))
        {
            throw new InvalidOperationException("OAuth 应用存在 Token 记录，不能删除。");
        }

        if (!await _oauthAppRepository.DeleteAsync(app, cancellationToken))
        {
            throw new InvalidOperationException("OAuth 应用删除失败。");
        }
    }

    /// <inheritdoc />
    public async Task<OAuthAppCommandResult> RegenerateOAuthAppSecretAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var app = await GetOAuthAppOrThrowAsync(id, cancellationToken);
        var clientSecret = GenerateSecret();
        app.ClientSecret = _passwordHasher.HashPassword(clientSecret);
        return new OAuthAppCommandResult(await _oauthAppRepository.UpdateAsync(app, cancellationToken), clientSecret);
    }

    /// <inheritdoc />
    public async Task<OAuthAppCommandResult> UpdateOAuthAppAsync(OAuthAppUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "OAuth 应用主键必须大于 0。");
        ValidateOAuthAppInput(command.AppName, clientId: "skip", command.AppType, command.GrantTypes, command.RedirectUris, command.Scopes, command.AccessTokenLifetime, command.RefreshTokenLifetime, command.AuthorizationCodeLifetime, command.Logo, command.Homepage, command.Remark);
        var app = await GetOAuthAppOrThrowAsync(command.BasicId, cancellationToken);
        app.AppName = Required(command.AppName, 100, nameof(command.AppName), "应用名称不能超过 100 个字符。");
        app.AppDescription = Optional(command.AppDescription, 500, nameof(command.AppDescription), "应用描述不能超过 500 个字符。");
        app.AppType = command.AppType;
        app.GrantTypes = Required(command.GrantTypes, 500, nameof(command.GrantTypes), "授权类型不能超过 500 个字符。");
        app.RedirectUris = Optional(command.RedirectUris, 2000, nameof(command.RedirectUris), "回调地址不能超过 2000 个字符。");
        app.Scopes = Optional(command.Scopes, 1000, nameof(command.Scopes), "授权范围不能超过 1000 个字符。");
        app.AccessTokenLifetime = command.AccessTokenLifetime;
        app.RefreshTokenLifetime = command.RefreshTokenLifetime;
        app.AuthorizationCodeLifetime = command.AuthorizationCodeLifetime;
        app.Logo = Optional(command.Logo, 500, nameof(command.Logo), "应用图标不能超过 500 个字符。");
        app.Homepage = Optional(command.Homepage, 500, nameof(command.Homepage), "应用主页不能超过 500 个字符。");
        app.SkipConsent = command.SkipConsent;
        app.Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。");

        return new OAuthAppCommandResult(await _oauthAppRepository.UpdateAsync(app, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<OAuthAppCommandResult> UpdateOAuthAppStatusAsync(OAuthAppStatusChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "OAuth 应用主键必须大于 0。");
        EnsureEnum(command.Status, nameof(command.Status));
        var app = await GetOAuthAppOrThrowAsync(command.BasicId, cancellationToken);
        app.Status = command.Status;
        app.Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。") ?? app.Remark;

        return new OAuthAppCommandResult(await _oauthAppRepository.UpdateAsync(app, cancellationToken));
    }

    private static void EnsureCodeHasNoWhitespace(string value, string message)
    {
        if (value.Any(char.IsWhiteSpace))
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void EnsurePositive(int value, string paramName, string message)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    private static string GenerateSecret()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    }

    private static void ValidateOAuthAppInput(
        string appName,
        string clientId,
        OAuthAppType appType,
        string grantTypes,
        string? redirectUris,
        string? scopes,
        int accessTokenLifetime,
        int refreshTokenLifetime,
        int authorizationCodeLifetime,
        string? logo,
        string? homepage,
        string? remark)
    {
        _ = Required(appName, 100, nameof(appName), "应用名称不能超过 100 个字符。");
        _ = Required(clientId, 100, nameof(clientId), "客户端主键不能超过 100 个字符。");
        EnsureEnum(appType, nameof(appType));
        _ = Required(grantTypes, 500, nameof(grantTypes), "授权类型不能超过 500 个字符。");
        _ = Optional(redirectUris, 2000, nameof(redirectUris), "回调地址不能超过 2000 个字符。");
        _ = Optional(scopes, 1000, nameof(scopes), "授权范围不能超过 1000 个字符。");
        _ = Optional(logo, 500, nameof(logo), "应用图标不能超过 500 个字符。");
        _ = Optional(homepage, 500, nameof(homepage), "应用主页不能超过 500 个字符。");
        _ = Optional(remark, 500, nameof(remark), "备注不能超过 500 个字符。");
        EnsurePositive(accessTokenLifetime, nameof(accessTokenLifetime), "访问令牌有效期必须大于 0。");
        EnsurePositive(refreshTokenLifetime, nameof(refreshTokenLifetime), "刷新令牌有效期必须大于 0。");
        EnsurePositive(authorizationCodeLifetime, nameof(authorizationCodeLifetime), "授权码有效期必须大于 0。");
    }

    private static void EnsureEnum<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "枚举值无效。");
        }
    }

    private static void EnsureId(long id, string message)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), message);
        }
    }

    private static string? Optional(string? value, int maxLength, string paramName, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }

        return normalized;
    }

    private static string Required(string? value, int maxLength, string paramName, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }

        return normalized;
    }

    private async Task<SysOAuthApp> GetOAuthAppOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        EnsureId(id, "OAuth 应用主键必须大于 0。");
        return await _oauthAppRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("OAuth 应用不存在。");
    }
}
