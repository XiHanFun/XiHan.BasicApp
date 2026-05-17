#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OAuthAppAppService
// Guid:f7d26b52-dfde-4cb5-b358-54d76f64f12c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;
using static XiHan.BasicApp.Saas.Application.AppServices.SaasCommandValidation;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// OAuth 应用命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "OAuth 应用")]
public sealed class OAuthAppAppService
    : SaasApplicationService, IOAuthAppAppService
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public OAuthAppAppService(
        IOAuthAppRepository oauthAppRepository,
        IOAuthCodeRepository oauthCodeRepository,
        IOAuthTokenRepository oauthTokenRepository)
    {
        _oauthAppRepository = oauthAppRepository;
        _oauthCodeRepository = oauthCodeRepository;
        _oauthTokenRepository = oauthTokenRepository;
    }

    private readonly IOAuthAppRepository _oauthAppRepository;
    private readonly IOAuthCodeRepository _oauthCodeRepository;
    private readonly IOAuthTokenRepository _oauthTokenRepository;

    /// <summary>
    /// 创建 OAuth 应用
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.OAuthApp.Create)]
    public async Task<OAuthAppSecretDto> CreateOAuthAppAsync(OAuthAppCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateOAuthAppInput(input.AppName, input.ClientId, input.AppType, input.GrantTypes, input.RedirectUris, input.Scopes, input.AccessTokenLifetime, input.RefreshTokenLifetime, input.AuthorizationCodeLifetime, input.Logo, input.Homepage, input.Remark);
        var clientId = Required(input.ClientId, 100, nameof(input.ClientId), "客户端主键不能超过 100 个字符。");
        EnsureCodeHasNoWhitespace(clientId, "客户端主键不能包含空白字符。");
        if (await _oauthAppRepository.AnyAsync(app => app.ClientId == clientId, cancellationToken))
        {
            throw new InvalidOperationException("客户端主键已存在。");
        }

        var clientSecret = string.IsNullOrWhiteSpace(input.ClientSecret)
            ? GenerateSecret()
            : Required(input.ClientSecret, 200, nameof(input.ClientSecret), "客户端密钥不能超过 200 个字符。");
        var app = new SysOAuthApp
        {
            AppName = Required(input.AppName, 100, nameof(input.AppName), "应用名称不能超过 100 个字符。"),
            AppDescription = Optional(input.AppDescription, 500, nameof(input.AppDescription), "应用描述不能超过 500 个字符。"),
            ClientId = clientId,
            ClientSecret = clientSecret,
            AppType = input.AppType,
            GrantTypes = Required(input.GrantTypes, 500, nameof(input.GrantTypes), "授权类型不能超过 500 个字符。"),
            RedirectUris = Optional(input.RedirectUris, 2000, nameof(input.RedirectUris), "回调地址不能超过 2000 个字符。"),
            Scopes = Optional(input.Scopes, 1000, nameof(input.Scopes), "授权范围不能超过 1000 个字符。"),
            AccessTokenLifetime = input.AccessTokenLifetime,
            RefreshTokenLifetime = input.RefreshTokenLifetime,
            AuthorizationCodeLifetime = input.AuthorizationCodeLifetime,
            Logo = Optional(input.Logo, 500, nameof(input.Logo), "应用图标不能超过 500 个字符。"),
            Homepage = Optional(input.Homepage, 500, nameof(input.Homepage), "应用主页不能超过 500 个字符。"),
            SkipConsent = input.SkipConsent,
            Status = input.Status,
            Remark = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。")
        };
        EnsureEnum(input.Status, nameof(input.Status));

        var savedApp = await _oauthAppRepository.AddAsync(app, cancellationToken);
        return ToSecretDto(savedApp);
    }

    /// <summary>
    /// 更新 OAuth 应用
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.OAuthApp.Update)]
    public async Task<OAuthAppDetailDto> UpdateOAuthAppAsync(OAuthAppUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(input.BasicId, "OAuth 应用主键必须大于 0。");
        ValidateOAuthAppInput(input.AppName, clientId: "skip", input.AppType, input.GrantTypes, input.RedirectUris, input.Scopes, input.AccessTokenLifetime, input.RefreshTokenLifetime, input.AuthorizationCodeLifetime, input.Logo, input.Homepage, input.Remark);
        var app = await GetOAuthAppOrThrowAsync(input.BasicId, cancellationToken);
        app.AppName = Required(input.AppName, 100, nameof(input.AppName), "应用名称不能超过 100 个字符。");
        app.AppDescription = Optional(input.AppDescription, 500, nameof(input.AppDescription), "应用描述不能超过 500 个字符。");
        app.AppType = input.AppType;
        app.GrantTypes = Required(input.GrantTypes, 500, nameof(input.GrantTypes), "授权类型不能超过 500 个字符。");
        app.RedirectUris = Optional(input.RedirectUris, 2000, nameof(input.RedirectUris), "回调地址不能超过 2000 个字符。");
        app.Scopes = Optional(input.Scopes, 1000, nameof(input.Scopes), "授权范围不能超过 1000 个字符。");
        app.AccessTokenLifetime = input.AccessTokenLifetime;
        app.RefreshTokenLifetime = input.RefreshTokenLifetime;
        app.AuthorizationCodeLifetime = input.AuthorizationCodeLifetime;
        app.Logo = Optional(input.Logo, 500, nameof(input.Logo), "应用图标不能超过 500 个字符。");
        app.Homepage = Optional(input.Homepage, 500, nameof(input.Homepage), "应用主页不能超过 500 个字符。");
        app.SkipConsent = input.SkipConsent;
        app.Remark = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。");

        var savedApp = await _oauthAppRepository.UpdateAsync(app, cancellationToken);
        return OAuthAppApplicationMapper.ToDetailDto(savedApp);
    }

    /// <summary>
    /// 更新 OAuth 应用状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.OAuthApp.Status)]
    public async Task<OAuthAppDetailDto> UpdateOAuthAppStatusAsync(OAuthAppStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(input.BasicId, "OAuth 应用主键必须大于 0。");
        EnsureEnum(input.Status, nameof(input.Status));
        var app = await GetOAuthAppOrThrowAsync(input.BasicId, cancellationToken);
        app.Status = input.Status;
        app.Remark = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。") ?? app.Remark;

        var savedApp = await _oauthAppRepository.UpdateAsync(app, cancellationToken);
        return OAuthAppApplicationMapper.ToDetailDto(savedApp);
    }

    /// <summary>
    /// 重置 OAuth 应用密钥
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.OAuthApp.Secret)]
    public async Task<OAuthAppSecretDto> RegenerateOAuthAppSecretAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var app = await GetOAuthAppOrThrowAsync(id, cancellationToken);
        app.ClientSecret = GenerateSecret();
        var savedApp = await _oauthAppRepository.UpdateAsync(app, cancellationToken);
        return ToSecretDto(savedApp);
    }

    /// <summary>
    /// 删除 OAuth 应用
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.OAuthApp.Delete)]
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

    private async Task<SysOAuthApp> GetOAuthAppOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        EnsureId(id, "OAuth 应用主键必须大于 0。");
        return await _oauthAppRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("OAuth 应用不存在。");
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

    private static void EnsurePositive(int value, string paramName, string message)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    private static void EnsureCodeHasNoWhitespace(string value, string message)
    {
        if (value.Any(char.IsWhiteSpace))
        {
            throw new InvalidOperationException(message);
        }
    }

    private static string GenerateSecret()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    }

    private static OAuthAppSecretDto ToSecretDto(SysOAuthApp app)
    {
        return new OAuthAppSecretDto
        {
            BasicId = app.BasicId,
            ClientId = app.ClientId,
            ClientSecret = app.ClientSecret
        };
    }
}
